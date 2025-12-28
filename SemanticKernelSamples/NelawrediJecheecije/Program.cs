// See https://aka.ms/new-console-template for more information

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ComponentModel;
using System.Globalization;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var keyFile = @"C:\lindexi\Work\deepseek.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://api.deepseek.com/v1")
});

var chatClient = openAiClient.GetChatClient("deepseek-chat");

AIFunction weatherFunction = AIFunctionFactory.Create(GetWeather);

ChatClientAgent weatherAgent = chatClient.CreateAIAgent
(
    name: "WeatherAgent",
    description: "An agent that answers questions about the weather.",
    tools:
    [
        weatherFunction,
        AIFunctionFactory.Create(GetDateTime),
    ]
);
// 将一个 Agent 当成工具
AIFunction weatherAgentFunction = weatherAgent.AsAIFunction();

var aiAgent = chatClient.CreateAIAgent(tools: [weatherAgentFunction], instructions: "You are a helpful assistant who responds in 中文.");

var agentThread = aiAgent.GetNewThread();
var agentRunOptions = new AgentRunOptions()
{
    AllowBackgroundResponses = true,
};

var agentRunResponse = await aiAgent.RunAsync("今天北京的天气咋样", agentThread, agentRunOptions);

Console.WriteLine(agentRunResponse);

Console.Read();
return;


[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location,
    [Description("查询天气的日期")] string date)
{
    return $"The weather in {location} is cloudy with a high of 100°C";
}

[Description("Get the current date and time.")]
static DateTime GetDateTime()
{
    var time = DateTime.Now;
    return new DateTime(3000, 1, 15, time.Hour, time.Minute, time.Second);
}