// See https://aka.ms/new-console-template for more information

using Microsoft.Agents.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;
using System.Text.Json.Schema;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),

});

/*
 *System.ClientModel.ClientResultException:“HTTP 400 (BadRequest: InvalidParameter)
   Parameter: response_format.type
   
   The parameter `response_format.type` specified in the request are not valid: `json_schema` is not supported by this model. Request id: 021774405147745e3c0010419774ef230e4bef6206f9366dbefa7”
   
 */
var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

//var jsonSerializerOptions = new JsonSerializerOptions();
//var jsonSchemaAsNode = jsonSerializerOptions.GetJsonSchemaAsNode(typeof(PersonInfo));
//var jsonString = jsonSchemaAsNode.ToJsonString();

JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(PersonInfo));

var agent = chatClient.AsAIAgent(options:new ChatClientAgentOptions()
{
    ChatOptions = new ChatOptions()
    {
        ResponseFormat = ChatResponseFormat.ForJsonSchema(schema, "PersonInfo")
    }
});

var response = await agent.RunAsync("Please provide information about John Smith, who is a 35-year-old software engineer.");

await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(new ChatMessage(ChatRole.User, "Please provide information about John Smith, who is a 35-year-old software engineer.")))
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

Console.WriteLine();

Console.Read();

class PersonInfo
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? Occupation { get; set; }
}