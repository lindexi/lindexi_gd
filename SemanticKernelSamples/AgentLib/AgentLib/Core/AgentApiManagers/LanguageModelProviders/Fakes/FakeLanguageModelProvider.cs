using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AgentLib.Core.AgentApiManagers.Contexts;

using Microsoft.Extensions.AI;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;

/// <summary>
/// 用于测试的伪语言模型提供商，使用 <see cref="FakeChatClient"/> 模拟聊天行为。
/// </summary>
public sealed record FakeLanguageModelProvider(IReadOnlyList<FakeLanguageModel> SupportedModels) : ILanguageModelProvider
{
    /// <summary>
    /// 使用单个伪聊天客户端创建提供商。
    /// </summary>
    /// <param name="fakeChatClient">伪聊天客户端。</param>
    public FakeLanguageModelProvider(FakeChatClient fakeChatClient) : this([new FakeLanguageModel(fakeChatClient)])
    {
    }

    /// <inheritdoc/>
    public IReadOnlyList<ILanguageModel> GetSupportedModels()
        => SupportedModels;
}

/// <summary>
/// 用于测试的伪语言模型，使用 <see cref="FakeChatClient"/> 模拟聊天行为。
/// </summary>
public sealed record FakeLanguageModel(FakeChatClient ChatClient) : ILanguageModel
{
    /// <summary>
    /// 模型定义，默认为 "Fake" 模型。
    /// </summary>
    public ModelDefinition ModelDefinition { get; set; }
        = new ModelDefinition()
        {
            ModelId = "Fake",
            ModelName = "Fake",
        };

    /// <inheritdoc/>
    public Task<IChatClient> GetChatClientAsync()
    {
        return Task.FromResult<IChatClient>(ChatClient);
    }
}

/// <summary>
/// 用于测试的伪聊天客户端，通过委托回调模拟 <see cref="IChatClient"/> 的行为。
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    /// <summary>
    /// 用于模拟 <see cref="IChatClient.GetResponseAsync"/> 的回调委托。
    /// </summary>
    public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>?
        OnGetResponseAsync
    { get; set; }

    /// <inheritdoc/>
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

    /// <summary>
    /// 用于模拟 <see cref="IChatClient.GetStreamingResponseAsync"/> 的回调委托。
    /// </summary>
    public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>?
        OnGetStreamingResponseAsync
    { get; set; }

    /// <inheritdoc/>
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

    /// <summary>
    /// 用于模拟 <see cref="IChatClient.GetService"/> 的回调委托。
    /// </summary>
    public Func<Type, object?, object?>? OnGetService { get; set; }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (OnGetService is null)
            return null;

        return OnGetService(serviceType, serviceKey);
    }


    /// <inheritdoc/>
    public void Dispose()
    {
        OnDispose?.Invoke();
    }

    /// <summary>
    /// 用于模拟 <see cref="IDisposable.Dispose"/> 的回调委托。
    /// </summary>
    public Action? OnDispose { get; set; }
}
