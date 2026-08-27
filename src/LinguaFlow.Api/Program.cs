using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
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
});

app.MapPost("/api/session/{id}/turn", async (Guid id, string message, IChatClient chatClient, CorrectionAgent correctionAgent, ISessionRepository sessionRepository, CancellationToken cancellationToken) =>
{
    var session = await sessionRepository.GetAsync(id, cancellationToken);
    if (session is null)
        return Results.NotFound(new { error = "Sessione non trovata" });

    var chatMessages = new List<ChatMessage>
    {
        new(ChatRole.System, ConversationPrompts.BuildSystemPrompt(session.TargetLanguage, session.Topic))
    };
    chatMessages.AddRange(session.Turns.Select(t =>
        new ChatMessage(t.Role == "user" ? ChatRole.User : ChatRole.Assistant, t.Text)));
    chatMessages.Add(new ChatMessage(ChatRole.User, message));

    // ConversationAgent e CorrectionAgent lavorano sullo stesso turno utente in parallelo:
    // il primo genera la risposta conversazionale (senza correggere), il secondo analizza
    // SOLO l'ultimo messaggio dello studente per estrarre correzioni strutturate.
    // Vedi BRIEF.md, "Decisione architetturale chiave: due agenti separati".
    var conversationTask = chatClient.GetResponseAsync(chatMessages);
    var correctionTask = correctionAgent.AnalyzeAsync(session.TargetLanguage, message);
    await Task.WhenAll(conversationTask, correctionTask);

    var response = conversationTask.Result;
    var corrections = correctionTask.Result;

    var userTurn = new ConversationTurn("user", message, DateTime.UtcNow) { Corrections = corrections };
    var assistantTurn = new ConversationTurn("assistant", response.Text, DateTime.UtcNow);
    await sessionRepository.AddTurnsAsync(id, [userTurn, assistantTurn], cancellationToken);

    return Results.Ok(new { reply = response.Text, corrections });
});

// Tracking progressi aggregato su tutte le sessioni (non sulla singola sessione come sopra).
app.MapGet("/api/progress", async (string? targetLanguage, IProgressRepository progressRepository, CancellationToken cancellationToken) =>
{
    var summary = await progressRepository.GetSummaryAsync(targetLanguage, cancellationToken);
    return Results.Ok(summary);
});

app.Run();
