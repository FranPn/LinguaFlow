namespace LinguaFlow.Core;

/// <summary>
/// Astrazione di persistenza per le sessioni conversazionali. Implementata in
/// LinguaFlow.Infrastructure (EF Core + SQLite); Core resta senza dipendenze esterne.
/// </summary>
public interface ISessionRepository
{
    Task<ConversationSession> CreateAsync(string targetLanguage, string topic, CancellationToken cancellationToken = default);

    Task<ConversationSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggiunge uno o più turni a una sessione esistente in un'unica operazione atomica
    /// (tipicamente la coppia turno-utente + turno-assistente di un singolo scambio).
    /// </summary>
    Task AddTurnsAsync(Guid sessionId, IReadOnlyList<ConversationTurn> turns, CancellationToken cancellationToken = default);
}
