using System.Text;
using AgentLib;
using AgentLib.Model;
using PptxGenerator.Evaluation;
using PptxGenerator.Models;
using PptxGenerator.Prompt;

namespace PptxGenerator.Pipeline;

/// <summary>
/// 提示词迭代优化编排器。
/// 自动循环：评估 → 优化提示词 → 重新生成 → 再评估，直到评分收敛或达到最大轮数。
/// </summary>
public sealed class PromptIterationPipeline
{
    private readonly SlideGenerationPipeline _generationPipeline;
    private readonly ISlideEvaluator _slideEvaluator;
    private readonly IPromptOptimizer _promptOptimizer;
    private readonly SlideMlPromptProvider _promptProvider;
    private readonly CopilotChatManager _copilotChatManager;

    public PromptIterationPipeline(
        SlideGenerationPipeline generationPipeline,
        ISlideEvaluator slideEvaluator,
        IPromptOptimizer promptOptimizer,
        SlideMlPromptProvider promptProvider,
        CopilotChatManager copilotChatManager)
    {
        _generationPipeline = generationPipeline ?? throw new ArgumentNullException(nameof(generationPipeline));
        _slideEvaluator = slideEvaluator ?? throw new ArgumentNullException(nameof(slideEvaluator));
        _promptOptimizer = promptOptimizer ?? throw new ArgumentNullException(nameof(promptOptimizer));
        _promptProvider = promptProvider ?? throw new ArgumentNullException(nameof(promptProvider));
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
    }

    /// <summary>
    /// 运行提示词迭代优化闭环。
    /// </summary>
    public async Task<IterationResult> RunIterationAsync(
        string userPrompt,
        IPreviewImage? originalScreenshot,
        IterationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userPrompt);

        options ??= new IterationOptions();
        var history = new List<IterationRound>(options.MaxRounds);
        double? lastScore = null;
        var convergenceCount = 0;

        // 迭代前先重置提示词
        _promptProvider.ResetToDefault();

        for (var round = 1; round <= options.MaxRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await AppendIterationMessageAsync($"🔄 第 {round} 轮迭代开始...").ConfigureAwait(false);

            // 1. 生成 SlideML（保持在当前会话中，确保迭代消息在 UI 中可见）
            // 跳过自动评估，迭代管道自行执行评估以避免双重评估
            await _generationPipeline.SendMessageAsync(
                    userPrompt,
                    isFirstMessage: true,
                    attachPreview: false,
                    createNewSession: false,
                    skipAutoEvaluation: true,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var renderTool = _generationPipeline.SlideMlRenderTool;

            if (string.IsNullOrWhiteSpace(renderTool.LatestSlideXml))
            {
                await AppendIterationMessageAsync($"⚠️ 第 {round} 轮未生成有效的 SlideML，终止迭代。").ConfigureAwait(false);
                break;
            }

            // 2. 评估 SlideML（含原始截图对比）
            byte[]? previewImageBytes = null;
            if (renderTool.LatestPreviewImage is { } previewImage)
            {
                using var memoryStream = new MemoryStream();
                previewImage.Save(memoryStream);
                previewImageBytes = memoryStream.ToArray();
            }

            var slideEvaluation = await _slideEvaluator.EvaluateAsync(
                    userPrompt,
                    renderTool.LatestSlideXml,
                    renderTool.LatestRenderedXml,
                    renderTool.LatestWarnings,
                    previewImageBytes,
                    originalScreenshot,
                    cancellationToken)
                .ConfigureAwait(false);

            // 3. 评估提示词
            var systemPrompt = _promptProvider.BuildSystemPrompt();
            var userPromptTemplate = _promptProvider.BuildInitialUserPrompt("{USER_INPUT}");

            // 4. 记录本轮结果
            var roundRecord = new IterationRound
            {
                Round = round,
                SystemPrompt = systemPrompt,
                UserPromptTemplate = userPromptTemplate,
                SlideEvaluation = slideEvaluation,
                Timestamp = DateTimeOffset.Now,
            };
            history.Add(roundRecord);

            // 5. 输出评估结果到聊天
            await AppendEvaluationToChatAsync(slideEvaluation, round).ConfigureAwait(false);

            // 6. 判断终止条件
            if (slideEvaluation.IsSuccess)
            {
                var currentScore = slideEvaluation.OverallScore;

                // 检查评分阈值
                if (currentScore >= options.ScoreThreshold)
                {
                    await AppendIterationMessageAsync($"✅ 综合评分 {currentScore:F1}/10 已达到阈值 {options.ScoreThreshold}，迭代收敛！").ConfigureAwait(false);
                    _generationPipeline.RaiseIterationProgress(roundRecord);
                    return BuildResult(history, isConverged: true, currentScore, round,
                        $"评分 {currentScore:F1} 已达到阈值 {options.ScoreThreshold}");
                }

                // 检查收敛
                if (lastScore.HasValue && currentScore <= lastScore.Value)
                {
                    convergenceCount++;
                    if (convergenceCount >= options.ConvergenceRounds)
                    {
                        await AppendIterationMessageAsync($"⏹ 连续 {options.ConvergenceRounds} 轮评分未提升（{currentScore:F1} ≤ {lastScore:F1}），迭代终止。").ConfigureAwait(false);
                        _generationPipeline.RaiseIterationProgress(roundRecord);
                        return BuildResult(history, isConverged: true, currentScore, round,
                            $"连续 {options.ConvergenceRounds} 轮评分未提升");
                    }
                }
                else
                {
                    convergenceCount = 0;
                }

                lastScore = currentScore;
            }

            // 7. 如果是最后一轮，不再优化
            if (round >= options.MaxRounds)
            {
                await AppendIterationMessageAsync($"⏹ 已达到最大迭代轮数 {options.MaxRounds}，迭代结束。").ConfigureAwait(false);
                _generationPipeline.RaiseIterationProgress(roundRecord);
                break;
            }

            // 8. 优化提示词
            var optimization = await _promptOptimizer.OptimizeAsync(
                    evaluation: slideEvaluation,
                    systemPrompt: systemPrompt,
                    userPromptTemplate: userPromptTemplate,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (optimization.IsSuccess)
            {
                _promptProvider.UpdatePrompts(optimization.OptimizedSystemPrompt, optimization.OptimizedUserPromptTemplate);

                // record 的 with 表达式创建新实例，需要替换 history 中的旧引用
                roundRecord = roundRecord with { Optimization = optimization };
                history[^1] = roundRecord;

                await AppendIterationMessageAsync($"📝 第 {round} 轮提示词优化完成：{optimization.ChangeDescription ?? "无变更说明"}").ConfigureAwait(false);
                await AppendIterationMessageAsync($"---\n优化后的 SystemPrompt:\n```\n{(_promptProvider.BuildSystemPrompt())}\n```").ConfigureAwait(false);
            }
            else
            {
                await AppendIterationMessageAsync($"⚠️ 第 {round} 轮提示词优化失败：{optimization.ErrorMessage}").ConfigureAwait(false);
            }

            // 9. 触发进度事件（优化完成后，携带完整数据）
            _generationPipeline.RaiseIterationProgress(roundRecord);
        }

        var finalScore = history.Count > 0 && history[^1].SlideEvaluation?.IsSuccess == true
            ? history[^1].SlideEvaluation!.OverallScore
            : 0;
        return BuildResult(history, isConverged: false, finalScore, history.Count,
            $"达到最大轮数 {options.MaxRounds}");
    }

    private static IterationResult BuildResult(
        List<IterationRound> history,
        bool isConverged,
        double finalScore,
        int totalRounds,
        string reason)
    {
        return new IterationResult
        {
            IterationHistory = history,
            IsConverged = isConverged,
            FinalScore = finalScore,
            TotalRounds = totalRounds,
            TerminateReason = reason,
        };
    }

    private async Task AppendIterationMessageAsync(string message)
    {
        var chatMessage = CopilotChatMessage.CreateUser(message);
        chatMessage.IsPresetInfo = true;
        await _copilotChatManager.AppendMessageAsync(chatMessage).ConfigureAwait(false);
    }

    private async Task AppendEvaluationToChatAsync(SlideEvaluationResult result, int round)
    {
        var builder = new StringBuilder(512);
        builder.AppendLine($"📊 第 {round} 轮评估 | 综合评分: {(result.IsSuccess ? result.OverallScore.ToString("F1") : "N/A")}/10");

        if (!result.IsSuccess)
        {
            builder.AppendLine($"评估失败：{result.ErrorMessage}");
        }
        else
        {
            builder.AppendLine($"  XML 规范: {result.XmlWellFormedness}/10");
            builder.AppendLine($"  布局结构: {result.LayoutStructure}/10");
            builder.AppendLine($"  视觉平衡: {result.VisualBalance}/10");
            builder.AppendLine($"  约束遵守: {result.ConstraintAdherence}/10");
            builder.AppendLine($"  语义对齐: {result.SemanticAlignment}/10");
            builder.AppendLine($"  美观度:   {result.AestheticQuality}/10");
            builder.AppendLine($"  截图还原: {result.ScreenshotFidelity}/10");

            if (result.Suggestions.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("改进建议:");
                foreach (var suggestion in result.Suggestions)
                {
                    builder.AppendLine($"  - {suggestion}");
                }
            }
        }

        var chatMessage = CopilotChatMessage.CreateUser(builder.ToString().TrimEnd());
        chatMessage.IsPresetInfo = true;
        await _copilotChatManager.AppendMessageAsync(chatMessage).ConfigureAwait(false);
    }
}
