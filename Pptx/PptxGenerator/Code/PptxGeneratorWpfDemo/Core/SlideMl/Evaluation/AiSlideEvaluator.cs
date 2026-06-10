using AgentLib;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PptxGenerator;

/// <summary>
/// AI 驱动的 SlideML 评估器，使用独立的 AI 模型调用对生成的 SlideML 进行多维度质量评估。
/// </summary>
public sealed class AiSlideEvaluator : ISlideEvaluator
{
    private readonly CopilotChatManager _copilotChatManager;

    /// <summary>
    /// 初始化 AI 驱动的 SlideML 评估器。
    /// </summary>
    /// <param name="copilotChatManager">用于评估的聊天管理器，可使用与主模型不同的模型。</param>
    public AiSlideEvaluator(CopilotChatManager copilotChatManager)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
    }

    /// <inheritdoc />
    public async Task<SlideEvaluationResult> EvaluateAsync(
        string userPrompt,
        string slideXml,
        string renderedXml,
        string warnings,
        byte[]? previewImageBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userPrompt);
        ArgumentNullException.ThrowIfNull(slideXml);

        try
        {
            var contents = new List<AIContent>(2)
            {
                new TextContent(BuildEvaluationPrompt(userPrompt, slideXml, renderedXml, warnings)),
            };

            if (previewImageBytes is { Length: > 0 })
            {
                contents.Add(new DataContent(previewImageBytes, "image/png"));
            }

            var request = new SendMessageRequest(contents)
            {
                WithHistory = false,
                CreateNewSession = true,
                SystemPrompt = BuildEvaluatorSystemPrompt(),
                CancellationToken = cancellationToken,
            };

            var result = _copilotChatManager.SendMessage(request);
            await result.RunTask;

            var responseText = result.AssistantChatMessage.Content;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return SlideEvaluationResult.Failed("评估者未返回有效响应。");
            }

            return ParseEvaluationResponse(responseText, userPrompt, slideXml);
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

    /// <summary>
    /// 构建评估者的系统提示词（裁判角色）。
    /// </summary>
    private static string BuildEvaluatorSystemPrompt()
    {
        return """
你是一个中立的 SlideML 排版质量评估专家。你的任务是对 AI 生成的 SlideML 幻灯片进行客观、结构化的质量评估。

## 评估规则
- 你需要从以下 6 个维度分别打分（1-10 分，10 分为最优）：
  1. XmlWellFormedness：XML 语法正确性、标签合规性、属性完整性
  2. LayoutStructure：层级结构清晰度、Panel 嵌套合理性
  3. VisualBalance：元素分布均衡性、留白是否充足
  4. ConstraintAdherence：是否遵守 1280×720 画布、颜色格式等约束
  5. SemanticAlignment：内容是否与用户需求对齐
  6. AestheticQuality：配色、字体大小层级、整体美观度

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
  "dimensionComments": {
    "xmlWellFormedness": "评分理由",
    "layoutStructure": "评分理由",
    "visualBalance": "评分理由",
    "constraintAdherence": "评分理由",
    "semanticAlignment": "评分理由",
    "aestheticQuality": "评分理由"
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
- 如果 SlideML 有语法错误，XmlWellFormedness 应低于 5 分。
""";
    }

    /// <summary>
    /// 构建评估请求的用户提示词。
    /// </summary>
    private static string BuildEvaluationPrompt(string userPrompt, string slideXml, string renderedXml, string warnings)
    {
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

请按照系统提示词中的 JSON 格式输出评估结果。
""";
    }

    /// <summary>
    /// 解析评估响应 JSON 为 <see cref="SlideEvaluationResult"/>。
    /// </summary>
    private static SlideEvaluationResult ParseEvaluationResponse(string responseText, string userPrompt, string slideXml)
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
                Suggestions = dto.Suggestions ?? Array.Empty<string>(),
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

    /// <summary>
    /// 从 AI 响应中提取 JSON 文本（处理可能的 markdown 代码块包裹）。
    /// </summary>
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

    /// <summary>
    /// 将评分限制在 1-10 范围内。
    /// </summary>
    private static int ClampScore(int score)
    {
        return Math.Clamp(score, 1, 10);
    }

    /// <summary>
    /// 评估响应 JSON 反序列化 DTO。
    /// </summary>
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

        [JsonPropertyName("suggestions")]
        public IReadOnlyList<string>? Suggestions { get; init; }
    }
}
