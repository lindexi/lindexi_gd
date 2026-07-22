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

        await restartService.RestartFromMessageAsync(targetMessage, cancellationToken).ConfigureAwait(false);
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
            // 首次消息或新建会话时重置流式状态
            if (isFirstMessage || createNewSession)
            {
                _streamingState = null;
            }

            _streamingState ??= new SlideStreamingState(
                _promptProvider, SlideMlRenderTool.RenderPipeline, _dispatcher);

            var generator = new StreamingSlideGenerator(
                _copilotChatManager, _promptProvider, SlideMlRenderTool, _dispatcher);

            await generator.GenerateAsync(
                userMessage, isFirstMessage, _streamingState, cancellationToken,
                attachPreview: attachPreview,
                attachedImageFiles: attachedImageFiles,
                requiredAttachedImageFiles: requiredAttachedImageFiles,
                chatClientOverride: chatClientOverride).ConfigureAwait(false);

            _ = _dispatcher.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(PreviewImage));
                OnPropertyChanged(nameof(CurrentSlideXml));
                OnPropertyChanged(nameof(RenderedXml));
                OnPropertyChanged(nameof(WarningText));
                return Task.CompletedTask;
            });
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
        await requestResult.RunTask.ConfigureAwait(false);

        bool doNotRender = string.IsNullOrEmpty(CurrentSlideXml);
        if (doNotRender)
        {
            var toolRequest = request with
            {
                Contents = [new TextContent("请调用 render_slide 工具进行渲染，根据渲染结果优化界面")],
                SystemPrompt = "**重要：生成 SlideML 后必须调用 render_slide 工具，不可跳过此步骤**",
            };
            await _copilotChatManager.SendMessage(toolRequest).RunTask.ConfigureAwait(false);
        }

        _ = _copilotChatManager.MainThreadDispatcher!.InvokeAsync(() =>
        {
            OnPropertyChanged(nameof(PreviewImage));
            OnPropertyChanged(nameof(CurrentSlideXml));
            OnPropertyChanged(nameof(RenderedXml));
            OnPropertyChanged(nameof(WarningText));
            return Task.CompletedTask;
        });

        if (!skipAutoEvaluation && _slideEvaluator is not null && !string.IsNullOrWhiteSpace(CurrentSlideXml))
        {
            var context = new PipelineContext { UserPrompt = userMessage };
            context.SnapshotFromRenderTool(SlideMlRenderTool);
            _ = EvaluateContextAsync(context, userMessage, cancellationToken);
        }
    }

    internal async Task ResetStreamingRestartStateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _streamingState = null;
        await SlideMlRenderTool.ResetLatestResultAsync().ConfigureAwait(false);
    }

    internal async Task ReplayStreamingAssistantTextAsync(string assistantText, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assistantText);

        _streamingState ??= new SlideStreamingState(
            _promptProvider, SlideMlRenderTool.RenderPipeline, _dispatcher);

        _streamingState.Context.Reset();
        _streamingState.Pipeline.ResetExtractor();

        void OnRendered(SlideMlRenderResult renderResult)
        {
            SlideMlRenderTool.ApplyRenderResult(renderResult);
        }

        _streamingState.Pipeline.Rendered += OnRendered;
        try
        {
            await _streamingState.Pipeline.ProcessIncrementalTextAsync(
                assistantText, _streamingState.Context, cancellationToken).ConfigureAwait(false);

            await _streamingState.Pipeline.ProcessStreamEndAsync(
                _streamingState.Context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _streamingState.Pipeline.Rendered -= OnRendered;
        }

        await _dispatcher.InvokeAsync(() =>
        {
            OnPropertyChanged(nameof(PreviewImage));
            OnPropertyChanged(nameof(CurrentSlideXml));
            OnPropertyChanged(nameof(RenderedXml));
            OnPropertyChanged(nameof(WarningText));
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    public async Task<SlideEvaluationResult?> EvaluateAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        if (_slideEvaluator is null)
        {
            return null;
        }

        var context = new PipelineContext { UserPrompt = userPrompt };
        context.SnapshotFromRenderTool(SlideMlRenderTool);

        if (string.IsNullOrWhiteSpace(context.SlideXml))
        {
            var result = SlideEvaluationResult.Failed("尚未生成 SlideML，无法评估。");
            LastSlideEvaluation = result;
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

        IsEvaluating = true;
        try
        {
            var systemPrompt = _promptProvider.BuildSystemPrompt();
            var userPromptTemplate = _promptProvider.BuildInitialUserPrompt("{USER_INPUT}");

            var result = await _promptEvaluator.EvaluateAsync(systemPrompt, userPromptTemplate, cancellationToken);

            LastPromptEvaluation = result;

            await AppendEvaluationMessageAsync(result);

            PromptEvaluationCompleted?.Invoke(this, result);
            return result;
        }
        finally
        {
            IsEvaluating = false;
        }
    }

    private async Task<SlideEvaluationResult> EvaluateContextAsync(
        PipelineContext context, string userPrompt, CancellationToken cancellationToken)
    {
        IsEvaluating = true;
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
                    cancellationToken: cancellationToken);

            context.SlideEvaluation = result;
            LastSlideEvaluation = result;

            await AppendEvaluationMessageAsync(result);

            EvaluationCompleted?.Invoke(this, result);
            return result;
        }
        finally
        {
            IsEvaluating = false;
        }
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
