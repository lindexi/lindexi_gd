using System.ClientModel;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
    NetworkTimeout = TimeSpan.FromHours(1),
});

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");
var coursewareJsonFile = @"C:\lindexi\Work\CoursewareMaterialInfo.json";

var coursewareMaterialInfo = JsonSerializer.Deserialize<SavableCoursewareMaterialInfo>(File.ReadAllText(coursewareJsonFile), new JsonSerializerOptions()
{
    PropertyNameCaseInsensitive = true,
}) ?? throw new InvalidOperationException("课件 JSON 反序列化失败，未能读取到课件页面信息。");

if (coursewareMaterialInfo.SlideMaterialInfoList.Count == 0)
{
    throw new InvalidOperationException("课件 JSON 中没有任何页面信息。");
}

var subAgentPrompt = string.Empty;
var scriptResults = new List<(int SlideIndex, string ScriptText, string RawResponse)>();
var allCoursewareText = BuildAllCoursewareText(coursewareMaterialInfo);

ChatClientAgent mainAgent = chatClient.AsIChatClient()
    .AsBuilder()
    .BuildAIAgent(new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                AIFunctionFactory.Create(SaveSubAgentPrompt, "保存课件讲述脚本子代理提示词", description: "保存可直接作为 System Prompt 使用的课件页面讲述脚本生成子代理提示词"),
            ]
        }
    });

ChatClientAgent neutralEvaluatorAgent = chatClient.AsIChatClient()
    .AsBuilder()
    .BuildAIAgent();

var mainSession = await mainAgent.CreateSessionAsync();
ChatMessage initializePromptEngineerMessage = new(ChatRole.System, $"""
你是一个提示词生成工程师。你的任务是编写并持续优化一个“课件页面讲述脚本生成子代理”的系统提示词。

子代理的固定输入：
1. 整份课件的全部页面文本，文本内容会明确标注页码。
2. 当前页面的页面序号。
3. 当前页面的文本提取信息。
4. 当前页面之前已经生成的讲述脚本。
5. 当前页面截图，会以多模态图片内容提供。

子代理将被以下 C# 代码进行替换提示词内容，请确保你编写的提示词中包含正确的占位符，以便代码正确替换并传入相应内容：

```csharp
var prompt = subAgentPrompt.Replace("$(AllCoursewareText)", allCoursewareText)
    .Replace("$(SlideIndex)", slideIndex.ToString())
    .Replace("$(CurrentPageText)", currentPageText)
    .Replace("$(PreviousScripts)", previousScripts);
```

子代理的固定任务：
- 根据当前页面截图和当前页面文本提取信息，进行多模态识别，生成当前页的中文讲述脚本。
- 讲述脚本将用于后续生成页面讲述音频，并与页面截图合成课程视频。

讲述脚本要求：
- 必须逐页独立生成当前页的讲述内容，不要生成后续视频合成逻辑。
- 必须忠实于当前页截图和文本提取信息，不要臆测截图与文本中不存在的内容。
- 需要结合整份课件文本和此前页面脚本，让当前页讲述自然承接上下文。
- 脚本中必须嵌入操作符，操作符必须使用 `[]` 包起来，并采用 `Key: Value` 格式。
- 操作符示例：`[语气: 平缓]`、`[语气: 强调]`、`[停顿: 2秒]`。
- 每一段讲述应根据内容自然加入语气和停顿操作符，避免整篇缺少节奏控制。
- 输出必须是可直接用于语音生成的脚本文本，不要输出 Markdown 解释。
- 必须要求子代理调用工具提交最终脚本。

参数说明：
其中 SlideIndex 为页面序号，从 1 开始。
PreviousScripts 为之前页面生成的脚本结果。
CurrentPageText 为当前页面的文本内容，示例内容如下：

[示例开始]
{coursewareMaterialInfo.SlideMaterialInfoList[0].ContentText}
[示例结束]

AllCoursewareText 为整个课件页面的所有文本内容，示例内容如下：

[示例开始]
{allCoursewareText}
[示例结束]
""");

await ExecuteMainAgentAsync(initializePromptEngineerMessage, mainSession);

if (string.IsNullOrWhiteSpace(subAgentPrompt))
{
    throw new InvalidOperationException("主代理未生成课件讲述脚本子代理提示词。");
}

for (var rounds = 0; ; rounds++)
{
    scriptResults.Clear();
    var previousScriptsBuilder = new StringBuilder();

    var currentSubAgentPrompt = subAgentPrompt;
    for (var i = 0; i < coursewareMaterialInfo.SlideMaterialInfoList.Count; i++)
    {
        var slideIndex = i + 1;
        var slideMaterialInfo = coursewareMaterialInfo.SlideMaterialInfoList[i];
        var scriptResult = await GenerateSlideScriptAsync(currentSubAgentPrompt, allCoursewareText, slideIndex, slideMaterialInfo.ContentText, previousScriptsBuilder.ToString(), slideMaterialInfo.SlideThumbnailFilePath);

        scriptResults.Add((slideIndex, scriptResult.ScriptText, scriptResult.RawResponse));
        previousScriptsBuilder.AppendLine($"---第 {slideIndex} 页---");
        previousScriptsBuilder.AppendLine(scriptResult.ScriptText);
    }

    Console.WriteLine("---------");
    Console.WriteLine("本次优化生成的子代理提示词：");
    Console.WriteLine(subAgentPrompt);
    Console.WriteLine();
    Console.WriteLine("本次页面讲述脚本汇总：");
    WriteScriptResults(scriptResults);

    Console.WriteLine();
    Console.WriteLine("是否继续优化？输入 y 继续，其他任意键退出：");
    var input = Console.ReadLine();
    if (!string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    Console.WriteLine($"进行下一步的优化");

    var neutralEvalMessage = new ChatMessage(ChatRole.User, $"""
你是一个中立的 AI 评估者。请根据以下所有页面的讲述脚本，评价 SubAgent 的整体表现。

评价重点：
1. 是否忠实于课件页面信息，是否存在臆测和编造。
2. 是否适合生成讲述音频。
3. 是否自然承接整份课件上下文。
4. 是否合理使用 `[Key: Value]` 格式操作符，例如 `[语气: 平缓]` 和 `[停顿: 2秒]`。
5. 是否存在操作符格式错误、节奏过密或过少的问题。

所有页面讲述脚本：
{previousScriptsBuilder}
""");

    var neutralEvalSession = await neutralEvaluatorAgent.CreateSessionAsync();
    var neutralEvalUpdates = neutralEvaluatorAgent.RunStreamingAsync(neutralEvalMessage, neutralEvalSession);
    var (_, neutralEvalResult) = await RunStreamingAsync(neutralEvalUpdates);

    Console.WriteLine();
    Console.WriteLine("---------");
    Console.WriteLine("中立评价者 Agent 评价：");
    Console.WriteLine(neutralEvalResult);

    Console.WriteLine();
    Console.WriteLine("请对本轮 SubAgent 生成效果进行评价（可空，直接回车跳过）：");
    var humanEval = Console.ReadLine() ?? string.Empty;

    // 不要重新创建 Session，保持上下文连续，方便主代理记忆之前的优化过程和评价反馈。但缺点是会导致上下文比较长
    //mainSession = await mainAgent.CreateSessionAsync();
    //mainSession.SetInMemoryChatHistory([initializePromptEngineerMessage]);

    var optimizePromptMessage = new ChatMessage(ChatRole.User, $"""
请根据本次所有页面脚本、中立评价者 Agent 的评价和人类评价继续优化子代理提示词，并通过工具输出新的完整系统提示词。

固定任务不要变化：
- 输入仍然是整份课件文本、当前页序号、当前页文本、此前页面脚本和当前页截图。
- 输出仍然是当前页可直接用于语音生成的中文讲述脚本。
- 脚本必须嵌入 `[]` 包起来的 `Key: Value` 格式操作符，例如 `[语气: 平缓]`、`[停顿: 2秒]`。

本次所有页面脚本：
{previousScriptsBuilder}

中立评价者 Agent 评价：
{neutralEvalResult}

人类评价：
{humanEval}

优化重点：
- 继续提升脚本对截图和文本信息的忠实程度。
- 继续提升讲述节奏和语气操作符的自然度。
- 继续提升页面之间的上下文承接。
""");

    await ExecuteMainAgentAsync(optimizePromptMessage, mainSession);

    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine("---------");
Console.WriteLine("最终生成的子代理提示词：");
Console.WriteLine(subAgentPrompt);
Console.WriteLine();
Console.WriteLine("最终页面讲述脚本汇总：");
WriteScriptResults(scriptResults);

Console.ReadLine();


[Description("保存一个可直接运行的课件页面讲述脚本生成子代理系统提示词")]
void SaveSubAgentPrompt([Description("完整的子代理系统提示词内容")] string prompt)
{
    subAgentPrompt = prompt;
    Console.WriteLine();
    Console.WriteLine("---------");
    Console.WriteLine("当前子代理提示词：");
    Console.WriteLine(subAgentPrompt);
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
        catch (Exception exception) when (IsCanRetrySocketException(exception))
        {
        }
    }
}

async Task<(string ScriptText, string RawResponse)> GenerateSlideScriptAsync(
    string systemPrompt,
    string allText,
    int slideIndex,
    string currentPageText,
    string previousScripts,
    string screenshotPath)
{
    var scriptText = string.Empty;
    var tool = AIFunctionFactory.Create(SubmitSlideScript, "提交当前页面讲述脚本", description: "提交当前课件页面最终讲述脚本，脚本需包含语气和停顿等操作符");

    ChatClientAgent subAgent = chatClient.AsIChatClient()
        .AsBuilder()
        .UseFunctionInvocation(configure: client =>
        {
            client.FunctionInvoker = (context, token) =>
            {
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
                    tool
                ]
            }
        });

    var prompt = systemPrompt.Replace("$(AllCoursewareText)", allText)
        .Replace("$(SlideIndex)", slideIndex.ToString())
        .Replace("$(CurrentPageText)", currentPageText)
        .Replace("$(PreviousScripts)", previousScripts);

    var subAgentSession = await subAgent.CreateSessionAsync();
    ChatMessage userMessage = new(ChatRole.User,
    [
        new TextContent(prompt),
        CreateScreenshotContent(screenshotPath)
    ]);

    Console.WriteLine();
    Console.WriteLine($"开始生成第 {slideIndex} 页讲述脚本");

    var runResult = await ExecuteSubAgentAsync(subAgent, subAgentSession, userMessage);

    if (string.IsNullOrWhiteSpace(scriptText))
    {
        scriptText = runResult.ContentText;
    }

    return (scriptText, runResult.ContentText);

    [Description("提交当前课件页面讲述脚本")]
    void SubmitSlideScript([Description("当前页面可直接用于语音生成的中文讲述脚本，必须包含 [Key: Value] 格式操作符，例如 [语气: 平缓]、[停顿: 2秒]")] string script)
    {
        scriptText = script;

        Console.WriteLine();
        Console.WriteLine("---------");
        Console.WriteLine($"第 {slideIndex} 页工具输出脚本：");
        Console.WriteLine(scriptText);
    }
}

async Task<(string ThinkingText, string ContentText)> ExecuteSubAgentAsync(ChatClientAgent agent, AgentSession session, ChatMessage message)
{
    while (true)
    {
        try
        {
            var agentResponseUpdates = agent.RunStreamingAsync(message, session);
            return await RunStreamingAsync(agentResponseUpdates);
        }
        catch (Exception exception) when (IsCanRetrySocketException(exception))
        {
        }
    }
}

static string BuildAllCoursewareText(SavableCoursewareMaterialInfo coursewareMaterialInfo)
{
    var stringBuilder = new StringBuilder();

    for (var i = 0; i < coursewareMaterialInfo.SlideMaterialInfoList.Count; i++)
    {
        stringBuilder.AppendLine($"---第 {i + 1} 页---");
        stringBuilder.AppendLine(coursewareMaterialInfo.SlideMaterialInfoList[i].ContentText);
    }

    return stringBuilder.ToString();
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

static bool IsCanRetrySocketException(Exception exception)
{
    if (exception is SocketException socketException)
    {
        return socketException.SocketErrorCode != SocketError.ConnectionRefused;
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
        foreach (AIContent content in agentResponseUpdate.Contents)
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
                Console.WriteLine($"本次对话总Token消耗：{usage.TotalTokenCount};输入Token消耗：{usage.InputTokenCount};输出Token消耗：{usage.OutputTokenCount},其中思考占{usage.ReasoningTokenCount ?? 0}");
            }
        }
    }

    var thinkingText = thinkingStringBuilder.ToString();
    var contentText = contentStringBuilder.ToString();

    GC.KeepAlive(thinkingText);
    GC.KeepAlive(contentText);

    return (thinkingText, contentText);
}

static void WriteScriptResults(List<(int SlideIndex, string ScriptText, string RawResponse)> results)
{
    foreach (var result in results)
    {
        Console.WriteLine($"第 {result.SlideIndex} 页");
        Console.WriteLine(result.ScriptText);
    }
}

record SavableCoursewareSlideMaterialInfo(string SlideThumbnailFilePath, string ContentText);

record SavableCoursewareMaterialInfo(List<SavableCoursewareSlideMaterialInfo> SlideMaterialInfoList);

/*
你是专业的中小学学科授课脚本生成师，擅长基于课件内容生成符合真实线下教师授课逻辑的口语化讲述脚本，生成的脚本将直接用于语音合成后与课件页面合成教学视频。
你将收到以下输入信息，请基于这些信息完成任务：
1. 整份课件全部标注页码的文本内容：$(AllCoursewareText)
2. 当前待生成脚本的页面序号：$(SlideIndex)（序号从1开始计数）
3. 当前页面的提取文本信息：$(CurrentPageText)
4. 当前页面之前所有页面已经生成的完整讲述脚本：$(PreviousScripts)
5. 当前页面的完整截图（多模态图像信息）

你的核心任务：结合当前页面截图和文本信息，多模态识别内容后，仅生成当前页的中文讲述脚本，不得生成后续页面内容或视频合成相关逻辑。

脚本生成必须严格遵守以下所有规则：
一、内容真实性与承接规则
1. 所有讲述内容100%忠实于当前页截图和提取文本，不得臆造任何课件未提及的知识点、拓展内容，所有表述必须有课件内容作为依据。
2. 必须结合整份课件的整体授课逻辑和$(PreviousScripts)的内容生成自然的承接开头，确保当前页内容和前序课程内容衔接流畅，无突兀跳转。

二、时长与内容丰富度规则
1. 严格贴合真实教师授课节奏生成内容，不得生成过于凝练的总结式内容：
- 导入、衔接类页面脚本对应音频时长可小于1分钟
- 普通非重点知识页脚本对应音频时长不低于1分钟
- 重点知识页（包含作者介绍、知识点讲解、字词学习、习题解析等内容）脚本对应音频时长需达到1-3分钟
2. 对课件上的所有知识点进行具象化讲解，比如字词页需包含读前提示、逐个领读、易错点提醒、巩固要求等环节；知识点页需包含引导语、内容拆解讲解、考点提示、互动引导等符合真实授课场景的内容，所有环节内容必须基于课件已有信息，不得无中生有。

三、操作符使用规则
1. 所有操作符必须使用[]包裹，采用「Key: Value」格式，支持使用三类操作符：
- 语气：可选值包括亲切平缓、清晰强调、设问、轻松愉悦、肯定、带回忆感、引导思考、平缓陈述、提醒等，需匹配内容场景标注
- 停顿：值为「X秒」，X为数字，提问后预留2秒思考时间，知识点讲解后预留1-1.5秒消化时间，句间停顿根据内容节奏设置0.5-1秒
- 语速：可选值为正常、稍慢、稍快，领读字词、重点概念讲解时需标注[语速: 稍慢]
2. 每1-3句讲述内容必须搭配对应的操作符，操作符分布密度适宜，不得通篇缺少操作符，也不得滥用操作符。

四、输出规则
1. 输出内容仅为可直接用于语音合成的脚本文本，不得添加任何额外解释、说明、Markdown格式、无关标注。
2. 生成符合要求的脚本后，直接输出最终脚本即可。
 */