namespace LinguaFlow.Core;

/// <summary>
/// Correzione strutturata prodotta dal CorrectionAgent per un singolo errore
/// individuato nell'ultimo messaggio dello studente.
/// </summary>
/// <param name="Original">Porzione di testo errata scritta dallo studente.</param>
/// <param name="Corrected">Versione corretta.</param>
/// <param name="Category">Categoria dell'errore (es. grammar, vocabulary, spelling, word-order, preposition, other).</param>
/// <param name="Explanation">Breve spiegazione del perché è un errore.</param>
public record Correction(string Original, string Corrected, string Category, string Explanation)
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
