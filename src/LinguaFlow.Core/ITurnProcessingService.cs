namespace LinguaFlow.Core;

/// <summary>
/// Orchestra un turno di conversazione: chiama ConversationAgent e CorrectionAgent,
/// persiste i turni risultanti tramite ISessionRepository. Implementato in
/// LinguaFlow.Infrastructure; Core resta senza dipendenze esterne.
/// </summary>
public interface ITurnProcessingService
{
    /// <summary>
    /// Ritorna null se la sessione <paramref name="sessionId"/> non esiste.
    /// </summary>
    Task<TurnResult?> ProcessTurnAsync(Guid sessionId, string userMessage, CancellationToken cancellationToken = default);
}

public record TurnResult(string Reply, List<Correction> Corrections);
