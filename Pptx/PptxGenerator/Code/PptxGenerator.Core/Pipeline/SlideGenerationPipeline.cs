using System.ComponentModel;
using System.Runtime.CompilerServices;
using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
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

        var replayPlan = CreateReplayPlan(targetMessage);
        var originalSession = _copilotChatManager.SelectedSession;
        var endpointManager = _copilotChatManager.AgentApiEndpointManager;
        ILanguageModel originalPrimaryModel = endpointManager.PrimaryModel;

        _streamingState = null;
        await SlideMlRenderTool.ResetLatestResultAsync().ConfigureAwait(false);

        if (replayPlan.PreviousTurns.Count > 0)
        {
            var replaySession = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now)
            {
                MainThreadDispatcher = _copilotChatManager.MainThreadDispatcher,
            };
            var replayModel = CreateReplayLanguageModel(replayPlan.PreviousTurns);

            try
            {
                _copilotChatManager.ChatSessions.Insert(0, replaySession);
                _copilotChatManager.SelectedSession = replaySession;
                endpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([replayModel]));
                endpointManager.PrimaryModel = replayModel;

                for (var i = 0; i < replayPlan.PreviousTurns.Count; i++)
                {
                    ReplayTurn turn = replayPlan.PreviousTurns[i];
                    await SendMessageAsync(
                        turn.UserText,
                        isFirstMessage: i == 0,
                        attachPreview: false,
                        skipAutoEvaluation: true,
                        useStreaming: true,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                endpointManager.PrimaryModel = originalPrimaryModel;
                _copilotChatManager.SelectedSession = originalSession;
            }

            originalSession.SetAgentSession(replaySession.AgentSession);
        }

        await TruncateOriginalSessionAsync(originalSession, replayPlan.TargetIndex, cancellationToken).ConfigureAwait(false);
        if (replayPlan.PreviousTurns.Count == 0)
        {
            originalSession.SetAgentSession(null);
        }

        _copilotChatManager.SelectedSession = originalSession;

        await SendMessageAsync(
            replayPlan.TargetUserText,
            replayPlan.IsTargetFirstMessage,
            attachPreview: false,
            skipAutoEvaluation: false,
            useStreaming: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
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
        CancellationToken cancellationToken = default
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
                attachPreview: attachPreview).ConfigureAwait(false);

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

        if (attachedImageFiles is { Count: > 0 })
        {
            foreach (var imageFile in attachedImageFiles)
            {
                if (!string.IsNullOrWhiteSpace(imageFile) && File.Exists(imageFile))
                {
                    var dataContent = await DataContent.LoadFromAsync(imageFile, cancellationToken: cancellationToken);
                    contents.Add(dataContent);
                }
            }
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

    private ReplayPlan CreateReplayPlan(CopilotChatMessage targetMessage)
    {
        if (targetMessage.Role != ChatRole.User || targetMessage.IsPresetInfo)
        {
            throw new ArgumentException("只能从普通用户消息重新开始。", nameof(targetMessage));
        }

        var currentSession = _copilotChatManager.SelectedSession;
        var messages = currentSession.ChatMessages;
        int targetIndex = messages.IndexOf(targetMessage);
        if (targetIndex < 0)
        {
            throw new InvalidOperationException("在当前会话中找不到要重新开始的用户消息。");
        }

        var previousTurns = new List<ReplayTurn>();
        for (var i = 0; i < targetIndex; i++)
        {
            CopilotChatMessage message = messages[i];
            if (message.IsPresetInfo || message.Role != ChatRole.User)
            {
                continue;
            }

            string userText = message.Content;
            if (string.IsNullOrWhiteSpace(userText))
            {
                continue;
            }

            CopilotChatMessage? assistantMessage = null;
            for (var j = i + 1; j < targetIndex; j++)
            {
                CopilotChatMessage candidate = messages[j];
                if (candidate.IsPresetInfo)
                {
                    continue;
                }

                if (candidate.Role == ChatRole.User)
                {
                    break;
                }

                if (candidate.Role == ChatRole.Assistant)
                {
                    assistantMessage = candidate;
                    i = j;
                    break;
                }
            }

            if (assistantMessage is null)
            {
                break;
            }

            previousTurns.Add(new ReplayTurn(userText, assistantMessage.Content));
        }

        return new ReplayPlan(
            targetIndex,
            targetMessage.Content,
            previousTurns,
            IsTargetFirstMessage: previousTurns.Count == 0);
    }

    private static FakeLanguageModel CreateReplayLanguageModel(IReadOnlyList<ReplayTurn> replayTurns)
    {
        var replayTexts = new Queue<string>(replayTurns.Select(turn => turn.AssistantText));
        var fakeChatClient = new FakeChatClient
        {
            OnGetResponseAsync = (_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty))),
            OnGetStreamingResponseAsync = (_, _, ct) => StreamNextReplayTextAsync(replayTexts, ct),
        };

        return new FakeLanguageModel(fakeChatClient)
        {
            ModelDefinition = new ModelDefinition
            {
                ModelId = $"SlideReplay-{Guid.NewGuid():N}",
                ModelName = "SlideML Replay",
                Provider = "Fake",
            },
        };
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamNextReplayTextAsync(
        Queue<string> replayTexts,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!replayTexts.TryDequeue(out string? text))
        {
            throw new InvalidOperationException("没有可用于回放的助手消息。");
        }

        foreach (char ch in text)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, ch.ToString());
            await Task.Yield();
        }
    }

    private async Task TruncateOriginalSessionAsync(
        CopilotChatSession originalSession,
        int targetIndex,
        CancellationToken cancellationToken)
    {
        await _dispatcher.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (originalSession.ChatMessages.Count > targetIndex)
            {
                originalSession.ChatMessages.RemoveAt(originalSession.ChatMessages.Count - 1);
            }

            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    private sealed record ReplayPlan(
        int TargetIndex,
        string TargetUserText,
        IReadOnlyList<ReplayTurn> PreviousTurns,
        bool IsTargetFirstMessage);

    private sealed record ReplayTurn(string UserText, string AssistantText);

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
