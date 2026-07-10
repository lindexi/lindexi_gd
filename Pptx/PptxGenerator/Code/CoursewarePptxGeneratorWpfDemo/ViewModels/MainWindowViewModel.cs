using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using AgentLib;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Threading;
using PptxGenerator;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.ViewModels;

/// <summary>
/// Provides the data and commands for the main courseware generation workspace.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject
{
    private readonly ISlideChatManagerFactory _slideChatManagerFactory;
    private readonly CoursewareFolderLoader _coursewareFolderLoader;
    private readonly CoursewareSlideSummaryService _slideSummaryService;
    private readonly IViewModelDispatcher _dispatcher;
    private readonly RelayCommand _sendMessageCommand;
    private readonly RelayCommand _rerenderCommand;
    private readonly RelayCommand _connectMcpCommand;
    private CoursewareSlideItem? _selectedSlide;
    private SlideChatManager _slideChatManager;
    private string _inputText = string.Empty;
    private string _editableSlideXml = string.Empty;
    private string _mcpServiceUrl = SlideChatManagerFactory.DefaultMcpServiceUrl;
    private string _mcpStatusText = "本地渲染";
    private bool _isBusy;
    private bool _isConnectingMcp;
    private bool _attachPreview;
    private string _statusText = "尚未加载课件";
    private CoursewareModelDisplayItem? _selectedModelItem;
    private DirectoryInfo? _coursewareFolder;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel" /> class.
    /// </summary>
    public MainWindowViewModel(
        ISlideChatManagerFactory slideChatManagerFactory,
        SlideChatManager slideChatManager,
        CoursewareFolderLoader coursewareFolderLoader,
        CoursewareSlideSummaryService slideSummaryService,
        IViewModelDispatcher? dispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(slideChatManagerFactory);
        ArgumentNullException.ThrowIfNull(slideChatManager);
        ArgumentNullException.ThrowIfNull(coursewareFolderLoader);
        ArgumentNullException.ThrowIfNull(slideSummaryService);

        _slideChatManagerFactory = slideChatManagerFactory;
        _coursewareFolderLoader = coursewareFolderLoader;
        _slideSummaryService = slideSummaryService;
        _dispatcher = dispatcher ?? WpfViewModelDispatcher.Instance;
        _slideChatManager = slideChatManager;
        _slideChatManager.PropertyChanged += OnSlideChatManagerPropertyChanged;

        Slides = new ObservableCollection<CoursewareSlideItem>();
        AvailableModelItems = new ObservableCollection<CoursewareModelDisplayItem>(
            SlideChatManager.AvailableModels.Select(model => new CoursewareModelDisplayItem(
                model.ModelDefinition.Provider,
                model.ModelDefinition.ModelName)));

        var currentModel = _slideChatManager.CurrentModel;
        _selectedModelItem = new CoursewareModelDisplayItem(
            currentModel.ModelDefinition.Provider,
            currentModel.ModelDefinition.ModelName);

        _sendMessageCommand = new RelayCommand(_ => _ = SendMessageAsync(), _ => !IsBusy && !string.IsNullOrWhiteSpace(InputText));
        _rerenderCommand = new RelayCommand(_ => _ = RerenderAsync(), _ => !IsBusy && !string.IsNullOrWhiteSpace(EditableSlideXml));
        _connectMcpCommand = new RelayCommand(_ => _ = ConnectMcpAsync(), _ => !IsBusy && !IsConnectingMcp && !string.IsNullOrWhiteSpace(McpServiceUrl));
        AddPageCommand = new RelayCommand(_ => AddEmptyPage());
        OpenCoursewareFolderCommand = new RelayCommand(parameter => _ = OpenCoursewareFolderAsync(parameter as string), _ => !IsBusy);

        var firstSlide = CreateEmptySlide(1, _slideChatManager);
        Slides.Add(firstSlide);
        SelectedSlide = firstSlide;
    }

    /// <summary>
    /// Gets the SlideML chat manager used by the current page.
    /// </summary>
    public SlideChatManager SlideChatManager => _slideChatManager;

    /// <summary>
    /// Gets the latest rendering log for the current slide.
    /// </summary>
    public string RenderingLog => string.IsNullOrWhiteSpace(SlideChatManager.WarningText)
        ? SelectedSlide?.RenderingLog ?? "尚未执行渲染。"
        : SlideChatManager.WarningText;

    /// <summary>
    /// Gets the latest callback XML for the current slide.
    /// </summary>
    public string CallbackXml => string.IsNullOrWhiteSpace(SlideChatManager.RenderedXml)
        ? SelectedSlide?.CallbackXml ?? string.Empty
        : SlideChatManager.RenderedXml;

    /// <summary>
    /// Gets a value indicating whether the current slide has a rendered preview image.
    /// </summary>
    public bool HasRenderedPreviewImage => SlideChatManager.PreviewImage is not null;

    /// <summary>
    /// Gets a value indicating whether the current slide has a source screenshot.
    /// </summary>
    public bool HasSourceScreenshot => SelectedSlide?.HasSourceScreenshot == true;

    /// <summary>
    /// Gets a value indicating whether the source screenshot should be displayed.
    /// </summary>
    public bool ShowSourceScreenshot => !HasRenderedPreviewImage && HasSourceScreenshot;

    /// <summary>
    /// Gets a value indicating whether any preview image is available.
    /// </summary>
    public bool HasAnyPreviewImage => HasRenderedPreviewImage || HasSourceScreenshot;

    /// <summary>
    /// Gets a value indicating whether the current slide has a rendered preview image.
    /// </summary>
    public bool HasPreviewImage => HasRenderedPreviewImage;

    /// <summary>
    /// Gets the Copilot chat manager used by the current page.
    /// </summary>
    public CopilotChatManager CopilotChatManager => SlideChatManager.Pipeline.ChatManager;

    /// <summary>
    /// Gets the slides displayed in the left navigation area.
    /// </summary>
    public ObservableCollection<CoursewareSlideItem> Slides { get; }

    /// <summary>
    /// Gets the available model options.
    /// </summary>
    public ObservableCollection<CoursewareModelDisplayItem> AvailableModelItems { get; }

    /// <summary>
    /// Gets the image files attached to the next message.
    /// </summary>
    public ObservableCollection<FileInfo> AttachedImageFiles { get; } = new();

    /// <summary>
    /// Gets or sets the current courseware folder.
    /// </summary>
    public DirectoryInfo? CoursewareFolder
    {
        get => _coursewareFolder;
        private set
        {
            if (SetProperty(ref _coursewareFolder, value))
            {
                OnPropertyChanged(nameof(CoursewareFolderDisplayText));
            }
        }
    }

    /// <summary>
    /// Gets the text displayed for the selected courseware folder.
    /// </summary>
    public string CoursewareFolderDisplayText => CoursewareFolder?.FullName ?? "尚未选择课件文件夹";

    /// <summary>
    /// Gets or sets the currently selected slide.
    /// </summary>
    public CoursewareSlideItem? SelectedSlide
    {
        get => _selectedSlide;
        set
        {
            if (!SetProperty(ref _selectedSlide, value))
            {
                return;
            }

            SyncSelectedSlideContext();
        }
    }

    /// <summary>
    /// Gets or sets the current chat input text.
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                _sendMessageCommand.RaiseCanExecuteChanged();
            }
        }
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
                _rerenderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected model.
    /// </summary>
    public CoursewareModelDisplayItem? SelectedModelItem
    {
        get => _selectedModelItem;
        set
        {
            if (SetProperty(ref _selectedModelItem, value) && value is not null)
            {
                SlideChatManager.SetModel(value.ModelName, string.IsNullOrEmpty(value.Provider) ? null : value.Provider);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the current preview should be attached to the next message.
    /// </summary>
    public bool AttachPreview
    {
        get => _attachPreview;
        set => SetProperty(ref _attachPreview, value);
    }

    /// <summary>
    /// Gets or sets the MCP service URL used by the current render pipeline.
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
    /// Gets the MCP connection status text.
    /// </summary>
    public string McpStatusText
    {
        get => _mcpStatusText;
        private set => SetProperty(ref _mcpStatusText, value);
    }

    /// <summary>
    /// Gets a value indicating whether the MCP connection is being established.
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
    /// Gets or sets the current status text.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Gets a value indicating whether an operation is running.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                _sendMessageCommand.RaiseCanExecuteChanged();
                _rerenderCommand.RaiseCanExecuteChanged();
                _connectMcpCommand.RaiseCanExecuteChanged();
                (OpenCoursewareFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether no operation is running.
    /// </summary>
    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Gets the command that sends a chat message.
    /// </summary>
    public ICommand SendMessageCommand => _sendMessageCommand;

    /// <summary>
    /// Gets the command that reloads the current SlideML preview.
    /// </summary>
    public ICommand RerenderCommand => _rerenderCommand;

    /// <summary>
    /// Gets the command that connects the current page render pipeline to MCP.
    /// </summary>
    public ICommand ConnectMcpCommand => _connectMcpCommand;

    /// <summary>
    /// Gets the command that adds an empty page.
    /// </summary>
    public ICommand AddPageCommand { get; }

    /// <summary>
    /// Gets the command that opens a courseware folder.
    /// </summary>
    public ICommand OpenCoursewareFolderCommand { get; }

    /// <summary>
    /// Adds image attachments selected by the view.
    /// </summary>
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

    private async Task ConnectMcpAsync()
    {
        if (string.IsNullOrWhiteSpace(McpServiceUrl))
        {
            return;
        }

        if (SlideChatManager.SlideMlRenderTool.RenderPipeline is not SwitchableSlideMlRenderPipeline renderPipeline)
        {
            McpStatusText = "当前管道不支持 MCP";
            return;
        }

        IsConnectingMcp = true;
        McpStatusText = "正在连接 MCP...";
        var mcpServiceUrl = McpServiceUrl.Trim();

        try
        {
            var isEnabled = await renderPipeline.TryEnableMcpAsync(mcpServiceUrl).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() =>
            {
                McpStatusText = isEnabled ? $"MCP 已连接：{mcpServiceUrl}" : "MCP 连接失败，当前使用本地渲染";
            });
        }
        catch (OperationCanceledException)
        {
            await _dispatcher.InvokeAsync(() => McpStatusText = "MCP 连接已取消");
        }
        finally
        {
            await _dispatcher.InvokeAsync(() => IsConnectingMcp = false);
        }
    }

    /// <summary>
    /// Opens the courseware folder selected by the view.
    /// </summary>
    public void OpenCoursewareFolder(string? folderPath)
    {
        _ = OpenCoursewareFolderAsync(folderPath);
    }

    /// <summary>
    /// Opens the courseware folder selected by the view and waits for the loading workflow to complete.
    /// </summary>
    /// <param name="folderPath">The selected folder path.</param>
    /// <returns>A task that represents the asynchronous loading operation.</returns>
    public async Task OpenCoursewareFolderAsync(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        IsBusy = true;
        StatusText = "正在加载课件导出目录...";

        try
        {
            var package = await _coursewareFolderLoader.LoadAsync(folderPath).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(async () =>
            {
                var slideItems = new List<CoursewareSlideItem>(package.Slides.Count);
                foreach (var slideInput in package.Slides)
                {
                    slideItems.Add(await CreateSlideItemAsync(slideInput).ConfigureAwait(true));
                }

                Slides.Clear();
                foreach (var slideItem in slideItems)
                {
                    Slides.Add(slideItem);
                }

                CoursewareFolder = package.RootDirectory;
                SelectedSlide = Slides.FirstOrDefault();
                StatusText = CreateLoadedStatusText(package);
            });
        }
        catch (InvalidDataException ex)
        {
            await _dispatcher.InvokeAsync(() => StatusText = $"加载课件目录失败：{ex.Message}");
        }
        catch (IOException ex)
        {
            await _dispatcher.InvokeAsync(() => StatusText = $"加载课件目录失败：{ex.Message}");
        }
        catch (JsonException ex)
        {
            await _dispatcher.InvokeAsync(() => StatusText = $"加载课件目录失败：JSON 格式无效。{ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            await _dispatcher.InvokeAsync(() => StatusText = $"加载课件目录失败：没有访问权限。{ex.Message}");
        }
        finally
        {
            await _dispatcher.InvokeAsync(() => IsBusy = false);
        }
    }

    private void OnSlideChatManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SlideChatManager.CurrentSlideXml):
                EditableSlideXml = SlideChatManager.CurrentSlideXml;
                OnPropertyChanged(nameof(SlideChatManager));
                break;
            case nameof(SlideChatManager.PreviewImage):
                OnPropertyChanged(nameof(HasRenderedPreviewImage));
                OnPropertyChanged(nameof(ShowSourceScreenshot));
                OnPropertyChanged(nameof(HasAnyPreviewImage));
                OnPropertyChanged(nameof(HasPreviewImage));
                OnPropertyChanged(nameof(SlideChatManager));
                break;
            case nameof(SlideChatManager.RenderedXml):
                OnPropertyChanged(nameof(CallbackXml));
                OnPropertyChanged(nameof(SlideChatManager));
                break;
            case nameof(SlideChatManager.WarningText):
                OnPropertyChanged(nameof(RenderingLog));
                OnPropertyChanged(nameof(SlideChatManager));
                break;
        }
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            return;
        }

        var message = InputText.Trim();
        InputText = string.Empty;
        IsBusy = true;
        StatusText = "正在处理当前页面...";

        var imageFiles = AttachedImageFiles.Count > 0
            ? AttachedImageFiles.Select(file => file.FullName).ToList()
            : null;
        AttachedImageFiles.Clear();

        try
        {
            await SlideChatManager.SendMessageAsync(
                    message,
                    isFirstMessage: string.IsNullOrWhiteSpace(EditableSlideXml),
                    attachPreview: AttachPreview,
                    attachedImageFiles: imageFiles,
                    useStreaming: true)
                .ConfigureAwait(false);

            await _dispatcher.InvokeAsync(() => StatusText = "处理完成");
        }
        catch (OperationCanceledException)
        {
            StatusText = "操作已取消";
        }
        catch (Exception)
        {
            StatusText = "处理失败";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RerenderAsync()
    {
        if (string.IsNullOrWhiteSpace(EditableSlideXml))
        {
            return;
        }

        IsBusy = true;
        StatusText = "正在重新加载预览...";
        try
        {
            var renderTool = SlideChatManager.SlideMlRenderTool;
            var renderResult = await renderTool.RenderPipeline.RenderAsync(EditableSlideXml).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() =>
            {
                renderTool.ApplyRenderResult(renderResult);
                StatusText = "重新加载完成";
            });
        }
        catch (OperationCanceledException)
        {
            StatusText = "重新加载已取消";
        }
        catch (Exception)
        {
            StatusText = "重新加载失败";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AddEmptyPage()
    {
        _ = AddEmptyPageAsync();
    }

    private void SyncSelectedSlideContext()
    {
        if (SelectedSlide is not null)
        {
            SetCurrentSlideChatManager(SelectedSlide.SlideChatManager);
        }

        EditableSlideXml = SelectedSlide?.SlideMl ?? string.Empty;
        PrepareSelectedSlideCopilotInput();
        StatusText = SelectedSlide?.Status ?? (CoursewareFolder is null ? "尚未加载课件" : "请选择页面");
        OnPropertyChanged(nameof(RenderingLog));
        OnPropertyChanged(nameof(CallbackXml));
        OnPropertyChanged(nameof(HasRenderedPreviewImage));
        OnPropertyChanged(nameof(HasSourceScreenshot));
        OnPropertyChanged(nameof(ShowSourceScreenshot));
        OnPropertyChanged(nameof(HasAnyPreviewImage));
        OnPropertyChanged(nameof(HasPreviewImage));
        RefreshMcpStatusText();
    }

    private async Task AddEmptyPageAsync()
    {
        var pageNumber = Slides.Count + 1;
        var slideChatManager = await _slideChatManagerFactory.CreateAsync().ConfigureAwait(false);
        var slide = CreateEmptySlide(pageNumber, slideChatManager);

        await _dispatcher.InvokeAsync(() =>
        {
            Slides.Add(slide);
            SelectedSlide = slide;
        });
    }

    private async Task<CoursewareSlideItem> CreateSlideItemAsync(CoursewareSlideInput input)
    {
        var (slideChatManager, initializationError) = await CreateSlideChatManagerForSlideAsync().ConfigureAwait(false);
        var status = CreateSlideStatus(input);
        return new CoursewareSlideItem
        {
            SlideChatManager = slideChatManager,
            PageNumber = input.PageNumber,
            SlideId = input.SlideId,
            SlideIndex = input.SlideIndex,
            Width = input.Width,
            Height = input.Height,
            Title = _slideSummaryService.CreateTitle(input.MarkdownText, input.PageNumber),
            Summary = _slideSummaryService.CreateSummary(input.MarkdownText),
            Status = initializationError is null ? status : $"{status}，聊天初始化失败",
            SourceMarkdownFilePath = input.MarkdownFile.FullName,
            SourceMarkdownText = input.MarkdownText,
            SourceScreenshotFilePath = input.ScreenshotFile?.FullName,
            LoadWarnings = input.Warnings,
            ErrorMessage = initializationError,
            SlideMl = string.Empty,
            RenderingLog = initializationError is null ? "尚未执行渲染。" : $"聊天管理器初始化失败：{initializationError}",
            CallbackXml = string.Empty,
        };
    }

    private async Task<(SlideChatManager SlideChatManager, string? InitializationError)> CreateSlideChatManagerForSlideAsync()
    {
        try
        {
            return (await _slideChatManagerFactory.CreateAsync().ConfigureAwait(false), null);
        }
        catch (InvalidOperationException ex)
        {
            return (_slideChatManager, ex.Message);
        }
        catch (IOException ex)
        {
            return (_slideChatManager, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return (_slideChatManager, ex.Message);
        }
    }

    private static string CreateSlideStatus(CoursewareSlideInput input)
    {
        return input.ScreenshotFile is null ? "已加载，缺失截图" : "已加载";
    }

    private static string CreateLoadedStatusText(CoursewareInputPackage package)
    {
        var warningCount = package.Warnings.Count(warning => warning.Level == CoursewareLoadWarningLevel.Warning);
        return warningCount == 0
            ? $"已加载 {package.SlideCount} 页，等待主题分析"
            : $"已加载 {package.SlideCount} 页，存在 {warningCount} 个警告，等待主题分析";
    }

    private void PrepareSelectedSlideCopilotInput()
    {
        AttachedImageFiles.Clear();
        if (SelectedSlide is null || string.IsNullOrWhiteSpace(SelectedSlide.SourceMarkdownText))
        {
            InputText = string.Empty;
            return;
        }

        if (!string.IsNullOrWhiteSpace(SelectedSlide.SourceScreenshotFilePath) && File.Exists(SelectedSlide.SourceScreenshotFilePath))
        {
            AttachedImageFiles.Add(new FileInfo(SelectedSlide.SourceScreenshotFilePath));
        }

        InputText = CreateBeautifyPrompt(SelectedSlide);
    }

    private static string CreateBeautifyPrompt(CoursewareSlideItem slide)
    {
        return $"""
请根据当前课件页面 Markdown 内容美化第 {slide.PageNumber} 页，保持教学语义准确，输出可渲染的 SlideML。

页面 Id：{slide.SlideId}
页面尺寸：{slide.Width:0} x {slide.Height:0}

页面 Markdown：
{slide.SourceMarkdownText}
""";
    }

    private void SetCurrentSlideChatManager(SlideChatManager slideChatManager)
    {
        if (ReferenceEquals(_slideChatManager, slideChatManager))
        {
            return;
        }

        _slideChatManager.PropertyChanged -= OnSlideChatManagerPropertyChanged;
        _slideChatManager = slideChatManager;
        _slideChatManager.PropertyChanged += OnSlideChatManagerPropertyChanged;
        OnPropertyChanged(nameof(SlideChatManager));
        OnPropertyChanged(nameof(CopilotChatManager));
        OnPropertyChanged(nameof(HasRenderedPreviewImage));
        OnPropertyChanged(nameof(ShowSourceScreenshot));
        OnPropertyChanged(nameof(HasAnyPreviewImage));
        OnPropertyChanged(nameof(HasPreviewImage));
        RefreshMcpStatusText();
    }

    private void RefreshMcpStatusText()
    {
        McpStatusText = SlideChatManager.SlideMlRenderTool.RenderPipeline is SwitchableSlideMlRenderPipeline { IsMcpEnabled: true }
            ? "MCP 已连接"
            : "本地渲染";
    }

    private static CoursewareSlideItem CreateEmptySlide(int pageNumber, SlideChatManager slideChatManager)
    {
        return new CoursewareSlideItem
        {
            SlideChatManager = slideChatManager,
            PageNumber = pageNumber,
            SlideId = $"empty-{pageNumber}",
            SlideIndex = pageNumber - 1,
            Width = 1280,
            Height = 720,
            Title = $"未命名页面 {pageNumber}",
            Summary = "尚未加载页面内容。",
            Status = "Empty",
            SourceMarkdownFilePath = string.Empty,
            SourceMarkdownText = string.Empty,
            SlideMl = string.Empty,
            RenderingLog = "尚未执行渲染。",
            CallbackXml = string.Empty,
        };
    }
}
