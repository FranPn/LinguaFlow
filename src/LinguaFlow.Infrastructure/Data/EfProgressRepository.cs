using LinguaFlow.Core;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Infrastructure.Data;

/// <summary>
/// Aggrega i progressi su tutte le sessioni. Le sessioni vengono materializzate
/// (Turns/Corrections sono owned collection, caricate insieme all'aggregate root)
/// e l'aggregazione avviene in memoria: per la scala di questo progetto è più
/// semplice ed è più affidabile che tradurre GroupBy annidati su owned types in SQL.
/// </summary>
public class EfProgressRepository(LinguaFlowDbContext db) : IProgressRepository
{
    public async Task<ProgressSummary> GetSummaryAsync(string? targetLanguage, CancellationToken cancellationToken = default)
    {
        var query = db.Sessions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(targetLanguage))
            query = query.Where(s => s.TargetLanguage == targetLanguage);

        var sessions = await query.OrderBy(s => s.StartedAt).ToListAsync(cancellationToken);

        var allCorrections = sessions
            .SelectMany(s => s.Turns)
            .SelectMany(t => t.Corrections)
            .ToList();

        var byCategory = allCorrections
            .GroupBy(c => c.Category)
            .Select(g => new CategoryCount(g.Key, g.Count()))
            .OrderByDescending(c => c.Count)
            .ToList();

        var sessionTrend = sessions
            .Select(s => new SessionProgress(
                s.Id,
                s.StartedAt,
                s.Topic,
                s.Turns.Sum(t => t.Corrections.Count)))
            .ToList();

        return new ProgressSummary(
            TotalSessions: sessions.Count,
            TotalTurns: sessions.Sum(s => s.Turns.Count),
            TotalCorrections: allCorrections.Count,
            ByCategory: byCategory,
            SessionTrend: sessionTrend);
    }
}
