

using System.ComponentModel;
using AgentLib.AgentExtensions;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers;

using Google.Protobuf;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using System.Diagnostics;
using Microsoft.Agents.AI.Reasoning;

var agentApiManagerConfiguration = LindexiAgentConfiguration.LoadDefault();
var agentApiEndpointManager = new AgentApiEndpointManager();
agentApiEndpointManager.LoadConfiguration(agentApiManagerConfiguration);

var languageModel = agentApiEndpointManager.GetModel("MiniMax-M3");
Debug.Assert(languageModel is not null);

var chatClient = await languageModel.GetChatClientAsync();

ChatClientAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    ChatOptions = new ChatOptions()
    {
        Tools = [AIFunctionFactory.Create(GetWeather)]
    },
    //ChatHistoryProvider = new FooChatHistoryProvider(),
#pragma warning disable MAAI001
    RequirePerServiceCallChatHistoryPersistence = true,
#pragma warning restore MAAI001
});
var session = await agent.CreateSessionAsync();

var userMessage = $"今天是 {DateTime.Now}。请问天气多少";

var cancellationTokenSource = new CancellationTokenSource();

var tokenCount = 0;

var chatMessageList = new List<ChatMessage>();

session.SetInMemoryChatHistory(new List<ChatMessage>()
{
    new ChatMessage(ChatRole.System, "如果用户询问你某个人的信息，你不应该直接回答，而是引导用户去查找相关信息。"),
});

try
{
    await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(
                       [new ChatMessage(ChatRole.User, userMessage)], session,
                       cancellationToken: cancellationTokenSource.Token))
    {
        Console.Write(reasoningAgentResponseUpdate.Reasoning);
        Console.Write(reasoningAgentResponseUpdate.Text);

        if (!string.IsNullOrEmpty(reasoningAgentResponseUpdate.Text))
        {
            tokenCount++;
        }

        chatMessageList.AddMessages(reasoningAgentResponseUpdate.Origin.AsChatResponseUpdate());

        var currentRunContext = ChatClientAgent.CurrentRunContext;

        if (tokenCount == 10)
        {
        }
    }
}
catch (OperationCanceledException e)
{
    if (cancellationTokenSource.IsCancellationRequested)
    {
        
    }
    else
    {
        Console.WriteLine(e);
        throw;
    }
}

session.TryGetInMemoryChatHistory(out var messageList);
Debug.Assert(messageList is not null);



Console.WriteLine("Hello, World!");

[Description("获取温度")]
string GetWeather()
{
    return "温度100度";
}

class FooChatHistoryProvider : ChatHistoryProvider
{
    protected override ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return base.InvokingCoreAsync(context, cancellationToken);
    }

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await base.ProvideChatHistoryAsync(context, cancellationToken);
        return result;
    }

    protected override ValueTask InvokedCoreAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return base.InvokedCoreAsync(context, cancellationToken);
    }

    protected override ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return base.StoreChatHistoryAsync(context, cancellationToken);
    }

    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        return base.GetService(serviceType, serviceKey);
    }
}