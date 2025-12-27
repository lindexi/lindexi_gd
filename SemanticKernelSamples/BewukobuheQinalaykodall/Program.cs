// See https://aka.ms/new-console-template for more information
using System;
using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;

using Microsoft.Agents.AI;

using OpenAI;
using OpenAI.Chat;

var keyFile = @"C:\lindexi\Work\deepseek.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key),new OpenAIClientOptions()
{
    Endpoint = new Uri("https://api.deepseek.com/v1")
});
var chatClient = openAiClient.GetChatClient("deepseek-chat");
var result = chatClient.CompleteChat([new UserChatMessage("Tell me a joke about a pirate.")]);
var chatCompletion = result.Value;

AIAgent agent = new AzureOpenAIClient(
        new Uri("https://api.deepseek.com/"),
        new ApiKeyCredential(key))
    .GetChatClient("deepseek-chat")
    .CreateAIAgent(instructions: "You are good at telling jokes.");

Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));

Console.WriteLine("Hello, World!");
