using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AgentLib;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using PptxGenerator;

namespace CoursewarePptxGeneratorWpfDemo.ViewModels;

/// <summary>
/// Provides the data and commands for the main courseware generation workspace.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject
{
    private readonly SlideChatManagerFactory _slideChatManagerFactory;
    private readonly RelayCommand _sendMessageCommand;
    private readonly RelayCommand _rerenderCommand;
    private CoursewareSlideItem? _selectedSlide;
    private SlideChatManager _slideChatManager;
    private string _inputText = string.Empty;
    private string _editableSlideXml = string.Empty;
    private bool _isBusy;
    private bool _attachPreview;
    private string _statusText = "尚未加载课件";
    private CoursewareModelDisplayItem? _selectedModelItem;
    private DirectoryInfo? _coursewareFolder;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel" /> class.
    /// </summary>
    public MainWindowViewModel(SlideChatManagerFactory slideChatManagerFactory, SlideChatManager slideChatManager)
    {
        ArgumentNullException.ThrowIfNull(slideChatManagerFactory);
        ArgumentNullException.ThrowIfNull(slideChatManager);

        _slideChatManagerFactory = slideChatManagerFactory;
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
        AddPageCommand = new RelayCommand(_ => AddEmptyPage());
        OpenCoursewareFolderCommand = new RelayCommand(parameter => OpenCoursewareFolder(parameter as string));

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
    public bool HasPreviewImage => SlideChatManager.PreviewImage is not null;

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
                _sendMessageCommand.RaiseCanExecuteChanged();
                _rerenderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the command that sends a chat message.
    /// </summary>
    public ICommand SendMessageCommand => _sendMessageCommand;

    /// <summary>
    /// Gets the command that reloads the current SlideML preview.
    /// </summary>
    public ICommand RerenderCommand => _rerenderCommand;

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

    /// <summary>
    /// Opens the courseware folder selected by the view.
    /// </summary>
    public void OpenCoursewareFolder(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        CoursewareFolder = new DirectoryInfo(folderPath);
        StatusText = "已选择课件文件夹";
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

            await Application.Current.Dispatcher.InvokeAsync(() => StatusText = "处理完成");
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
            await Application.Current.Dispatcher.InvokeAsync(() =>
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
        StatusText = SelectedSlide?.Status ?? (CoursewareFolder is null ? "尚未加载课件" : "请选择页面");
        OnPropertyChanged(nameof(RenderingLog));
        OnPropertyChanged(nameof(CallbackXml));
        OnPropertyChanged(nameof(HasPreviewImage));
    }

    private async Task AddEmptyPageAsync()
    {
        var pageNumber = Slides.Count + 1;
        var slideChatManager = await _slideChatManagerFactory.CreateAsync().ConfigureAwait(false);
        var slide = CreateEmptySlide(pageNumber, slideChatManager);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Slides.Add(slide);
            SelectedSlide = slide;
        });
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
        OnPropertyChanged(nameof(HasPreviewImage));
    }

    private static CoursewareSlideItem CreateEmptySlide(int pageNumber, SlideChatManager slideChatManager)
    {
        return new CoursewareSlideItem
        {
            SlideChatManager = slideChatManager,
            PageNumber = pageNumber,
            Title = $"未命名页面 {pageNumber}",
            Summary = "尚未加载页面内容。",
            Status = "Empty",
            SlideMl = string.Empty,
            RenderingLog = "尚未执行渲染。",
            CallbackXml = string.Empty,
        };
    }
}
