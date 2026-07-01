

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

ChatClientAgent agent = chatClient.AsAIAgent();
var session = await agent.CreateSessionAsync();

var userMessage = $"今天是 {DateTime.Now}。 Hello, I am a user. I want to use the MiniMax-M3 model to chat with you. 请你回忆你的记忆，告诉我技术领域的 lindexi 是谁，他做了那些技术";

var cancellationTokenSource = new CancellationTokenSource();

var tokenCount = 0;

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
