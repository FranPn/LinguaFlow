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
        // Riusa l'istanza già tracciata se la sessione è stata caricata in precedenza nello
        // stesso DbContext (tipicamente da GetAsync, chiamato prima nello stesso request scope
        // per costruire la history). Ricaricarla con una seconda query confonde il change
        // tracker sulla owned collection Turns e causa DbUpdateConcurrencyException in SaveChanges.
        var session = db.ChangeTracker.Entries<ConversationSession>()
            .Select(e => e.Entity)
            .FirstOrDefault(s => s.Id == sessionId)
            ?? await db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Sessione {sessionId} non trovata");

        session.Turns.AddRange(turns);

        // ConversationTurn/Correction assegnano Id = Guid.NewGuid() lato client (record default).
        // EF Core, trovandole raggiungibili per fixup con una chiave già "set" (non default),
        // le marca Modified invece di Added e genera una UPDATE invece di una INSERT (0 righe
        // interessate → DbUpdateConcurrencyException). Forziamo esplicitamente lo stato Added.
        foreach (var turn in turns)
        {
            db.Entry(turn).State = EntityState.Added;
            foreach (var correction in turn.Corrections)
                db.Entry(correction).State = EntityState.Added;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
