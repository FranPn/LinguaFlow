using LinguaFlow.Core;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Infrastructure.Data;

public class EfSessionRepository(LinguaFlowDbContext db) : ISessionRepository
{
    public async Task<ConversationSession> CreateAsync(string targetLanguage, string topic, CancellationToken cancellationToken = default)
    {
        var session = new ConversationSession { TargetLanguage = targetLanguage, Topic = topic };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public Task<ConversationSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

    public async Task AddTurnsAsync(Guid sessionId, IReadOnlyList<ConversationTurn> turns, CancellationToken cancellationToken = default)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Sessione {sessionId} non trovata");

        session.Turns.AddRange(turns);
        await db.SaveChangesAsync(cancellationToken);
    }
}
