using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Anthropic;
using LinguaFlow.Core;
using LinguaFlow.Infrastructure.Agents;
using LinguaFlow.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Consente al frontend PWA (Vite dev server) di chiamare l'API in sviluppo.
const string WebClientCorsPolicy = "WebClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebClientCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var anthropicApiKey = builder.Configuration["Anthropic:ApiKey"]
    ?? throw new InvalidOperationException("Anthropic:ApiKey non configurata. Usa: dotnet user-secrets set \"Anthropic:ApiKey\" \"...\"");

builder.Services.AddSingleton<IChatClient>(_ =>
{
    var client = new AnthropicClient { ApiKey = anthropicApiKey };
    return client.AsIChatClient("claude-sonnet-5")
        .AsBuilder()
        .UseFunctionInvocation()
        .Build();
});

builder.Services.AddKernel();
builder.Services.AddSingleton<CorrectionAgent>();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=linguaflow.db";
builder.Services.AddDbContext<LinguaFlowDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<ISessionRepository, EfSessionRepository>();
builder.Services.AddScoped<IProgressRepository, EfProgressRepository>();
builder.Services.AddScoped<ITurnProcessingService, TurnProcessingService>();

// App solo locale, nessuna autenticazione ancora: rate limiting come unica protezione
// da uso eccessivo (accidentale o da script) sugli endpoint che chiamano l'LLM.
// Vedi BRIEF.md, "Sicurezza e hardening".
const string RateLimitPolicy = "fixed";
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(RateLimitPolicy, limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

// Applica le migration EF Core all'avvio (progetto personale, no pipeline di deploy separata).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LinguaFlowDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(WebClientCorsPolicy);
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/chat/test", async (IChatClient chatClient, string message) =>
{
    var response = await chatClient.GetResponseAsync(message);
    return Results.Ok(new { reply = response.Text });
});

// i due endpoint di sessione
app.MapPost("/api/session/start", async (string targetLanguage, string topic, ISessionRepository sessionRepository) =>
{
    var session = await sessionRepository.CreateAsync(targetLanguage, topic);
    return Results.Ok(new { sessionId = session.Id });
}).RequireRateLimiting(RateLimitPolicy);

const int MaxMessageLength = 1000;

app.MapPost("/api/session/{id}/turn", async (Guid id, string message, ITurnProcessingService turnProcessingService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(message) || message.Length > MaxMessageLength)
        return Results.BadRequest(new { error = $"Messaggio vuoto o troppo lungo (max {MaxMessageLength} caratteri)" });

    var result = await turnProcessingService.ProcessTurnAsync(id, message, cancellationToken);
    return result is null
        ? Results.NotFound(new { error = "Sessione non trovata" })
        : Results.Ok(new { reply = result.Reply, corrections = result.Corrections });
}).RequireRateLimiting(RateLimitPolicy);

// Tracking progressi aggregato su tutte le sessioni (non sulla singola sessione come sopra).
app.MapGet("/api/progress", async (string? targetLanguage, IProgressRepository progressRepository, CancellationToken cancellationToken) =>
{
    var summary = await progressRepository.GetSummaryAsync(targetLanguage, cancellationToken);
    return Results.Ok(summary);
});

app.Run();
