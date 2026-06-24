using System.Text.Json;
using System.Text.Json.Serialization;
using AgentLib;
using AgentLib.Model;
using Microsoft.Extensions.AI;
using PptxGenerator.Models;

namespace PptxGenerator.Evaluation;

/// <summary>
/// AI 驱动的 SlideML 评估器，使用独立的 AI 模型调用对生成的 SlideML 进行多维度质量评估。
/// </summary>
public sealed class AiSlideEvaluator : ISlideEvaluator
{
    private readonly CopilotChatManager _copilotChatManager;

    public AiSlideEvaluator(CopilotChatManager copilotChatManager)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
    }

    /// <inheritdoc />
    public async Task<SlideEvaluationResult> EvaluateAsync
    (
        string userPrompt,
        string slideXml,
        string renderedXml,
        string warnings,
        byte[]? previewImageBytes,
        IPreviewImage? originalScreenshot = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(userPrompt);
        ArgumentNullException.ThrowIfNull(slideXml);

        try
        {
            var hasOriginalScreenshot = originalScreenshot is not null;
            var contents = new List<AIContent>(3)
            {
                new TextContent(BuildEvaluationPrompt(userPrompt, slideXml, renderedXml, warnings, hasOriginalScreenshot)),
            };

            if (previewImageBytes is { Length: > 0 })
            {
                contents.Add(new DataContent(previewImageBytes, "image/png"));
            }

            if (originalScreenshot is not null)
            {
                using var memoryStream = new MemoryStream();
                originalScreenshot.Save(memoryStream);
                contents.Add(new DataContent(memoryStream.ToArray(), "image/png"));
            }

            var request = new SendMessageRequest(contents)
            {
                WithHistory = false,
                // 禁止切换 Session 保持在当前对话上下文中
                //CreateNewSession = true,
                SystemPrompt = BuildEvaluatorSystemPrompt(),
                CancellationToken = cancellationToken,

                // 禁用默认的工具
                AppendDefaultTools = false,
            };

            var result = _copilotChatManager.SendMessage(request);
            await result.RunTask.ConfigureAwait(false);

            var responseText = result.AssistantChatMessage.Content;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return SlideEvaluationResult.Failed("评估者未返回有效响应。");
            }

            return ParseEvaluationResponse(responseText, userPrompt, slideXml, hasOriginalScreenshot);
        }
        catch (OperationCanceledException)
        {
            return SlideEvaluationResult.Failed("评估被取消。");
        }
        catch (Exception ex)
        {
            return SlideEvaluationResult.Failed($"评估过程发生异常：{ex.Message}");
        }
    }

    private static string BuildEvaluatorSystemPrompt()
    {
        return """
你是一个中立的 SlideML 排版质量评估专家。你的任务是对 AI 生成的 SlideML 幻灯片进行客观、结构化的质量评估。

## 评估规则
- 你需要从以下 7 个维度分别打分（1-10 分，10 分为最优）：
  1. XmlWellFormedness：XML 语法正确性、标签合规性、属性完整性
  2. LayoutStructure：层级结构清晰度、Panel 嵌套合理性、流式布局（Horizontal/Vertical）使用是否恰当
  3. VisualBalance：元素分布均衡性、留白是否充足
  4. ConstraintAdherence：是否遵守 1280×720 画布、颜色格式等约束
  5. SemanticAlignment：内容是否与用户需求对齐
  6. AestheticQuality：配色、字体大小层级、整体美观度，含渐变/阴影/富文本等 V2 表现力的运用是否恰当
  7. ScreenshotFidelity：生成的 SlideML 渲染截图与原始 PPT 截图的还原匹配程度（仅在提供了原始截图时评估，否则给 5 分中性值）

- 每个维度给出 1-2 句简短的评分理由。
- 最后给出 3-5 条具体的改进建议。

## 输出格式
你必须严格输出以下 JSON 格式，不要包含任何其他文本：

```json
{
  "xmlWellFormedness": 8,
  "layoutStructure": 7,
  "visualBalance": 6,
  "constraintAdherence": 9,
  "semanticAlignment": 8,
  "aestheticQuality": 7,
  "screenshotFidelity": 8,
  "dimensionComments": {
    "xmlWellFormedness": "评分理由",
    "layoutStructure": "评分理由",
    "visualBalance": "评分理由",
    "constraintAdherence": "评分理由",
    "semanticAlignment": "评分理由",
    "aestheticQuality": "评分理由",
    "screenshotFidelity": "评分理由"
  },
  "suggestions": [
    "改进建议 1",
    "改进建议 2",
    "改进建议 3"
  ]
}
```

## 注意事项
- 评分要客观、有区分度，不要全部给相同分数。
- 如果提供了渲染截图，请结合视觉呈现进行判断。
- 如果提供了原始 PPT 截图（最后一张图片），请仔细对比两张截图：
  - 比较布局结构、元素位置、颜色、字体大小等方面的一致性
  - ScreenshotFidelity 评分应反映两张截图的整体相似度
- 如果 SlideML 有语法错误，XmlWellFormedness 应低于 5 分。
- 流式布局（Layout="Horizontal"/"Vertical"）应优先用于需要等间距排列的场景，如果模型用了流式布局但子元素仍手动指定排列轴坐标，属于使用不当。
- 渐变、阴影、Span 富文本等 V2 能力的使用应服务于内容表达，过度使用或在不恰当场景使用应扣分。

## SlideML V2 能力清单
评估时请识别以下 V2 标签/属性，并评估其使用质量：
- `Layout="Horizontal"/"Vertical"` + `Gap` + `Margin`：流式布局，子元素沿排列轴自动排列
- `<Fill><LinearGradient X1 Y1 X2 Y2><Stop Offset Color/></LinearGradient></Fill>`：渐变填充
- `<Stroke><LinearGradient>...</LinearGradient></Stroke>`：渐变描边
- `Shadow="OffsetX OffsetY Blur Color"` 属性或 `<Shadow OffsetX OffsetY Blur Color Opacity/>` 子元素：阴影
- `IsBold="True"` / `IsItalic="True"`：字体粗细和斜体
- `<Span Text FontSize Foreground IsBold IsItalic TextDecoration/>`：富文本片段
- `CornerRadius`：圆角半径
- `StrokeDashArray="4,2"`：虚线描边
""";
    }

    private static string BuildEvaluationPrompt(string userPrompt, string slideXml, string renderedXml, string warnings, bool hasOriginalScreenshot)
    {
        var screenshotNote = hasOriginalScreenshot
            ? "\n## 原始 PPT 截图\n（已作为第二张图片附加，请对比生成截图与原始截图的还原度）"
            : "";

        return $"""
请评估以下 SlideML 生成结果的质量。

## 用户原始需求
{userPrompt}

## 生成的 SlideML XML
```xml
{slideXml}
```

## 渲染回填后的 XML（含 ActualWidth/ActualHeight）
```xml
{renderedXml}
```

## 渲染警告
{warnings}
{screenshotNote}
请按照系统提示词中的 JSON 格式输出评估结果。
""";
    }

    private static SlideEvaluationResult ParseEvaluationResponse(string responseText, string userPrompt, string slideXml, bool hasOriginalScreenshot)
    {
        try
        {
            var jsonText = ExtractJson(responseText);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = JsonSerializer.Deserialize<SlideEvaluationDto>(jsonText, options);
            if (dto is null)
            {
                return SlideEvaluationResult.Failed("无法解析评估响应 JSON。");
            }

            return new SlideEvaluationResult
            {
                XmlWellFormedness = ClampScore(dto.XmlWellFormedness),
                LayoutStructure = ClampScore(dto.LayoutStructure),
                VisualBalance = ClampScore(dto.VisualBalance),
                ConstraintAdherence = ClampScore(dto.ConstraintAdherence),
                SemanticAlignment = ClampScore(dto.SemanticAlignment),
                AestheticQuality = ClampScore(dto.AestheticQuality),
                ScreenshotFidelity = ClampScore(dto.ScreenshotFidelity),
                HasOriginalScreenshot = hasOriginalScreenshot,
                Suggestions = dto.Suggestions ?? Array.Empty<string>(),
                DimensionComments = dto.DimensionComments,
                UserPrompt = userPrompt,
                SlideXml = slideXml,
                IsSuccess = true,
            };
        }
        catch (Exception ex)
        {
            return SlideEvaluationResult.Failed($"解析评估响应失败：{ex.Message}");
        }
    }

    private static string ExtractJson(string text)
    {
        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return text[jsonStart..(jsonEnd + 1)];
        }

        return text;
    }

    private static int ClampScore(int score) => Math.Clamp(score, 1, 10);

    internal sealed class SlideEvaluationDto
    {
        [JsonPropertyName("xmlWellFormedness")]
        public int XmlWellFormedness { get; init; }

        [JsonPropertyName("layoutStructure")]
        public int LayoutStructure { get; init; }

        [JsonPropertyName("visualBalance")]
        public int VisualBalance { get; init; }

        [JsonPropertyName("constraintAdherence")]
        public int ConstraintAdherence { get; init; }

        [JsonPropertyName("semanticAlignment")]
        public int SemanticAlignment { get; init; }

        [JsonPropertyName("aestheticQuality")]
        public int AestheticQuality { get; init; }

        [JsonPropertyName("screenshotFidelity")]
        public int ScreenshotFidelity { get; init; }

        [JsonPropertyName("dimensionComments")]
        public Dictionary<string, string>? DimensionComments { get; init; }

        [JsonPropertyName("suggestions")]
        public IReadOnlyList<string>? Suggestions { get; init; }
    }
}
