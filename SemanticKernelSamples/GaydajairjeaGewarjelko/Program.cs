// See https://aka.ms/new-console-template for more information
using System;
using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
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

JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(ElementOperationInfo));
ChatOptions chatOptions = new()
{
    // {"type":"object","properties":{"ElementId":{"type":["string","null"]},"OperationName":{"description":"\u5305\u62EC\u79FB\u52A8\u3001\u5220\u9664\u7B49","type":["string","null"]}}}
    ResponseFormat = ChatResponseFormat.ForJsonSchema(
        schema: schema,
        schemaName: "ElementOperationInfo",
        schemaDescription: "对某个元素的处理的信息"),
};

chatOptions.Instructions = "你是一个 PPT 文档处理助手";
ChatClientAgent aiAgent = chatClient.CreateAIAgent(new ChatClientAgentOptions()
{
    Name = "HelpfulAssistant",
    //Instructions = "You are a helpful assistant.",
    ChatOptions = chatOptions
});

var response = await aiAgent.RunAsync("请帮我删除 Id 为 f1 的元素");

// {{"ElementId": "f1", "OperationName": "删除"}}
var elementOperationInfo = response.Deserialize<ElementOperationInfo>(JsonSerializerOptions.Web);

Console.WriteLine();

Console.Read();


public class ElementOperationInfo
{
    [JsonPropertyName("ElementId")]
    public string? ElementId { get; set; }

    [JsonPropertyName("OperationName")]
    [Description("包括移动、删除等")]
    public string? OperationName { get; set; }
}