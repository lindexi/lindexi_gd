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
    Endpoint = new Uri("http://127.0.0.1:54731/mcp"),
    TransportMode = HttpTransportMode.AutoDetect
}));

IList<McpClientTool> toolList = await mcpClient.ListToolsAsync();

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

AIAgent aiAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    ChatOptions = new ChatOptions()
    {
        Tools = [.. toolList],
    }
})
.AsBuilder()
.Use(CustomFunctionCallingMiddleware)
.Build();

ChatMessage message =
            new ChatMessage(ChatRole.User,
                [
                    new TextContent("我现在的这个页面看起来排版不好看，请帮我优化一下。你应该在每次调整的时候，都先读取一下页面截图或元素截图。如果一个元素在组合内，那么应该先将元素解组，才能对此元素进行操作，否则将提示找不到元素错误"),
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
    else
    {
        foreach (var content in agentRunResponseUpdate.Origin.Contents)
        {
            if (content is FunctionCallContent functionCallContent)
            {
                Console.WriteLine($"Function Call: {functionCallContent}");
            }

            /*
             * TextContent	文本内容可以是输入，例如，来自用户或开发人员，以及代理的输出。 通常包含代理的文本结果。
               DataContent	可以是输入和输出的二进制内容。 可用于向代理传入和传出图像、音频或视频数据（其中受支持）。
               UriContent	通常指向托管内容（如图像、音频或视频）的 URL。
               FunctionCallContent	推理服务调用函数工具的请求。
               FunctionResultContent	函数工具调用的结果。
             */
        }
    }
}

Console.WriteLine();
Console.WriteLine($"输出完成");

Console.Read();

// https://learn.microsoft.com/zh-cn/agent-framework/agents/middleware/defining-middleware?pivots=programming-language-csharp

async ValueTask<object?> CustomFunctionCallingMiddleware
(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken
)
{
    Console.WriteLine($"Function Name: {context!.Function.Name}");
    var result = await next(context, cancellationToken);
    Console.WriteLine($"Function Call Result: {result}");
    if (result is DataContent dataContent)
    {
        Console.WriteLine($"Function Call Result is DataContent Name={dataContent.Name} Data={dataContent.Data.Length}");
    }

    return result;
}