using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using LinguaFlow.Core;

namespace LinguaFlow.Infrastructure.Agents;

public static class CorrectionPrompts
{
    public static string BuildSystemPrompt(string targetLanguage) => $$"""
        Sei un correttore linguistico automatico per {{targetLanguage}}. Riceverai UN SOLO messaggio
        scritto da uno studente e dovrai individuare eventuali errori grammaticali, lessicali,
        di ortografia o di costruzione della frase.

        Rispondi SOLO con un array JSON valido, senza testo prima o dopo, senza blocchi di codice
        markdown (niente ```). Se il messaggio non contiene errori, rispondi con un array vuoto: []

        Ogni elemento dell'array deve avere esattamente questa forma:
        {
          "original": "porzione di testo errata scritta dallo studente",
          "corrected": "versione corretta",
          "category": "una tra: grammar, vocabulary, spelling, word-order, preposition, other",
          "explanation": "breve spiegazione in italiano del perché è un errore"
        }

        Correggi solo errori oggettivi, non lo stile o le preferenze personali. Non inventare
        errori se il messaggio è corretto: in quel caso l'array deve restare vuoto.
        """;
}

/// <summary>
/// Analizza in isolamento l'ultimo messaggio dello studente e produce correzioni
/// strutturate, senza toccare il flusso conversazionale del ConversationAgent.
/// Chiamato in parallelo con Task.WhenAll sullo stesso turno utente (vedi BRIEF.md).
/// </summary>
public class CorrectionAgent(IChatClient chatClient, ILogger<CorrectionAgent> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<Correction>> AnalyzeAsync(
        string targetLanguage, string userMessage, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.System, CorrectionPrompts.BuildSystemPrompt(targetLanguage)),
            new(ChatRole.User, userMessage)
        ];

        string rawText;
        try
        {
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            rawText = response.Text;
        }
        catch (Exception ex)
        {
            // Errore di rete/API verso Anthropic: non deve far fallire l'intero turno,
            // il ConversationAgent può comunque rispondere allo studente.
            logger.LogWarning(ex, "CorrectionAgent: chiamata LLM fallita, nessuna correzione per questo turno");
            return [];
        }

        try
        {
            var json = ExtractJsonArray(rawText);
            var dtos = JsonSerializer.Deserialize<List<CorrectionDto>>(json, JsonOptions);
            if (dtos is null)
                return [];

            return dtos
                .Where(d => !string.IsNullOrWhiteSpace(d.Original) && !string.IsNullOrWhiteSpace(d.Corrected))
                .Select(d => new Correction(d.Original!, d.Corrected!, d.Category ?? "other", d.Explanation ?? ""))
                .ToList();
        }
        catch (JsonException ex)
        {
            // Output LLM malformato (JSON troncato, campi mancanti, ecc.): meglio nessuna
            // correzione che un 500 sull'intera richiesta.
            logger.LogWarning(ex, "CorrectionAgent: impossibile parsare la risposta JSON dell'LLM. Risposta grezza: {RawText}", rawText);
            return [];
        }
    }

    // L'LLM a volte ignora l'istruzione "niente markdown" e avvolge il JSON in ```json ... ```
    // oppure aggiunge una frase introduttiva/finale: isoliamo la sottostringa tra la prima
    // '[' e l'ultima ']' per tollerare questi casi senza rompere il parsing.
    private static string ExtractJsonArray(string text)
    {
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end < start)
            throw new JsonException("Nessun array JSON trovato nella risposta dell'LLM");

        return text[start..(end + 1)];
    }

    private sealed class CorrectionDto
    {
        public string? Original { get; set; }
        public string? Corrected { get; set; }
        public string? Category { get; set; }
        public string? Explanation { get; set; }
    }
}
