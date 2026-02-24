// See https://aka.ms/new-console-template for more information
using System;
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var chatClient = openAiClient.GetChatClient("ep-20260115192014-kgkxq");

var inMemoryChatHistoryProvider = new InMemoryChatHistoryProvider();
ChatClientAgent aiAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    ChatHistoryProvider = inMemoryChatHistoryProvider
});

AgentSession session = await aiAgent.CreateSessionAsync();

var chatHistoryProvider = aiAgent.GetService<ChatHistoryProvider>();
Debug.Assert(ReferenceEquals(inMemoryChatHistoryProvider, chatHistoryProvider));

var chatMessages = inMemoryChatHistoryProvider.GetMessages(session);
chatMessages.Add(new ChatMessage(ChatRole.System, "你是一个讲笑话机器人。你将给学龄前的小孩子讲笑话"));
// 无需调用设置方法，因为直接返回的就是内容本身了
//inMemoryChatHistoryProvider.SetMessages(session, chatMessages);

ChatMessage message = new ChatMessage(ChatRole.User, "请讲一个笑话");

await foreach (var agentRunResponseUpdate in aiAgent.RunReasoningStreamingAsync(message, session,new AgentRunOptions()
               {
                   
               }))
{
    if (agentRunResponseUpdate.IsFirstThinking)
    {
        Console.WriteLine("思考：");
    }

    if (agentRunResponseUpdate.Reasoning is not null)
    {
        Console.Write(agentRunResponseUpdate.Reasoning);
    }

    if (agentRunResponseUpdate.IsThinkingEnd)
    {
        Console.WriteLine();
        Console.WriteLine("--------");
    }

    var text = agentRunResponseUpdate.Text;
    if (!string.IsNullOrEmpty(text))
    {
        Console.Write(text);
    }
}

Console.WriteLine();

Console.Read();