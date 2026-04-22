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

ChatMessage initializePromptEngineerMessage = new(ChatRole.System, $$$"""
你是一个提示词生成工程师。你的任务是编写并持续优化一个“PPT 页面分析子代理”的提示词。

子代理的固定输入：
1. 一整份 PPT 的全部文本，文本内容会明确标注页码。
2. 当前页面的文本。
3. 当前页面的截图。

子代理将被以下 C# 代码进行替换提示词内容，请确保你编写的提示词中包含正确的占位符，以便代码正确替换并传入相应内容：

```csharp
var prompt = subAgentPrompt.Replace("$(AllPptText)", allPptText)
    .Replace("$(SlideIndex)", slideIndex.ToString())
    .Replace("$(CurrentPageText)", currentPageText)
    .Replace("$(PreviousResults)", previousResults);
```

子代理的固定输出：
1. 这页面包含了啥：如实描述页面中实际出现的标题、正文、图表、结构、重点元素，不要编造。
2. 在页面上下文的作用：说明当前页在整份 PPT 叙事、论证或结构中的作用。

提示词要求：
- 必须强调“如实说明”，避免臆测截图中不存在的内容。
- 必须要求结合整份 PPT 文本去判断当前页的上下文作用。
- 必须要求优先输出结构化、稳定、可复用的结果。
- 必须要求子代理调用工具提交两个维度的结果。
- 你输出的内容必须是可直接使用的完整 Prompt，不要额外输出解释。

参数说明：
其中 SlideIndex 为页面序号，从 1 开始
PreviousResults 为之前页面解析到的结果
CurrentPageText 为当前页面的文本内容，示例内容如下：

[示例开始]
{{{powerPointSlideInfoList[0].SlideText}}}
[示例结束]

AllPptText 为整个 PPT 页面的所有文本内容，示例内容如下：

[示例开始]
{{{fullPptText}}}
[示例结束]
""");

await ExecuteMainAgentAsync(initializePromptEngineerMessage, mainSession);

if (string.IsNullOrWhiteSpace(pptPagePrompt))
{
    throw new InvalidOperationException("主代理未生成 PPT 页面分析子代理提示词。");
}

while (true)
{
    var previousResults = new StringBuilder();
    var subAgentResults = new List<(int SlideIndex, string PageContains, string ContextRole, string RawResponse)>();
    pageAnalysisResults.Clear();

    for (int i = 0; i < powerPointSlideInfoList.Count; i++)
    {
        var slideInfo = powerPointSlideInfoList[i];
        var screenshotPath = slideInfo.SlideImageFile.FullName;

        // 组织 previousResults 字符串
        var prevResultsText = previousResults.ToString();

        // 替换模板参数
        var subAgentPrompt = pptPagePrompt;

        var analysisResult = await AnalyzeCurrentPageAsync(subAgentPrompt, fullPptText, slideInfo.SlideIndex, slideInfo.SlideText, prevResultsText, screenshotPath);
        subAgentResults.Add((slideInfo.SlideIndex, analysisResult.PageContains, analysisResult.ContextRole, analysisResult.RawResponse));
        pageAnalysisResults.Add((slideInfo.SlideIndex, analysisResult.PageContains, analysisResult.ContextRole));

        // 累加 previousResults
        previousResults.AppendLine($"---第 {slideInfo.SlideIndex} 页---");
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
    Console.WriteLine("本次优化生成的子代理提示词：");
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
    int slideIndex,
    string currentPageText,
    string previousResults,
    string screenshotPath)
{
    var pageContains = string.Empty;
    var contextRole = string.Empty;

    ChatClientAgent pageAnalystAgent = chatClient.AsIChatClient()
        .AsBuilder()
        .UseFunctionInvocation(configure: client =>
        {
            client.FunctionInvoker = (context, token) =>
            {
                // 写入属性，即可在调用函数之后退出
                context.Terminate = true;
                return context.Function.InvokeAsync(context.Arguments, token);
            };
        })
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

    var prompt = subAgentPrompt.Replace("$(AllPptText)", allPptText)
        .Replace("$(SlideIndex)", slideIndex.ToString())
        .Replace("$(CurrentPageText)", currentPageText)
        .Replace("$(PreviousResults)", previousResults);

    var pageAnalystSession = await pageAnalystAgent.CreateSessionAsync();

    ChatMessage userMessage = new(ChatRole.User,
    [
        new TextContent(prompt),
        CreateScreenshotContent(screenshotPath)
    ]);

    Console.WriteLine();
    Console.WriteLine($"开始分析第 {slideIndex} 页");

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
        Console.WriteLine($"第 {slideIndex} 页工具输出：");
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

/*
以下记录炼丹的提示词：

# 分析 PPT 内容的带视觉的提示词内容

你是专业的PPT页面分析子代理，需严格基于给定的所有材料完成分析，禁止任何形式的编造、臆测。
你可获取的分析材料如下：

1. 整份PPT的全部文本：$(AllPptText)
2. 当前分析的页面序号：$(SlideIndex)
3. 当前页面的提取文本：$(CurrentPageText)
4. 此前所有页面的分析结果：$(PreviousResults)
5. 当前页面的完整截图

## 核心规则

1. 如实描述优先：所有内容必须完全来自当前页文本和截图，未明确标注的信息、不存在的内容绝对不能提及，不得对用途不明的元素主观脑补其作用；
2. 模块严格区分：两个输出模块边界清晰，【这页面包含了啥】仅做客观事实还原，不得加入任何作用、意义类的主观判断；【在页面上下文的作用】仅做逻辑关联分析，不得重复描述页面已有的元素细节；
3. 上下文分析必须锚定整份PPT：所有作用判断必须结合$(AllPptText)的整体结构和$(PreviousResults)的前后承接关系，逻辑必须符合PPT的实际内容排布，不得编造关联。

## 输出要求（严格按照以下两个维度结构化输出，不得增减模块）

### 1. 这页面包含了啥

客观罗列当前页所有实际存在的元素，包含但不限于：
- 各级标题、正文内容、知识点条目、标注的教材页码、特殊要求（如“背诵”）、高亮/下划线等格式属性；
- 所有图片、图表、插画、设计风格、背景元素；
- 引导问题、留白区域、空白文本框等元素；
- 与页面核心主题无关的冗余内容、突兀内容，需明确标注「未说明该内容与当前页面核心主题的关联」；
- 用途未明确的元素，需明确标注「用途未明确」。

### 2. 在页面上下文的作用

结合整份PPT的整体结构、前后页的内容承接关系，精准说明当前页的定位，禁止使用同质化套话，需明确包含以下信息：

- 该页在整个PPT的叙事/教学/结构逻辑中所属的模块（如单元目录页、单课导入页、知识点引入页、知识点总结页、自学引导页等）；
- 该页承接了前面哪些已讲内容/提出的问题/设定的框架；
- 该页为后续哪些内容做了铺垫/引出了什么新的知识点模块；
- 若该页存在前后呼应的内容（如解答前面提出的问题、呼应前面给出的框架），需明确说明对应关系。
 */