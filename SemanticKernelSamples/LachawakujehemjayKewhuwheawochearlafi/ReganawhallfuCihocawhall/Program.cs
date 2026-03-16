// See https://aka.ms/new-console-template for more information
using System;
using System.ClientModel;

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

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

ChatClientAgent aiAgent = chatClient.AsAIAgent();

ChatMessage message = 
        
            new ChatMessage(ChatRole.User,
                [
                    new TextContent("根据以下图片，生成矢量图的 Path 路径，要求 Path 路径是 svg 路径。你可以生成多段 Path 路径，分别给这些 Path 路径着色"),
                    new UriContent("http://cdn.lindexi.site/lindexi-20260313160236.jpg","image/jpg")
                ])
        ;

await foreach (var agentRunResponseUpdate in aiAgent.RunReasoningStreamingAsync(message))
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