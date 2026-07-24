using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Resources;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Threading;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.ViewModels;

/// <summary>
/// Coordinates the real single-slide beautification workspace for one analyzed courseware package.
/// </summary>
public sealed class CoursewareSlideWorkspaceViewModel : ObservableObject, IDisposable
{
    private const string DefaultGenerationInstruction = "请根据当前页面完整 Markdown、可用的原始截图和全课件主题完成页面美化，保持教学语义准确。";
    private readonly CoursewareWorkspaceSession _session;
    private readonly ICoursewareSlidePromptBuilder _promptBuilder;
    private CoursewareSlidePromptSource _promptSource;
    private readonly IViewModelDispatcher _dispatcher;
    private readonly CancellationTokenSource _workspaceCancellationTokenSource = new();
    private readonly AsyncRelayCommand _sendMessageCommand;
    private readonly AsyncRelayCommand _rerenderCommand;
    private readonly AsyncRelayCommand _connectMcpCommand;
    private readonly RelayCommand _cancelSelectedSlideCommand;
    private CancellationTokenSource? _selectionInitializationCancellationTokenSource;
    private CoursewareSlideItemViewModel? _selectedSlide;
    private Task _selectedSlideInitializationTask = Task.CompletedTask;
    private CoursewareSlideWorkspaceSummary _summary = CreateEmptySummary();
    private string _mcpServiceUrl = SlideChatManagerFactory.DefaultMcpServiceUrl;
    private string? _enabledMcpServiceUrl;
    private string _mcpStatusText = "本地渲染";
    private bool _isConnectingMcp;
    private bool _isActive;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareSlideWorkspaceViewModel" /> class.
    /// </summary>
    /// <param name="session">The analyzed courseware workspace session.</param>
    /// <param name="slideChatManagerFactory">The lazy page runtime factory.</param>
    /// <param name="promptBuilder">The structured page prompt builder.</param>
    /// <param name="summaryService">The deterministic Markdown summary service.</param>
    /// <param name="dispatcher">The dispatcher used for observable state updates.</param>
    public CoursewareSlideWorkspaceViewModel(
        CoursewareWorkspaceSession session,
        ISlideChatManagerFactory slideChatManagerFactory,
        ICoursewareSlidePromptBuilder promptBuilder,
        CoursewareSlideSummaryService summaryService,
        IViewModelDispatcher? dispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(slideChatManagerFactory);
        ArgumentNullException.ThrowIfNull(promptBuilder);
        ArgumentNullException.ThrowIfNull(summaryService);
        if (session.ThemeAnalysisResult is null)
        {
            throw new ArgumentException("创建页面工作台前必须完成课件主题分析。", nameof(session));
        }

        _session = session;
        _promptBuilder = promptBuilder;
        _dispatcher = dispatcher ?? WpfViewModelDispatcher.Instance;
        _promptSource = promptBuilder.PrepareSource(
            session.InputPackage,
            session.ThemeAnalysisResult,
            _workspaceCancellationTokenSource.Token);
        Slides = new ObservableCollection<CoursewareSlideItemViewModel>(
            session.InputPackage.Slides.Select(slide => new CoursewareSlideItemViewModel(
                slide,
                summaryService.CreateTitle(slide.MarkdownText, slide.PageNumber),
                summaryService.CreateSummary(slide.MarkdownText),
                slideChatManagerFactory,
                _dispatcher)));
        foreach (var slide in Slides)
        {
            slide.PropertyChanged += OnSlidePropertyChanged;
        }

        _sendMessageCommand = new AsyncRelayCommand(
            parameter => ExecutePageCommandAsync(parameter, SendMessageAsync),
            parameter => CanSendMessage(GetCommandSlide(parameter)),
            allowsConcurrentExecutions: true);
        _rerenderCommand = new AsyncRelayCommand(
            parameter => ExecutePageCommandAsync(parameter, RerenderSlideAsync),
            parameter => CanRerenderSlide(GetCommandSlide(parameter)),
            allowsConcurrentExecutions: true);
        _connectMcpCommand = new AsyncRelayCommand(
            _ => ConnectMcpAsync(),
            _ => !IsConnectingMcp && !string.IsNullOrWhiteSpace(McpServiceUrl),
            HandleUnexpectedCommandException);
        _cancelSelectedSlideCommand = new RelayCommand(
            _ => SelectedSlide?.CancelActiveOperation(),
            _ => SelectedSlide?.IsBusy == true);
        _selectedSlide = Slides.FirstOrDefault();
        RefreshSummary();
    }

    /// <summary>
    /// Gets the courseware title.
    /// </summary>
    public string CoursewareTitle => _session.InputPackage.CoursewareName;

    /// <summary>
    /// Gets the validated theme title.
    /// </summary>
    public string ThemeTitle => _session.ThemeAnalysisResult?.ThemeTitle ?? string.Empty;

    /// <summary>
    /// Gets the real slides displayed by the workspace.
    /// </summary>
    public ObservableCollection<CoursewareSlideItemViewModel> Slides { get; }

    /// <summary>
    /// Gets or sets the selected real courseware slide.
    /// </summary>
    public CoursewareSlideItemViewModel? SelectedSlide
    {
        get => _selectedSlide;
        set
        {
            if (!SetProperty(ref _selectedSlide, value))
            {
                return;
            }

            OnSelectedSlideChanged();
            if (_isActive && value is not null)
            {
                SelectedSlideInitializationTask = StartSelectedSlideInitialization(value);
            }
        }
    }

    /// <summary>
    /// Gets the active selected-slide initialization task for deterministic awaiting in callers and tests.
    /// </summary>
    public Task SelectedSlideInitializationTask
    {
        get => _selectedSlideInitializationTask;
        private set => _selectedSlideInitializationTask = value;
    }

    /// <summary>
    /// Gets the current workspace execution summary.
    /// </summary>
    public CoursewareSlideWorkspaceSummary Summary
    {
        get => _summary;
        private set
        {
            if (SetProperty(ref _summary, value))
            {
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    /// <summary>
    /// Gets the compact user-facing execution summary.
    /// </summary>
    public string SummaryText => $"已完成 {Summary.CompletedCount} / {Summary.TotalCount}，"
        + $"进行中 {Summary.InProgressCount}，失败 {Summary.FailedCount}，已取消 {Summary.CanceledCount}";

    /// <summary>
    /// Gets or sets the MCP service URL used by initialized page runtimes.
    /// </summary>
    public string McpServiceUrl
    {
        get => _mcpServiceUrl;
        set
        {
            if (SetProperty(ref _mcpServiceUrl, value))
            {
                _connectMcpCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the MCP status for the selected page runtime.
    /// </summary>
    public string McpStatusText
    {
        get => _mcpStatusText;
        private set => SetProperty(ref _mcpStatusText, value);
    }

    /// <summary>
    /// Gets a value indicating whether an MCP connection attempt is running.
    /// </summary>
    public bool IsConnectingMcp
    {
        get => _isConnectingMcp;
        private set
        {
            if (SetProperty(ref _isConnectingMcp, value))
            {
                _connectMcpCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the command that sends the selected page input as an initial request or follow-up message.
    /// </summary>
    public AsyncRelayCommand SendMessageCommand => _sendMessageCommand;

    /// <summary>
    /// Gets the command that renders the selected page's editable SlideML without requiring a model.
    /// </summary>
    public AsyncRelayCommand RerenderCommand => _rerenderCommand;

    /// <summary>
    /// Gets the command that cancels the selected page operation.
    /// </summary>
    public ICommand CancelSelectedSlideCommand => _cancelSelectedSlideCommand;

    /// <summary>
    /// Gets the command that connects initialized page runtimes to MCP rendering.
    /// </summary>
    public AsyncRelayCommand ConnectMcpCommand => _connectMcpCommand;

    /// <summary>
    /// Activates the workspace and initializes only the currently selected page runtime.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel activation.</param>
    /// <returns>A task that represents workspace activation.</returns>
    public async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _isActive = true;
        if (SelectedSlide is null)
        {
            return;
        }

        SelectedSlideInitializationTask = StartSelectedSlideInitialization(SelectedSlide, cancellationToken);
        await SelectedSlideInitializationTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Deactivates the workspace and cancels current page operations while preserving page state for re-entry.
    /// </summary>
    public void Deactivate()
    {
        _isActive = false;
        CancelSelectionInitialization();
        CancelActiveOperations();
    }

    /// <summary>
    /// Replaces the theme source used by future initial page-generation requests without recreating pages or conversations.
    /// </summary>
    /// <param name="analysisResult">The latest successful whole-courseware theme analysis result.</param>
    public void UpdateThemeAnalysisResult(CoursewareThemeAnalysisResult analysisResult)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(analysisResult);
        _session.ThemeAnalysisResult = analysisResult;
        _promptSource = _promptBuilder.PrepareSource(
            _session.InputPackage,
            analysisResult,
            _workspaceCancellationTokenSource.Token);
        foreach (var slide in Slides.Where(slide =>
                     slide.IsInitialPromptPrepared
                     && !slide.IsInitialPromptDirty
                     && !slide.HasStartedGenerationConversation))
        {
            PrepareInitialDraft(slide, force: true);
        }

        OnPropertyChanged(nameof(ThemeTitle));
    }

    /// <summary>
    /// Adds valid local image attachments to the selected page.
    /// </summary>
    /// <param name="filePaths">The selected image paths.</param>
    public void AddAttachedImageFiles(IEnumerable<string> filePaths)
    {
        SelectedSlide?.AddAttachedImageFiles(filePaths);
    }

    /// <summary>
    /// Cancels all active page operations while keeping the workspace reusable.
    /// </summary>
    public void CancelActiveOperations()
    {
        foreach (var slide in Slides)
        {
            slide.CancelActiveOperation();
        }
    }

    /// <summary>
    /// Cancels the workspace and releases all page-scoped resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _isActive = false;
        CancelSelectionInitialization();
        _workspaceCancellationTokenSource.Cancel();
        CancelActiveOperations();
        foreach (var slide in Slides)
        {
            slide.PropertyChanged -= OnSlidePropertyChanged;
            slide.Dispose();
        }

        _workspaceCancellationTokenSource.Dispose();
    }

    private Task StartSelectedSlideInitialization(
        CoursewareSlideItemViewModel slide,
        CancellationToken cancellationToken = default)
    {
        CancelSelectionInitialization();
        _selectionInitializationCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            _workspaceCancellationTokenSource.Token,
            cancellationToken);
        return InitializeSlideAsync(slide, _selectionInitializationCancellationTokenSource.Token);
    }

    private async Task InitializeSlideAsync(
        CoursewareSlideItemViewModel slide,
        CancellationToken cancellationToken)
    {
        try
        {
            await InvokeIfNotDisposedAsync(() => PrepareInitialDraft(slide)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var runtimeCreationTask = slide.EnsureRuntimeAsync(_workspaceCancellationTokenSource.Token);
            await runtimeCreationTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            await ApplyMcpSettingAsync(slide, cancellationToken).ConfigureAwait(false);
            await InvokeIfNotDisposedAsync(() =>
            {
                RefreshMcpStatusText();
                RaiseCommandStates();
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await InvokeIfNotDisposedAsync(() =>
            {
                slide.ErrorMessage = ex.Message;
                slide.RenderingLog = ex.ToString();
                slide.RuntimeState = CoursewareSlideRuntimeState.Failed;
                slide.State = CoursewareSlideState.Failed;
            }).ConfigureAwait(false);
        }
    }

    private void PrepareInitialDraft(
        CoursewareSlideItemViewModel slide,
        bool force = false)
    {
        if (slide.HasStartedGenerationConversation
            || (!force && slide.IsInitialPromptPrepared)
            || (force && slide.IsInitialPromptDirty))
        {
            return;
        }

        var sourceScreenshotAttached = slide.EnsureSourceScreenshotAttachment();
        var promptResult = _promptBuilder.Build(
            _promptSource,
            slide.SlideIndex,
            DefaultGenerationInstruction,
            sourceScreenshotAttached,
            _workspaceCancellationTokenSource.Token);
        slide.ApplyInitialPrompt(promptResult.Prompt);
    }

    private async Task ExecutePageCommandAsync(
        object? parameter,
        Func<CoursewareSlideItemViewModel, Task> executeAsync)
    {
        var slide = GetCommandSlide(parameter);
        if (slide is null)
        {
            return;
        }

        try
        {
            await executeAsync(slide).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ApplyUnexpectedPageExceptionAsync(slide, ex).ConfigureAwait(false);
        }
    }

    private Task SendMessageAsync(CoursewareSlideItemViewModel slide)
    {
        if (string.IsNullOrWhiteSpace(slide.InputText))
        {
            return Task.CompletedTask;
        }

        return SendPageMessageAsync(slide);
    }

    private async Task SendPageMessageAsync(CoursewareSlideItemViewModel slide)
    {
        if (!slide.TryBeginOperation(
                _workspaceCancellationTokenSource.Token,
                out var cancellationToken))
        {
            return;
        }

        var snapshot = slide.CreateMessageSnapshot();
        var isFirstMessage = snapshot.IsFirstMessage;
        var sourceScreenshotAttached = snapshot.Attachments.Any(attachment =>
            attachment.Kind == CoursewareChatImageAttachmentKind.SourceScreenshot);
        try
        {
            var unavailableAttachment = snapshot.Attachments.FirstOrDefault(attachment => !attachment.IsAvailable);
            if (unavailableAttachment is not null)
            {
                await InvokeIfNotDisposedAsync(() =>
                {
                    slide.ErrorMessage = $"附件文件不存在：{unavailableAttachment.DisplayName}";
                    slide.RenderingLog = "请移除失效附件或重新选择文件后再发送。";
                    slide.GenerationState = CoursewareSlideGenerationState.Failed;
                    slide.State = CoursewareSlideState.Failed;
                }).ConfigureAwait(false);
                return;
            }

            var runtime = await slide.EnsureRuntimeAsync(cancellationToken).ConfigureAwait(false);
            await ApplyMcpSettingAsync(slide, cancellationToken).ConfigureAwait(false);
            if (!runtime.IsAiGenerationAvailable)
            {
                await InvokeIfNotDisposedAsync(() =>
                {
                    slide.ErrorMessage = runtime.InitializationError ?? "当前页面的语言模型不可用。";
                    slide.RenderingLog = "语言模型不可用，不能生成或追问；仍可编辑 SlideML 后使用重新渲染。";
                    slide.GenerationState = CoursewareSlideGenerationState.Failed;
                    slide.State = CoursewareSlideState.Failed;
                }).ConfigureAwait(false);
                return;
            }

            if (isFirstMessage)
            {
                if (runtime.SlideChatManager.Pipeline.ChatManager.ChatMessages.Count > 0)
                {
                    runtime.SlideChatManager.Pipeline.ChatManager.CreateNewSession();
                }

                _ = CoursewareSlideContextBudgetValidator.ValidateIfConfigured(
                    runtime.SlideChatManager.CurrentModel.ModelDefinition,
                    runtime.SlideChatManager.Pipeline.PromptProvider,
                    runtime.SlideChatManager.SlideMlRenderTool,
                    slide.PageNumber,
                    snapshot.Message,
                    cancellationToken);
            }

            await InvokeIfNotDisposedAsync(() =>
            {
                slide.ErrorMessage = null;
                slide.GenerationState = CoursewareSlideGenerationState.Generating;
                if (isFirstMessage)
                {
                    slide.ScreenshotAttachmentState = sourceScreenshotAttached
                        ? CoursewareScreenshotAttachmentState.Attached
                        : CoursewareScreenshotAttachmentState.FileMissing;
                }

                slide.State = CoursewareSlideState.Generating;
            }).ConfigureAwait(false);
            await runtime.SlideChatManager.SendMessageAsync(
                snapshot.Message,
                isFirstMessage,
                attachPreview: snapshot.AttachPreview,
                snapshot.Attachments.Select(attachment => attachment.FullName).ToArray(),
                requiredAttachedImageFiles: sourceScreenshotAttached
                    ? snapshot.Attachments
                        .Where(attachment => attachment.Kind == CoursewareChatImageAttachmentKind.SourceScreenshot)
                        .Select(attachment => attachment.FullName)
                        .ToArray()
                    : null,
                useStreaming: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(runtime.SlideChatManager.CurrentSlideXml))
            {
                await InvokeIfNotDisposedAsync(() =>
                {
                    slide.ErrorMessage = "模型未生成可渲染的 SlideML。";
                    slide.RenderingLog = "本次对话已完成，但未获得可渲染的 SlideML，请修改要求后重试。";
                    slide.GenerationState = CoursewareSlideGenerationState.Failed;
                    slide.State = CoursewareSlideState.Failed;
                }).ConfigureAwait(false);
                return;
            }

            await InvokeIfNotDisposedAsync(() =>
            {
                slide.ApplySuccessfulSend(snapshot);
                slide.ApplyGeneratedSlideXml(runtime.SlideChatManager.CurrentSlideXml);
                slide.CallbackXml = runtime.SlideChatManager.RenderedXml;
                slide.RenderingLog = string.IsNullOrWhiteSpace(runtime.SlideChatManager.WarningText)
                    ? CoursewareUiStrings.SlideGenerationCompleted
                    : runtime.SlideChatManager.WarningText;
                slide.GenerationState = CoursewareSlideGenerationState.Completed;
                slide.State = CoursewareSlideState.Completed;
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await InvokeIfNotDisposedAsync(() =>
            {
                slide.ErrorMessage = null;
                slide.RenderingLog = CoursewareUiStrings.SlideGenerationCanceled;
                slide.GenerationState = CoursewareSlideGenerationState.Canceled;
                slide.State = CoursewareSlideState.Canceled;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await InvokeIfNotDisposedAsync(() =>
            {
                slide.ErrorMessage = ex.Message;
                slide.RenderingLog = ex.ToString();
                if (isFirstMessage && sourceScreenshotAttached)
                {
                    slide.ScreenshotAttachmentState = CoursewareScreenshotAttachmentState.SendFailed;
                }

                slide.GenerationState = CoursewareSlideGenerationState.Failed;
                slide.State = CoursewareSlideState.Failed;
            }).ConfigureAwait(false);
        }
        finally
        {
            slide.CompleteOperation(cancellationToken);
        }
    }

    private async Task RerenderSlideAsync(CoursewareSlideItemViewModel slide)
    {
        if (string.IsNullOrWhiteSpace(slide.EditableSlideXml))
        {
            return;
        }

        if (!slide.TryBeginOperation(
                _workspaceCancellationTokenSource.Token,
                out var cancellationToken))
        {
            return;
        }

        try
        {
            var runtime = await slide.EnsureRuntimeAsync(cancellationToken).ConfigureAwait(false);
            await ApplyMcpSettingAsync(slide, cancellationToken).ConfigureAwait(false);
            await InvokeIfNotDisposedAsync(() =>
            {
                slide.ErrorMessage = null;
                slide.GenerationState = CoursewareSlideGenerationState.Rendering;
                slide.State = CoursewareSlideState.Rendering;
            }).ConfigureAwait(false);
            var renderResult = await runtime.SlideChatManager.SlideMlRenderTool.RenderPipeline
                .RenderAsync(slide.EditableSlideXml, cancellationToken).ConfigureAwait(false);
            runtime.SlideChatManager.SlideMlRenderTool.ApplyRenderResult(renderResult);
            await InvokeIfNotDisposedAsync(() =>
            {
                slide.CallbackXml = renderResult.OutputXml;
                slide.RenderingLog = renderResult.Warnings.Count == 0
                    ? CoursewareUiStrings.SlideRerenderCompleted
                    : string.Join(Environment.NewLine, renderResult.Warnings);
                slide.State = renderResult.Errors.Count == 0
                    ? CoursewareSlideState.Completed
                    : CoursewareSlideState.Failed;
                slide.ErrorMessage = renderResult.Errors.Count == 0
                    ? null
                    : string.Join(Environment.NewLine, renderResult.Errors);
                slide.GenerationState = renderResult.Errors.Count == 0
                    ? CoursewareSlideGenerationState.Completed
                    : CoursewareSlideGenerationState.Failed;
                if (renderResult.Errors.Count == 0)
                {
                    slide.HasUnsavedChanges = false;
                }
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await InvokeIfNotDisposedAsync(() =>
            {
                slide.RenderingLog = CoursewareUiStrings.SlideRerenderCanceled;
                slide.GenerationState = CoursewareSlideGenerationState.Canceled;
                slide.State = CoursewareSlideState.Canceled;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await InvokeIfNotDisposedAsync(() =>
            {
                slide.ErrorMessage = ex.Message;
                slide.RenderingLog = ex.ToString();
                slide.GenerationState = CoursewareSlideGenerationState.Failed;
                slide.State = CoursewareSlideState.Failed;
            }).ConfigureAwait(false);
        }
        finally
        {
            slide.CompleteOperation(cancellationToken);
        }
    }

    private async Task ConnectMcpAsync()
    {
        if (string.IsNullOrWhiteSpace(McpServiceUrl))
        {
            return;
        }

        IsConnectingMcp = true;
        McpStatusText = "正在连接 MCP...";
        try
        {
            var initializedPipelines = Slides
                .Select(slide => slide.Runtime?.SlideChatManager.SlideMlRenderTool.RenderPipeline)
                .OfType<SwitchableSlideMlRenderPipeline>()
                .Distinct()
                .ToArray();
            if (initializedPipelines.Length == 0)
            {
                McpStatusText = "尚无已初始化页面";
                return;
            }

            var serviceUrl = McpServiceUrl.Trim();
            var results = await Task.WhenAll(initializedPipelines.Select(pipeline =>
                pipeline.TryEnableMcpAsync(serviceUrl, _workspaceCancellationTokenSource.Token))).ConfigureAwait(false);
            var connectedCount = results.Count(result => result);
            _enabledMcpServiceUrl = connectedCount > 0 ? serviceUrl : null;
            await InvokeIfNotDisposedAsync(() =>
            {
                McpStatusText = connectedCount == initializedPipelines.Length
                    ? $"MCP 已连接：{connectedCount} 个已初始化页面"
                    : connectedCount == 0
                        ? "MCP 连接失败，继续使用本地渲染"
                        : $"MCP 部分连接：{connectedCount}/{initializedPipelines.Length} 个已初始化页面";
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_workspaceCancellationTokenSource.IsCancellationRequested)
        {
            await InvokeIfNotDisposedAsync(() => McpStatusText = "MCP 连接已取消").ConfigureAwait(false);
        }
        finally
        {
            await InvokeIfNotDisposedAsync(() => IsConnectingMcp = false).ConfigureAwait(false);
        }
    }

    private async Task ApplyMcpSettingAsync(
        CoursewareSlideItemViewModel slide,
        CancellationToken cancellationToken)
    {
        if (_enabledMcpServiceUrl is null
            || slide.Runtime?.SlideChatManager.SlideMlRenderTool.RenderPipeline is not SwitchableSlideMlRenderPipeline renderPipeline
            || renderPipeline.IsMcpEnabled)
        {
            return;
        }

        _ = await renderPipeline.TryEnableMcpAsync(
            _enabledMcpServiceUrl,
            cancellationToken).ConfigureAwait(false);
    }

    private CoursewareSlideItemViewModel? GetCommandSlide(object? parameter)
    {
        return parameter as CoursewareSlideItemViewModel ?? SelectedSlide;
    }

    private static bool CanSendMessage(CoursewareSlideItemViewModel? slide)
    {
        return slide is { IsBusy: false, HasUnsavedChanges: false }
            && !string.IsNullOrWhiteSpace(slide.InputText)
            && (slide.Runtime is null || slide.IsAiGenerationAvailable);
    }

    private static bool CanRerenderSlide(CoursewareSlideItemViewModel? slide)
    {
        return slide is { IsBusy: false }
            && !string.IsNullOrWhiteSpace(slide.EditableSlideXml);
    }

    private void OnSelectedSlideChanged()
    {
        CancelSelectionInitialization();
        OnPropertyChanged(nameof(SelectedSlide));
        RefreshMcpStatusText();
        RaiseCommandStates();
    }

    private void OnSlidePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CoursewareSlideItemViewModel.State))
        {
            RefreshSummary();
        }

        if (ReferenceEquals(sender, SelectedSlide))
        {
            RaiseCommandStates();
            if (e.PropertyName is nameof(CoursewareSlideItemViewModel.Runtime))
            {
                RefreshMcpStatusText();
            }
        }
    }

    private void RefreshSummary()
    {
        Summary = new CoursewareSlideWorkspaceSummary
        {
            TotalCount = Slides.Count,
            NotStartedCount = Slides.Count(slide => slide.State == CoursewareSlideState.NotStarted),
            InProgressCount = Slides.Count(slide => slide.State is CoursewareSlideState.Initializing or CoursewareSlideState.Generating or CoursewareSlideState.Rendering),
            ReadyCount = Slides.Count(slide => slide.State == CoursewareSlideState.Ready),
            CompletedCount = Slides.Count(slide => slide.State == CoursewareSlideState.Completed),
            FailedCount = Slides.Count(slide => slide.State == CoursewareSlideState.Failed),
            CanceledCount = Slides.Count(slide => slide.State == CoursewareSlideState.Canceled),
        };
    }

    private void RefreshMcpStatusText()
    {
        McpStatusText = SelectedSlide?.Runtime?.SlideChatManager.SlideMlRenderTool.RenderPipeline
            is SwitchableSlideMlRenderPipeline { IsMcpEnabled: true }
                ? "当前页面使用 MCP 渲染"
                : "当前页面使用本地渲染";
    }

    private void RaiseCommandStates()
    {
        _sendMessageCommand.RaiseCanExecuteChanged();
        _rerenderCommand.RaiseCanExecuteChanged();
        _cancelSelectedSlideCommand.RaiseCanExecuteChanged();
    }

    private void CancelSelectionInitialization()
    {
        var cancellationTokenSource = Interlocked.Exchange(
            ref _selectionInitializationCancellationTokenSource,
            null);
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }

    private async Task ApplyUnexpectedPageExceptionAsync(
        CoursewareSlideItemViewModel slide,
        Exception exception)
    {
        if (_isDisposed)
        {
            return;
        }

        await InvokeIfNotDisposedAsync(() =>
        {
            slide.ErrorMessage = exception.Message;
            slide.RenderingLog = exception.ToString();
            slide.GenerationState = CoursewareSlideGenerationState.Failed;
            slide.State = CoursewareSlideState.Failed;
        }).ConfigureAwait(false);
    }

    private void HandleUnexpectedCommandException(Exception exception)
    {
        _ = InvokeIfNotDisposedAsync(() =>
        {
            McpStatusText = $"MCP 操作失败：{exception.Message}";
            IsConnectingMcp = false;
        });
    }

    private Task InvokeIfNotDisposedAsync(Action action)
    {
        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        return _dispatcher.InvokeAsync(() =>
        {
            if (!_isDisposed)
            {
                action();
            }
        });
    }

    private static CoursewareSlideWorkspaceSummary CreateEmptySummary()
    {
        return new CoursewareSlideWorkspaceSummary
        {
            TotalCount = 0,
            NotStartedCount = 0,
            InProgressCount = 0,
            ReadyCount = 0,
            CompletedCount = 0,
            FailedCount = 0,
            CanceledCount = 0,
        };
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(CoursewareSlideWorkspaceViewModel));
        }
    }
}
