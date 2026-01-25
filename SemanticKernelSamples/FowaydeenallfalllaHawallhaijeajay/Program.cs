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

ChatClientAgent aiAgent = chatClient.CreateAIAgent(tools:
[
    AIFunctionFactory.Create(GetRegionId),
    AIFunctionFactory.Create(DeleteElement),
]);

var agentThread = aiAgent.GetNewThread();

ChatMessage message = new(ChatRole.User, 
[
    new TextContent("请评价这一页的 PPT 的美观程度和删除影响美观的元素"),
    new UriContent("http://cdn.lindexi.site/lindexi-2026116112065501.jpg", "image/jpeg")
]);

await foreach (var agentRunResponseUpdate in aiAgent.RunStreamingAsync(message, agentThread))
{
    Console.Write(agentRunResponseUpdate.Text);
}

Console.WriteLine();

Console.Read();


[Description("获取某个区域范围的元素 Id 号")]
static string GetRegionId([Description("区域范围，采用 X Y 宽度 高度 的格式")]string region)
{
    return "F1";
}

[Description("删除给定 Id 号的元素")]
static void DeleteElement(string elementId)
{

}

[Description("移动给定 Id 号的元素")]
static void MoveElement(string elementId)
{

}