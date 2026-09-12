using Microsoft.Extensions.AI;
using LinguaFlow.Core;

namespace LinguaFlow.Infrastructure.Agents;

public class TurnProcessingService(
    IChatClient chatClient,
    CorrectionAgent correctionAgent,
    ISessionRepository sessionRepository) : ITurnProcessingService
{
    public async Task<TurnResult?> ProcessTurnAsync(
        Guid sessionId, string userMessage, CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetAsync(sessionId, cancellationToken);
        if (session is null)
            return null;

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, ConversationPrompts.BuildSystemPrompt(session.TargetLanguage, session.Topic))
        };
        chatMessages.AddRange(session.Turns.Select(t =>
            new ChatMessage(t.Role == "user" ? ChatRole.User : ChatRole.Assistant, t.Text)));
        chatMessages.Add(new ChatMessage(ChatRole.User, userMessage));

        // ConversationAgent e CorrectionAgent lavorano sullo stesso turno utente in parallelo:
        // il primo genera la risposta conversazionale (senza correggere), il secondo analizza
        // SOLO l'ultimo messaggio dello studente per estrarre correzioni strutturate.
        // Vedi BRIEF.md, "Decisione architetturale chiave: due agenti separati".
        var conversationTask = chatClient.GetResponseAsync(chatMessages, cancellationToken: cancellationToken);
        var correctionTask = correctionAgent.AnalyzeAsync(session.TargetLanguage, userMessage, cancellationToken);
        await Task.WhenAll(conversationTask, correctionTask);

        var response = conversationTask.Result;
        var corrections = correctionTask.Result;

        var userTurn = new ConversationTurn("user", userMessage, DateTime.UtcNow) { Corrections = corrections };
        var assistantTurn = new ConversationTurn("assistant", response.Text, DateTime.UtcNow);
        await sessionRepository.AddTurnsAsync(sessionId, [userTurn, assistantTurn], cancellationToken);

        return new TurnResult(response.Text, corrections);
    }
}
