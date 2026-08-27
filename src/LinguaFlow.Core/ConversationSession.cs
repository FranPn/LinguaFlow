namespace LinguaFlow.Core;

public class ConversationSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string TargetLanguage { get; init; }
    public required string Topic { get; init; }
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public List<ConversationTurn> Turns { get; init; } = [];
}

public record ConversationTurn(string Role, string Text, DateTime Timestamp)
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Correzioni strutturate individuate dal CorrectionAgent per questo turno (popolate solo
    /// sui turni "user"; lista vuota per i turni "assistant" o quando non ci sono errori).
    /// Non è un parametro del costruttore primario: EF Core non può fare constructor-binding
    /// su navigazioni verso owned type, deve poterle assegnare dopo la costruzione.
    /// </summary>
    public List<Correction> Corrections { get; init; } = [];
}