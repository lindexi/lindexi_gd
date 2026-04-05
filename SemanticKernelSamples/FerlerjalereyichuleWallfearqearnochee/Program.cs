using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using OpenAI;

using System.ClientModel;
using System.ComponentModel;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
});

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

var agent = chatClient.AsIChatClient()
    .AsBuilder()
    .BuildAIAgent(new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                AIFunctionFactory.Create(OpenApplication),
                //AIFunctionFactory.Create(WriteFileInfo)
            ]
        }
    });

// - 打开西瓜白板录音.mp3: https://pro-en-ali-pub.en5static.com/easinote5_public/uwixjonmhhqjjhnohwvvwyyhvzphihhh.mp3 失败，豆包看不到内容。 用户现在只说了“以下是我说话的内容”，但没说具体要干啥啊？不对，哦不对？不对，等下，用户还没给出具体的需求？不对不对，哦是不是用户没说完？不对不对，等下看，哦不对
// - 打开西瓜白板录音.mp4: https://pro-en-ali-pub.en5static.com/easinote5_public/uwizqwnxhhqjjhnohwvvxlixjjphihhh.mp4

ChatMessage[] chatMessageList =
[
    new ChatMessage(ChatRole.System,"你是一个应用软件使用助手，你可以调用工具帮助用户实现电脑的操作"),
    new ChatMessage(ChatRole.User,
        [
            //new TextContent("以下是我说话的内容"),
            new UriContent("https://pro-en-ali-pub.en5static.com/easinote5_public/uwizqwnxhhqjjhnohwvvxlixjjphihhh.mp4", "video/mpeg")
        ]),

];

var testInput = new ChatMessage(ChatRole.User, "请问 mp3 的 MIME 类型字符串是什么？");
_ = testInput;

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

[Description("打开应用程序")]
void OpenApplication([Description("应用名")] string applicationName)
{

}