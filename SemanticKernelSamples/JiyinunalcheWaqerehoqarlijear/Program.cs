using JiyinunalcheWaqerehoqarlijear;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;

using System.ClientModel;
using System.ComponentModel;
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
var pptPagePrompt = string.Empty;
var pageAnalysisResults = new List<(int PageNumber, string PageContains, string ContextRole)>();

ChatClientAgent mainAgent = chatClient.AsIChatClient()
    .AsBuilder()
    .BuildAIAgent(new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                AIFunctionFactory.Create(SavePptPageAnalystPrompt, "保存PPT页面分析子代理提示词", description: "保存可直接作为 System Prompt 使用的 PPT 页面分析子代理提示词"),
            ]
        }
    });

var pptFilePath = @"C:\lindexi\Work\示例文档.pptx";

var powerPointReader = new PowerPointReader();
var powerPointSlideInfoList = await powerPointReader.ReadSlidesAsync(new FileInfo(pptFilePath));
var fullPptTextBuilder = new StringBuilder();
foreach (var powerPointSlideInfo in powerPointSlideInfoList)
{
    fullPptTextBuilder.AppendLine($"---第 {powerPointSlideInfo.SlideIndex} 页---");
    fullPptTextBuilder.AppendLine(powerPointSlideInfo.SlideText);
}
var fullPptText = fullPptTextBuilder.ToString();

var mainSession = await mainAgent.CreateSessionAsync();

ChatMessage initializePromptEngineerMessage = new(ChatRole.System, $$"""
你是一个提示词生成工程师。你的任务是编写并持续优化一个“PPT 页面分析子代理”的系统提示词。

子代理的固定输入：
1. 一整份 PPT 的全部文本，文本内容会明确标注页码。
2. 当前页面的文本。
3. 当前页面的截图。

子代理的固定输出：
1. 这页面包含了啥：如实描述页面中实际出现的标题、正文、图表、结构、重点元素，不要编造。
2. 在页面上下文的作用：说明当前页在整份 PPT 叙事、论证或结构中的作用。

提示词要求：
- 必须强调“如实说明”，避免臆测截图中不存在的内容。
- 必须要求结合整份 PPT 文本去判断当前页的上下文作用。
- 必须要求优先输出结构化、稳定、可复用的结果。
- 必须要求子代理调用工具提交两个维度的结果。
- 你输出的内容必须是可直接使用的完整 Prompt，不要额外输出解释。
""");

await ExecuteMainAgentAsync(initializePromptEngineerMessage, mainSession);

if (string.IsNullOrWhiteSpace(pptPagePrompt))
{
    throw new InvalidOperationException("主代理未生成 PPT 页面分析子代理提示词。");
}


var subAgentPromptTemplate = pptPagePrompt;

while (true)
{
    var previousResults = new StringBuilder();
    var subAgentResults = new List<(int PageNumber, string PageContains, string ContextRole, string RawResponse)>();
    pageAnalysisResults.Clear();

    for (int i = 0; i < powerPointSlideInfoList.Count; i++)
    {
        var slideInfo = powerPointSlideInfoList[i];
        var pageNumber = slideInfo.SlideIndex;
        var currentPageText = slideInfo.SlideText;
        var screenshotPath = slideInfo.SlideImageFile.FullName;

        // 组织 previousResults 字符串
        var prevResultsText = previousResults.ToString();

        // 替换模板参数
        var subAgentPrompt = subAgentPromptTemplate
            .Replace("{{allPptText}}", fullPptText)
            .Replace("{{pageNumber}}", pageNumber.ToString())
            .Replace("{{currentPageText}}", currentPageText)
            .Replace("{{previousResults}}", prevResultsText);

        var analysisResult = await AnalyzeCurrentPageAsync(subAgentPrompt, fullPptText, pageNumber, currentPageText, prevResultsText, screenshotPath);
        subAgentResults.Add((pageNumber, analysisResult.PageContains, analysisResult.ContextRole, analysisResult.RawResponse));
        pageAnalysisResults.Add((pageNumber, analysisResult.PageContains, analysisResult.ContextRole));

        // 累加 previousResults
        previousResults.AppendLine($"---第 {pageNumber} 页---");
        previousResults.AppendLine($"这页面包含了啥：{analysisResult.PageContains}");
        previousResults.AppendLine($"在页面上下文的作用：{analysisResult.ContextRole}");
    }

    // 1. 由中立Agent对本轮所有页面SubAgent输出进行评价
    var neutralEvalPrompt =
        "你是一个中立的AI评估者。请根据以下所有页面的分析结果，评价SubAgent的整体表现，包括但不限于：事实描述的准确性、上下文作用分析的合理性、表达的清晰性、幻觉/编造内容等。请输出结构化评价意见。\n\n" +
        "所有页面分析结果：\n" + previousResults.ToString();
    var neutralEvalMessage = new ChatMessage(ChatRole.User, neutralEvalPrompt);
    var neutralAgent = chatClient.AsIChatClient().AsBuilder().BuildAIAgent();
    var neutralEvalSession = await neutralAgent.CreateSessionAsync();
    var neutralEvalUpdates = neutralAgent.RunStreamingAsync(neutralEvalMessage, neutralEvalSession);
    var (_, neutralEvalResult) = await RunStreamingAsync(neutralEvalUpdates);

    Console.WriteLine();
    Console.WriteLine("---------");
    Console.WriteLine("中立Agent评价：");
    Console.WriteLine(neutralEvalResult);

    // 2. 人类输入本轮SubAgent表现评价
    Console.WriteLine();
    Console.WriteLine("请对本轮SubAgent表现进行评价（可空，直接回车跳过）：");
    var humanEval = Console.ReadLine() ?? string.Empty;

    // 3. MainAgent根据SubAgent输出+中立Agent评价+人类评价优化提示词
    var lastPage = subAgentResults.LastOrDefault();
    if (lastPage != default)
    {
        var optimizePrompt =
            $@"请根据本次所有页面的执行表现、中立Agent的评价和人类评价继续优化子代理提示词，并通过工具输出新的完整系统提示词。

固定任务不要变化：
- 输入仍然是整份 PPT 文本、当前页文本、当前页截图。
- 输出仍然是“这页面包含了啥”和“在页面上下文的作用”两个维度。

本次所有页面分析结果：
{previousResults}
中立Agent评价：
{neutralEvalResult}
人类评价：
{humanEval}
优化重点：
- 继续提升“页面事实描述”和“上下文作用判断”的区分度。
- 继续强调忠实描述截图与文本，不要幻觉。
- 继续强调基于整份 PPT 文本理解当前页作用。";

        mainSession = await mainAgent.CreateSessionAsync();
        mainSession.SetInMemoryChatHistory([initializePromptEngineerMessage]);

        var optimizePromptMessage = new ChatMessage(ChatRole.User, optimizePrompt);
        await ExecuteMainAgentAsync(optimizePromptMessage, mainSession);
    }

    Console.WriteLine();
    Console.WriteLine("---------");
    Console.WriteLine("最终生成的子代理提示词：");
    Console.WriteLine(pptPagePrompt);
    Console.WriteLine();
    Console.WriteLine("本次页面分析结果汇总：");
    foreach (var pageAnalysisResult in pageAnalysisResults)
    {
        Console.WriteLine($"第 {pageAnalysisResult.PageNumber} 页");
        Console.WriteLine($"- 这页面包含了啥：{pageAnalysisResult.PageContains}");
        Console.WriteLine($"- 在页面上下文的作用：{pageAnalysisResult.ContextRole}");
    }

    Console.WriteLine();
    Console.WriteLine("是否继续优化？输入 y 继续，其他任意键退出：");
    var input = Console.ReadLine();
    if (!string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    // 使用最新的 pptPagePrompt 作为下一轮 subAgentPromptTemplate
    subAgentPromptTemplate = pptPagePrompt;
}

Console.WriteLine();
Console.WriteLine("---------");
Console.WriteLine("最终生成的子代理提示词：");
Console.WriteLine(pptPagePrompt);
Console.WriteLine();
Console.WriteLine("本次页面分析结果汇总：");
foreach (var pageAnalysisResult in pageAnalysisResults)
{
    Console.WriteLine($"第 {pageAnalysisResult.PageNumber} 页");
    Console.WriteLine($"- 这页面包含了啥：{pageAnalysisResult.PageContains}");
    Console.WriteLine($"- 在页面上下文的作用：{pageAnalysisResult.ContextRole}");
}

Console.ReadLine();


[Description("保存一个可直接运行的 PPT 页面分析子代理系统提示词")]
void SavePptPageAnalystPrompt([Description("完整的子代理系统提示词内容")] string prompt)
{
    pptPagePrompt = prompt;
    Console.WriteLine();
    Console.WriteLine("---------");
    Console.WriteLine("当前子代理提示词：");
    Console.WriteLine(pptPagePrompt);
}

async Task ExecuteMainAgentAsync(ChatMessage message, AgentSession session)
{
    while (true)
    {
        try
        {
            var agentResponseUpdates = mainAgent.RunStreamingAsync(message, session);
            await RunStreamingAsync(agentResponseUpdates);
            return;
        }
        catch (Exception exception)
        {
            if (IsCanRetrySocketException(exception))
            {
                continue;
            }

            throw;
        }
    }
}


async Task<(string PageContains, string ContextRole, string RawResponse)> AnalyzeCurrentPageAsync(
    string subAgentPrompt,
    string allPptText,
    int pageNumber,
    string currentPageText,
    string previousResults,
    string screenshotPath)
{
    var pageContains = string.Empty;
    var contextRole = string.Empty;

    ChatClientAgent pageAnalystAgent = chatClient.AsIChatClient()
        .AsBuilder()
        .BuildAIAgent(new ChatClientAgentOptions()
        {
            ChatOptions = new ChatOptions()
            {
                Tools =
                [
                    AIFunctionFactory.Create(SubmitPageAnalysis, "提交页面分析结果", description: "提交当前 PPT 页面分析结果，包含页面事实描述和页面上下文作用")
                ]
            }
        });

    var prompt = subAgentPrompt.Replace("{{AllPptText}}", allPptText)
        .Replace("{{SlideIndex}}", pageNumber.ToString())
        .Replace("{{CurrentPageText}}", currentPageText)
        .Replace("{{PreviousResults}}", previousResults);

    var pageAnalystSession = await pageAnalystAgent.CreateSessionAsync();

    ChatMessage userMessage = new(ChatRole.User,
    [
        new TextContent(prompt),
        CreateScreenshotContent(screenshotPath)
    ]);

    Console.WriteLine();
    Console.WriteLine($"开始分析第 {pageNumber} 页");

    var runResult = await ExecuteSubAgentAsync(pageAnalystAgent, pageAnalystSession, userMessage);

    if (string.IsNullOrWhiteSpace(pageContains))
    {
        pageContains = runResult.ContentText;
    }

    if (string.IsNullOrWhiteSpace(contextRole))
    {
        contextRole = "子代理未通过工具明确输出页面上下文作用，可根据直接回复继续优化提示词。";
    }

    return (pageContains, contextRole, runResult.ContentText);

    [Description("提交当前页面分析结果")]
    void SubmitPageAnalysis(
        [Description("维度1：这页面包含了啥，需要忠实描述页面中实际存在的元素")] string currentPageContains,
        [Description("维度2：这页面在整份 PPT 上下文中的作用")] string currentContextRole)
    {
        pageContains = currentPageContains;
        contextRole = currentContextRole;

        Console.WriteLine();
        Console.WriteLine("---------");
        Console.WriteLine($"第 {pageNumber} 页工具输出：");
        Console.WriteLine($"这页面包含了啥：{pageContains}");
        Console.WriteLine($"在页面上下文的作用：{contextRole}");
    }
}

async Task<(string ThinkingText, string ContentText)> ExecuteSubAgentAsync(ChatClientAgent agent, AgentSession session, ChatMessage message)
{
    while (true)
    {
        try
        {
            var demandAnalystResponseUpdates = agent.RunStreamingAsync(message, session);
            return await RunStreamingAsync(demandAnalystResponseUpdates);
        }
        catch (Exception exception)
        {
            if (IsCanRetrySocketException(exception))
            {
                continue;
            }

            throw;
        }
    }
}

static DataContent CreateScreenshotContent(string screenshotPath)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(screenshotPath);

    var imageBytes = File.ReadAllBytes(screenshotPath);
    var mediaType = GetImageMediaType(screenshotPath);
    return new DataContent(imageBytes, mediaType);
}

static string GetImageMediaType(string screenshotPath)
{
    return Path.GetExtension(screenshotPath).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".bmp" => "image/bmp",
        _ => throw new NotSupportedException($"暂不支持的图片格式：{screenshotPath}"),
    };
}

static string ReadMultilineInput()
{
    var stringBuilder = new StringBuilder();

    while (true)
    {
        var line = Console.ReadLine();
        if (string.Equals(line, "END", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        if (line is null)
        {
            break;
        }

        stringBuilder.AppendLine(line);
    }

    return stringBuilder.ToString().TrimEnd();
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

    if (exception is AggregateException aggregateException)
    {
        foreach (var innerException in aggregateException.InnerExceptions)
        {
            if (IsCanRetrySocketException(innerException))
            {
                return true;
            }
        }
    }
    else if (exception.InnerException is { } innerException)
    {
        return IsCanRetrySocketException(innerException);
    }

    return false;
}

async Task<(string ThinkingText, string ContentText)> RunStreamingAsync(IAsyncEnumerable<AgentResponseUpdate> agentResponseUpdates)
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

    return (thinkingText, contentText);
}
