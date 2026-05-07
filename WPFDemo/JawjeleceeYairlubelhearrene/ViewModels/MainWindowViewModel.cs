using JawjeleceeYairlubelhearrene.Properties;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JawjeleceeYairlubelhearrene;

internal sealed class MainWindowViewModel : ObservableObject
{
    private readonly PowerPointReader _powerPointReader = new();
    private readonly CoursewareSpeechVideoGenerator _coursewareSpeechVideoGenerator = new();
    private readonly AsyncRelayCommand _browsePptCommand;
    private readonly RelayCommand _browseFfmpegCommand;
    private readonly AsyncRelayCommand _generateCommand;
    private readonly RelayCommand _openOutputFolderCommand;
    private string _pptFilePath = string.Empty;
    private string _openSpeechApiKey = string.Empty;
    private string _resourceId = string.Empty;
    private SpeakerOption? _selectedSpeaker;
    private string _openAiApiKey = string.Empty;
    private string _openAiEndpoint = string.Empty;
    private string _openAiModel = string.Empty;
    private string _ffmpegExecutablePath = string.Empty;
    private string _outputDirectoryPath = string.Empty;
    private string _statusMessage = Resources.StatusReady;
    private string _logText = string.Empty;
    private string _generatedVideoPath = string.Empty;
    private bool _isBusy;
    private PowerPointReadResult? _currentReadResult;

    public MainWindowViewModel()
    {
        SpeakerOptions = new ObservableCollection<SpeakerOption>(SpeakerCatalogService.LoadSpeakerOptions());
        Slides = new ObservableCollection<SlidePreviewViewModel>();
        _browsePptCommand = new AsyncRelayCommand(BrowsePptAsync, () => !IsBusy);
        _browseFfmpegCommand = new RelayCommand(BrowseFfmpeg, () => !IsBusy);
        _generateCommand = new AsyncRelayCommand(GenerateAsync, CanGenerate);
        _openOutputFolderCommand = new RelayCommand(OpenOutputFolder, CanOpenOutputFolder);

        LoadDefaults();
    }

    public ObservableCollection<SpeakerOption> SpeakerOptions { get; }

    public ObservableCollection<SlidePreviewViewModel> Slides { get; }

    public AsyncRelayCommand BrowsePptCommand => _browsePptCommand;

    public RelayCommand BrowseFfmpegCommand => _browseFfmpegCommand;

    public AsyncRelayCommand GenerateCommand => _generateCommand;

    public RelayCommand OpenOutputFolderCommand => _openOutputFolderCommand;

    public string PptFilePath
    {
        get => _pptFilePath;
        set
        {
            if (SetProperty(ref _pptFilePath, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public bool HasSlides => Slides.Count > 0;

    public string OpenSpeechApiKey
    {
        get => _openSpeechApiKey;
        set
        {
            if (SetProperty(ref _openSpeechApiKey, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string ResourceId
    {
        get => _resourceId;
        set
        {
            if (SetProperty(ref _resourceId, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public SpeakerOption? SelectedSpeaker
    {
        get => _selectedSpeaker;
        set
        {
            if (SetProperty(ref _selectedSpeaker, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string OpenAiApiKey
    {
        get => _openAiApiKey;
        set
        {
            if (SetProperty(ref _openAiApiKey, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string OpenAiEndpoint
    {
        get => _openAiEndpoint;
        set
        {
            if (SetProperty(ref _openAiEndpoint, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string OpenAiModel
    {
        get => _openAiModel;
        set
        {
            if (SetProperty(ref _openAiModel, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string FfmpegExecutablePath
    {
        get => _ffmpegExecutablePath;
        set
        {
            if (SetProperty(ref _ffmpegExecutablePath, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string OutputDirectoryPath
    {
        get => _outputDirectoryPath;
        set
        {
            if (SetProperty(ref _outputDirectoryPath, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
    }

    public string GeneratedVideoPath
    {
        get => _generatedVideoPath;
        private set
        {
            if (SetProperty(ref _generatedVideoPath, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public async Task HandleDroppedFileAsync(string filePath)
    {
        if (!IsSupportedPowerPointFile(filePath))
        {
            StatusMessage = Resources.DroppedFileInvalid;
            return;
        }

        PptFilePath = filePath;
        await LoadPresentationAsync();
    }

    public void PlayVideoRequested()
    {
        if (!string.IsNullOrWhiteSpace(GeneratedVideoPath))
        {
            StatusMessage = string.Format(Resources.GenerationCompletedFormat, GeneratedVideoPath);
        }
    }

    private void LoadDefaults()
    {
        var defaults = LocalDefaultsProvider.Load();
        OpenSpeechApiKey = defaults.OpenSpeechApiKey;
        ResourceId = defaults.ResourceId;
        OpenAiApiKey = defaults.OpenAiApiKey;
        OpenAiEndpoint = defaults.OpenAiEndpoint;
        OpenAiModel = defaults.OpenAiModel;
        FfmpegExecutablePath = defaults.FfmpegExecutablePath;
        OutputDirectoryPath = defaults.OutputDirectoryPath;
        SelectedSpeaker = SpeakerOptions.FirstOrDefault(t => string.Equals(t.VoiceType, defaults.Speaker, StringComparison.Ordinal))
            ?? SpeakerOptions.FirstOrDefault();
    }

    private async Task BrowsePptAsync()
    {
        var filePath = FileDialogService.SelectPowerPointFile();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        PptFilePath = filePath;
        await LoadPresentationAsync();
    }

    private void BrowseFfmpeg()
    {
        var filePath = FileDialogService.SelectExecutableFile();
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            FfmpegExecutablePath = filePath;
        }
    }

    private async Task GenerateAsync()
    {
        var validationMessage = ValidateBeforeGenerate();
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            StatusMessage = validationMessage;
            return;
        }

        try
        {
            IsBusy = true;
            AppendLog(StatusMessage);

            if (_currentReadResult is null || !string.Equals(_currentReadResult.SourceFile.FullName, PptFilePath, StringComparison.OrdinalIgnoreCase))
            {
                await LoadPresentationAsync();
            }

            if (_currentReadResult is null)
            {
                throw new InvalidOperationException(Resources.ValidationSelectPpt);
            }

            var outputDirectory = new System.IO.DirectoryInfo(System.IO.Path.Combine(OutputDirectoryPath, DateTime.Now.ToString("yyyyMMdd_HHmmss")));
            outputDirectory.Create();

            var generationOptions = new SpeechVideoGenerationOptions(
                new System.IO.FileInfo(FfmpegExecutablePath),
                OpenSpeechApiKey,
                ResourceId,
                SelectedSpeaker!.VoiceType,
                OpenAiApiKey,
                new Uri(OpenAiEndpoint, UriKind.Absolute),
                OpenAiModel,
                outputDirectory);

            var coursewareMaterialInfo = new CoursewareMaterialInfo(_currentReadResult.Slides.Select(t => new CoursewareSlideMaterialInfo(t.SlideImageFile, t.SlideText)).ToArray());
            var progress = new Progress<string>(message =>
            {
                StatusMessage = message;
                AppendLog(message);
            });

            var result = await _coursewareSpeechVideoGenerator.GenerateAsync(coursewareMaterialInfo, generationOptions, progress, CancellationToken.None);
            GeneratedVideoPath = result.OutputVideoFile.FullName;
            for (var i = 0; i < Math.Min(Slides.Count, result.CoursewareSpeechInfo.SlideInfoList.Count); i++)
            {
                Slides[i].GeneratedScript = result.CoursewareSpeechInfo.SlideInfoList[i].PlainScriptText;
            }

            StatusMessage = string.Format(Resources.GenerationCompletedFormat, result.OutputVideoFile.FullName);
            AppendLog(StatusMessage);
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
            AppendLog(exception.ToString());
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadPresentationAsync()
    {
        if (!File.Exists(PptFilePath))
        {
            return;
        }

        try
        {
            IsBusy = true;
            Slides.Clear();
            OnPropertyChanged(nameof(HasSlides));
            GeneratedVideoPath = string.Empty;
            var progress = new Progress<string>(message =>
            {
                StatusMessage = message;
                AppendLog(message);
            });
            var readResult = await _powerPointReader.ReadSlidesAsync(new System.IO.FileInfo(PptFilePath), progress, CancellationToken.None);
            _currentReadResult = readResult;

            foreach (var slide in readResult.Slides)
            {
                Slides.Add(new SlidePreviewViewModel(slide.SlideIndex, slide.SlideText, slide.SlideImageFile.FullName));
            }

            OnPropertyChanged(nameof(HasSlides));

            StatusMessage = $"已读取 {Slides.Count} 页内容。";
            AppendLog(StatusMessage);
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
            AppendLog(exception.ToString());
            Slides.Clear();
            _currentReadResult = null;
            OnPropertyChanged(nameof(HasSlides));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string ValidateBeforeGenerate()
    {
        if (!File.Exists(PptFilePath))
        {
            return Resources.ValidationSelectPpt;
        }

        if (string.IsNullOrWhiteSpace(OpenSpeechApiKey))
        {
            return Resources.ValidationOpenSpeechApiKey;
        }

        if (string.IsNullOrWhiteSpace(ResourceId))
        {
            return Resources.ValidationResourceId;
        }

        if (SelectedSpeaker is null)
        {
            return Resources.ValidationSpeaker;
        }

        if (string.IsNullOrWhiteSpace(OpenAiApiKey))
        {
            return Resources.ValidationOpenAiApiKey;
        }

        if (!Uri.TryCreate(OpenAiEndpoint, UriKind.Absolute, out _))
        {
            return Resources.ValidationOpenAiEndpoint;
        }

        if (string.IsNullOrWhiteSpace(OpenAiModel))
        {
            return Resources.ValidationOpenAiModel;
        }

        if (!File.Exists(FfmpegExecutablePath))
        {
            return Resources.ValidationFfmpegPath;
        }

        if (string.IsNullOrWhiteSpace(OutputDirectoryPath))
        {
            return Resources.ValidationOutputDirectory;
        }

        return string.Empty;
    }

    private bool CanGenerate()
    {
        return !IsBusy;
    }

    private bool CanOpenOutputFolder()
    {
        return !string.IsNullOrWhiteSpace(GeneratedVideoPath) && File.Exists(GeneratedVideoPath);
    }

    private void OpenOutputFolder()
    {
        if (!CanOpenOutputFolder())
        {
            return;
        }

        var directory = Path.GetDirectoryName(GeneratedVideoPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{directory}\"",
            UseShellExecute = true
        });
    }

    private void AppendLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        LogText = string.IsNullOrWhiteSpace(LogText)
            ? $"[{DateTime.Now:HH:mm:ss}] {message}"
            : LogText + Environment.NewLine + $"[{DateTime.Now:HH:mm:ss}] {message}";
    }

    private void UpdateCommandStates()
    {
        _browsePptCommand.RaiseCanExecuteChanged();
        _browseFfmpegCommand.RaiseCanExecuteChanged();
        _generateCommand.RaiseCanExecuteChanged();
        _openOutputFolderCommand.RaiseCanExecuteChanged();
    }

    private static bool IsSupportedPowerPointFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        return extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".pptm", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".ppsx", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".ppsm", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".potx", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".potm", StringComparison.OrdinalIgnoreCase);
    }
}
