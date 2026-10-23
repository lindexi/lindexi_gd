using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.DeepSeek;

using OpenAI;

using System.ClientModel;

var keyFile = @"C:\lindexi\Work\deepseek.txt";
var key = File.ReadAllText(keyFile);

using var chatClient = new DeepSeekChatClient(key, "deepseek-v4-pro");

// 如果用 OpenAIClient 的话，会出现 HTTP 400 (invalid_request_error: invalid_request_error)： The `reasoning_content` in the thinking mode must be passed back to the API. 错误
//var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
//{
//    Endpoint = new Uri("https://api.deepseek.com/v1")
//});
//var openAiChatClient = openAiClient.GetChatClient("deepseek-v4-pro").AsIChatClient();

ChatClientAgent agent = chatClient
    .AsBuilder()
    .BuildAIAgent(tools: [AIFunctionFactory.Create(GetWeather)]);
/*
 *System.ClientModel.ClientResultException:“HTTP 400 (invalid_request_error: invalid_request_error)

   The `reasoning_content` in the thinking mode must be passed back to the API.”
 */

var session = await agent.CreateSessionAsync();

bool isThinking = false;
await foreach (var agentResponseUpdate in agent.RunStreamingAsync("北京天气怎样", session))
{
    foreach (var aiContent in agentResponseUpdate.Contents)
    {
        if (aiContent is TextReasoningContent textReasoningContent)
        {
            if (string.IsNullOrEmpty(textReasoningContent.Text))
            {
                continue;
            }

            isThinking = true;
            Console.Write(textReasoningContent.Text);
        }
        else if (aiContent is TextContent textContent)
        {
            if (string.IsNullOrEmpty(textContent.Text))
            {
                continue;
            }

            if (isThinking)
            {
                Console.WriteLine();
            }

            isThinking = false;

            Console.Write(textContent.Text);
        }
    }
}

Console.WriteLine();

Console.WriteLine("Hello, World!");


string GetWeather(string city)
{
    return $"The weather in {city} is sunny.";
}