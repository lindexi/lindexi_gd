using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;
using System.IO.Enumeration;
using System.Net.Sockets;
using System.Text;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
    NetworkTimeout = TimeSpan.FromHours(1),
});

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

ChatClientAgent mainAgent = chatClient.AsIChatClient()
    .AsBuilder()
    .BuildAIAgent(new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                AIFunctionFactory.Create(BuildDemandAnalyst, "创建需求分析师", description: "创建需求分析师的工具，需要传入需求分析师的提示词内容"),
            ]
        }
    });
ChatMessage message = new ChatMessage(ChatRole.System,
    "你是一个辅助编写提示词的助手。现在你需要使用提示词技术制作一个需求分析师角色的 Agent，你需要先给出需求分析师的职责和任务的提示词内容，比如要求需求分析师进行多轮对话进行细致的需求分析，了解实质的需求，考虑周全问题，最后让需求分析师调用工具输出需求文档。你再根据需求分析师输出的需求分析文档和对话，对提示词进行优化，如此迭代已实现一个强大的需求分析师。");

var mainSession = await mainAgent.CreateSessionAsync();
IAsyncEnumerable<AgentResponseUpdate> agentResponseUpdates = mainAgent.RunStreamingAsync(message, mainSession);
await RunStreamingAsync(agentResponseUpdates);
var demandAnalystPrompt = string.Empty;

var fileContentList = new List<(string FileSystemName, string Content)>();

// 迭代 10 次，拍脑袋定的迭代次数
for (int i = 0; i < 10; i++)
{
    ChatClientAgent demandAnalystAgent = chatClient.AsIChatClient()
        .AsBuilder()
        .BuildAIAgent(new ChatClientAgentOptions()
        {
            ChatOptions = new ChatOptions()
            {
                Tools =
                [
                    AIFunctionFactory.Create(WriteDocument, "写需求文档"),
                    AIFunctionFactory.Create(FinishDemandAnalysis, "标记需求分析完成")
                ]
            }
        });

    var demandAnalystSession = await demandAnalystAgent.CreateSessionAsync();

    // 传入 MainAgent 提供的提示词
    ChatMessage demandAnalystSystemPrompt = new ChatMessage(ChatRole.System, demandAnalystPrompt);
    demandAnalystSession.SetInMemoryChatHistory([demandAnalystSystemPrompt]);

    Console.WriteLine($"请输入需求");
    bool isFinish = false;

    while (!isFinish)
    {
        var inputText = Console.ReadLine();
        Console.WriteLine($"开始处理");

        var userMessage = new ChatMessage(ChatRole.User, inputText);
        while (true)
        {
            try
            {
                var demandAnalystResponseUpdates
                    = demandAnalystAgent.RunStreamingAsync(userMessage, demandAnalystSession);
                await RunStreamingAsync(demandAnalystResponseUpdates);
                break;
            }
            catch (Exception e)
            {
                if (IsCanRetrySocketException(e))
                {
                    continue;
                }
                else
                {
                    throw;
                }
            }
        }
    }


    [Description("完成需求分析，当你认为需求已经分析完成了，请调用此方法结束对话")]
    void FinishDemandAnalysis()
    {
        isFinish = true;
    }

    static bool IsCanRetrySocketException(Exception exception)
    {
        if (exception is SocketException socketException)
        {
            if (socketException.SocketErrorCode == SocketError.ConnectionRefused)
            {
                return false;
            }

            return true;
        }
        else if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                if (IsCanRetrySocketException(innerException))
                {
                    return true;
                }
            }
        }
        else
        {
            if (exception.InnerException is { } innerException)
            {
                return IsCanRetrySocketException(innerException);
            }
        }

        return false;
    }
}

Console.WriteLine("Hello, World!");
Console.ReadLine();


[Description("构建一个需求分析师")]
async Task BuildDemandAnalyst([Description("传入的提示词内容")] string prompt)
{
    demandAnalystPrompt = prompt;
    Console.WriteLine();
    Console.WriteLine("---------");
    Console.WriteLine("需求分析师的提示词内容：");
    Console.WriteLine(demandAnalystPrompt);
}

[Description("编写文档内容")]
void WriteDocument(string fileName, string content)
{
    fileContentList.Add((fileName, content));
}


async Task RunStreamingAsync(IAsyncEnumerable<AgentResponseUpdate> agentResponseUpdates)
{
    var isThinking = false;
    var thinkingStringBuilder = new StringBuilder();
    var contentStringBuilder = new StringBuilder();

    await foreach (var agentResponseUpdate in agentResponseUpdates)
    {
        foreach (var content in agentResponseUpdate.Contents)
        {
            if (content is TextReasoningContent textReasoningContent)
            {
                isThinking = true;
                Console.Write(textReasoningContent.Text);
                thinkingStringBuilder.Append(textReasoningContent.Text);
            }
            else if (content is TextContent textContent)
            {
                if (string.IsNullOrEmpty(textContent.Text))
                {
                    continue;
                }

                if (isThinking)
                {
                    Console.WriteLine();
                    Console.WriteLine("---------");
                }

                isThinking = false;
                Console.Write(textContent.Text);
                contentStringBuilder.Append(textContent.Text);
            }
            else if (content is UsageContent usageContent)
            {
                Console.WriteLine();
                var usage = usageContent.Details;
                Console.WriteLine(
                    $"本次对话总Token消耗：{usage.TotalTokenCount};输入Token消耗：{usage.InputTokenCount};输出Token消耗：{usage.OutputTokenCount},其中思考占{usage.ReasoningTokenCount ?? 0}");
            }
        }
    }

    var thinkingText = thinkingStringBuilder.ToString();
    var contentText = contentStringBuilder.ToString();

    GC.KeepAlive(thinkingText);
    GC.KeepAlive(contentText);
}