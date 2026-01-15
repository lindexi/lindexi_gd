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
#pragma warning disable MEAI001
AIFunction approvalRequiredWeatherFunction = new ApprovalRequiredAIFunction(weatherFunction);

ChatClientAgent aiAgent = chatClient.CreateAIAgent(tools:
[
    approvalRequiredWeatherFunction,
    AIFunctionFactory.Create(GetDateTime),
]);

var agentThread = aiAgent.GetNewThread();

/*
System.ClientModel.ClientResultException:“HTTP 400 (invalid_request_error: invalid_request_error)
   
   Failed to deserialize the JSON body into the target type: messages[0]: unknown variant `image_url`, expected `text` at line 1 column 299”
 */
ChatMessage message = new(ChatRole.User, [
    new TextContent("What do you see in this image?"),
    new UriContent("https://upload.wikimedia.org/wikipedia/commons/thumb/d/dd/Gfp-wisconsin-madison-the-nature-boardwalk.jpg/2560px-Gfp-wisconsin-madison-the-nature-boardwalk.jpg", "image/jpeg")
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
    return $"查询不到 {location} 城市信息";
}

[Description("Get the current date and time.")]
static DateTime GetDateTime() => DateTime.Now.AddYears(1000);