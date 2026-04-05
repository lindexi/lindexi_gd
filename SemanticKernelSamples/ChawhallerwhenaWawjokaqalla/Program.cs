using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using OpenAI;

using System.ClientModel;

var keyFile = @"C:\lindexi\Work\Qwen.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1"),
});

var chatClient = openAiClient.GetChatClient("qwen3.5-omni-plus"); // System.ClientModel.ClientResultException:“HTTP 403 (access_denied: access_denied)

var agent = chatClient.AsIChatClient()
    .AsBuilder()
    .BuildAIAgent(new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                //AIFunctionFactory.Create(OpenApplication),
                //AIFunctionFactory.Create(WriteFileInfo)
            ]
        }
    });

ChatMessage[] chatMessageList =
[
    new ChatMessage(ChatRole.System,"你是一个应用软件使用助手，你可以调用工具帮助用户实现电脑的操作"),
    new ChatMessage(ChatRole.User,
    [
        //new TextContent("以下是我说话的内容"),
        //new UriContent("https://pro-en-ali-pub.en5static.com/easinote5_public/uwizqwnxhhqjjhnohwvvxlixjjphihhh.mp4", "video/mpeg"),
        //new UriContent("https://pro-en-ali-pub.en5static.com/easinote5_public/uwixjonmhhqjjhnohwvvwyyhvzphihhh.mp3", "audio/mpeg"),
    ]),
];
//var testInput = new ChatMessage(ChatRole.User, "请问 mp3 的 MIME 类型字符串是什么？");
//_ = testInput;

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

Console.WriteLine("Hello, World!");


Console.WriteLine("Hello, World!");
