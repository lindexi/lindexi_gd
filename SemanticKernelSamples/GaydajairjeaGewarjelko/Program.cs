// See https://aka.ms/new-console-template for more information
using System;
using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

var chatClient = openAiClient.GetChatClient("ep-20260115192014-kgkxq");

JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(PersonInfo));
ChatOptions chatOptions = new()
{
    ResponseFormat = ChatResponseFormat.ForJsonSchema(
        schema: schema,
        schemaName: "PersonInfo",
        schemaDescription: "Information about a person including their name, age, and occupation"),
};

chatOptions.Instructions = "You are a helpful assistant.";
ChatClientAgent aiAgent = chatClient.CreateAIAgent(new ChatClientAgentOptions()
{
    Name = "HelpfulAssistant",
    //Instructions = "You are a helpful assistant.",
    ChatOptions = chatOptions
});

var response = await aiAgent.RunAsync("Please provide information about John Smith, who is a 35-year-old software engineer.");

var personInfo = response.Deserialize<PersonInfo>(JsonSerializerOptions.Web);
Console.WriteLine($"Name: {personInfo.Name}, Age: {personInfo.Age}, Occupation: {personInfo.Occupation}");

Console.WriteLine();

Console.Read();


public class PersonInfo
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? Occupation { get; set; }
}