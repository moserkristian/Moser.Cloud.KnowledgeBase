using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

using OpenAI;

using System;
using System.ClientModel;
using System.Net.Http;

namespace Moser.RagAi.Assistant.Infrastructure;

internal static class AiClientFactory
{
    public const string DefaultOllamaChatModel = "llama3.2";
    public const string DefaultOllamaEmbeddingModel = "nomic-embed-text";

    public static AiStack Create(IConfiguration configuration)
    {
        var assistant = configuration.GetSection(AssistantOptions.SectionName);
        var cloudEndpoint = FirstNonEmpty(
            assistant["Endpoint"],
            configuration["OPENAI_ENDPOINT"],
            configuration["AZURE_OPENAI_ENDPOINT"],
            configuration["FOUNDRY_ENDPOINT"]);
        var apiKey = FirstNonEmpty(
            assistant["ApiKey"],
            configuration["OPENAI_API_KEY"],
            configuration["AZURE_OPENAI_API_KEY"],
            configuration["FOUNDRY_API_KEY"]);

        if (!string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(cloudEndpoint))
        {
            var chatModel = FirstNonEmpty(assistant["ChatModel"], "gpt-4o-mini")!;
            var embedModel = FirstNonEmpty(assistant["EmbeddingModel"], "text-embedding-3-small")!;
            return new AiStack(
                new OpenAI.Chat.ChatClient(chatModel, apiKey).AsIChatClient(),
                new OpenAI.Embeddings.EmbeddingClient(embedModel, apiKey).AsIEmbeddingGenerator(),
                new AssistantRuntimeInfo("openai", chatModel, embedModel, IsStub: false, Endpoint: null));
        }

        if (!string.IsNullOrWhiteSpace(cloudEndpoint) && !IsOllama(cloudEndpoint))
        {
            var chatModel = FirstNonEmpty(assistant["ChatModel"], "gpt-4o-mini")!;
            var embedModel = FirstNonEmpty(assistant["EmbeddingModel"], "text-embedding-3-small")!;
            var key = string.IsNullOrWhiteSpace(apiKey) ? "not-needed" : apiKey;
            var client = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = new Uri(cloudEndpoint.TrimEnd('/')) });
            return new AiStack(
                client.GetChatClient(chatModel).AsIChatClient(),
                client.GetEmbeddingClient(embedModel).AsIEmbeddingGenerator(),
                new AssistantRuntimeInfo("cloud", chatModel, embedModel, IsStub: false, cloudEndpoint));
        }

        var ollama = FirstNonEmpty(
            assistant["OllamaEndpoint"],
            configuration["OLLAMA_ENDPOINT"],
            configuration.GetConnectionString("ollama"),
            cloudEndpoint);

        if (!string.IsNullOrWhiteSpace(ollama) && OllamaGateway.IsReachable(ollama))
        {
            var chatModel = FirstNonEmpty(assistant["ChatModel"], configuration["OLLAMA_CHAT_MODEL"], DefaultOllamaChatModel)!;
            var embedModel = FirstNonEmpty(assistant["EmbeddingModel"], configuration["OLLAMA_EMBED_MODEL"], DefaultOllamaEmbeddingModel)!;
            var normalized = OllamaGateway.NormalizeEndpoint(ollama);
            var client = new OpenAIClient(
                new ApiKeyCredential("ollama"),
                new OpenAIClientOptions { Endpoint = new Uri(normalized) });
            return new AiStack(
                client.GetChatClient(chatModel).AsIChatClient(),
                client.GetEmbeddingClient(embedModel).AsIEmbeddingGenerator(),
                new AssistantRuntimeInfo("ollama", chatModel, embedModel, IsStub: false, ollama.TrimEnd('/')));
        }

        return new AiStack(
            new StubChatClient(),
            new StubEmbeddingGenerator(),
            new AssistantRuntimeInfo("stub", "stub-extractive", "hash-bag-of-words", IsStub: true, ollama));
    }

    private static bool IsOllama(string endpoint)
        => endpoint.Contains("11434", StringComparison.Ordinal) ||
           endpoint.Contains("ollama", StringComparison.OrdinalIgnoreCase);

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

internal sealed record AiStack(
    IChatClient Chat,
    IEmbeddingGenerator<string, Embedding<float>> Embeddings,
    AssistantRuntimeInfo Info);
