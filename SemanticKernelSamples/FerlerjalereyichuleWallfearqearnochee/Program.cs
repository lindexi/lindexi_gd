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



ChatMessage[] chatMessageList =
[
    /*
  思考：
    
    用户现在只说了“以下是我说话的内容”，但没说具体要干啥啊？不对，哦不对？不对，等下，用户还没给出具体的需求？不对不对，哦是不是用户没说完？不对不对，等下看，哦不对，用户现在的输入只有“以下是我说话的内容”，没有具体的操作请求，比如要打开什么应用之类的？不对不对，不对，我再想想？哦不对，会不会是用户漏了？不对不对，不对，等下，现在的情况是用户没有提供具体的需求，比如没有说要打开哪个应用，或者要进行什么操作，所以是不是应该询问用户具体需要什么帮助？不对不对，等下哦，不对，我再看一遍问题。哦不对，用户现在只发了“以下是我说话的内容”，后面没内容了？那我应该请用户说明具体的操作需求，比如想要打开什么应用，或者进行什么操作，这样我才能调用对应的工具帮忙。不对不对，等下有没有可能我漏看了？哦没有，用户的输入就只有那一句。那我应该回复询问用户具体的需求对吧？不对不对，等下不对，哦是不是用户的内容没粘贴全？对，应该是用户只说了开头，没说具体要做什么，所以我需要让用户告知具体的操作需求，比如需要打开什么应用，或者完成什么电脑操作，我才能提供对应的帮助。
    ----------
    请您告知具体的操作需求哦，比如您想要打开什么应用程序，或者需要完成什么电脑操作，我会帮您调用对应的工具来实现。
    */
    new ChatMessage(ChatRole.System,"你是一个应用软件使用助手，你可以调用工具帮助用户实现电脑的操作"),
    new ChatMessage(ChatRole.User,
        [
            new TextContent("以下是我说话的内容"),
            new UriContent("https://pro-en-ali-pub.en5static.com/easinote5_public/uwixjonmhhqjjhnohwvvwyyhvzphihhh.mp3", "audio/mpeg")
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