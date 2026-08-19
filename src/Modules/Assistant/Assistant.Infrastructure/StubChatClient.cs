using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Assistant.Infrastructure;

internal sealed class StubChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        var question = list.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
        var context = ExtractContext(list);
        var answer = ExtractiveAnswer(question, context);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceKey is not null)
        {
            return null;
        }

        if (serviceType == typeof(ChatClientMetadata))
        {
            return new ChatClientMetadata("stub", null, "stub-extractive");
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
    }

    private static string ExtractContext(IReadOnlyList<ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            var text = message.Text ?? string.Empty;
            var index = text.IndexOf("Context:", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return text[(index + "Context:".Length)..];
            }
        }

        return string.Join('\n', messages.Select(m => m.Text));
    }

    private static string ExtractiveAnswer(string question, string context)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return Domain.Policy.RefuseMessage;
        }

        var qTokens = question.Split([' ', '\n', '?', '!', '.', ',', ':', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sentences = context.Split(['.', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ranked = sentences
            .Select(s => (Sentence: s, Score: qTokens.Count(t => t.Length > 3 && s.Contains(t, StringComparison.OrdinalIgnoreCase))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(3)
            .Select(x => x.Sentence.Trim())
            .ToList();

        if (ranked.Count == 0)
        {
            return Domain.Policy.RefuseMessage;
        }

        return string.Join(". ", ranked) + ".";
    }
}
