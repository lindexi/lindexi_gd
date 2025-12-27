// See https://aka.ms/new-console-template for more information

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

var keyFile = @"C:\lindexi\Work\deepseek.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://api.deepseek.com/v1")
});

var chatClient = openAiClient.GetChatClient("deepseek-chat");

JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(PersonInfo));

ChatOptions chatOptions = new()
{
    ResponseFormat = ChatResponseFormat.ForJsonSchema
    (
        schema: schema,
        schemaName: "PersonInfo",
        schemaDescription: "Information about a person including their name, age, and occupation"
    )
};

var chatClientAgent = chatClient.CreateAIAgent(options: new ChatClientAgentOptions()
{
    Name = "HelpfulAssistant",
    //Instructions = "You are a helpful assistant.",
    ChatOptions = chatOptions,
    Description = "You are a helpful assistant.",
});

/*
System.ClientModel.ClientResultException:“HTTP 400 (invalid_request_error: invalid_request_error)
   
   This response_format type is unavailable now”
 */

var response = await chatClientAgent.RunAsync("Please provide information about John Smith, who is a 35-year-old software engineer.");

var responseText = response.Text;

Console.WriteLine(response);
Console.Read();

class PersonInfo
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? Occupation { get; set; }
}