using AgentLib;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PptxGenerator;

/// <summary>
/// AI 驱动的提示词质量评估器，对 SlideML 生成所用的提示词进行元评估。
/// </summary>
public sealed class AiPromptEvaluator : IPromptEvaluator
{
    private readonly CopilotChatManager _copilotChatManager;

    public AiPromptEvaluator(CopilotChatManager copilotChatManager)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
    }

    /// <inheritdoc />
    public async Task<PromptEvaluationResult> EvaluateAsync(
        string systemPrompt,
        string userPromptTemplate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(userPromptTemplate);

        try
        {
            var contents = new List<AIContent>(1)
            {
                new TextContent(BuildEvaluationPrompt(systemPrompt, userPromptTemplate)),
            };

            var request = new SendMessageRequest(contents)
            {
                WithHistory = false,
                CreateNewSession = true,
                SystemPrompt = BuildEvaluatorSystemPrompt(),
                CancellationToken = cancellationToken,
            };

            var result = _copilotChatManager.SendMessage(request);
            await result.RunTask.ConfigureAwait(false);

            var responseText = result.AssistantChatMessage.Content;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return PromptEvaluationResult.Failed("评估者未返回有效响应。");
            }

            return ParseEvaluationResponse(responseText, systemPrompt, userPromptTemplate);
        }
        catch (OperationCanceledException)
        {
            return PromptEvaluationResult.Failed("评估被取消。");
        }
        catch (Exception ex)
        {
            return PromptEvaluationResult.Failed($"评估过程发生异常：{ex.Message}");
        }
    }

    private static string BuildEvaluatorSystemPrompt()
    {
        return """
你是一个专业的 Prompt Engineering 评估专家。你的任务是对用于 AI 幻灯片生成的提示词进行客观的质量评估。

## 评估规则
- 你需要从以下 5 个维度分别打分（1-10 分，10 分为最优）：
  1. Clarity：指令清晰度、无歧义
  2. Completeness：是否覆盖所有必要规则
  3. ConstraintQuality：约束是否有效、无矛盾
  4. Actionability：是否可被模型准确执行
  5. Redundancy：是否存在冗余/重复内容（分数越高表示冗余越少）

- 每个维度给出 1-2 句简短的评分理由。
- 最后给出 3-5 条具体的优化建议，建议要具体可操作。

## 输出格式
你必须严格输出以下 JSON 格式，不要包含任何其他文本：

```json
{
  "clarity": 8,
  "completeness": 7,
  "constraintQuality": 8,
  "actionability": 9,
  "redundancy": 7,
  "dimensionComments": {
    "clarity": "评分理由",
    "completeness": "评分理由",
    "constraintQuality": "评分理由",
    "actionability": "评分理由",
    "redundancy": "评分理由"
  },
  "suggestions": [
    "优化建议 1",
    "优化建议 2",
    "优化建议 3"
  ]
}
```

## 注意事项
- 评分要客观、有区分度。
- 建议要具体，指出哪里可以改进以及如何改进。
- 关注提示词中是否存在矛盾、遗漏或过度约束。
""";
    }

    private static string BuildEvaluationPrompt(string systemPrompt, string userPromptTemplate)
    {
        return $"""
请评估以下 SlideML 生成系统的提示词质量。

## System Prompt（系统提示词）
```
{systemPrompt}
```

## User Prompt Template（用户提示词模板）
```
{userPromptTemplate}
```

请按照系统提示词中的 JSON 格式输出评估结果。
""";
    }

    private static PromptEvaluationResult ParseEvaluationResponse(string responseText, string systemPrompt, string userPromptTemplate)
    {
        try
        {
            var jsonText = ExtractJson(responseText);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = JsonSerializer.Deserialize<PromptEvaluationDto>(jsonText, options);
            if (dto is null)
            {
                return PromptEvaluationResult.Failed("无法解析评估响应 JSON。");
            }

            return new PromptEvaluationResult
            {
                Clarity = ClampScore(dto.Clarity),
                Completeness = ClampScore(dto.Completeness),
                ConstraintQuality = ClampScore(dto.ConstraintQuality),
                Actionability = ClampScore(dto.Actionability),
                Redundancy = ClampScore(dto.Redundancy),
                Suggestions = dto.Suggestions ?? Array.Empty<string>(),
                SystemPrompt = systemPrompt,
                UserPromptTemplate = userPromptTemplate,
                IsSuccess = true,
            };
        }
        catch (Exception ex)
        {
            return PromptEvaluationResult.Failed($"解析评估响应失败：{ex.Message}");
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

    internal sealed class PromptEvaluationDto
    {
        [JsonPropertyName("clarity")]
        public int Clarity { get; init; }

        [JsonPropertyName("completeness")]
        public int Completeness { get; init; }

        [JsonPropertyName("constraintQuality")]
        public int ConstraintQuality { get; init; }

        [JsonPropertyName("actionability")]
        public int Actionability { get; init; }

        [JsonPropertyName("redundancy")]
        public int Redundancy { get; init; }

        [JsonPropertyName("suggestions")]
        public IReadOnlyList<string>? Suggestions { get; init; }
    }
}