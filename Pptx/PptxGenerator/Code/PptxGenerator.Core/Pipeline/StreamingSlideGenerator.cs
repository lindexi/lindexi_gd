using System.Collections.Concurrent;
using System.Text;

using AgentLib;
using AgentLib.Model;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using PptxGenerator.Models;
using PptxGenerator.Prompt;
using PptxGenerator.Streaming;

#pragma warning disable MAAI001

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

    /// <summary>
    /// 初始化 <see cref="StreamingSlideGenerator"/> 的新实例。
    /// </summary>
    /// <param name="copilotChatManager">聊天管理器。</param>
    /// <param name="promptProvider">提示词提供者。</param>
    /// <param name="renderTool">渲染工具。</param>
    public StreamingSlideGenerator(
        CopilotChatManager copilotChatManager,
        ISlideMlPromptProvider promptProvider,
        SlideMlRenderTool renderTool)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
        _promptProvider = promptProvider ?? throw new ArgumentNullException(nameof(promptProvider));
        _renderTool = renderTool ?? throw new ArgumentNullException(nameof(renderTool));
    }

    /// <summary>
    /// 以流式方式生成 SlideML，逐片段接收 LLM 输出并实时渲染。
    /// 检测到异常时取消当前流式输出，将错误反馈重新发送给 agent，最多重试 <see cref="MaxRetries"/> 次。
    /// 重试和跨轮对话时复用 <paramref name="streamingState"/> 中的合并器 DOM 树和 Id/StyleId 索引。
    /// </summary>
    /// <param name="userMessage">用户自然语言需求。</param>
    /// <param name="isFirstMessage">是否为首次消息。</param>
    /// <param name="streamingState">跨轮复用的流式生成状态，包含合并器和渲染上下文。</param>
    /// <param name="attachPreview">是否将当前预览图附带给 LLM。</param>
    /// <param name="attachedImageContents">附加到本次首次请求的预加载图片内容。</param>
    /// <param name="attachedImageFiles">附加到本次首轮请求的本地图片文件。</param>
    /// <param name="requiredAttachedImageFiles">必须成功加载的本地图片文件。</param>
    /// <param name="chatClientOverride">用于测试或专用调用的聊天客户端。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>本次流式生成的明确结果。</returns>
    public async Task<SlideStreamingGenerationResult> GenerateAsync(
        string userMessage, bool isFirstMessage, SlideStreamingState streamingState,
        CancellationToken cancellationToken,
        bool attachPreview = false,
        IReadOnlyList<DataContent>? attachedImageContents = null,
        IReadOnlyList<string>? attachedImageFiles = null,
        IReadOnlyCollection<string>? requiredAttachedImageFiles = null,
        IChatClient? chatClientOverride = null)
    {
        ArgumentNullException.ThrowIfNull(streamingState);

        var currentMessage = isFirstMessage
            ? _promptProvider.BuildStreamingUserPrompt(userMessage)
            : userMessage;
        var includeSystemPrompt = isFirstMessage;
        var acceptedFragmentCount = 0;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            // 每轮重试前清空诊断信息，但保留合并器 DOM 树和 Id/StyleId 索引
            streamingState.Context.Reset();
            // 清空片段提取器缓冲区（上一轮残留内容无意义），但保留合并器状态
            streamingState.Pipeline.ResetExtractor();

            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var attemptResult = await RunStreamingLoopAsync(
                currentMessage, includeSystemPrompt,
                linkedCancellationTokenSource, cancellationToken,
                streamingState.Pipeline, streamingState.Context,
                attachPreview && attempt == 0,
                attempt == 0 ? attachedImageContents : null,
                attempt == 0 ? attachedImageFiles : null,
                attempt == 0 ? requiredAttachedImageFiles : null,
                chatClientOverride);
            acceptedFragmentCount += attemptResult.AcceptedFragmentCount;

            var attemptCount = attempt + 1;
            var finalSlideXml = streamingState.Pipeline.CurrentMergedXml;
            if (!attemptResult.HasErrors)
            {
                if (attemptResult.AcceptedFragmentCount == 0)
                {
                    return CreateFailureResult(
                        attemptCount,
                        acceptedFragmentCount,
                        attemptResult.AcceptedFragmentCount,
                        "最终一次模型尝试没有成功合并任何新片段。");
                }

                if (string.IsNullOrWhiteSpace(finalSlideXml))
                {
                    return CreateFailureResult(
                        attemptCount,
                        acceptedFragmentCount,
                        attemptResult.AcceptedFragmentCount,
                        "最终一次模型尝试没有形成非空 SlideML 页面。");
                }

                return new SlideStreamingGenerationResult
                {
                    IsSuccess = true,
                    AttemptCount = attemptCount,
                    AcceptedFragmentCount = acceptedFragmentCount,
                    FinalAttemptAcceptedFragmentCount = attemptResult.AcceptedFragmentCount,
                    FinalSlideXml = finalSlideXml,
                    ErrorMessage = null,
                };
            }

            if (attempt == MaxRetries)
            {
                return CreateFailureResult(
                    attemptCount,
                    acceptedFragmentCount,
                    attemptResult.AcceptedFragmentCount,
                    $"流式生成在 {attemptCount} 次尝试后仍有错误：{attemptResult.ErrorMessage}");
            }

            // 组织错误反馈消息，重新调用 agent
            currentMessage = attemptResult.ErrorFeedback;
            includeSystemPrompt = false;
        }

        throw new InvalidOperationException("流式生成尝试循环意外结束。");
    }

    /// <summary>
    /// 执行单轮流式生成循环。在检测到 XML 解析异常或渲染异常时，
    /// 通过 <paramref name="errorCancellationTokenSource"/> 取消当前流式输出，并返回错误反馈信息。
    /// </summary>
    /// <param name="userMessage">本轮发送的用户消息文本。</param>
    /// <param name="includeSystemPrompt">是否包含系统提示词。</param>
    /// <param name="errorCancellationTokenSource">用于在检测到异常时取消流式输出的令牌源（已与外部令牌关联）。</param>
    /// <param name="externalCancellationToken">外部取消令牌。</param>
    /// <param name="streamingPipeline">流式渲染管道（跨轮复用，保留合并器状态）。</param>
    /// <param name="context">渲染上下文（跨轮复用，诊断信息已由调用方重置）。</param>
    /// <param name="attachPreview">是否将当前预览图附带给 LLM。</param>
    /// <param name="attachedImageContents">附加到本轮请求的预加载图片内容。</param>
    /// <param name="attachedImageFiles">附加到本轮请求的本地图片文件。</param>
    /// <param name="requiredAttachedImageFiles">必须成功加载的本地图片文件。</param>
    /// <param name="chatClientOverride">用于测试或专用调用的聊天客户端。</param>
    /// <returns>本轮尝试的错误状态、反馈文本和成功合并片段数。</returns>
    private async Task<StreamingAttemptResult> RunStreamingLoopAsync
    (
        string userMessage, bool includeSystemPrompt,
        CancellationTokenSource errorCancellationTokenSource, CancellationToken externalCancellationToken,
        SlideStreamingPipeline streamingPipeline, SlideMlPipelineContext context,
        bool attachPreview = false,
        IReadOnlyList<DataContent>? attachedImageContents = null,
        IReadOnlyList<string>? attachedImageFiles = null,
        IReadOnlyCollection<string>? requiredAttachedImageFiles = null,
        IChatClient? chatClientOverride = null
    )
    {
        var renderErrors = new ConcurrentQueue<string>();
        var pendingRenderResults = new Queue<SlideMlRenderResult>();
        var acceptedFragmentCount = 0;

        // 将流式渲染结果同步到 SlideMlRenderTool，使 Latest* 属性保持最新
        Action<SlideMlRenderResult> onRendered = renderResult =>
        {
            pendingRenderResults.Enqueue(renderResult);

            // 收集渲染阶段产生的错误（作为 context.Errors 的补充，如 ProcessStreamEndAsync 产生的错误）
            if (renderResult.Errors is { Count: > 0 })
            {
                foreach (var error in renderResult.Errors)
                {
                    renderErrors.Enqueue(error);
                }
            }
        };
        void OnFragmentReceived(string _) => acceptedFragmentCount++;

        streamingPipeline.Rendered += onRendered;
        streamingPipeline.FragmentReceived += OnFragmentReceived;

        try
        {
            var manualContext = await _copilotChatManager
                .CreateManualSendMessageContextAsync(externalCancellationToken);

            // 填充用户消息
            manualContext.UserChatMessage.AppendText(userMessage);

            var systemPrompt = includeSystemPrompt
                ? _promptProvider.BuildStreamingSystemPrompt()
                : null;

            ChatOptions chatOptions = CreateStreamingChatOptions();
            ChatClientAgent agent;
            if (chatClientOverride is null)
            {
                agent = await manualContext.GetChatClientAgentAsync(options => options.ChatOptions = chatOptions,
                    externalCancellationToken);
            }
            else
            {
                agent = chatClientOverride.AsAIAgent(new ChatClientAgentOptions
                {
                    ChatOptions = chatOptions,
                    ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()),
                    RequirePerServiceCallChatHistoryPersistence = true,
                });
            }

            AgentSession session = await manualContext.GetAgentSessionAsync(externalCancellationToken);

            ChatMessage userChatMessage = manualContext.UserChatMessage.ToChatMessage();
            await AppendAttachedImageContentsAsync(
                userChatMessage,
                attachedImageContents,
                attachedImageFiles,
                externalCancellationToken,
                requiredAttachedImageFiles);

            // 附带当前预览图供 LLM 参考（仅在用户勾选且存在预览图时）
            if (attachPreview)
            {
                var previewDataContent = await _renderTool.CreatePreviewDataContentAsync(externalCancellationToken);
                if (previewDataContent is not null)
                {
                    userChatMessage.Contents.Add(previewDataContent);
                }
            }

            var inputMessages = systemPrompt is not null
                ? new[] { new ChatMessage(ChatRole.System, systemPrompt), userChatMessage }
                : new[] { userChatMessage };

            await manualContext.AppendMessagesToSessionAsync();

            using IDisposable chattingScope = manualContext.StartChatting();

            var loopToken = errorCancellationTokenSource.Token;

            try
            {
                await foreach (AgentResponseUpdate update in agent.RunWithHistoryCompletionAsync(
                    inputMessages, session, cancellationToken: loopToken))
                {
                    manualContext.AppendResponseUpdate(update);

                    if (string.IsNullOrEmpty(update.Text))
                    {
                        continue;
                    }

                    // 将增量文本喂给流式管道
                    await streamingPipeline.ProcessIncrementalTextAsync(
                        update.Text, context, loopToken);
                    await ApplyPendingRenderResultsAsync(pendingRenderResults);

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

            var allErrors = CollectErrors(context, renderErrors);

            if (allErrors.Count == 0)
            {
                // 未检测到异常，执行流结束渲染
                await streamingPipeline.ProcessStreamEndAsync(context, externalCancellationToken);
                await ApplyPendingRenderResultsAsync(pendingRenderResults);

                // ProcessStreamEndAsync 及其最终渲染也可能产生错误
                allErrors = CollectErrors(context, renderErrors);
                if (allErrors.Count > 0)
                {
                    var feedback = BuildErrorFeedback(streamingPipeline.CurrentMergedXml, allErrors);
                    return new StreamingAttemptResult(
                        true,
                        feedback,
                        string.Join(Environment.NewLine, allErrors),
                        acceptedFragmentCount);
                }

                return new StreamingAttemptResult(false, string.Empty, null, acceptedFragmentCount);
            }

            // 有异常，组织错误反馈
            var errorFeedback = BuildErrorFeedback(streamingPipeline.CurrentMergedXml, allErrors);
            return new StreamingAttemptResult(
                true,
                errorFeedback,
                string.Join(Environment.NewLine, allErrors),
                acceptedFragmentCount);
        }
        finally
        {
            // 确保在任何路径下都取消订阅，避免跨轮重复累积
            streamingPipeline.Rendered -= onRendered;
            streamingPipeline.FragmentReceived -= OnFragmentReceived;
        }
    }

    private static List<string> CollectErrors(
        SlideMlPipelineContext context,
        ConcurrentQueue<string> renderErrors)
    {
        var allErrors = new List<string>(context.Errors.Count + renderErrors.Count);
        allErrors.AddRange(context.Errors);
        while (renderErrors.TryDequeue(out var renderError))
        {
            allErrors.Add(renderError);
        }

        return allErrors.Distinct().ToList();
    }

    private static SlideStreamingGenerationResult CreateFailureResult(
        int attemptCount,
        int acceptedFragmentCount,
        int finalAttemptAcceptedFragmentCount,
        string errorMessage)
    {
        return new SlideStreamingGenerationResult
        {
            IsSuccess = false,
            AttemptCount = attemptCount,
            AcceptedFragmentCount = acceptedFragmentCount,
            FinalAttemptAcceptedFragmentCount = finalAttemptAcceptedFragmentCount,
            FinalSlideXml = string.Empty,
            ErrorMessage = errorMessage,
        };
    }

    private async Task ApplyPendingRenderResultsAsync(Queue<SlideMlRenderResult> pendingRenderResults)
    {
        while (pendingRenderResults.TryDequeue(out var renderResult))
        {
            await _renderTool.ApplyRenderResultAsync(renderResult);
        }
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
        sb.AppendLine("上一轮生成的 SlideML 存在以下错误，请根据错误信息修正：");
        sb.AppendLine();

        for (var i = 0; i < errors.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {errors[i]}");
        }

        sb.AppendLine();
        sb.AppendLine("出错的片段已被自动丢弃，不会保留在合并结果中。以下方已合并的 SlideML 为基础，仅输出修正和后续片段。");

        if (!string.IsNullOrWhiteSpace(mergedXml))
        {
            sb.AppendLine();
            sb.AppendLine("当前已合并的（可能不完整的）SlideML 如下，供参考：");
            sb.AppendLine(mergedXml);
        }

        sb.AppendLine();
        sb.AppendLine("请修正上述错误，仅输出修正和后续片段。");
        sb.AppendLine("不要重复已成功的片段，合并器已保留之前成功合并的内容。");

        return sb.ToString();
    }

    private ChatOptions CreateStreamingChatOptions()
    {
        return new ChatOptions
        {
            // 流式模式使用查询工具而非渲染工具，渲染由管道自动完成
            Tools =
            [
                _renderTool.CreateSlideStateTool(),
                _renderTool.CreatePreviewTool()
            ],
        };
    }

    internal static async Task AppendAttachedImageContentsAsync(
        ChatMessage userChatMessage,
        IReadOnlyList<DataContent>? attachedImageContents,
        IReadOnlyList<string>? attachedImageFiles,
        CancellationToken cancellationToken,
        IReadOnlyCollection<string>? requiredAttachedImageFiles = null)
    {
        ArgumentNullException.ThrowIfNull(userChatMessage);
        cancellationToken.ThrowIfCancellationRequested();

        if (attachedImageContents is { Count: > 0 })
        {
            foreach (var imageContent in attachedImageContents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(imageContent);
                userChatMessage.Contents.Add(imageContent);
            }
        }

        var requiredFiles = requiredAttachedImageFiles?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (attachedImageFiles is not { Count: > 0 })
        {
            if (requiredFiles is { Count: > 0 })
            {
                throw new FileNotFoundException("必需的图片附件未能加入请求。");
            }

            return;
        }

        var loadedFiles = requiredFiles is { Count: > 0 }
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : null;
        foreach (var imageFile in attachedImageFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(imageFile) || !File.Exists(imageFile))
            {
                if (imageFile is not null && requiredFiles?.Contains(imageFile) == true)
                {
                    throw new FileNotFoundException("必需的图片附件不存在。", imageFile);
                }

                continue;
            }

            var imageContent = await DataContent.LoadFromAsync(
                imageFile,
                cancellationToken: cancellationToken);
            userChatMessage.Contents.Add(imageContent);
            loadedFiles?.Add(imageFile);
        }

        if (requiredFiles is { Count: > 0 }
            && requiredFiles.Any(file => loadedFiles?.Contains(file) != true))
        {
            throw new FileNotFoundException("必需的图片附件未能加入请求。");
        }
    }

    private sealed record StreamingAttemptResult(
        bool HasErrors,
        string ErrorFeedback,
        string? ErrorMessage,
        int AcceptedFragmentCount);

}
