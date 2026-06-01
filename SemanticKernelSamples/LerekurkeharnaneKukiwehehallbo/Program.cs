using System.Text.Json;
using AgentLib.AgentExtensions;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

var configFile = @"C:\lindexi\Work\Key\AgentConfiguration.json";

var agentApiEndpointManager = new AgentApiEndpointManager();

await agentApiEndpointManager.LoadConfigurationFromJsonFileAsync(new FileInfo(configFile));

agentApiEndpointManager.PrimaryModel =
    agentApiEndpointManager.GetBestModel(model => model.ModelDefinition.ModelName.Contains("M3"));

var fakeChatClient = new FakeChatClient()
{
    OnGetStreamingResponseAsync = OnGetStreamingResponseAsync
};

async IAsyncEnumerable<ChatResponseUpdate> OnGetStreamingResponseAsync(IEnumerable<ChatMessage> arg1, ChatOptions? arg2,
    CancellationToken arg3)
{
    yield return new ChatResponseUpdate(ChatRole.Assistant, "Message");

    for (int i = 0; i < int.MaxValue; i++)
    {
        var callId = $"Tool{i}";
        var toolName = Random.Shared.Next(2)== 0 ? "Tool" : "Tool1";

        yield return new ChatResponseUpdate(ChatRole.Assistant, new List<AIContent>()
        {
            new FunctionCallContent(callId, toolName)
            {
            },
        })
        {
            FinishReason = ChatFinishReason.ToolCalls,
        };
        yield break;
    }
}

IChatClient chatClient = fakeChatClient;

var count = 0;

ChatClientAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    ChatHistoryProvider = new FakeChatHistoryProvider(),
    //    new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()
    //{
    //    ChatReducer = new FakeChatReducer()
    //}),
    ChatOptions = new ChatOptions()
    {
        Tools =
        [
            AIFunctionFactory.Create(Tool, "Tool"),
            AIFunctionFactory.Create(Tool, "Tool1"),
        ],
    }
});

string Tool() => $"Ok {count++}";

await agent.RunStreamingAndLogToConsoleAsync([new ChatMessage(ChatRole.User, "你好，我准备开发 MCP 服务器，请你给我一些建议")]);

Console.WriteLine("Hello, World!");


class FakeChatReducer : IChatReducer
{
    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(messages);
    }
}

class FakeChatHistoryProvider : ChatHistoryProvider
{
    protected override ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return base.InvokingCoreAsync(context, cancellationToken);
    }

    protected override ValueTask InvokedCoreAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return base.InvokedCoreAsync(context, cancellationToken);
    }

    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return base.ProvideChatHistoryAsync(context, cancellationToken);
    }
}