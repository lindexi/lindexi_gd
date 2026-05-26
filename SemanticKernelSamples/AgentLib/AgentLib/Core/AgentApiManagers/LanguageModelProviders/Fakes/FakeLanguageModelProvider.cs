using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AgentLib.Core.AgentApiManagers.Contexts;

using Microsoft.Extensions.AI;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;

public sealed record FakeLanguageModelProvider(IReadOnlyList<FakeLanguageModel> SupportedModels) : ILanguageModelProvider
{
    public FakeLanguageModelProvider(FakeChatClient fakeChatClient) : this([new FakeLanguageModel(fakeChatClient)])
    {
    }

    public IReadOnlyList<ILanguageModel> GetSupportedModels()
        => SupportedModels;
}

public sealed record FakeLanguageModel(FakeChatClient ChatClient) : ILanguageModel
{
    public ModelDefinition ModelDefinition { get; set; }
        = new ModelDefinition()
        {
            ModelId = "Fake",
            ModelName = "Fake",
        };

    public Task<IChatClient> GetChatClientAsync()
    {
        return Task.FromResult<IChatClient>(ChatClient);
    }
}

public sealed class FakeChatClient : IChatClient
{
    // ── GetResponseAsync ────────────────────────────────────────
    public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>?
        OnGetResponseAsync
    { get; set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (OnGetResponseAsync is null)
            throw new InvalidOperationException(
                $"{nameof(FakeChatClient)}.{nameof(OnGetResponseAsync)} has not been configured.");

        return OnGetResponseAsync(messages, options, cancellationToken);
    }

    // ── GetStreamingResponseAsync ───────────────────────────────
    public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>?
        OnGetStreamingResponseAsync
    { get; set; }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (OnGetStreamingResponseAsync is null)
            throw new InvalidOperationException(
                $"{nameof(FakeChatClient)}.{nameof(OnGetStreamingResponseAsync)} has not been configured.");

        return OnGetStreamingResponseAsync(messages, options, cancellationToken);
    }

    // ── GetService ──────────────────────────────────────────────
    public Func<Type, object?, object?>? OnGetService { get; set; }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (OnGetService is null)
            return null;

        return OnGetService(serviceType, serviceKey);
    }


    public void Dispose()
    {
        OnDispose?.Invoke();
    }

    public Action? OnDispose { get; set; }
}