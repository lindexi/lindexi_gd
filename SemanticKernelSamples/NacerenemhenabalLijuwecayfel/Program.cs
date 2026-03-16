// See https://aka.ms/new-console-template for more information
using System;
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Chat;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var mcpClient = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions()
{
    Endpoint = new Uri("http://127.0.0.1:53779/mcp"),
    TransportMode = HttpTransportMode.AutoDetect
}));

IList<McpClientTool> toolList = await mcpClient.ListToolsAsync();

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

ChatClientAgent aiAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    ChatOptions = new ChatOptions()
    {
        Tools = [.. toolList],
    }
});

ChatMessage message = 
        
            new ChatMessage(ChatRole.User,
                [
                    new TextContent("我画板里面有很多页面，这些页面有一些元素是被组合的，我想你帮我一页页课件检查，将组合的元素解除组合。将锁定的元素解锁"),
                ])
        ;

await foreach (var agentRunResponseUpdate in aiAgent.RunReasoningStreamingAsync(message))
{
    var type = agentRunResponseUpdate.GetType();
    Debug.WriteLine(type.FullName);

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
Console.WriteLine($"输出完成");

Console.Read();