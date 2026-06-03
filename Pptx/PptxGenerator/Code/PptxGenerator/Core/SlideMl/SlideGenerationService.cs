using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PptxGenerator;

public sealed class SlideGenerationService
{
    private const int MaxAttempts = 4;

    private readonly ChatClientCreator _chatClientCreator;
    private readonly SlideRenderer _slideRenderer;

    public SlideGenerationService(ChatClientCreator chatClientCreator, SlideRenderer slideRenderer)
    {
        _chatClientCreator = chatClientCreator ?? throw new ArgumentNullException(nameof(chatClientCreator));
        _slideRenderer = slideRenderer ?? throw new ArgumentNullException(nameof(slideRenderer));
    }

    public async Task<SlideGenerationSessionResult> GenerateAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            throw new ArgumentException("提示词不能为空。", nameof(userPrompt));
        }

        var chatClient = _chatClientCreator.GetChatClient();
        var history = new List<ChatMessage>
        {
            new(ChatRole.System, BuildSystemPrompt()),
            new(ChatRole.User, BuildInitialUserPrompt(userPrompt)),
        };

        var conversationLog = new List<string>
        {
            $"[System]\n{BuildSystemPrompt()}",
            $"[User]\n{BuildInitialUserPrompt(userPrompt)}",
        };

        var iterations = new List<SlideGenerationIteration>();
        string? finalXml = null;
        SlideRenderResult? finalRenderResult = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await chatClient.GetResponseAsync(history, cancellationToken: cancellationToken).ConfigureAwait(false);
            var responseText = response.Text ?? response.ToString() ?? string.Empty;
            history.AddMessages(response);
            conversationLog.Add($"[Assistant]\n{responseText}");

            var slideXml = SlideXmlUtilities.ExtractXml(responseText);
            var renderResult = await _slideRenderer.RenderAsync(slideXml, cancellationToken).ConfigureAwait(false);

            iterations.Add(new SlideGenerationIteration
            {
                Attempt = attempt,
                RequestPrompt = history.LastOrDefault(t => t.Role == ChatRole.User)?.Text ?? string.Empty,
                ModelResponse = responseText,
                SlideXml = slideXml,
                RenderResult = renderResult,
            });

            finalXml = slideXml;
            finalRenderResult = renderResult;

            if (renderResult.Warnings.Count == 0 || attempt == MaxAttempts)
            {
                break;
            }

            var feedbackPrompt = BuildFeedbackPrompt(renderResult);
            history.Add(new ChatMessage(ChatRole.User, feedbackPrompt));
            conversationLog.Add($"[User]\n{feedbackPrompt}");
        }

        return new SlideGenerationSessionResult
        {
            UserPrompt = userPrompt,
            FinalSlideXml = finalXml ?? string.Empty,
            FinalRenderResult = finalRenderResult ?? throw new InvalidOperationException("未生成渲染结果。"),
            Iterations = iterations,
            ConversationMessages = conversationLog,
        };
    }

    public async Task<SlideGenerationSessionResult> ContinueConversationAsync(string originalPrompt, IReadOnlyList<string> priorMessages, string currentSlideXml, string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalPrompt))
        {
            throw new ArgumentException("原始提示不能为空。", nameof(originalPrompt));
        }

        if (string.IsNullOrWhiteSpace(currentSlideXml))
        {
            throw new ArgumentException("当前 Slide XML 不能为空。", nameof(currentSlideXml));
        }

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("对话内容不能为空。", nameof(userMessage));
        }

        var chatClient = _chatClientCreator.GetChatClient();
        var history = new List<ChatMessage>
        {
            new(ChatRole.System, BuildSystemPrompt()),
            new(ChatRole.User, BuildContinuationPrompt(originalPrompt, currentSlideXml, userMessage)),
        };

        var conversationLog = new List<string>();
        if (priorMessages is not null)
        {
            conversationLog.AddRange(priorMessages);
        }

        conversationLog.Add($"[User]\n{BuildContinuationPrompt(originalPrompt, currentSlideXml, userMessage)}");

        var iterations = new List<SlideGenerationIteration>();
        string? finalXml = null;
        SlideRenderResult? finalRenderResult = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await chatClient.GetResponseAsync(history, cancellationToken: cancellationToken).ConfigureAwait(false);
            var responseText = response.Text ?? response.ToString() ?? string.Empty;
            history.AddMessages(response);
            conversationLog.Add($"[Assistant]\n{responseText}");

            var slideXml = SlideXmlUtilities.ExtractXml(responseText);
            var renderResult = await _slideRenderer.RenderAsync(slideXml, cancellationToken).ConfigureAwait(false);

            iterations.Add(new SlideGenerationIteration
            {
                Attempt = attempt,
                RequestPrompt = history.LastOrDefault(t => t.Role == ChatRole.User)?.Text ?? string.Empty,
                ModelResponse = responseText,
                SlideXml = slideXml,
                RenderResult = renderResult,
            });

            finalXml = slideXml;
            finalRenderResult = renderResult;

            if (renderResult.Warnings.Count == 0 || attempt == MaxAttempts)
            {
                break;
            }

            var feedbackPrompt = BuildFeedbackPrompt(renderResult);
            history.Add(new ChatMessage(ChatRole.User, feedbackPrompt));
            conversationLog.Add($"[User]\n{feedbackPrompt}");
        }

        return new SlideGenerationSessionResult
        {
            UserPrompt = originalPrompt,
            FinalSlideXml = finalXml ?? currentSlideXml,
            FinalRenderResult = finalRenderResult ?? throw new InvalidOperationException("未生成渲染结果。"),
            Iterations = iterations,
            ConversationMessages = conversationLog,
        };
    }

    private static string BuildSystemPrompt()
    {
        return """
你是一个专业的幻灯片排版引擎。你的任务是根据用户的需求，生成一份 SlideML 格式的 XML 文档。

## SlideML 基本规则
- 画布尺寸固定为 1280×720 像素，坐标原点在左上角
- 所有尺寸单位为 px（不写单位），颜色格式为 #RRGGBB 或 #AARRGGBB
- 标签必须严格遵守定义，不要创造新标签或新属性
- 元素 Id 可以不写，引擎会自动分配

## 标签与属性
### Page
属性: Background（背景色，可选，默认 #FFFFFF）
### Panel
属性: X, Y, Width, Height（均可选）, Padding（可选，默认 0）, Background（可选）
### Rect
属性: X, Y, Width, Height（均可选）, Fill, Stroke, StrokeThickness, CornerRadius, HorizontalAlignment（Left/Center/Right）, VerticalAlignment（Top/Center/Bottom）, Opacity（0.0~1.0）
### TextElement
属性: X, Y, Width, Height（均可选）, Text（必填）, FontName（默认 Microsoft YaHei）, FontSize（默认 16）, Foreground（默认 #000000）, TextAlignment（Left/Center/Right/Justify，默认 Left）, LineHeight（默认 1.2）, HorizontalAlignment, VerticalAlignment, Opacity
### Image
属性: X, Y, Width, Height（均可选）, Source（必填，图片资源ID）, Stretch（None/Fill/Uniform/UniformToFill，默认 Uniform）, HorizontalAlignment, VerticalAlignment, Opacity

## 排版规则
1. 所有子元素相对于直接父容器定位
2. Z 序按文档出现顺序，后出现的在上层
3. 文本设置 Width 后会自动换行，不设置则单行
4. Panel 不设置 Width/Height 时自动包裹子元素
5. 子元素超出父容器的部分会被裁剪

## 禁止事项
- 不要写 ActualWidth、ActualHeight、ActualLineCount 属性
- 不要创造未定义的标签或属性
- 不要使用 XAML、CSS、HTML 等其他语法

## 输出格式
- 直接输出 XML，不要使用 markdown 代码块包裹
- 第一行必须是 <?xml version="1.0" encoding="UTF-8"?>
- 根元素必须是 <Page>
- 只输出最终 XML，不要追加解释

## 实验目标
- 当前只需要生成单页
- 优先让版面完整、层级清晰、留白充足
- 如果收到渲染警告和回填后的 XML，请根据反馈修改并重新输出完整 XML
""";
    }

    private static string BuildInitialUserPrompt(string userPrompt)
    {
        return $"""
请根据以下需求生成单页 SlideML：

{userPrompt}

要求：
1. 尽量使用浅色主题，视觉清爽
2. 标题、副标题、正文层级明显
3. 页面内容要适合 1280x720
4. 如果需要图片，可以使用占位资源 ID，如 image_001
5. 只输出 XML
""";
    }

    private static string BuildContinuationPrompt(string originalPrompt, string currentSlideXml, string userMessage)
    {
        return $"""
这是一个正在迭代中的 SlideML 单页实验。

原始需求：
{originalPrompt}

当前版本 XML：
{currentSlideXml}

用户新的修改意见：
{userMessage}

请综合原始需求和新的修改意见，输出一份完整的、可直接渲染的新版 SlideML XML。只输出 XML。
""";
    }

    private static string BuildFeedbackPrompt(SlideRenderResult renderResult)
    {
        var builder = new StringBuilder();
        builder.AppendLine("上一版 SlideML 已完成渲染，请根据反馈继续修正，输出完整 XML。");
        builder.AppendLine();
        builder.AppendLine("回填后的 XML：");
        builder.AppendLine(renderResult.OutputXml);
        builder.AppendLine();
        builder.AppendLine("Warning 列表：");
        if (renderResult.Warnings.Count == 0)
        {
            builder.AppendLine("[Warning] none");
        }
        else
        {
            foreach (var warning in renderResult.Warnings)
            {
                builder.AppendLine(warning);
            }
        }

        builder.AppendLine();
        builder.AppendLine("请修正这些问题，避免文本溢出、元素越界、图片缺失等问题。只输出新的 XML。");
        return builder.ToString();
    }
}
