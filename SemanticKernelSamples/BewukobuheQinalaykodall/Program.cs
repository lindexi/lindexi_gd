// See https://aka.ms/new-console-template for more information
using System;
using System.ClientModel;

using Microsoft.Agents.AI;

using OpenAI;
using OpenAI.Chat;

var keyFile = @"C:\lindexi\Work\deepseek.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://api.deepseek.com/v1")
});

var chatClient = openAiClient.GetChatClient("deepseek-reasoner");

ChatClientAgent aiAgent = chatClient.CreateAIAgent();

await foreach (var agentRunResponseUpdate in aiAgent.RunStreamingAsync("告诉我一个关于海盗的笑话"))
{
    Console.Write(agentRunResponseUpdate.Text);
}

Console.Read();
