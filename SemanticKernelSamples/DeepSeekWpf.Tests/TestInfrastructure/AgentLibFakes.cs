using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using DeepSeekWpf.Services;
using Microsoft.Extensions.AI;

namespace DeepSeekWpf.Tests.TestInfrastructure;

internal static class AgentLibFakes
{
    public static TestChatClient CreateChatClient() => new();

    public static AgentApiEndpointManager CreateManager(params (string Provider, string Name, string? Id)[] models)
    {
        var manager = new AgentApiEndpointManager();
        var testModels = models.Select(model => new TestLanguageModel(CreateChatClient())
        {
            ModelDefinition = new ModelDefinition
            {
                Provider = model.Provider,
                ModelName = model.Name,
                ModelId = model.Id,
            },
        }).ToArray();
        manager.RegisterLanguageModelProvider(new TestLanguageModelProvider(testModels));
        return manager;
    }
}

internal sealed record TestLanguageModelProvider(IReadOnlyList<TestLanguageModel> SupportedModels) : ILanguageModelProvider
{
    public IReadOnlyList<ILanguageModel> GetSupportedModels() => SupportedModels;
}

internal sealed record TestLanguageModel(TestChatClient ChatClient) : ILanguageModel
{
    public required ModelDefinition ModelDefinition { get; set; }

    public Task<IChatClient> GetChatClientAsync() => Task.FromResult<IChatClient>(ChatClient);
}

internal sealed class TestChatClient : IChatClient
{
    public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>? OnGetResponseAsync { get; set; }

    public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? OnGetStreamingResponseAsync { get; set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return OnGetResponseAsync?.Invoke(messages, options, cancellationToken)
            ?? throw new InvalidOperationException("测试聊天客户端尚未配置非流式响应。");
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return OnGetStreamingResponseAsync?.Invoke(messages, options, cancellationToken)
            ?? throw new InvalidOperationException("测试聊天客户端尚未配置流式响应。");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

internal sealed class FakeAgentApiEndpointManagerFactory(params AgentApiEndpointManager[] managers)
    : IAgentApiEndpointManagerFactory
{
    private readonly Queue<AgentApiEndpointManager> _managers = new(managers);

    public AgentApiEndpointManager Create()
    {
        return _managers.Count > 0 ? _managers.Dequeue() : new AgentApiEndpointManager();
    }
}