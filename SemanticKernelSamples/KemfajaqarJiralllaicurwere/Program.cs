using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
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

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

var fileInfoManager = new List<VirtualFileInfo>();

var agent = chatClient.AsIChatClient()
    .AsBuilder()
    .BuildAIAgent(new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                AIFunctionFactory.Create(ReadFileInfo),
                AIFunctionFactory.Create(WriteFileInfo)
            ]
        }
    });

ChatMessage[] chatMessageList =
[
    new ChatMessage(ChatRole.System,"你一个产品经理，你也是一个需求分析师，你能够根据用户输入的内容进行需求分析。你需要将需求内容进行细化，按照软件工程的方式实践。开始分析之前，你需要列举计划，调用工具，将计划存放到文件里面作为文档内容。再根据计划的内容逐步进行梳理需求，每一次梳理都要有文档记录，你应当调用工具将记录的文档写入到文件中"),
    new ChatMessage(ChatRole.User,"我想要制作一个给不会用电脑的人用的文本编辑器")
];

await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(chatMessageList))
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
Console.WriteLine($"结束");
Console.WriteLine();
Console.ReadLine();

[Description("调用此方法时，需要传入文件名，将会读取对应文件名的内容。如果传入不存在的文件名，则会返回找不到错误")]
string ReadFileInfo(string fileName)
{
    var fileInfo = fileInfoManager.FirstOrDefault(t => t.FileName == fileName);
    if (fileInfo is null)
    {
        return "错误： 找不到文件";
    }

    return fileInfo.Content;
}

[Description("调用此方法时，需要传入文件名和文件的描述以及文件的内容。文件的描述应该能够简短地说明此文件的用途")]
void WriteFileInfo(string fileName, string description, string content)
{
    var fileInfo = new VirtualFileInfo(fileName, description, content);
    fileInfoManager.Add(fileInfo);
}

record VirtualFileInfo(string FileName, string Description, string Content)
{
}