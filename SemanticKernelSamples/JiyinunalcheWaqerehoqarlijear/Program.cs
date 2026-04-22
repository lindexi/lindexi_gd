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
- 你输出的内容必须是可直接使用的完整 System Prompt，不要额外输出解释。
""");

await ExecuteMainAgentAsync(initializePromptEngineerMessage);


if (string.IsNullOrWhiteSpace(pptPagePrompt))
{
    throw new InvalidOperationException("主代理未生成 PPT 页面分析子代理提示词。");
}

// 假设 MainAgent 输出的 pptPagePrompt 里包含 {{allPptText}} {{pageNumber}} {{currentPageText}} {{previousResults}} 等模板参数
var previousResults = new StringBuilder();
var subAgentPromptTemplate = pptPagePrompt;
var subAgentResults = new List<(int PageNumber, string PageContains, string ContextRole, string RawResponse)>();

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

// 全部页面处理完后再调用 MainAgent 进行优化
var lastPage = subAgentResults.LastOrDefault();
if (lastPage != default)
{
    var optimizePrompt =
        "请根据本次所有页面的执行表现继续优化子代理提示词，并通过工具输出新的完整系统提示词。\n\n" +
        "固定任务不要变化：\n- 输入仍然是整份 PPT 文本、当前页文本、当前页截图。\n- 输出仍然是“这页面包含了啥”和“在页面上下文的作用”两个维度。\n\n" +
        "本次所有页面分析结果：\n" + previousResults.ToString() +
        "\n优化重点：\n- 继续提升“页面事实描述”和“上下文作用判断”的区分度。\n- 继续强调忠实描述截图与文本，不要幻觉。\n- 继续强调基于整份 PPT 文本理解当前页作用。\n- 如果当前提示词已经足够好，也请通过工具重新输出一份完整提示词。";
    var optimizePromptMessage = new ChatMessage(ChatRole.User, optimizePrompt);
    await ExecuteMainAgentAsync(optimizePromptMessage);
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

async Task ExecuteMainAgentAsync(ChatMessage message)
{
    while (true)
    {
        try
        {
            var agentResponseUpdates = mainAgent.RunStreamingAsync(message, mainSession);
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

    var pageAnalystSession = await pageAnalystAgent.CreateSessionAsync();
    pageAnalystSession.SetInMemoryChatHistory([new ChatMessage(ChatRole.System, subAgentPrompt)]);

    ChatMessage userMessage = new(ChatRole.User,
    [
        new TextContent($$"""
请分析当前 PPT 页面。

整份 PPT 的全部文本：
{{allPptText}}

当前页页码：{{pageNumber}}

当前页文本：
{{currentPageText}}

前序页面分析结果：
{{previousResults}}

请结合当前页截图，完成分析并调用工具输出结果。
"""
            .Replace("{{allPptText}}", allPptText)
            .Replace("{{pageNumber}}", pageNumber.ToString())
            .Replace("{{currentPageText}}", currentPageText)
            .Replace("{{previousResults}}", previousResults)
        ),
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
