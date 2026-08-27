namespace LinguaFlow.Core;

/// <summary>
/// Conteggio correzioni per categoria d'errore, aggregato su tutte le sessioni
/// (eventualmente filtrate per lingua target).
/// </summary>
public record CategoryCount(string Category, int Count);

/// <summary>
/// Numero di correzioni ricevute in una singola sessione, usato per il trend
/// nel tempo (es. per verificare se lo studente commette meno errori con l'uso).
/// </summary>
public record SessionProgress(Guid SessionId, DateTime StartedAt, string Topic, int CorrectionCount);

/// <summary>
/// Vista aggregata dei progressi dello studente, calcolata su tutte le sessioni
/// (non sulla singola sessione come /api/session/{id}/turn).
/// </summary>
public record ProgressSummary(
    int TotalSessions,
    int TotalTurns,
    int TotalCorrections,
    IReadOnlyList<CategoryCount> ByCategory,
    IReadOnlyList<SessionProgress> SessionTrend);
