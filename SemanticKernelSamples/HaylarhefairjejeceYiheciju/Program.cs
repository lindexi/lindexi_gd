

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
        
    },
    ChatHistoryProvider = new FooChatHistoryProvider()
});
var session = await agent.CreateSessionAsync();

var userMessage = $"今天是 {DateTime.Now}。 Hello, I am a user. I want to use the MiniMax-M3 model to chat with you. 请你回忆你的记忆，告诉我技术领域的 lindexi 是谁，他做了那些技术";

var cancellationTokenSource = new CancellationTokenSource();

var tokenCount = 0;

var chatMessageList = new List<ChatMessage>();

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


class FooChatHistoryProvider : ChatHistoryProvider
{
    protected override ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return base.InvokingCoreAsync(context, cancellationToken);
    }

    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return base.ProvideChatHistoryAsync(context, cancellationToken);
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