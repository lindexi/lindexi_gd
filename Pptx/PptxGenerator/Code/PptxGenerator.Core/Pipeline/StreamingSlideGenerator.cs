using System.Collections.Concurrent;
using System.Text;
using AgentLib;
using AgentLib.Model;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using PptxGenerator.Models;
using PptxGenerator.Prompt;
using PptxGenerator.Streaming;

namespace PptxGenerator.Pipeline;

/// <summary>
/// 流式 SlideML 生成器，负责逐片段接收 LLM 输出并实时渲染。
/// 当检测到 XML 解析异常或渲染异常时，立即取消当前流式输出，
/// 将错误信息组织为反馈文本重新发送给 agent，最多重试 <see cref="MaxRetries"/> 次。
/// </summary>
internal sealed class StreamingSlideGenerator
{
    /// <summary>
    /// 流式生成最大重试次数。
    /// </summary>
    private const int MaxRetries = 100;

    private readonly CopilotChatManager _copilotChatManager;
    private readonly ISlideMlPromptProvider _promptProvider;
    private readonly SlideMlRenderTool _renderTool;
    private readonly IMainThreadDispatcher _dispatcher;

    /// <summary>
    /// 初始化 <see cref="StreamingSlideGenerator"/> 的新实例。
    /// </summary>
    /// <param name="copilotChatManager">聊天管理器。</param>
    /// <param name="promptProvider">提示词提供者。</param>
    /// <param name="renderTool">渲染工具。</param>
    /// <param name="dispatcher">主线程调度器。</param>
    public StreamingSlideGenerator(
        CopilotChatManager copilotChatManager,
        ISlideMlPromptProvider promptProvider,
        SlideMlRenderTool renderTool,
        IMainThreadDispatcher dispatcher)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
        _promptProvider = promptProvider ?? throw new ArgumentNullException(nameof(promptProvider));
        _renderTool = renderTool ?? throw new ArgumentNullException(nameof(renderTool));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// 以流式方式生成 SlideML，逐片段接收 LLM 输出并实时渲染。
    /// 检测到异常时取消当前流式输出，将错误反馈重新发送给 agent，最多重试 <see cref="MaxRetries"/> 次。
    /// </summary>
    /// <param name="userMessage">用户自然语言需求。</param>
    /// <param name="isFirstMessage">是否为首次消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <param name="onPropertiesChanged">流式结束后在主线程调用的回调，用于刷新外部属性通知。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task GenerateAsync(
        string userMessage, bool isFirstMessage, CancellationToken cancellationToken,
        Action? onPropertiesChanged = null)
    {
        var currentMessage = isFirstMessage
            ? _promptProvider.BuildStreamingUserPrompt(userMessage)
            : userMessage;
        var includeSystemPrompt = isFirstMessage;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var (hasErrors, errorFeedback) = await RunStreamingLoopAsync(
                currentMessage, includeSystemPrompt, linkedCancellationTokenSource, cancellationToken);

            if (!hasErrors || attempt == MaxRetries)
            {
                break;
            }

            // 组织错误反馈消息，重新调用 agent
            currentMessage = errorFeedback;
            includeSystemPrompt = false;
        }

        await _dispatcher.InvokeAsync(() =>
        {
            onPropertiesChanged?.Invoke();
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// 执行单轮流式生成循环。在检测到 XML 解析异常或渲染异常时，
    /// 通过 <paramref name="errorCancellationTokenSource"/> 取消当前流式输出，并返回错误反馈信息。
    /// </summary>
    /// <param name="userMessage">本轮发送的用户消息文本。</param>
    /// <param name="includeSystemPrompt">是否包含系统提示词。</param>
    /// <param name="errorCancellationTokenSource">用于在检测到异常时取消流式输出的令牌源（已与外部令牌关联）。</param>
    /// <param name="externalCancellationToken">外部取消令牌。</param>
    /// <returns>是否检测到异常，以及错误反馈文本（无异常时为空字符串）。</returns>
    private async Task<(bool HasErrors, string ErrorFeedback)> RunStreamingLoopAsync(
        string userMessage, bool includeSystemPrompt,
        CancellationTokenSource errorCancellationTokenSource, CancellationToken externalCancellationToken)
    {
        var context = new SlideMlPipelineContext();
        var renderErrors = new ConcurrentQueue<string>();
        var streamingPipeline = new SlideStreamingPipeline(_promptProvider, _renderTool.RenderPipeline, _dispatcher);

        // 将流式渲染结果同步到 SlideMlRenderTool，使 Latest* 属性保持最新
        streamingPipeline.Rendered += renderResult =>
        {
            _renderTool.ApplyRenderResult(new SlideMlRenderResult
            {
                InputXml = renderResult.InputXml,
                OutputXml = renderResult.OutputXml,
                Warnings = renderResult.Warnings,
                Errors = renderResult.Errors,
                PreviewImage = renderResult.PreviewImage,
            });

            // 收集渲染阶段产生的错误
            if (renderResult.Errors is { Count: > 0 })
            {
                foreach (var error in renderResult.Errors)
                {
                    renderErrors.Enqueue(error);
                }

                // 渲染出错，取消当前流式输出
                errorCancellationTokenSource.Cancel();
            }
        };

        var manualContext = await _copilotChatManager
            .CreateManualSendMessageContextAsync(externalCancellationToken).ConfigureAwait(false);

        // 填充用户消息
        manualContext.UserChatMessage.AppendText(userMessage);

        var systemPrompt = includeSystemPrompt
            ? _promptProvider.BuildStreamingSystemPrompt()
            : null;

        // 追加消息到会话
        await manualContext.AppendMessagesToSessionAsync().ConfigureAwait(false);

        using var _ = manualContext.StartChatting();

        var agent = await manualContext.GetChatClientAgentAsync(externalCancellationToken).ConfigureAwait(false);
        var session = await manualContext.GetAgentSessionAsync(externalCancellationToken).ConfigureAwait(false);

        var messages = systemPrompt is not null
            ? new[] { new ChatMessage(ChatRole.System, systemPrompt), manualContext.UserChatMessage.ToChatMessage() }
            : new[] { manualContext.UserChatMessage.ToChatMessage() };

        var loopToken = errorCancellationTokenSource.Token;

        try
        {
            await foreach (var update in agent.RunStreamingAsync(
                messages, session, cancellationToken: loopToken).ConfigureAwait(false))
            {
                manualContext.AppendResponseUpdate(update);

                if (string.IsNullOrEmpty(update.Text))
                {
                    continue;
                }

                // 将增量文本喂给流式管道
                await streamingPipeline.ProcessIncrementalTextAsync(
                    update.Text, context, loopToken).ConfigureAwait(false);

                // 检查合并阶段是否产生了 XML 解析错误
                if (context.Errors.Count > 0)
                {
                    errorCancellationTokenSource.Cancel();
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (errorCancellationTokenSource.IsCancellationRequested && !externalCancellationToken.IsCancellationRequested)
        {
            // 由异常检测触发的取消，非外部取消，继续走错误反馈流程
        }

        // 收集所有错误信息
        var allErrors = new List<string>(context.Errors.Count + renderErrors.Count);
        allErrors.AddRange(context.Errors);
        while (renderErrors.TryDequeue(out var renderError))
        {
            allErrors.Add(renderError);
        }

        if (allErrors.Count == 0)
        {
            // 未检测到异常，执行流结束渲染
            await streamingPipeline.ProcessStreamEndAsync(context, externalCancellationToken).ConfigureAwait(false);

            // ProcessStreamEndAsync 可能也会产生错误（缓冲区残留等）
            if (context.Errors.Count > 0)
            {
                var feedback = BuildErrorFeedback(streamingPipeline.CurrentMergedXml, context.Errors);
                context.Reset();
                return (true, feedback);
            }

            return (false, string.Empty);
        }

        // 有异常，组织错误反馈
        var errorFeedback = BuildErrorFeedback(streamingPipeline.CurrentMergedXml, allErrors);
        context.Reset();
        return (true, errorFeedback);
    }

    /// <summary>
    /// 将错误信息组织为可发送给 agent 的反馈文本。
    /// </summary>
    /// <param name="mergedXml">当前已合并的 XML（可能不完整）。</param>
    /// <param name="errors">错误信息列表。</param>
    /// <returns>反馈文本。</returns>
    private static string BuildErrorFeedback(string mergedXml, IReadOnlyList<string> errors)
    {
        var sb = new StringBuilder(512 + errors.Count * 128);
        sb.AppendLine("上一轮生成的 SlideML 存在以下错误，请根据错误信息修正后重新生成完整的 SlideML：");
        sb.AppendLine();

        for (var i = 0; i < errors.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {errors[i]}");
        }

        if (!string.IsNullOrWhiteSpace(mergedXml))
        {
            sb.AppendLine();
            sb.AppendLine("上一轮已生成的（可能不完整的）SlideML 如下，供参考：");
            sb.AppendLine(mergedXml);
        }

        sb.AppendLine();
        sb.AppendLine("请修正上述错误，重新输出完整的 SlideML XML。");

        return sb.ToString();
    }
}
