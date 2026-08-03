using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Resources;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Threading;
using Microsoft.Extensions.AI;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.ViewModels;

/// <summary>
/// Coordinates the real single-slide beautification workspace for one analyzed courseware package.
/// </summary>
public sealed class CoursewareSlideWorkspaceViewModel : ObservableObject, IDisposable
{
    private const string DefaultGenerationInstruction = "请根据当前页完整内容、可用视觉附件和全课件主题完成页面美化，保持教学语义准确、信息完整、层级清晰，并确保所有内容适合当前画布。";
    private readonly CoursewareWorkspaceSession _session;
    private readonly ICoursewareSlidePromptBuilder _promptBuilder;
    private CoursewareSlidePromptSource _promptSource;
    private readonly IViewModelThreadAccess _threadAccess;
    private readonly ICoursewareImageAttachmentLoader _imageAttachmentLoader;
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
    private string _mcpStatusText = "当前使用本地渲染";
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
    /// <param name="threadAccess">Verifies access to the thread that owns observable state.</param>
    /// <param name="imageAttachmentLoader">The image attachment loader used to atomically prepare requests.</param>
    public CoursewareSlideWorkspaceViewModel(
        CoursewareWorkspaceSession session,
        ISlideChatManagerFactory slideChatManagerFactory,
        ICoursewareSlidePromptBuilder promptBuilder,
        CoursewareSlideSummaryService summaryService,
        IViewModelThreadAccess? threadAccess = null)
        : this(
            session,
            slideChatManagerFactory,
            promptBuilder,
            summaryService,
            threadAccess,
            new CoursewareImageAttachmentLoader())
    {
    }

    internal CoursewareSlideWorkspaceViewModel(
        CoursewareWorkspaceSession session,
        ISlideChatManagerFactory slideChatManagerFactory,
        ICoursewareSlidePromptBuilder promptBuilder,
        CoursewareSlideSummaryService summaryService,
        IViewModelThreadAccess? threadAccess,
        ICoursewareImageAttachmentLoader imageAttachmentLoader)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(slideChatManagerFactory);
        ArgumentNullException.ThrowIfNull(promptBuilder);
        ArgumentNullException.ThrowIfNull(summaryService);
        ArgumentNullException.ThrowIfNull(imageAttachmentLoader);
        if (session.ThemeAnalysisResult is null)
        {
            throw new ArgumentException("创建页面工作台前必须完成课件主题分析。", nameof(session));
        }

        _session = session;
        _promptBuilder = promptBuilder;
        _threadAccess = threadAccess ?? WpfViewModelThreadAccess.Instance;
        _imageAttachmentLoader = imageAttachmentLoader;
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
                _threadAccess)));
        foreach (var slide in Slides)
        {
            slide.PropertyChanged += OnSlidePropertyChanged;
            slide.ConfigureInitialPromptReset(ResetInitialPromptToLatestTheme);
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
    public string ThemeTitle => _session.ThemeAnalysisResult?.Theme.Style ?? string.Empty;

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
            VerifyAccess();
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
            VerifyAccess();
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
        VerifyAccess();
        ThrowIfDisposed();
        _isActive = true;
        if (SelectedSlide is null)
        {
            return;
        }

        SelectedSlideInitializationTask = StartSelectedSlideInitialization(SelectedSlide, cancellationToken);
        await SelectedSlideInitializationTask;
    }

    /// <summary>
    /// Deactivates the workspace and cancels current page operations while preserving page state for re-entry.
    /// </summary>
    public void Deactivate()
    {
        VerifyAccess();
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
        VerifyAccess();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(analysisResult);
        _session.ThemeAnalysisResult = analysisResult;
        _promptSource = _promptBuilder.PrepareSource(
            _session.InputPackage,
            analysisResult,
            _workspaceCancellationTokenSource.Token);

        foreach (var slide in Slides)
        {
            if (slide.HasStartedGenerationConversation || !slide.IsInitialPromptPrepared)
            {
                continue;
            }

            if (slide.IsOperationActive || slide.IsInitialPromptDirty)
            {
                slide.IsInitialPromptThemeOutdated = true;
                continue;
            }

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
        VerifyAccess();
        SelectedSlide?.AddAttachedImageFiles(filePaths);
    }

    /// <summary>
    /// Cancels all active page operations while keeping the workspace reusable.
    /// </summary>
    public void CancelActiveOperations()
    {
        VerifyAccess();
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
        VerifyAccess();
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
    }

    private Task StartSelectedSlideInitialization(
        CoursewareSlideItemViewModel slide,
        CancellationToken cancellationToken = default)
    {
        VerifyAccess();
        CancelSelectionInitialization();
        var initializationCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            _workspaceCancellationTokenSource.Token,
            cancellationToken);
        _selectionInitializationCancellationTokenSource = initializationCancellationTokenSource;
        return InitializeSlideAsync(
            slide,
            initializationCancellationTokenSource,
            _enabledMcpServiceUrl);
    }

    private async Task InitializeSlideAsync(
        CoursewareSlideItemViewModel slide,
        CancellationTokenSource initializationCancellationTokenSource,
        string? enabledMcpServiceUrl)
    {
        var cancellationToken = initializationCancellationTokenSource.Token;
        try
        {
            PrepareInitialDraft(slide);
            cancellationToken.ThrowIfCancellationRequested();
            var runtimeCreationTask = slide.EnsureRuntimeAsync(_workspaceCancellationTokenSource.Token);
            var runtime = await runtimeCreationTask.WaitAsync(cancellationToken);
            await ApplyMcpSettingAsync(runtime, enabledMcpServiceUrl, cancellationToken);
            RefreshMcpStatusText();
            RaiseCommandStates();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (!_isDisposed)
            {
                slide.ErrorMessage = ex.Message;
                slide.RenderingLog = ex.ToString();
                slide.RuntimeState = CoursewareSlideRuntimeState.Failed;
                slide.State = CoursewareSlideState.Failed;
            }
        }
        finally
        {
            if (ReferenceEquals(
                    _selectionInitializationCancellationTokenSource,
                    initializationCancellationTokenSource))
            {
                _selectionInitializationCancellationTokenSource = null;
            }

            initializationCancellationTokenSource.Dispose();
        }
    }

    private void PrepareInitialDraft(
        CoursewareSlideItemViewModel slide,
        bool force = false,
        bool overwriteDirty = false)
    {
        if (slide.HasStartedGenerationConversation
            || (!overwriteDirty && slide.IsInitialPromptDirty)
            || (!force && slide.IsInitialPromptPrepared)
            || (force && slide.IsInitialPromptDirty && !overwriteDirty))
        {
            return;
        }

        if (!slide.EnsureSourceScreenshotAttachment())
        {
            slide.ErrorMessage = CoursewareUiStrings.SourceScreenshotRequired;
            return;
        }

        var prompt = _promptBuilder.BuildInitialPrompt(
            _promptSource,
            slide.SlideIndex,
            slide.Canvas,
            DefaultGenerationInstruction,
            _workspaceCancellationTokenSource.Token);
        slide.ApplyInitialPrompt(prompt);
        slide.ErrorMessage = null;
    }

    private void ResetInitialPromptToLatestTheme(CoursewareSlideItemViewModel slide)
    {
        if (!slide.CanResetInitialPromptToLatestTheme)
        {
            return;
        }

        PrepareInitialDraft(slide, force: true, overwriteDirty: true);
    }

    private async Task ExecutePageCommandAsync(
        object? parameter,
        Func<CoursewareSlideItemViewModel, Task> executeAsync)
    {
        VerifyAccess();
        var slide = GetCommandSlide(parameter);
        if (slide is null)
        {
            return;
        }

        try
        {
            await executeAsync(slide);
        }
        catch (Exception ex)
        {
            ApplyUnexpectedPageException(slide, ex);
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
        VerifyAccess();
        if (string.IsNullOrWhiteSpace(slide.InputText) || slide.HasUnsavedChanges)
        {
            return;
        }

        if (!slide.TryBeginOperation(
                _workspaceCancellationTokenSource.Token,
                out var operation))
        {
            return;
        }

        var cancellationToken = operation.CancellationToken;
        var snapshot = slide.CreateMessageSnapshot();
        PageMessageCompletion completion;
        try
        {
            var preparation = await PreparePageMessageAsync(
                slide,
                snapshot,
                _enabledMcpServiceUrl,
                cancellationToken);
            if (preparation.Failure is not null)
            {
                completion = preparation.Failure;
            }
            else
            {
                BeginPageGeneration(slide, snapshot.IsFirstMessage);
                completion = await GeneratePageMessageAsync(
                    preparation.Runtime!,
                    snapshot,
                    preparation.AttachedImageContents!,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            completion = PageMessageCompletion.Canceled();
        }
        catch (CoursewareImageAttachmentLoadException ex)
        {
            completion = PageMessageCompletion.Failed(
                ex.Message,
                ex.ToString(),
                snapshot.IsFirstMessage
                && ex.Attachment.Kind == CoursewareChatImageAttachmentKind.SourceScreenshot
                    ? CoursewareScreenshotAttachmentState.SendFailed
                    : null);
        }
        catch (Exception ex)
        {
            completion = PageMessageCompletion.Failed(ex.Message, ex.ToString());
        }

        try
        {
            if (!_isDisposed)
            {
                CommitPageMessageCompletion(slide, snapshot, completion);
            }
        }
        finally
        {
            slide.CompleteOperation(operation);
        }
    }

    private async Task<PageMessagePreparation> PreparePageMessageAsync(
        CoursewareSlideItemViewModel slide,
        CoursewareSlideMessageSnapshot snapshot,
        string? enabledMcpServiceUrl,
        CancellationToken cancellationToken)
    {
        if (snapshot.IsFirstMessage)
        {
            var sourceScreenshot = snapshot.Attachments.FirstOrDefault(attachment =>
                attachment.Kind == CoursewareChatImageAttachmentKind.SourceScreenshot);
            if (sourceScreenshot is null)
            {
                return PageMessagePreparation.FromFailure(PageMessageCompletion.Failed(
                    CoursewareUiStrings.SourceScreenshotRequired,
                    CoursewareUiStrings.SourceScreenshotRequired,
                    CoursewareScreenshotAttachmentState.FileMissing));
            }

            if (!sourceScreenshot.IsAvailable)
            {
                var errorMessage = string.Format(
                    CultureInfo.CurrentCulture,
                    CoursewareUiStrings.AttachmentFileMissingFormat,
                    sourceScreenshot.DisplayName);
                return PageMessagePreparation.FromFailure(PageMessageCompletion.Failed(
                    errorMessage,
                    errorMessage,
                    CoursewareScreenshotAttachmentState.FileMissing));
            }
        }

        var unavailableAttachment = snapshot.Attachments.FirstOrDefault(attachment => !attachment.IsAvailable);
        if (unavailableAttachment is not null)
        {
            var errorMessage = string.Format(
                CultureInfo.CurrentCulture,
                CoursewareUiStrings.AttachmentFileMissingFormat,
                unavailableAttachment.DisplayName);
            return PageMessagePreparation.FromFailure(PageMessageCompletion.Failed(errorMessage, errorMessage));
        }

        var runtime = await slide.EnsureRuntimeAsync(cancellationToken);
        await ApplyMcpSettingAsync(runtime, enabledMcpServiceUrl, cancellationToken);
        if (!runtime.IsAiGenerationAvailable)
        {
            return PageMessagePreparation.FromFailure(PageMessageCompletion.Failed(
                runtime.InitializationError ?? "当前页面的智能生成功能不可用。",
                "智能生成功能不可用，暂时不能生成或继续调整；仍可在高级页面编辑中修改后重新渲染。"));
        }

        if (snapshot.IsFirstMessage)
        {
            _ = CoursewareSlideContextBudgetValidator.ValidateIfConfigured(
                runtime.SlideChatManager.CurrentModel.ModelDefinition,
                runtime.SlideChatManager.Pipeline.PromptProvider,
                runtime.SlideChatManager.SlideMlRenderTool,
                slide.PageNumber,
                snapshot.Message,
                cancellationToken);
        }

        var attachedImageContents = (await _imageAttachmentLoader.LoadAsync(
            snapshot.Attachments,
            cancellationToken)).ToList();
        if (snapshot.AttachPreview)
        {
            var previewContent = CapturePreviewDataContent(runtime.SlideChatManager.PreviewImage);
            if (previewContent is null)
            {
                return PageMessagePreparation.FromFailure(PageMessageCompletion.Failed(
                    CoursewareUiStrings.PreviewAttachmentUnavailable,
                    CoursewareUiStrings.PreviewAttachmentUnavailable));
            }

            attachedImageContents.Add(previewContent);
        }

        if (snapshot.IsFirstMessage)
        {
            await runtime.SlideChatManager.SlideMlRenderTool.ResetLatestResultAsync();
        }

        return PageMessagePreparation.Ready(runtime, attachedImageContents);
    }

    private static async Task<PageMessageCompletion> GeneratePageMessageAsync(
        CoursewareSlideRuntime runtime,
        CoursewareSlideMessageSnapshot snapshot,
        IReadOnlyList<DataContent> attachedImageContents,
        CancellationToken cancellationToken)
    {
        var result = await runtime.SlideChatManager.SendStreamingMessageWithResultAsync(
            snapshot.Message,
            snapshot.IsFirstMessage,
            attachedImageContents,
            createNewSession: snapshot.IsFirstMessage,
            cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (!result.IsSuccess)
        {
            var errorMessage = result.ErrorMessage ?? CoursewareUiStrings.SlideGenerationFailed;
            return PageMessageCompletion.Failed(errorMessage, errorMessage);
        }

        return PageMessageCompletion.Succeeded(
            result.FinalSlideXml,
            runtime.SlideChatManager.RenderedXml,
            string.IsNullOrWhiteSpace(runtime.SlideChatManager.WarningText)
                ? CoursewareUiStrings.SlideGenerationCompleted
                : runtime.SlideChatManager.WarningText);
    }

    private static void BeginPageGeneration(
        CoursewareSlideItemViewModel slide,
        bool isFirstMessage)
    {
        slide.ErrorMessage = null;
        slide.GenerationState = CoursewareSlideGenerationState.Generating;
        if (isFirstMessage)
        {
            slide.ScreenshotAttachmentState = CoursewareScreenshotAttachmentState.Attached;
        }

        slide.State = CoursewareSlideState.Generating;
    }

    private static void CommitPageMessageCompletion(
        CoursewareSlideItemViewModel slide,
        CoursewareSlideMessageSnapshot snapshot,
        PageMessageCompletion completion)
    {
        slide.ErrorMessage = completion.ErrorMessage;
        slide.RenderingLog = completion.RenderingLog;
        if (completion.ScreenshotAttachmentState is not null)
        {
            slide.ScreenshotAttachmentState = completion.ScreenshotAttachmentState.Value;
        }

        switch (completion.Kind)
        {
            case PageMessageCompletionKind.Succeeded:
                slide.ApplySuccessfulSend(snapshot);
                slide.ApplyGeneratedSlideXml(completion.FinalSlideXml!);
                slide.CallbackXml = completion.CallbackXml!;
                slide.GenerationState = CoursewareSlideGenerationState.Completed;
                slide.State = CoursewareSlideState.Completed;
                break;
            case PageMessageCompletionKind.Canceled:
                slide.GenerationState = CoursewareSlideGenerationState.Canceled;
                slide.State = CoursewareSlideState.Canceled;
                break;
            case PageMessageCompletionKind.Failed:
                slide.GenerationState = CoursewareSlideGenerationState.Failed;
                slide.State = CoursewareSlideState.Failed;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(completion));
        }
    }

    private static DataContent? CapturePreviewDataContent(IPreviewImage? previewImage)
    {
        if (previewImage is null)
        {
            return null;
        }

        using var memoryStream = new MemoryStream();
        previewImage.Save(memoryStream);
        return new DataContent(memoryStream.ToArray(), "image/png");
    }

    private async Task RerenderSlideAsync(CoursewareSlideItemViewModel slide)
    {
        VerifyAccess();
        var slideXml = slide.EditableSlideXml;
        if (string.IsNullOrWhiteSpace(slideXml))
        {
            return;
        }

        if (!slide.TryBeginOperation(
                _workspaceCancellationTokenSource.Token,
                out var operation))
        {
            return;
        }

        var cancellationToken = operation.CancellationToken;
        PageRerenderCompletion completion;
        try
        {
            var runtime = await slide.EnsureRuntimeAsync(cancellationToken);
            await ApplyMcpSettingAsync(runtime, _enabledMcpServiceUrl, cancellationToken);
            slide.ErrorMessage = null;
            slide.GenerationState = CoursewareSlideGenerationState.Rendering;
            slide.State = CoursewareSlideState.Rendering;
            completion = await RenderSlideAsync(runtime, slideXml, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            completion = PageRerenderCompletion.Canceled();
        }
        catch (Exception ex)
        {
            completion = PageRerenderCompletion.Failed(ex.Message, ex.ToString());
        }

        try
        {
            if (!_isDisposed)
            {
                CommitPageRerenderCompletion(slide, completion);
            }
        }
        finally
        {
            slide.CompleteOperation(operation);
        }
    }

    private static async Task<PageRerenderCompletion> RenderSlideAsync(
        CoursewareSlideRuntime runtime,
        string slideXml,
        CancellationToken cancellationToken)
    {
        var renderResult = await runtime.SlideChatManager.SlideMlRenderTool.RenderPipeline
            .RenderAsync(slideXml, cancellationToken);
        await runtime.SlideChatManager.SlideMlRenderTool
            .ApplyRenderResultAsync(renderResult);

        if (renderResult.Errors.Count > 0)
        {
            var errorMessage = string.Join(Environment.NewLine, renderResult.Errors);
            return PageRerenderCompletion.Failed(
                errorMessage,
                renderResult.Warnings.Count == 0
                    ? errorMessage
                    : string.Join(Environment.NewLine, renderResult.Warnings),
                renderResult.OutputXml);
        }

        return PageRerenderCompletion.Succeeded(
            renderResult.OutputXml,
            renderResult.Warnings.Count == 0
                ? CoursewareUiStrings.SlideRerenderCompleted
                : string.Join(Environment.NewLine, renderResult.Warnings));
    }

    private static void CommitPageRerenderCompletion(
        CoursewareSlideItemViewModel slide,
        PageRerenderCompletion completion)
    {
        slide.ErrorMessage = completion.ErrorMessage;
        slide.RenderingLog = completion.RenderingLog;
        if (completion.CallbackXml is not null)
        {
            slide.CallbackXml = completion.CallbackXml;
        }

        switch (completion.Kind)
        {
            case PageRerenderCompletionKind.Succeeded:
                slide.HasUnsavedChanges = false;
                slide.GenerationState = CoursewareSlideGenerationState.Completed;
                slide.State = CoursewareSlideState.Completed;
                break;
            case PageRerenderCompletionKind.Canceled:
                slide.GenerationState = CoursewareSlideGenerationState.Canceled;
                slide.State = CoursewareSlideState.Canceled;
                break;
            case PageRerenderCompletionKind.Failed:
                slide.GenerationState = CoursewareSlideGenerationState.Failed;
                slide.State = CoursewareSlideState.Failed;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(completion));
        }
    }

    private async Task ConnectMcpAsync()
    {
        VerifyAccess();
        if (string.IsNullOrWhiteSpace(McpServiceUrl))
        {
            return;
        }

        IsConnectingMcp = true;
        McpStatusText = "正在连接远程渲染服务...";
        try
        {
            var initializedPipelines = Slides
                .Select(slide => slide.Runtime?.SlideChatManager.SlideMlRenderTool.RenderPipeline)
                .OfType<SwitchableSlideMlRenderPipeline>()
                .Distinct()
                .ToArray();
            if (initializedPipelines.Length == 0)
            {
                McpStatusText = "尚无已准备完成的页面";
                return;
            }

            var serviceUrl = McpServiceUrl.Trim();
            var results = await Task.WhenAll(initializedPipelines.Select(pipeline =>
                pipeline.TryEnableMcpAsync(serviceUrl, _workspaceCancellationTokenSource.Token)));
            var connectedCount = results.Count(result => result);
            _enabledMcpServiceUrl = connectedCount > 0 ? serviceUrl : null;
            McpStatusText = connectedCount == initializedPipelines.Length
                ? $"远程渲染服务已连接：{connectedCount} 个页面"
                : connectedCount == 0
                    ? "远程渲染服务连接失败，继续使用本地渲染"
                    : $"远程渲染服务已连接部分页面：{connectedCount}/{initializedPipelines.Length}";
        }
        catch (OperationCanceledException) when (_workspaceCancellationTokenSource.IsCancellationRequested)
        {
            if (!_isDisposed)
            {
                McpStatusText = "远程渲染服务连接已取消";
            }
        }
        finally
        {
            if (!_isDisposed)
            {
                IsConnectingMcp = false;
            }
        }
    }

    private static async Task ApplyMcpSettingAsync(
        CoursewareSlideRuntime runtime,
        string? enabledMcpServiceUrl,
        CancellationToken cancellationToken)
    {
        if (enabledMcpServiceUrl is null
            || runtime.SlideChatManager.SlideMlRenderTool.RenderPipeline is not SwitchableSlideMlRenderPipeline renderPipeline
            || renderPipeline.IsMcpEnabled)
        {
            return;
        }

        _ = await renderPipeline.TryEnableMcpAsync(
            enabledMcpServiceUrl,
            cancellationToken);
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
        VerifyAccess();
        CancelSelectionInitialization();
        OnPropertyChanged(nameof(SelectedSlide));
        RefreshMcpStatusText();
        RaiseCommandStates();
    }

    private void OnSlidePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        VerifyAccess();
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
        VerifyAccess();
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
        VerifyAccess();
        McpStatusText = SelectedSlide?.Runtime?.SlideChatManager.SlideMlRenderTool.RenderPipeline
            is SwitchableSlideMlRenderPipeline { IsMcpEnabled: true }
                ? "当前页面使用远程渲染"
                : "当前页面使用本地渲染";
    }

    private void RaiseCommandStates()
    {
        VerifyAccess();
        _sendMessageCommand.RaiseCanExecuteChanged();
        _rerenderCommand.RaiseCanExecuteChanged();
        _cancelSelectedSlideCommand.RaiseCanExecuteChanged();
    }

    private void CancelSelectionInitialization()
    {
        var cancellationTokenSource = _selectionInitializationCancellationTokenSource;
        _selectionInitializationCancellationTokenSource = null;
        cancellationTokenSource?.Cancel();
    }

    private void ApplyUnexpectedPageException(
        CoursewareSlideItemViewModel slide,
        Exception exception)
    {
        if (_isDisposed)
        {
            return;
        }

        VerifyAccess();
        slide.ErrorMessage = exception.Message;
        slide.RenderingLog = exception.ToString();
        slide.GenerationState = CoursewareSlideGenerationState.Failed;
        slide.State = CoursewareSlideState.Failed;
    }

    private void HandleUnexpectedCommandException(Exception exception)
    {
        if (!_isDisposed)
        {
            VerifyAccess();
            McpStatusText = $"远程渲染服务操作失败：{exception.Message}";
            IsConnectingMcp = false;
        }
    }

    private void VerifyAccess()
    {
        if (!_threadAccess.CheckAccess())
        {
            throw new InvalidOperationException("页面工作台状态只能由所属的 ViewModel Dispatcher 修改。");
        }
    }

    private sealed record PageMessagePreparation(
        CoursewareSlideRuntime? Runtime,
        IReadOnlyList<DataContent>? AttachedImageContents,
        PageMessageCompletion? Failure)
    {
        internal static PageMessagePreparation Ready(
            CoursewareSlideRuntime runtime,
            IReadOnlyList<DataContent> attachedImageContents) =>
            new(runtime, attachedImageContents, Failure: null);

        internal static PageMessagePreparation FromFailure(PageMessageCompletion failure) =>
            new(Runtime: null, AttachedImageContents: null, failure);
    }

    private sealed record PageMessageCompletion(
        PageMessageCompletionKind Kind,
        string? ErrorMessage,
        string RenderingLog,
        string? FinalSlideXml = null,
        string? CallbackXml = null,
        CoursewareScreenshotAttachmentState? ScreenshotAttachmentState = null)
    {
        internal static PageMessageCompletion Succeeded(
            string finalSlideXml,
            string callbackXml,
            string renderingLog) =>
            new(
                PageMessageCompletionKind.Succeeded,
                ErrorMessage: null,
                renderingLog,
                finalSlideXml,
                callbackXml);

        internal static PageMessageCompletion Failed(
            string errorMessage,
            string renderingLog,
            CoursewareScreenshotAttachmentState? screenshotAttachmentState = null) =>
            new(
                PageMessageCompletionKind.Failed,
                errorMessage,
                renderingLog,
                ScreenshotAttachmentState: screenshotAttachmentState);

        internal static PageMessageCompletion Canceled() =>
            new(
                PageMessageCompletionKind.Canceled,
                ErrorMessage: null,
                CoursewareUiStrings.SlideGenerationCanceled);
    }

    private enum PageMessageCompletionKind
    {
        Succeeded,
        Failed,
        Canceled,
    }

    private sealed record PageRerenderCompletion(
        PageRerenderCompletionKind Kind,
        string? ErrorMessage,
        string RenderingLog,
        string? CallbackXml = null)
    {
        internal static PageRerenderCompletion Succeeded(
            string callbackXml,
            string renderingLog) =>
            new(
                PageRerenderCompletionKind.Succeeded,
                ErrorMessage: null,
                renderingLog,
                callbackXml);

        internal static PageRerenderCompletion Failed(
            string errorMessage,
            string renderingLog,
            string? callbackXml = null) =>
            new(
                PageRerenderCompletionKind.Failed,
                errorMessage,
                renderingLog,
                callbackXml);

        internal static PageRerenderCompletion Canceled() =>
            new(
                PageRerenderCompletionKind.Canceled,
                ErrorMessage: null,
                CoursewareUiStrings.SlideRerenderCanceled);
    }

    private enum PageRerenderCompletionKind
    {
        Succeeded,
        Failed,
        Canceled,
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
