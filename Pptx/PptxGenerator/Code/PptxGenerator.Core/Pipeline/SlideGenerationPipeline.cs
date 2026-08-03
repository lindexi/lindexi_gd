using System.ComponentModel;
using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Model;
using Microsoft.Extensions.AI;
using PptxGenerator.Evaluation;
using PptxGenerator.Models;
using PptxGenerator.Prompt;

namespace PptxGenerator.Pipeline;

/// <summary>
/// SlideML 生成管道编排器，管理 生成 → 渲染 → 评估 的完整生命周期。
/// </summary>
public sealed class SlideGenerationPipeline : INotifyPropertyChanged
{
    private readonly CopilotChatManager _copilotChatManager;
    private readonly ISlideMlPromptProvider _promptProvider;
    private readonly ISlideEvaluator? _slideEvaluator;
    private readonly IPromptEvaluator? _promptEvaluator;
    private readonly IPromptOptimizer? _promptOptimizer;
    private readonly IMainThreadDispatcher _dispatcher;

    /// <summary>
    /// 流式生成状态，跨重试轮次和跨轮对话复用。
    /// 在首次消息或新建会话时重置。
    /// </summary>
    private SlideStreamingState? _streamingState;

    public SlideGenerationPipeline(
        CopilotChatManager copilotChatManager,
        ISlideMlPromptProvider promptProvider,
        SlideMlRenderTool slideMlRenderTool,
        ISlideEvaluator? slideEvaluator = null,
        IPromptEvaluator? promptEvaluator = null,
        IPromptOptimizer? promptOptimizer = null)
    {
        _copilotChatManager = copilotChatManager ?? throw new ArgumentNullException(nameof(copilotChatManager));
        _promptProvider = promptProvider ?? throw new ArgumentNullException(nameof(promptProvider));
        SlideMlRenderTool = slideMlRenderTool ?? throw new ArgumentNullException(nameof(slideMlRenderTool));
        _slideEvaluator = slideEvaluator;
        _promptEvaluator = promptEvaluator;
        _promptOptimizer = promptOptimizer;
        _dispatcher = slideMlRenderTool.Dispatcher;

        slideMlRenderTool.SlideRendered += OnSlideRendered;
    }

    /// <summary>
    /// 使用 <see cref="PipelineConfiguration"/> 配置对象创建管道。
    /// </summary>
    public SlideGenerationPipeline(
        CopilotChatManager copilotChatManager,
        ISlideMlPromptProvider promptProvider,
        SlideMlRenderTool slideMlRenderTool,
        PipelineConfiguration configuration)
        : this(copilotChatManager, promptProvider, slideMlRenderTool,
              slideEvaluator: configuration.SlideEvaluator,
              promptEvaluator: configuration.PromptEvaluator,
              promptOptimizer: configuration.PromptOptimizer)
    {
    }

    public SlideMlRenderTool SlideMlRenderTool { get; }

    public CopilotChatManager ChatManager => _copilotChatManager;

    public ISlideMlPromptProvider PromptProvider => _promptProvider;

    public IPreviewImage? PreviewImage => SlideMlRenderTool.LatestPreviewImage;

    public string CurrentSlideXml => SlideMlRenderTool.LatestSlideXml;

    public string RenderedXml => SlideMlRenderTool.LatestRenderedXml;

    public string WarningText => SlideMlRenderTool.LatestWarnings;

    private SlideEvaluationResult? _lastSlideEvaluation;
    public SlideEvaluationResult? LastSlideEvaluation
    {
        get => _lastSlideEvaluation;
        private set
        {
            _lastSlideEvaluation = value;
            OnPropertyChanged(nameof(LastSlideEvaluation));
        }
    }

    private PromptEvaluationResult? _lastPromptEvaluation;
    public PromptEvaluationResult? LastPromptEvaluation
    {
        get => _lastPromptEvaluation;
        private set
        {
            _lastPromptEvaluation = value;
            OnPropertyChanged(nameof(LastPromptEvaluation));
        }
    }

    private bool _isEvaluating;
    private int _evaluationCount;
    private long _automaticEvaluationGeneration;
    private readonly object _automaticEvaluationTaskSyncRoot = new();
    private Task _automaticEvaluationTask = Task.CompletedTask;
    private CancellationTokenSource? _automaticEvaluationCancellationTokenSource;
    public bool IsEvaluating
    {
        get => _isEvaluating;
        private set
        {
            _isEvaluating = value;
            OnPropertyChanged(nameof(IsEvaluating));
        }
    }

    public bool CanEvaluate => _slideEvaluator is not null;
    public bool CanEvaluatePrompt => _promptEvaluator is not null;

    public Task AutomaticEvaluationTask => Volatile.Read(ref _automaticEvaluationTask);

    public event EventHandler<SlideEvaluationResult>? EvaluationCompleted;
    public event EventHandler<PromptEvaluationResult>? PromptEvaluationCompleted;
    public event Action? SlideRendered;
    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task SendSlideRequestAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        await SendMessageAsync(userPrompt, isFirstMessage: true, attachPreview: false, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 从指定用户消息重新开始流式生成。
    /// </summary>
    /// <param name="targetMessage">作为重新生成起点的用户消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task RestartFromMessageAsync(CopilotChatMessage targetMessage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetMessage);

        var restartService = new SlideStreamingRestartService(this);

        await restartService.RestartFromMessageAsync(targetMessage, cancellationToken);
    }

    public async Task SendMessageAsync
    (
        string userMessage,
        bool isFirstMessage,
        bool attachPreview,
        IReadOnlyList<string>? attachedImageFiles = null,
        string? systemPrompt = null,
        bool createNewSession = false,
        bool skipAutoEvaluation = false,
        bool useStreaming = false,
        CancellationToken cancellationToken = default,
        IChatClient? chatClientOverride = null,
        IReadOnlyCollection<string>? requiredAttachedImageFiles = null
    )
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return;
        }

        if (useStreaming)
        {
            _ = await SendStreamingMessageCoreAsync(
                userMessage,
                isFirstMessage,
                createNewSession,
                attachPreview: attachPreview,
                attachedImageContents: null,
                attachedImageFiles: attachedImageFiles,
                requiredAttachedImageFiles: requiredAttachedImageFiles,
                chatClientOverride: chatClientOverride,
                cancellationToken: cancellationToken);
            return;
        }

        var tools = new[] { SlideMlRenderTool.CreateTool(), SlideMlRenderTool.CreatePreviewTool() };

        var processedText = isFirstMessage
            ? _promptProvider.BuildInitialUserPrompt(userMessage)
            : userMessage;

        if (isFirstMessage && systemPrompt is null)
        {
            // 仅首次且无系统提示词时，才使用默认系统提示词
            systemPrompt = _promptProvider.BuildSystemPrompt();
        }

        var initialCapacity = 1 + (attachedImageFiles?.Count ?? 0) + (attachPreview ? 1 : 0);
        var contents = new List<AIContent>(initialCapacity) { new TextContent(processedText) };
        var requiredFiles = requiredAttachedImageFiles?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (attachedImageFiles is { Count: > 0 })
        {
            var loadedFiles = requiredFiles is { Count: > 0 }
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : null;
            foreach (var imageFile in attachedImageFiles)
            {
                if (string.IsNullOrWhiteSpace(imageFile) || !File.Exists(imageFile))
                {
                    if (imageFile is not null && requiredFiles?.Contains(imageFile) == true)
                    {
                        throw new FileNotFoundException("必需的图片附件不存在。", imageFile);
                    }

                    continue;
                }

                var dataContent = await DataContent.LoadFromAsync(imageFile, cancellationToken: cancellationToken);
                contents.Add(dataContent);
                loadedFiles?.Add(imageFile);
            }

            if (requiredFiles is { Count: > 0 }
                && requiredFiles.Any(file => loadedFiles?.Contains(file) != true))
            {
                throw new FileNotFoundException("必需的图片附件未能加入请求。");
            }
        }
        else if (requiredFiles is { Count: > 0 })
        {
            throw new FileNotFoundException("必需的图片附件未能加入请求。");
        }

        if (attachPreview)
        {
            var previewDataContent = await SlideMlRenderTool.CreatePreviewDataContentAsync(cancellationToken);
            if (previewDataContent is not null)
            {
                contents.Add(previewDataContent);
            }
        }

        var request = new SendMessageRequest(contents)
        {
            WithHistory = true,
            CreateNewSession = createNewSession,
            Tools = tools,
            SystemPrompt = systemPrompt,
            CancellationToken = cancellationToken,

            // 禁用默认的工具，防止去尝试读取本地文件
            AppendDefaultTools = false,
        };

        var requestResult = _copilotChatManager.SendMessage(request);
        await requestResult.RunTask;

        bool doNotRender = string.IsNullOrEmpty(CurrentSlideXml);
        if (doNotRender)
        {
            var toolRequest = request with
            {
                Contents = [new TextContent("请调用 render_slide 工具进行渲染，根据渲染结果优化界面")],
                SystemPrompt = "**重要：生成 SlideML 后必须调用 render_slide 工具，不可跳过此步骤**",
            };
            await _copilotChatManager.SendMessage(toolRequest).RunTask;
        }

        if (!skipAutoEvaluation && _slideEvaluator is not null && !string.IsNullOrWhiteSpace(CurrentSlideXml))
        {
            var context = new PipelineContext { UserPrompt = userMessage };
            context.SnapshotFromRenderTool(SlideMlRenderTool);
            StartAutomaticEvaluation(context, userMessage, cancellationToken);
        }
    }

    /// <summary>
    /// 发送流式消息并返回本次生成的明确结果。
    /// </summary>
    /// <param name="userMessage">用户消息。</param>
    /// <param name="isFirstMessage">是否为首次消息。</param>
    /// <param name="attachedImageContents">发送前已预加载的图片内容。</param>
    /// <param name="createNewSession">是否创建新会话。</param>
    /// <param name="skipAutoEvaluation">是否跳过自动评估；流式模式当前不执行自动评估。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>本次流式生成的明确结果。</returns>
    public Task<SlideStreamingGenerationResult> SendStreamingMessageWithResultAsync(
        string userMessage,
        bool isFirstMessage,
        IReadOnlyList<DataContent>? attachedImageContents = null,
        bool createNewSession = false,
        bool skipAutoEvaluation = false,
        CancellationToken cancellationToken = default)
    {
        _ = skipAutoEvaluation;

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return Task.FromResult(new SlideStreamingGenerationResult
            {
                IsSuccess = false,
                AttemptCount = 0,
                AcceptedFragmentCount = 0,
                FinalAttemptAcceptedFragmentCount = 0,
                FinalSlideXml = string.Empty,
                ErrorMessage = "用户消息不能为空。",
            });
        }

        return SendStreamingMessageCoreAsync(
            userMessage,
            isFirstMessage,
            createNewSession,
            attachPreview: false,
            attachedImageContents,
            attachedImageFiles: null,
            requiredAttachedImageFiles: null,
            chatClientOverride: null,
            cancellationToken);
    }

    private async Task<SlideStreamingGenerationResult> SendStreamingMessageCoreAsync(
        string userMessage,
        bool isFirstMessage,
        bool createNewSession,
        bool attachPreview,
        IReadOnlyList<DataContent>? attachedImageContents,
        IReadOnlyList<string>? attachedImageFiles,
        IReadOnlyCollection<string>? requiredAttachedImageFiles,
        IChatClient? chatClientOverride,
        CancellationToken cancellationToken)
    {
        // 首次消息或新建会话时重置流式状态
        if (isFirstMessage || createNewSession)
        {
            _streamingState = null;
        }

        if (createNewSession)
        {
            _copilotChatManager.CreateNewSession();
        }

        _streamingState ??= new SlideStreamingState(
            _promptProvider, SlideMlRenderTool.RenderPipeline);

        var generator = new StreamingSlideGenerator(
            _copilotChatManager, _promptProvider, SlideMlRenderTool);

        return await generator.GenerateAsync(
            userMessage,
            isFirstMessage,
            _streamingState,
            cancellationToken,
            attachPreview,
            attachedImageContents,
            attachedImageFiles,
            requiredAttachedImageFiles,
            chatClientOverride);
    }

    internal async Task ResetStreamingRestartStateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _streamingState = null;
        await SlideMlRenderTool.ResetLatestResultAsync();
    }

    internal async Task ReplayStreamingAssistantTextAsync(string assistantText, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assistantText);

        _streamingState ??= new SlideStreamingState(
            _promptProvider, SlideMlRenderTool.RenderPipeline);

        _streamingState.Context.Reset();
        _streamingState.Pipeline.ResetExtractor();
        var pendingRenderResults = new Queue<SlideMlRenderResult>();

        void OnRendered(SlideMlRenderResult renderResult)
        {
            pendingRenderResults.Enqueue(renderResult);
        }

        _streamingState.Pipeline.Rendered += OnRendered;
        try
        {
            await _streamingState.Pipeline.ProcessIncrementalTextAsync(
                assistantText, _streamingState.Context, cancellationToken);
            await ApplyPendingRenderResultsAsync(pendingRenderResults);

            await _streamingState.Pipeline.ProcessStreamEndAsync(
                _streamingState.Context, cancellationToken);
            await ApplyPendingRenderResultsAsync(pendingRenderResults);
        }
        finally
        {
            _streamingState.Pipeline.Rendered -= OnRendered;
        }
    }

    private async Task ApplyPendingRenderResultsAsync(Queue<SlideMlRenderResult> pendingRenderResults)
    {
        while (pendingRenderResults.TryDequeue(out var renderResult))
        {
            await SlideMlRenderTool.ApplyRenderResultAsync(renderResult);
        }
    }

    public async Task<SlideEvaluationResult?> EvaluateAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        if (_slideEvaluator is null)
        {
            return null;
        }

        CancelIfActive(Volatile.Read(ref _automaticEvaluationCancellationTokenSource));
        var context = new PipelineContext { UserPrompt = userPrompt };
        context.SnapshotFromRenderTool(SlideMlRenderTool);

        if (string.IsNullOrWhiteSpace(context.SlideXml))
        {
            var result = SlideEvaluationResult.Failed("尚未生成 SlideML，无法评估。");
            await CommitSlideEvaluationAsync(result).ConfigureAwait(false);
            return result;
        }

        return await EvaluateContextAsync(context, userPrompt, cancellationToken);
    }

    public async Task<PromptEvaluationResult?> EvaluatePromptAsync(CancellationToken cancellationToken = default)
    {
        if (_promptEvaluator is null)
        {
            return null;
        }

        await BeginEvaluationAsync().ConfigureAwait(false);
        try
        {
            var systemPrompt = _promptProvider.BuildSystemPrompt();
            var userPromptTemplate = _promptProvider.BuildInitialUserPrompt("{USER_INPUT}");

            var result = await _promptEvaluator.EvaluateAsync(
                systemPrompt, userPromptTemplate, cancellationToken).ConfigureAwait(false);

            await AppendEvaluationMessageAsync(result).ConfigureAwait(false);

            await CommitPromptEvaluationAsync(result).ConfigureAwait(false);
            return result;
        }
        finally
        {
            await EndEvaluationAsync().ConfigureAwait(false);
        }
    }

    private async Task<SlideEvaluationResult> EvaluateContextAsync(
        PipelineContext context,
        string userPrompt,
        CancellationToken cancellationToken,
        long? automaticEvaluationGeneration = null)
    {
        await BeginEvaluationAsync().ConfigureAwait(false);
        try
        {
            byte[]? previewImageBytes = null;
            if (context.PreviewImage is { } image)
            {
                using var memoryStream = new MemoryStream();
                image.Save(memoryStream);
                previewImageBytes = memoryStream.ToArray();
            }

            var result = await _slideEvaluator!.EvaluateAsync(
                    userPrompt,
                    context.SlideXml ?? string.Empty,
                    context.RenderedXml ?? string.Empty,
                    context.Warnings ?? string.Empty,
                    previewImageBytes,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            context.SlideEvaluation = result;

            if (automaticEvaluationGeneration is null
                || IsCurrentAutomaticEvaluation(automaticEvaluationGeneration.Value))
            {
                await AppendEvaluationMessageAsync(result).ConfigureAwait(false);
            }

            await CommitSlideEvaluationAsync(result, automaticEvaluationGeneration).ConfigureAwait(false);
            return result;
        }
        finally
        {
            await EndEvaluationAsync().ConfigureAwait(false);
        }
    }

    private void StartAutomaticEvaluation(
        PipelineContext context,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var generation = Interlocked.Increment(ref _automaticEvaluationGeneration);
        var evaluationCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var previousCancellationTokenSource = Interlocked.Exchange(
            ref _automaticEvaluationCancellationTokenSource,
            evaluationCancellationTokenSource);
        CancelIfActive(previousCancellationTokenSource);

        var evaluationTask = RunAutomaticEvaluationAsync(
            context,
            userPrompt,
            generation,
            evaluationCancellationTokenSource);
        lock (_automaticEvaluationTaskSyncRoot)
        {
            Volatile.Write(
                ref _automaticEvaluationTask,
                AwaitAutomaticEvaluationsAsync(_automaticEvaluationTask, evaluationTask));
        }
    }

    private async Task RunAutomaticEvaluationAsync(
        PipelineContext context,
        string userPrompt,
        long generation,
        CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            await EvaluateContextAsync(
                context,
                userPrompt,
                cancellationTokenSource.Token,
                generation).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
        finally
        {
            Interlocked.CompareExchange(
                ref _automaticEvaluationCancellationTokenSource,
                null,
                cancellationTokenSource);
            cancellationTokenSource.Dispose();
        }
    }

    private static async Task AwaitAutomaticEvaluationsAsync(Task previousTask, Task currentTask)
    {
        await Task.WhenAll(previousTask, currentTask).ConfigureAwait(false);
    }

    private static void CancelIfActive(CancellationTokenSource? cancellationTokenSource)
    {
        if (cancellationTokenSource is null)
        {
            return;
        }

        try
        {
            cancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private bool IsCurrentAutomaticEvaluation(long generation) =>
        generation == Volatile.Read(ref _automaticEvaluationGeneration);

    private Task BeginEvaluationAsync()
    {
        if (Interlocked.Increment(ref _evaluationCount) != 1)
        {
            return Task.CompletedTask;
        }

        return _dispatcher.InvokeAsync(() =>
        {
            IsEvaluating = true;
            return Task.CompletedTask;
        });
    }

    private Task EndEvaluationAsync()
    {
        var count = Interlocked.Decrement(ref _evaluationCount);
        if (count < 0)
        {
            Interlocked.Exchange(ref _evaluationCount, 0);
            throw new InvalidOperationException("评估活动计数不能小于零。");
        }

        if (count != 0)
        {
            return Task.CompletedTask;
        }

        return _dispatcher.InvokeAsync(() =>
        {
            IsEvaluating = false;
            return Task.CompletedTask;
        });
    }

    private Task CommitSlideEvaluationAsync(
        SlideEvaluationResult result,
        long? automaticEvaluationGeneration = null)
    {
        return _dispatcher.InvokeAsync(() =>
        {
            if (automaticEvaluationGeneration is not null
                && !IsCurrentAutomaticEvaluation(automaticEvaluationGeneration.Value))
            {
                return Task.CompletedTask;
            }

            LastSlideEvaluation = result;
            EvaluationCompleted?.Invoke(this, result);
            return Task.CompletedTask;
        });
    }

    private Task CommitPromptEvaluationAsync(PromptEvaluationResult result)
    {
        return _dispatcher.InvokeAsync(() =>
        {
            LastPromptEvaluation = result;
            PromptEvaluationCompleted?.Invoke(this, result);
            return Task.CompletedTask;
        });
    }

    private void OnSlideRendered()
    {
        OnPropertyChanged(nameof(PreviewImage));
        OnPropertyChanged(nameof(CurrentSlideXml));
        OnPropertyChanged(nameof(RenderedXml));
        OnPropertyChanged(nameof(WarningText));
        SlideRendered?.Invoke();
    }

    /// <summary>
    /// 是否可以进行提示词迭代优化。
    /// </summary>
    public bool CanRunIteration => _slideEvaluator is not null && _promptOptimizer is not null;

    /// <summary>
    /// 单轮迭代进度事件。
    /// </summary>
    public event EventHandler<IterationRound>? IterationProgress;

    /// <summary>
    /// 供 <see cref="PromptIterationPipeline"/> 触发迭代进度事件。
    /// </summary>
    internal void RaiseIterationProgress(IterationRound round)
    {
        IterationProgress?.Invoke(this, round);
    }

    /// <summary>
    /// 迭代完成事件。
    /// </summary>
    public event EventHandler<IterationResult>? IterationCompleted;

    /// <summary>
    /// 运行提示词迭代优化闭环。
    /// </summary>
    /// <param name="userPrompt">用户原始自然语言需求。</param>
    /// <param name="originalScreenshot">原始 PPT 截图，用于还原度对比评估。</param>
    /// <param name="options">迭代选项，为 <see langword="null"/> 时使用默认值。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>迭代结果。</returns>
    public async Task<IterationResult?> RunPromptIterationAsync(
        string userPrompt,
        IPreviewImage? originalScreenshot,
        IterationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!CanRunIteration)
        {
            return null;
        }

        if (_promptProvider is not Prompt.SlideMlPromptProvider mutableProvider)
        {
            return null;
        }

        var iterationPipeline = new PromptIterationPipeline(
            this,
            _slideEvaluator!,
            _promptOptimizer!,
            mutableProvider,
            _copilotChatManager);

        var result = await iterationPipeline.RunIterationAsync(userPrompt, originalScreenshot, options, cancellationToken)
            .ConfigureAwait(false);

        IterationCompleted?.Invoke(this, result);
        return result;
    }

    private async Task AppendEvaluationMessageAsync(SlideEvaluationResult result)
    {
        var message = CopilotChatMessage.CreateUser(result.ToDisplayText());
        message.IsPresetInfo = true;
        await _copilotChatManager.AppendMessageAsync(message);
    }

    private async Task AppendEvaluationMessageAsync(PromptEvaluationResult result)
    {
        var message = CopilotChatMessage.CreateUser(result.ToDisplayText());
        message.IsPresetInfo = true;
        await _copilotChatManager.AppendMessageAsync(message);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
