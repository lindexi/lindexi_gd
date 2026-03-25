// See https://aka.ms/new-console-template for more information

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
});

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

var agent = chatClient.AsIChatClient()
    .AsBuilder()
    // 在 ChatClientExtensions.cs 的 WithDefaultAgentMiddleware 会判断是否有 FunctionInvokingChatClient 从而来决定是否注册
    //.Use((innerClient, services) =>
    //{
    //    var functionInvokingChatClient = innerClient.GetService<FunctionInvokingChatClient>();
    //    var invokingChatClient = services.GetService<FunctionInvokingChatClient>();

    //    return innerClient;
    //})
    .UseFunctionInvocation(configure: functionInvokingChatClient =>
    {
        functionInvokingChatClient.FunctionInvoker = (context, token) =>
        {
            // 写入属性，即可在调用函数之后退出
            context.Terminate = true;
            return context.Function.InvokeAsync(context.Arguments, token);
        };
    })
    .BuildAIAgent(options: new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                AIFunctionFactory.Create(GetWeather)
            ]
        }
    });

await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(new ChatMessage(ChatRole.User,
                   "今天北京的天气咋样.")))
{
    if (reasoningAgentResponseUpdate.IsFirstThinking)
    {
        Console.WriteLine($"思考：");
    }

    if (reasoningAgentResponseUpdate.IsThinkingEnd && reasoningAgentResponseUpdate.IsFirstOutputContent)
    {
        Console.WriteLine();
        Console.WriteLine("----------");
    }

    Console.Write(reasoningAgentResponseUpdate.Reasoning);
    Console.Write(reasoningAgentResponseUpdate.Text);
}

Console.WriteLine($"结束");

Console.WriteLine();

Console.Read();

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location,
    [Description("查询天气的日期")] string date)
{
    Console.WriteLine($"调用函数");
    return $"查询不到 {location} 城市信息";
}