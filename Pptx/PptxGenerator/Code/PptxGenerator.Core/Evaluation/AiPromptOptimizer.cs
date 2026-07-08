using System.ComponentModel;
using AgentLib;
using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace PptxGenerator.Evaluation;

/// <summary>
/// AI 驱动的提示词优化器，根据评估反馈通过工具回调 + 超大循环迭代优化 system prompt 和 user prompt template。
/// 复用 <see cref="CopilotChatManager"/> 发送独立请求（<c>WithHistory = false</c>）。
/// </summary>
public sealed class AiPromptOptimizer : IPromptOptimizer
{
    private const int MaxRetries = 100;

    private const string SlideMlSpecification = """
        ## 基本约定
        - 画布尺寸：1280 × 720 像素
        - 坐标原点：左上角，X 轴向右，Y 轴向下
        - 所有数值单位默认为 px，不写单位
        - 颜色格式：#RRGGBB 或 #AARRGGBB（八位含透明度）
        - 标签名和属性名采用 PascalCase

        ## 标签定义

        ### Page — 根元素
        属性: Background（颜色，可选，默认 #FFFFFF）

        ### Panel — 容器
        属性:
        - Id（字符串，可选）、X, Y（数字，可选，默认 0,0）
        - Width, Height（数字，可选，不写则自动撑开包裹子元素）
        - Padding（数字，可选，默认 0）、Background（颜色，可选，默认透明）
        - Layout（枚举，可选）：Absolute（默认）/ Horizontal / Vertical
        - Gap（数字，可选，默认 0）：流式布局下子元素间距
        - HorizontalAlignment（Left/Center/Right）、VerticalAlignment（Top/Center/Bottom）
        - Opacity（0.0~1.0，默认 1.0）
        - Margin（字符串，可选，逗号分隔 1~4 个值）
        子元素：支持 <Fill> 定义渐变背景（优先于 Background 属性）
        流式布局规则：仅支持单向，排列轴方向上的 X/Y 被忽略；子元素超出时产生 Warning

        ### Rect — 矩形
        属性:
        - Id（字符串，可选）、X, Y（数字，可选）、Width, Height（数字，可选）
        - Fill（颜色，可选，默认透明）、Stroke（颜色，可选）、StrokeThickness（数字，可选，默认 0）
        - CornerRadius（数字，可选，默认 0）：四角统一圆角半径
        - StrokeDashArray（字符串，可选）：虚线描边，如 "4,2"
        - Shadow（字符串，可选）：阴影，格式 "OffsetX OffsetY Blur Color"，如 "0 4 12 #00000033"
        - HorizontalAlignment、VerticalAlignment、Opacity、Margin
        子元素：支持 <Fill>（渐变填充）、<Stroke>（渐变描边）、<Shadow>（精细阴影）

        ### TextElement — 文本
        属性:
        - Id（字符串，可选）、X, Y（数字，可选）、Width, Height（数字，可选）
        - Text（字符串，必填，若有 Span 子元素则可省略）
        - FontName（字符串，可选，默认 Microsoft YaHei）
        - FontSize（数字，可选，默认 16）
        - IsBold（布尔，可选，默认 False）、IsItalic（布尔，可选，默认 False）
        - Foreground（颜色，可选，默认 #000000）
        - TextAlignment（Left/Center/Right/Justify，默认 Left）
        - HorizontalAlignment、VerticalAlignment、Opacity、Margin
        子元素：<Span> 支持富文本混排，属性: Text（必填）、FontSize、FontName、Foreground、IsBold、IsItalic、TextDecoration（None/Underline）

        ### Image — 图片
        属性:
        - Id（字符串，可选）、X, Y（数字，可选）、Width, Height（数字，可选）
        - Source（字符串，必填）：图片资源 ID，非 URL
        - Stretch（None/Fill/Uniform/UniformToFill，默认 Uniform）
        - HorizontalAlignment、VerticalAlignment、Opacity、Margin

        ### 渐变定义
        LinearGradient：X1, Y1, X2, Y2 范围 0~1，表示相对元素尺寸的比例
        Stop：Offset（0~1）、Color（颜色字符串），至少需要两个 Stop

        ## 引擎回填属性（模型不应输出）
        - RenderSize：渲染后的实际尺寸，格式为 "宽x高"
        - RenderLocation：渲染后的实际布局位置，格式为 "XxY"
        - ActualLineCount：文本换行后的实际行数（仅 TextElement）

        ## 排版规则
        1. 默认绝对定位：子元素位置由 X、Y 决定，相对于直接父容器左上角
        2. 流式布局：Panel 设置 Layout="Horizontal"/"Vertical" 时自动排列
        3. Z 序按文档顺序：后出现的元素渲染在上层
        4. 阴影在 Opacity 之前绘制，不受元素自身 Opacity 影响
        5. 渐变填充优先于纯色填充
        6. Panel 自动尺寸：未指定 Width/Height 时自动扩展包裹子元素
        7. TextElement 自动换行：指定 Width 时在约束宽度内自动换行
        8. 裁剪：子元素超出父容器边界被裁剪

        ## 禁止事项
        - 不要写 RenderSize、RenderLocation、ActualLineCount 属性
        - 不要创造未定义的标签或属性
        - 不要使用 XAML、CSS、HTML 等其他语法

        ## 输出格式
        - 直接输出 XML，不要使用 markdown 代码块包裹
        - 第一行必须是 <?xml version="1.0" encoding="UTF-8"?>
        - 根元素必须是 <Page>
        """;

    private readonly CopilotChatManager _copilotChatManager;

    /// <summary>
    /// 初始化 <see cref="AiPromptOptimizer"/> 的新实例。
    /// </summary>
    /// <param name="copilotChatManager">聊天管理器，用于发送独立请求。</param>
    public AiPromptOptimizer(CopilotChatManager copilotChatManager)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
    }

    private string PromptOptimizationSystemPrompt => $$"""
        你是一个专业的 Prompt Engineering 优化专家。你的任务是根据 SlideML 评估反馈，
        对 SlideML 幻灯片生成的 system prompt 和 user prompt template 进行迭代优化。

        ## 你的任务
        你会收到：
        1. 当前轮次的 system prompt
        2. 当前轮次的 user prompt template
        3. 评估反馈（各维度评分 + 改进建议）

        你需要产出优化后的 system prompt 和 user prompt template。

        ## system prompt 优化方向
        - 当前 system prompt 可能只是罗列规范清单，缺乏指导性
        - 你可以加入：设计模式示例、配色方案建议、排版范例、常见错误提醒
        - 让生成 LLM 读完 system prompt 就知道「怎么做才能做出美观的 SlideML」
        - 但不能删除或篡改附录中 SlideML 规范的任何事实信息

        ## user prompt template 优化方向
        - 调整指令表述，增强清晰度和可执行性
        - 针对评估中低分维度增加约束
        - 优化结构，消除冗余或矛盾
        - 必须保留 {USER_INPUT} 作为用户输入占位符

        ## 提交方式
        你必须调用 submit_prompt_optimization 工具提交优化结果。
        系统会验证你的输出。验证失败时会告诉你具体问题，请修正后重新提交。

        ---

        ## 附录：SlideML 规范（权威参考，不可修改的事实）
        注：以下规范里的画布尺寸写了固定的 1280 × 720 像素，但事实上这是两个变量，
        需要替换为 $(SlideWidth) 和 $(SlideHeight) 字符串。
        以下是 SlideML 的完整规范。你改写的任何提示词必须遵守此规范，
        不得引入不存在的标签或属性：

        {{SlideMlSpecification}}
        """;

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

        string? optimizedSystem = null;
        string? optimizedTemplate = null;
        string? changeDescription = null;

        var submitTool = AIFunctionFactory.Create(
            ([Description("优化后的完整系统提示词")] string system,
             [Description("优化后的完整用户提示词模板，使用 {USER_INPUT} 作为用户输入占位符")] string template,
             [Description("本次优化的主要改动说明（1-3 句话）")] string description) =>
            {
                var errors = new List<string>();

                // 校验 1: {USER_INPUT} 必须存在
                if (!template.Contains("{USER_INPUT}", StringComparison.Ordinal))
                {
                    errors.Add("userPromptTemplate 必须包含 {USER_INPUT} 占位符");
                }

                // 校验 2: 5 个核心标签必须存在
                foreach (var tag in new[] { "<Page", "<Panel", "<Rect", "<TextElement", "<Image" })
                {
                    if (!system.Contains(tag, StringComparison.Ordinal))
                    {
                        errors.Add($"systemPrompt 中缺少 {tag} 标签的定义");
                    }
                }

                if (errors.Count > 0)
                {
                    return "验证失败：\n- " + string.Join("\n- ", errors)
                        + "\n\n请修正后重新调用 submit_prompt_optimization。";
                }

                optimizedSystem = system;
                optimizedTemplate = template;
                changeDescription = description;
                return "优化结果已通过验证并记录。";
            },
            name: "submit_prompt_optimization",
            description: "提交优化后的提示词。验证失败时会返回错误信息，请根据错误修正后重新调用。");

        try
        {
            for (var retry = 0; retry < MaxRetries; retry++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var contents = new List<AIContent>(1)
                {
                    new TextContent(retry == 0
                        ? BuildOptimizationRequest(evaluation, systemPrompt, userPromptTemplate)
                        : "请调用 submit_prompt_optimization 工具提交优化后的提示词。"),
                };

                var request = new SendMessageRequest(contents)
                {
                    WithHistory = false,
                    Tools = [submitTool],
                    SystemPrompt = PromptOptimizationSystemPrompt,
                    CancellationToken = cancellationToken,
                    AppendDefaultTools = false,
                };

                var result = _copilotChatManager.SendMessage(request);
                result.UserChatMessage.IsPresetInfo = true;
                result.AssistantChatMessage.IsPresetInfo = true;
                await result.RunTask.ConfigureAwait(false);

                if (optimizedSystem is not null && optimizedTemplate is not null)
                {
                    return new PromptOptimizationResult
                    {
                        OptimizedSystemPrompt = optimizedSystem,
                        OptimizedUserPromptTemplate = optimizedTemplate,
                        ChangeDescription = changeDescription,
                        IsSuccess = true,
                    };
                }
            }

            return PromptOptimizationResult.Failed(
                $"提示词优化失败：在 {MaxRetries} 次尝试后仍未通过校验。");
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

    /// <summary>
    /// 构建发送给提示词优化器的请求文本，包含评估反馈和当前提示词。
    /// </summary>
    private static string BuildOptimizationRequest(
        SlideEvaluationResult evaluation,
        string systemPrompt,
        string userPromptTemplate)
    {
        var suggestions = evaluation.Suggestions.Count > 0
            ? string.Join("\n", evaluation.Suggestions.Select((s, i) => $"{i + 1}. {s}"))
            : "（无改进建议）";

        var screenshotInfo = evaluation.HasOriginalScreenshot
            ? $"- 截图还原:   {evaluation.ScreenshotFidelity}/10"
            : "- 截图还原:   未评估（未提供原始截图）";

        var dimensionComments = evaluation.DimensionComments is { Count: > 0 } comments
            ? "\n\n## 维度评语\n" + string.Join("\n", comments.Select(kv => $"- {kv.Key}: {kv.Value}"))
            : "";

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
            {screenshotInfo}{dimensionComments}

            ## 改进建议
            {suggestions}

            ## 当前 System Prompt
            ```
            {systemPrompt}
            ```

            ## 当前 User Prompt Template
            ```
            {userPromptTemplate}
            ```

            请调用 submit_prompt_optimization 工具提交优化后的提示词。
            """;
    }
}
