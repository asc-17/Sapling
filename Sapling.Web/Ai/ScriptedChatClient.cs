using Microsoft.Extensions.AI;

namespace Sapling.Web.Ai;

/// <summary>
/// Stands in for a hosted model when no API key is configured. Responses are deterministic and
/// drawn from the profile facts in the prompt, which keeps demos honest and offline-capable.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("scripted", new Uri("inproc://sapling-scripted"));

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = string.Join('\n', messages.Select(m => m.Text));
        var reply = Respond(prompt);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }

    private static string Respond(string prompt)
    {
        if (prompt.Contains(Prompts.InterviewQuestionTag, StringComparison.Ordinal))
        {
            return "Walk me through a project where you had to choose between two designs. What did you pick, and what did the other option cost you?";
        }

        if (prompt.Contains(Prompts.InterviewFeedbackTag, StringComparison.Ordinal))
        {
            return "Your answers carried real detail, but the structure wandered. Lead with the situation in one sentence, "
                 + "then the decision, then the measurable outcome. Cut filler openings such as \"basically\" and \"so yeah\".";
        }

        if (prompt.Contains(Prompts.ExplainTag, StringComparison.Ordinal))
        {
            return "This came out ahead because your verified skills already cover most of what the role asks for, "
                 + "the remaining gaps are the cheapest ones on your list to close, and demand for the role in your region is rising.";
        }

        return "A hosted model is not configured, so this is the scripted response. "
             + "Set Ai:Anthropic:ApiKey to use a live model.";
    }
}
