using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using AgentLib;
using AgentLib.Model;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Resources;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Threading;
using PptxGenerator;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.ViewModels;

/// <summary>
/// Represents the observable input, lazy runtime, and execution result of one real courseware slide.
/// </summary>
public sealed class CoursewareSlideItemViewModel : ObservableObject, IDisposable
{
    private readonly ISlideChatManagerFactory _slideChatManagerFactory;
    private readonly IViewModelDispatcher _dispatcher;
    private readonly object _runtimeSyncRoot = new();
    private CoursewareSlideRuntime? _runtime;
    private Task<CoursewareSlideRuntime>? _runtimeCreationTask;
    private CancellationTokenSource? _runtimeCreationCancellationTokenSource;
    private CancellationTokenSource? _operationCancellationTokenSource;
    private CoursewareSlideState _state = CoursewareSlideState.NotStarted;
    private CoursewareSlideRuntimeState _runtimeState = CoursewareSlideRuntimeState.NotCreated;
    private CoursewareSlideGenerationState _generationState = CoursewareSlideGenerationState.NotStarted;
    private CoursewareScreenshotAttachmentState _screenshotAttachmentState;
    private string _inputText = string.Empty;
    private string _editableSlideXml = string.Empty;
    private string _renderingLog = CoursewareUiStrings.SlideInitialRenderingLog;
    private string _callbackXml = string.Empty;
    private string? _errorMessage;
    private bool _attachPreview;
    private bool _hasStartedGenerationConversation;
    private bool _hasUnsavedChanges;
    private CoursewareModelDisplayItem? _selectedModelItem;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareSlideItemViewModel" /> class.
    /// </summary>
    /// <param name="input">The loaded slide input.</param>
    /// <param name="title">The deterministic display title.</param>
    /// <param name="summary">The deterministic display summary.</param>
    /// <param name="slideChatManagerFactory">The lazy page runtime factory.</param>
    /// <param name="dispatcher">The dispatcher used for observable state updates.</param>
    public CoursewareSlideItemViewModel(
        CoursewareSlideInput input,
        string title,
        string summary,
        ISlideChatManagerFactory slideChatManagerFactory,
        IViewModelDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("页面标题不能为空。", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("页面摘要不能为空。", nameof(summary));
        }

        ArgumentNullException.ThrowIfNull(slideChatManagerFactory);
        ArgumentNullException.ThrowIfNull(dispatcher);

        Input = input;
        Title = title;
        Summary = summary;
        Canvas = CoursewareCanvasAdapter.CreateCanvas(input);
        DocumentContext = Canvas.DocumentContext;
        _slideChatManagerFactory = slideChatManagerFactory;
        _dispatcher = dispatcher;
        AttachedImageFiles = new ObservableCollection<FileInfo>();
        AvailableModelItems = new ObservableCollection<CoursewareModelDisplayItem>();
        _screenshotAttachmentState = input.ScreenshotFile is null
            ? CoursewareScreenshotAttachmentState.FileMissing
            : CoursewareScreenshotAttachmentState.NotPrepared;
    }

    /// <summary>
    /// Gets the immutable source input for the slide.
    /// </summary>
    public CoursewareSlideInput Input { get; }

    /// <summary>
    /// Gets the single validated canvas adaptation for the page.
    /// </summary>
    public CoursewareSlideCanvas Canvas { get; }

    /// <summary>
    /// Gets the page document context used by prompting, layout, and rendering.
    /// </summary>
    public SlideDocumentContext DocumentContext { get; }

    /// <summary>
    /// Gets the zero-based slide index.
    /// </summary>
    public int SlideIndex => Input.SlideIndex;

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int PageNumber => Input.PageNumber;

    /// <summary>
    /// Gets the stable slide identifier.
    /// </summary>
    public string SlideId => Input.SlideId;

    /// <summary>
    /// Gets the actual page canvas width.
    /// </summary>
    public int CanvasWidth => DocumentContext.CanvasWidth;

    /// <summary>
    /// Gets the actual page canvas height.
    /// </summary>
    public int CanvasHeight => DocumentContext.CanvasHeight;

    /// <summary>
    /// Gets the user-facing converted canvas size.
    /// </summary>
    public string CanvasSizeText => $"{CanvasWidth} × {CanvasHeight} px";

    /// <summary>
    /// Gets the original logical page width.
    /// </summary>
    public double LogicalWidth => Canvas.LogicalWidth;

    /// <summary>
    /// Gets the original logical page height.
    /// </summary>
    public double LogicalHeight => Canvas.LogicalHeight;

    /// <summary>
    /// Gets the deterministic display title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the deterministic display summary.
    /// </summary>
    public string Summary { get; }

    /// <summary>
    /// Gets the complete source Markdown loaded for this slide.
    /// </summary>
    public string SourceMarkdownText => Input.MarkdownText;

    /// <summary>
    /// Gets the source screenshot path when available.
    /// </summary>
    public string? SourceScreenshotFilePath => Input.ScreenshotFile?.FullName;

    /// <summary>
    /// Gets a value indicating whether the source screenshot is available.
    /// </summary>
    public bool HasSourceScreenshot => Input.ScreenshotFile is not null;

    /// <summary>
    /// Gets the page-scoped load warnings.
    /// </summary>
    public IReadOnlyList<CoursewareLoadWarning> Warnings => Input.Warnings;

    /// <summary>
    /// Gets canvas conversion diagnostics that apply to this page.
    /// </summary>
    public IReadOnlyList<string> CanvasDiagnostics => Canvas.Diagnostics;

    /// <summary>
    /// Gets a value indicating whether canvas conversion produced diagnostics.
    /// </summary>
    public bool HasCanvasDiagnostics => CanvasDiagnostics.Count > 0;

    /// <summary>
    /// Gets the combined page input and canvas diagnostics.
    /// </summary>
    public string InputDiagnosticText
    {
        get
        {
            var diagnostics = Warnings
                .Select(warning => $"{warning.Code}: {warning.Message}")
                .Concat(CanvasDiagnostics)
                .ToArray();
            return diagnostics.Length == 0
                ? "未报告输入或画布诊断。"
                : string.Join(Environment.NewLine, diagnostics);
        }
    }

    /// <summary>
    /// Gets a compact warning summary for tooltips.
    /// </summary>
    public string WarningSummary => Warnings.Count == 0
        ? "当前页面没有输入警告。"
        : string.Join(Environment.NewLine, Warnings.Select(warning => warning.Message));

    /// <summary>
    /// Gets a value indicating whether the source slide has load warnings.
    /// </summary>
    public bool HasWarning => Warnings.Count > 0;

    /// <summary>
    /// Gets the accessible slide label.
    /// </summary>
    public string AccessibleName => Warnings.Count == 0
        ? $"第 {PageNumber} 页，{Title}"
        : $"第 {PageNumber} 页，{Title}，存在输入警告";

    /// <summary>
    /// Gets the short page number label.
    /// </summary>
    public string NumberText => PageNumber.ToString("00");

    /// <summary>
    /// Gets or sets the current page execution state.
    /// </summary>
    public CoursewareSlideState State
    {
        get => _state;
        internal set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    /// <summary>
    /// Gets or sets the page runtime lifecycle state.
    /// </summary>
    public CoursewareSlideRuntimeState RuntimeState
    {
        get => _runtimeState;
        internal set
        {
            if (SetProperty(ref _runtimeState, value))
            {
                OnPropertyChanged(nameof(RuntimeStatusText));
            }
        }
    }

    /// <summary>
    /// Gets or sets the latest page generation or rendering state.
    /// </summary>
    public CoursewareSlideGenerationState GenerationState
    {
        get => _generationState;
        internal set => SetProperty(ref _generationState, value);
    }

    /// <summary>
    /// Gets the localized runtime availability text.
    /// </summary>
    public string RuntimeStatusText => RuntimeState switch
    {
        CoursewareSlideRuntimeState.NotCreated => CoursewareUiStrings.SlideRuntimeNotCreated,
        CoursewareSlideRuntimeState.Creating => CoursewareUiStrings.SlideRuntimeCreating,
        CoursewareSlideRuntimeState.Ready => CoursewareUiStrings.SlideRuntimeReady,
        CoursewareSlideRuntimeState.ModelUnavailable => CoursewareUiStrings.SlideRuntimeModelUnavailable,
        CoursewareSlideRuntimeState.Failed => CoursewareUiStrings.SlideRuntimeFailed,
        CoursewareSlideRuntimeState.Canceled => CoursewareUiStrings.SlideRuntimeCanceled,
        _ => throw new ArgumentOutOfRangeException(),
    };

    /// <summary>
    /// Gets a value indicating whether the page is initializing, generating, or rendering.
    /// </summary>
    public bool IsBusy => State is CoursewareSlideState.Initializing or CoursewareSlideState.Generating or CoursewareSlideState.Rendering;

    /// <summary>
    /// Gets the localized page status text.
    /// </summary>
    public string StatusText => State switch
    {
        CoursewareSlideState.NotStarted => CoursewareUiStrings.SlideStatusNotStarted,
        CoursewareSlideState.Initializing => CoursewareUiStrings.SlideStatusInitializing,
        CoursewareSlideState.Ready => CoursewareUiStrings.SlideStatusReady,
        CoursewareSlideState.Generating => CoursewareUiStrings.SlideStatusGenerating,
        CoursewareSlideState.Rendering => CoursewareUiStrings.SlideStatusRendering,
        CoursewareSlideState.Completed => CoursewareUiStrings.SlideStatusCompleted,
        CoursewareSlideState.Failed => CoursewareUiStrings.SlideStatusFailed,
        CoursewareSlideState.Canceled => CoursewareUiStrings.SlideStatusCanceled,
        _ => throw new ArgumentOutOfRangeException(),
    };

    /// <summary>
    /// Gets the lazily created runtime when initialized.
    /// </summary>
    public CoursewareSlideRuntime? Runtime => _runtime;

    /// <summary>
    /// Gets the page SlideML chat manager when initialized.
    /// </summary>
    public SlideChatManager? SlideChatManager => _runtime?.SlideChatManager;

    /// <summary>
    /// Gets the page Copilot chat manager when initialized.
    /// </summary>
    public CopilotChatManager? CopilotChatManager => SlideChatManager?.Pipeline.ChatManager;

    /// <summary>
    /// Gets a value indicating whether language-model generation is available for this page.
    /// </summary>
    public bool IsAiGenerationAvailable => _runtime?.IsAiGenerationAvailable == true;

    /// <summary>
    /// Gets the available model options after runtime initialization.
    /// </summary>
    public ObservableCollection<CoursewareModelDisplayItem> AvailableModelItems { get; }

    /// <summary>
    /// Gets or sets the selected model option.
    /// </summary>
    public CoursewareModelDisplayItem? SelectedModelItem
    {
        get => _selectedModelItem;
        set
        {
            if (ReferenceEquals(_selectedModelItem, value))
            {
                return;
            }

            if (value is not null && (!IsAiGenerationAvailable || SlideChatManager is null
                || !SlideChatManager.SetModel(value.ModelName, string.IsNullOrEmpty(value.Provider) ? null : value.Provider)))
            {
                return;
            }

            SetProperty(ref _selectedModelItem, value);
        }
    }

    /// <summary>
    /// Gets image files attached to the next page message.
    /// </summary>
    public ObservableCollection<FileInfo> AttachedImageFiles { get; }

    /// <summary>
    /// Gets or sets the source screenshot attachment state for the initial request.
    /// </summary>
    public CoursewareScreenshotAttachmentState ScreenshotAttachmentState
    {
        get => _screenshotAttachmentState;
        internal set
        {
            if (SetProperty(ref _screenshotAttachmentState, value))
            {
                OnPropertyChanged(nameof(ScreenshotAttachmentStatusText));
            }
        }
    }

    /// <summary>
    /// Gets the user-facing source screenshot attachment status.
    /// </summary>
    public string ScreenshotAttachmentStatusText => ScreenshotAttachmentState switch
    {
        CoursewareScreenshotAttachmentState.NotPrepared => CoursewareUiStrings.ScreenshotNotPrepared,
        CoursewareScreenshotAttachmentState.Attached => CoursewareUiStrings.ScreenshotAttached,
        CoursewareScreenshotAttachmentState.FileMissing => CoursewareUiStrings.ScreenshotFileMissing,
        CoursewareScreenshotAttachmentState.SendFailed => CoursewareUiStrings.ScreenshotSendFailed,
        _ => throw new ArgumentOutOfRangeException(),
    };

    /// <summary>
    /// Gets or sets the current page chat input.
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    /// <summary>
    /// Gets or sets whether the current rendered preview should be attached to the next message.
    /// </summary>
    public bool AttachPreview
    {
        get => _attachPreview;
        set => SetProperty(ref _attachPreview, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the page has sent its initial structured generation request.
    /// </summary>
    public bool HasStartedGenerationConversation
    {
        get => _hasStartedGenerationConversation;
        internal set => SetProperty(ref _hasStartedGenerationConversation, value);
    }

    /// <summary>
    /// Gets or sets the editable SlideML XML.
    /// </summary>
    public string EditableSlideXml
    {
        get => _editableSlideXml;
        set
        {
            if (SetProperty(ref _editableSlideXml, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the editable SlideML differs from the latest applied result.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        internal set => SetProperty(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Replaces editable SlideML with a pipeline result without marking it as a user edit.
    /// </summary>
    /// <param name="slideXml">The latest pipeline SlideML.</param>
    internal void ApplyGeneratedSlideXml(string slideXml)
    {
        if (SetProperty(ref _editableSlideXml, slideXml, nameof(EditableSlideXml)))
        {
            HasUnsavedChanges = false;
        }
    }

    /// <summary>
    /// Gets the latest rendering log.
    /// </summary>
    public string RenderingLog
    {
        get => _renderingLog;
        internal set => SetProperty(ref _renderingLog, value);
    }

    /// <summary>
    /// Gets the latest callback XML.
    /// </summary>
    public string CallbackXml
    {
        get => _callbackXml;
        internal set => SetProperty(ref _callbackXml, value);
    }

    /// <summary>
    /// Gets the current page error message.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        internal set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current page exposes an operation error.
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// Gets the latest rendered preview image.
    /// </summary>
    public IPreviewImage? PreviewImage => SlideChatManager?.PreviewImage;

    /// <summary>
    /// Gets a value indicating whether a rendered preview image is available.
    /// </summary>
    public bool HasRenderedPreviewImage => PreviewImage is not null;

    /// <summary>
    /// Gets a value indicating whether the source screenshot should be displayed.
    /// </summary>
    public bool ShowSourceScreenshot => !HasRenderedPreviewImage && HasSourceScreenshot;

    /// <summary>
    /// Gets a value indicating whether any preview image is available.
    /// </summary>
    public bool HasAnyPreviewImage => HasRenderedPreviewImage || HasSourceScreenshot;

    /// <summary>
    /// Gets or creates the independent page runtime. Concurrent calls share one creation task.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel runtime initialization.</param>
    /// <returns>The independent page runtime.</returns>
    public Task<CoursewareSlideRuntime> EnsureRuntimeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        Task<CoursewareSlideRuntime> runtimeCreationTask;
        lock (_runtimeSyncRoot)
        {
            if (_runtime is not null)
            {
                return Task.FromResult(_runtime);
            }

            if (_runtimeCreationTask is null)
            {
                _runtimeCreationCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _runtimeCreationTask = CreateRuntimeAsync(_runtimeCreationCancellationTokenSource.Token);
            }

            runtimeCreationTask = _runtimeCreationTask;
        }

        return runtimeCreationTask;
    }

    /// <summary>
    /// Tries to start one page operation with a token linked to the workspace lifetime.
    /// </summary>
    /// <param name="workspaceCancellationToken">The workspace lifetime token.</param>
    /// <param name="operationCancellationToken">The page operation cancellation token when the operation starts.</param>
    /// <returns><see langword="true" /> when no other operation is active for this page.</returns>
    public bool TryBeginOperation(
        CancellationToken workspaceCancellationToken,
        out CancellationToken operationCancellationToken)
    {
        ThrowIfDisposed();
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(workspaceCancellationToken);
        if (Interlocked.CompareExchange(
                ref _operationCancellationTokenSource,
                cancellationTokenSource,
                null) is not null)
        {
            cancellationTokenSource.Dispose();
            operationCancellationToken = default;
            return false;
        }

        operationCancellationToken = cancellationTokenSource.Token;
        return true;
    }

    /// <summary>
    /// Cancels the active page operation and chat stream.
    /// </summary>
    public void CancelActiveOperation()
    {
        _operationCancellationTokenSource?.Cancel();
        if (RuntimeState == CoursewareSlideRuntimeState.Creating)
        {
            lock (_runtimeSyncRoot)
            {
                _runtimeCreationCancellationTokenSource?.Cancel();
            }
        }

        CopilotChatManager?.CancelCurrentChat();
    }

    /// <summary>
    /// Clears and disposes the active page operation token source when it belongs to the completed operation.
    /// </summary>
    /// <param name="cancellationToken">The completed operation token.</param>
    public void CompleteOperation(CancellationToken cancellationToken)
    {
        var cancellationTokenSource = _operationCancellationTokenSource;
        if (cancellationTokenSource is null)
        {
            return;
        }

        try
        {
            if (cancellationTokenSource.Token != cancellationToken)
            {
                return;
            }
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        if (ReferenceEquals(
                Interlocked.CompareExchange(ref _operationCancellationTokenSource, null, cancellationTokenSource),
                cancellationTokenSource))
        {
            cancellationTokenSource.Dispose();
        }
    }

    /// <summary>
    /// Adds valid image attachments selected for the next page message.
    /// </summary>
    /// <param name="filePaths">The selected local image paths.</param>
    public void AddAttachedImageFiles(IEnumerable<string> filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        foreach (var filePath in filePaths)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                AttachedImageFiles.Add(new FileInfo(filePath));
            }
        }
    }

    /// <summary>
    /// Releases page-scoped cancellation and event subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        CancelActiveOperation();
        _operationCancellationTokenSource?.Dispose();
        _operationCancellationTokenSource = null;
        lock (_runtimeSyncRoot)
        {
            _runtimeCreationCancellationTokenSource?.Cancel();
            _runtimeCreationCancellationTokenSource?.Dispose();
            _runtimeCreationCancellationTokenSource = null;
        }

        if (_runtime is not null)
        {
            _runtime.SlideChatManager.PropertyChanged -= OnSlideChatManagerPropertyChanged;
        }
    }

    private async Task<CoursewareSlideRuntime> CreateRuntimeAsync(CancellationToken cancellationToken)
    {
        await InvokeIfNotDisposedAsync(() =>
        {
            RuntimeState = CoursewareSlideRuntimeState.Creating;
            State = CoursewareSlideState.Initializing;
        });
        var options = new SlideChatManagerFactoryOptions(DocumentContext)
        {
            TryEnableDefaultMcp = false,
        };
        CoursewareSlideRuntime runtime;
        try
        {
            var slideChatManager = await _slideChatManagerFactory.CreateAsync(options, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            runtime = new CoursewareSlideRuntime(slideChatManager, isAiGenerationAvailable: true);
        }
        catch (OperationCanceledException)
        {
            await InvokeIfNotDisposedAsync(() =>
            {
                RuntimeState = CoursewareSlideRuntimeState.Canceled;
                State = CoursewareSlideState.Canceled;
            });
            lock (_runtimeSyncRoot)
            {
                _runtimeCreationTask = null;
                _runtimeCreationCancellationTokenSource?.Dispose();
                _runtimeCreationCancellationTokenSource = null;
            }

            throw;
        }
        catch (Exception ex)
        {
            try
            {
                runtime = new CoursewareSlideRuntime(
                    _slideChatManagerFactory.CreateFallback(options),
                    isAiGenerationAvailable: false,
                    initializationError: ex.Message);
            }
            catch (Exception fallbackException)
            {
                lock (_runtimeSyncRoot)
                {
                    _runtimeCreationTask = null;
                    _runtimeCreationCancellationTokenSource?.Dispose();
                    _runtimeCreationCancellationTokenSource = null;
                }

                await InvokeIfNotDisposedAsync(() =>
                {
                    RuntimeState = CoursewareSlideRuntimeState.Failed;
                    ErrorMessage = fallbackException.Message;
                    RenderingLog = fallbackException.ToString();
                    State = CoursewareSlideState.Failed;
                });
                throw new AggregateException("页面运行时和本地渲染 fallback 均初始化失败。", ex, fallbackException);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        await InvokeIfNotDisposedAsync(() => AttachRuntime(runtime));
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        lock (_runtimeSyncRoot)
        {
            _runtimeCreationCancellationTokenSource?.Dispose();
            _runtimeCreationCancellationTokenSource = null;
        }

        return runtime;
    }

    private void AttachRuntime(CoursewareSlideRuntime runtime)
    {
        _runtime = runtime;
        runtime.SlideChatManager.PropertyChanged += OnSlideChatManagerPropertyChanged;
        AvailableModelItems.Clear();
        if (runtime.IsAiGenerationAvailable)
        {
            foreach (var model in runtime.SlideChatManager.AvailableModels)
            {
                AvailableModelItems.Add(new CoursewareModelDisplayItem(
                    model.ModelDefinition.Provider,
                    model.ModelDefinition.ModelName));
            }

            var currentModel = runtime.SlideChatManager.CurrentModel;
            _selectedModelItem = AvailableModelItems.FirstOrDefault(item =>
                string.Equals(item.Provider, currentModel.ModelDefinition.Provider, StringComparison.Ordinal)
                && string.Equals(item.ModelName, currentModel.ModelDefinition.ModelName, StringComparison.Ordinal));
        }
        else
        {
            _selectedModelItem = null;
        }

        RuntimeState = runtime.IsAiGenerationAvailable
            ? CoursewareSlideRuntimeState.Ready
            : CoursewareSlideRuntimeState.ModelUnavailable;
        State = CoursewareSlideState.Ready;
        ErrorMessage = runtime.InitializationError;
        RenderingLog = runtime.InitializationError is null
            ? "页面运行时已就绪，等待生成。"
            : $"语言模型初始化失败，已保留本地重新渲染：{runtime.InitializationError}";
        OnPropertyChanged(nameof(Runtime));
        OnPropertyChanged(nameof(SlideChatManager));
        OnPropertyChanged(nameof(CopilotChatManager));
        OnPropertyChanged(nameof(IsAiGenerationAvailable));
        OnPropertyChanged(nameof(SelectedModelItem));
    }

    private void OnSlideChatManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        _ = InvokeIfNotDisposedAsync(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(SlideChatManager.CurrentSlideXml):
                    if (!HasUnsavedChanges)
                    {
                        ApplyGeneratedSlideXml(SlideChatManager?.CurrentSlideXml ?? string.Empty);
                    }

                    break;
                case nameof(SlideChatManager.PreviewImage):
                    OnPropertyChanged(nameof(PreviewImage));
                    OnPropertyChanged(nameof(HasRenderedPreviewImage));
                    OnPropertyChanged(nameof(ShowSourceScreenshot));
                    OnPropertyChanged(nameof(HasAnyPreviewImage));
                    break;
                case nameof(SlideChatManager.RenderedXml):
                    CallbackXml = SlideChatManager?.RenderedXml ?? string.Empty;
                    break;
                case nameof(SlideChatManager.WarningText):
                    RenderingLog = string.IsNullOrWhiteSpace(SlideChatManager?.WarningText)
                        ? "渲染完成，未报告警告。"
                        : SlideChatManager.WarningText;
                    break;
            }
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

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(CoursewareSlideItemViewModel));
        }
    }
}
