// See https://aka.ms/new-console-template for more information
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ComponentModel;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var chatClient = openAiClient.GetChatClient("ep-20260115192014-kgkxq");

AIFunction weatherFunction = AIFunctionFactory.Create(GetWeather);

ChatClientAgent aiAgent = chatClient.CreateAIAgent(tools:
[
    weatherFunction,
    AIFunctionFactory.Create(GetDateTime),
]);

var agentThread = aiAgent.GetNewThread();

ChatMessage message = new(ChatRole.User, 
[
    new TextContent("图中这个地方今天的天气怎样"),
    new UriContent("http://cdn.lindexi.site/lindexi-20261151939253242.jpg", "image/jpeg")
]);

await foreach (var agentRunResponseUpdate in aiAgent.RunStreamingAsync(message, agentThread))
{
    Console.Write(agentRunResponseUpdate.Text);
}

Console.WriteLine();

await foreach (var agentRunResponseUpdate in aiAgent.RunStreamingAsync("这个笑话不好笑，给我换一个", agentThread))
{
    Console.Write(agentRunResponseUpdate.Text);
}

Console.Read();

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location, [Description("查询天气的日期")] string date)
{
    return $"{location} 城市温度为 100 摄氏度";
}

[Description("Get the current date and time.")]
static DateTime GetDateTime() => DateTime.Now.AddYears(1000);