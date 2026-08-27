namespace LinguaFlow.Core;

/// <summary>
/// Astrazione per l'aggregazione dei progressi dello studente su più sessioni nel tempo
/// (distinta da ISessionRepository, che opera su una singola sessione).
/// </summary>
public interface IProgressRepository
{
    /// <summary>
    /// Calcola il riepilogo aggregato. Se <paramref name="targetLanguage"/> è null,
    /// aggrega su tutte le lingue.
    /// </summary>
    Task<ProgressSummary> GetSummaryAsync(string? targetLanguage, CancellationToken cancellationToken = default);
}
