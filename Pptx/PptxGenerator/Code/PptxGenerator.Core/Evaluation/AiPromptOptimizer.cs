using System.Text.Json;
using System.Text.Json.Serialization;
using AgentLib;
using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace PptxGenerator.Evaluation;

/// <summary>
/// AI 驱动的提示词优化器，根据评估反馈生成改进后的提示词。
/// 复用 <see cref="CopilotChatManager"/> 发送独立请求（<c>WithHistory = false</c>）。
/// </summary>
public sealed class AiPromptOptimizer : IPromptOptimizer
{
    private readonly CopilotChatManager _copilotChatManager;

    public AiPromptOptimizer(CopilotChatManager copilotChatManager)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
    }

    /// <inheritdoc />
    public async Task<PromptOptimizationResult> OptimizeAsync(
        SlideEvaluationResult evaluation,
        string systemPrompt,
        string userPromptTemplate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(userPromptTemplate);

        try
        {
            var contents = new List<AIContent>(1)
            {
                new TextContent(BuildOptimizationPrompt(evaluation, systemPrompt, userPromptTemplate)),
            };

            var request = new SendMessageRequest(contents)
            {
                WithHistory = false,
                SystemPrompt = BuildOptimizerSystemPrompt(),
                CancellationToken = cancellationToken,
                AppendDefaultTools = false,
            };

            var result = _copilotChatManager.SendMessage(request);
            await result.RunTask.ConfigureAwait(false);

            var responseText = result.AssistantChatMessage.Content;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return PromptOptimizationResult.Failed("优化器未返回有效响应。");
            }

            return ParseOptimizationResponse(responseText);
        }
        catch (OperationCanceledException)
        {
            return PromptOptimizationResult.Failed("优化被取消。");
        }
        catch (Exception ex)
        {
            return PromptOptimizationResult.Failed($"优化过程发生异常：{ex.Message}");
        }
    }

    private static string BuildOptimizerSystemPrompt()
    {
        return """
你是一个专业的 Prompt Engineering 优化专家。你的任务是根据评估反馈，对 SlideML 幻灯片生成的提示词进行优化。

## 优化原则
1. 保持原有提示词的核心结构和关键约束不变
2. 针对评估中低分维度进行重点改进
3. 增强指令的清晰度和可执行性
4. 消除冗余、矛盾或模糊的表述
5. 补充遗漏的关键规则或约束
6. 优化后的提示词应该比原版更有效、更精确

## 输出格式
你必须严格输出以下 JSON 格式，不要包含任何其他文本：

```json
{
  "systemPrompt": "优化后的完整系统提示词",
  "userPromptTemplate": "优化后的完整用户提示词模板，使用 {USER_INPUT} 作为用户输入占位符",
  "changeDescription": "简要描述本次优化的主要改动（1-3 句话）"
}
```

## 注意事项
- 输出的 systemPrompt 和 userPromptTemplate 必须是完整的、可直接使用的文本
- 不要省略任何必要的规则或约束
- 如果某个维度评分已经很高（≥8），保持该部分内容基本不变
- 变更说明要具体，指出改了什么以及为什么改
""";
    }

    private static string BuildOptimizationPrompt(
        SlideEvaluationResult evaluation,
        string systemPrompt,
        string userPromptTemplate)
    {
        var screenshotInfo = evaluation.HasOriginalScreenshot
            ? $"- 截图还原:   {evaluation.ScreenshotFidelity}/10"
            : "- 截图还原:   未评估（未提供原始截图）";

        return $"""
请根据以下评估反馈优化 SlideML 生成系统的提示词。

## 评估结果
- 综合评分:   {evaluation.OverallScore:F1}/10
- XML 规范:   {evaluation.XmlWellFormedness}/10
- 布局结构:   {evaluation.LayoutStructure}/10
- 视觉平衡:   {evaluation.VisualBalance}/10
- 约束遵守:   {evaluation.ConstraintAdherence}/10
- 语义对齐:   {evaluation.SemanticAlignment}/10
- 美观度:     {evaluation.AestheticQuality}/10
{screenshotInfo}

## 改进建议
{string.Join("\n", evaluation.Suggestions.Select((s, i) => $"{i + 1}. {s}"))}

## 当前 System Prompt
```
{systemPrompt}
```

## 当前 User Prompt Template
```
{userPromptTemplate}
```

请按照系统提示词中的 JSON 格式输出优化后的提示词。
""";
    }

    private static PromptOptimizationResult ParseOptimizationResponse(string responseText)
    {
        try
        {
            var jsonText = ExtractJson(responseText);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = JsonSerializer.Deserialize<OptimizationDto>(jsonText, options);
            if (dto is null)
            {
                return PromptOptimizationResult.Failed("无法解析优化响应 JSON。");
            }

            if (string.IsNullOrWhiteSpace(dto.SystemPrompt) && string.IsNullOrWhiteSpace(dto.UserPromptTemplate))
            {
                return PromptOptimizationResult.Failed("优化器未返回有效的提示词内容。");
            }

            return new PromptOptimizationResult
            {
                OptimizedSystemPrompt = dto.SystemPrompt,
                OptimizedUserPromptTemplate = dto.UserPromptTemplate,
                ChangeDescription = dto.ChangeDescription,
                IsSuccess = true,
            };
        }
        catch (Exception ex)
        {
            return PromptOptimizationResult.Failed($"解析优化响应失败：{ex.Message}");
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

    private sealed class OptimizationDto
    {
        [JsonPropertyName("systemPrompt")]
        public string? SystemPrompt { get; init; }

        [JsonPropertyName("userPromptTemplate")]
        public string? UserPromptTemplate { get; init; }

        [JsonPropertyName("changeDescription")]
        public string? ChangeDescription { get; init; }
    }
}
