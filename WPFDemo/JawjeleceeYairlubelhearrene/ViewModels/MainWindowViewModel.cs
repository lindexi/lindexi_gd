using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using JawjeleceeYairlubelhearrene.Infrastructure;
using JawjeleceeYairlubelhearrene.Models;
using JawjeleceeYairlubelhearrene.Properties;
using JawjeleceeYairlubelhearrene.Services;

namespace JawjeleceeYairlubelhearrene.ViewModels;

internal sealed class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel()
    {
        SpeakerOptions = new ObservableCollection<SpeakerOption>(SpeakerCatalogService.LoadSpeakerOptions());
        Slides = new ObservableCollection<SlidePreviewViewModel>();
        _browsePptCommand = new AsyncRelayCommand(BrowsePptAsync, () => !IsBusy);
        _browseFfmpegCommand = new RelayCommand(BrowseFfmpeg, () => !IsBusy);
        _applyWatermarkCommand = new AsyncRelayCommand(ApplyWatermarkAsync, CanApplyWatermark);
        _generateScriptsCommand = new AsyncRelayCommand(GenerateScriptsAsync, CanGenerateScripts);
        _generateVideoFromScriptsCommand = new AsyncRelayCommand(GenerateVideoFromScriptsAsync, CanGenerateVideoFromScripts);
        _generateCommand = new AsyncRelayCommand(GenerateAsync, CanGenerate);
        _openOutputFolderCommand = new RelayCommand(OpenOutputFolder, CanOpenOutputFolder);

        LoadDefaults();
    }

    private readonly PowerPointReader _powerPointReader = new();

    private readonly SlideImageWatermarkService _slideImageWatermarkService = new();

    private readonly CoursewareSpeechVideoGenerator _coursewareSpeechVideoGenerator = new();

    private readonly AsyncRelayCommand _browsePptCommand;

    private readonly RelayCommand _browseFfmpegCommand;

    private readonly AsyncRelayCommand _applyWatermarkCommand;

    private readonly AsyncRelayCommand _generateScriptsCommand;

    private readonly AsyncRelayCommand _generateVideoFromScriptsCommand;

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

    private bool _enableWatermark;

    private string _watermarkText = string.Empty;

    private bool _hasPendingWatermarkChanges;

    private string _outputDirectoryPath = string.Empty;

    private string _statusMessage = Resources.StatusReady;

    private string _logText = string.Empty;

    private string _generatedVideoPath = string.Empty;

    private bool _isBusy;

    private PowerPointReadResult? _currentReadResult;

    private CoursewareSpeechInfo? _currentCoursewareSpeechInfo;

    private string _lastGenerationOutputDirectoryPath = string.Empty;

    private System.IO.DirectoryInfo? _currentWorkingDirectory;

    public ObservableCollection<SpeakerOption> SpeakerOptions { get; }

    public ObservableCollection<SlidePreviewViewModel> Slides { get; }

    public AsyncRelayCommand BrowsePptCommand => _browsePptCommand;

    public RelayCommand BrowseFfmpegCommand => _browseFfmpegCommand;

    public AsyncRelayCommand ApplyWatermarkCommand => _applyWatermarkCommand;

    public AsyncRelayCommand GenerateCommand => _generateCommand;

    public AsyncRelayCommand GenerateScriptsCommand => _generateScriptsCommand;

    public AsyncRelayCommand GenerateVideoFromScriptsCommand => _generateVideoFromScriptsCommand;

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

    public bool EnableWatermark
    {
        get => _enableWatermark;
        set
        {
            if (SetProperty(ref _enableWatermark, value))
            {
                UpdateCommandStates();
                _hasPendingWatermarkChanges = true;
            }
        }
    }

    public string WatermarkText
    {
        get => _watermarkText;
        set
        {
            if (SetProperty(ref _watermarkText, value))
            {
                UpdateCommandStates();
                _hasPendingWatermarkChanges = true;
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
        LocalDefaultValues defaults = LocalDefaultsProvider.Load();
        _localDefaultValues = defaults;
        //OpenSpeechApiKey = defaults.OpenSpeechApiKey;
        ResourceId = defaults.ResourceId;
        //OpenAiApiKey = defaults.OpenAiApiKey;
        OpenAiEndpoint = defaults.OpenAiEndpoint;
        OpenAiModel = defaults.OpenAiModel;
        FfmpegExecutablePath = defaults.FfmpegExecutablePath;
        EnableWatermark = false;
        WatermarkText = string.Empty;
        _hasPendingWatermarkChanges = false;
        OutputDirectoryPath = defaults.OutputDirectoryPath;
        SelectedSpeaker = SpeakerOptions.FirstOrDefault(t => string.Equals(t.VoiceType, defaults.Speaker, StringComparison.Ordinal))
            ?? SpeakerOptions.FirstOrDefault();
    }

    private LocalDefaultValues? _localDefaultValues;

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
        var validationMessage = ValidateBeforeGenerateAll();
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

            if (_hasPendingWatermarkChanges)
            {
                await ApplyWatermarkAsync();
            }

            var generationOptions = CreateGenerationOptions(requireOpenAi: true, requireOpenSpeech: true, requireFfmpeg: true);
            var coursewareMaterialInfo = BuildCoursewareMaterialInfo();
            var progress = new Progress<string>(message =>
            {
                StatusMessage = message;
                AppendLog(message);
            });
            var speechProgress = new Progress<CoursewareSpeechGenerationProgress>(ApplyGeneratedScriptProgress);

            ClearGeneratedScripts();
            var result = await _coursewareSpeechVideoGenerator.GenerateAsync(coursewareMaterialInfo, generationOptions, progress, speechProgress, CancellationToken.None);
            ApplyGenerationResult(result);
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

    private async Task GenerateScriptsAsync()
    {
        var validationMessage = ValidateBeforeGenerateScripts();
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

            if (_hasPendingWatermarkChanges)
            {
                await ApplyWatermarkAsync();
            }

            var generationOptions = CreateGenerationOptions(requireOpenAi: true, requireOpenSpeech: false, requireFfmpeg: false);
            var coursewareMaterialInfo = BuildCoursewareMaterialInfo();
            var progress = new Progress<string>(message =>
            {
                StatusMessage = message;
                AppendLog(message);
            });
            var speechProgress = new Progress<CoursewareSpeechGenerationProgress>(ApplyGeneratedScriptProgress);

            ClearGeneratedScripts();
            var speechInfo = await _coursewareSpeechVideoGenerator.GenerateSpeechAsync(coursewareMaterialInfo, generationOptions, progress, speechProgress, CancellationToken.None);
            _currentCoursewareSpeechInfo = speechInfo;
            ApplyGeneratedScripts(speechInfo);
            StatusMessage = Resources.ScriptGenerationCompleted;
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

    private CoursewareMaterialInfo BuildCoursewareMaterialInfo()
    {
        if (_currentReadResult is null || Slides.Count == 0)
        {
            throw new InvalidOperationException(Resources.ValidationSelectPpt);
        }

        var slideMaterialInfoList = new CoursewareSlideMaterialInfo[Slides.Count];
        for (var i = 0; i < Slides.Count; i++)
        {
            slideMaterialInfoList[i] = new CoursewareSlideMaterialInfo(new System.IO.FileInfo(Slides[i].ImageFilePath), Slides[i].SlideText);
        }

        return new CoursewareMaterialInfo(slideMaterialInfoList);
    }

    private async Task GenerateVideoFromScriptsAsync()
    {
        var validationMessage = ValidateBeforeGenerateVideoFromScripts();
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            StatusMessage = validationMessage;
            return;
        }

        try
        {
            IsBusy = true;
            AppendLog(StatusMessage);

            if (_hasPendingWatermarkChanges)
            {
                await ApplyWatermarkAsync();
            }

            UpdateCoursewareSpeechInfoPreviewImages();

            var generationOptions = CreateGenerationOptions(requireOpenAi: false, requireOpenSpeech: true, requireFfmpeg: true);
            var progress = new Progress<string>(message =>
            {
                StatusMessage = message;
                AppendLog(message);
            });

            var result = await _coursewareSpeechVideoGenerator.GenerateVideoAsync(_currentCoursewareSpeechInfo!, generationOptions, progress, CancellationToken.None);
            ApplyGenerationResult(result);
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
            _currentCoursewareSpeechInfo = null;
            _lastGenerationOutputDirectoryPath = string.Empty;
            _currentWorkingDirectory = null;
            var progress = new Progress<string>(message =>
            {
                StatusMessage = message;
                AppendLog(message);
            });
            var readResult = await _powerPointReader.ReadSlidesAsync(new System.IO.FileInfo(PptFilePath), progress, CancellationToken.None);
            _currentReadResult = readResult;
            _currentWorkingDirectory = CreatePresentationWorkingDirectory(readResult.SourceFile);

            var slidePreviewItems = new List<SlidePreviewViewModel>(readResult.Slides.Count);
            foreach (var slide in readResult.Slides)
            {
                var previewImagePath = GetSlidePreviewImage(slide.SlideImageFile);
                slidePreviewItems.Add(new SlidePreviewViewModel(slide.SlideIndex, slide.SlideText, slide.SlideImageFile.FullName, previewImagePath.FullName));
            }

            foreach (var slidePreviewItem in slidePreviewItems)
            {
                Slides.Add(slidePreviewItem);
            }

            _hasPendingWatermarkChanges = false;

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
        return ValidateBeforeGenerateAll();
    }

    private string ValidateBeforeGenerateAll()
    {
        return BuildValidationMessage(
            Resources.GenerateButton,
            requirePpt: true,
            requireOpenAi: true,
            requireOpenSpeech: true,
            requireFfmpeg: true,
            requireOutputDirectory: true,
            requireGeneratedScripts: false);
    }

    private string ValidateBeforeGenerateScripts()
    {
        return BuildValidationMessage(
            Resources.GenerateScriptsButton,
            requirePpt: true,
            requireOpenAi: true,
            requireOpenSpeech: false,
            requireFfmpeg: false,
            requireOutputDirectory: true,
            requireGeneratedScripts: false);
    }

    private string ValidateBeforeGenerateVideoFromScripts()
    {
        return BuildValidationMessage(
            Resources.GenerateVideoFromScriptsButton,
            requirePpt: true,
            requireOpenAi: false,
            requireOpenSpeech: true,
            requireFfmpeg: true,
            requireOutputDirectory: true,
            requireGeneratedScripts: true);
    }

    private bool CanGenerate()
    {
        return !IsBusy;
    }

    private bool CanGenerateScripts()
    {
        return !IsBusy;
    }

    private bool CanGenerateVideoFromScripts()
    {
        return !IsBusy;
    }

    private bool CanApplyWatermark()
    {
        return !IsBusy && EnableWatermark;
    }

    private bool CanOpenOutputFolder()
    {
        return !string.IsNullOrWhiteSpace(GetOutputFolderToOpen());
    }

    private void OpenOutputFolder()
    {
        var directory = GetOutputFolderToOpen();
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

    private string GetOutputFolderToOpen()
    {
        if (!string.IsNullOrWhiteSpace(_lastGenerationOutputDirectoryPath))
        {
            return _lastGenerationOutputDirectoryPath;
        }

        return string.IsNullOrWhiteSpace(OutputDirectoryPath) ? string.Empty : OutputDirectoryPath;
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
        _applyWatermarkCommand.RaiseCanExecuteChanged();
        _generateScriptsCommand.RaiseCanExecuteChanged();
        _generateVideoFromScriptsCommand.RaiseCanExecuteChanged();
        _generateCommand.RaiseCanExecuteChanged();
        _openOutputFolderCommand.RaiseCanExecuteChanged();
    }

    private string BuildValidationMessage(
        string actionName,
        bool requirePpt,
        bool requireOpenAi,
        bool requireOpenSpeech,
        bool requireFfmpeg,
        bool requireOutputDirectory,
        bool requireGeneratedScripts)
    {
        List<string> issues = [];

        if (requirePpt)
        {
            AddPptValidationIssues(issues);
        }

        if (requireOpenAi)
        {
            AddOpenAiValidationIssues(issues);
        }

        if (requireOpenSpeech)
        {
            AddOpenSpeechValidationIssues(issues);
        }

        if (requireFfmpeg)
        {
            AddFfmpegValidationIssues(issues);
        }

        AddWatermarkValidationIssues(issues);

        if (requireOutputDirectory)
        {
            AddOutputDirectoryValidationIssues(issues);
        }

        if (requireGeneratedScripts)
        {
            AddGeneratedScriptsValidationIssues(issues);
        }

        if (issues.Count == 0)
        {
            return string.Empty;
        }

        return $"{string.Format(Resources.ActionValidationSummaryFormat, actionName)}{Environment.NewLine}{string.Join(Environment.NewLine, issues.Select((issue, index) => $"{index + 1}. {issue}"))}";
    }

    private void AddPptValidationIssues(List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(PptFilePath))
        {
            issues.Add(Resources.ValidationPptFileRequired);
            return;
        }

        if (!File.Exists(PptFilePath))
        {
            issues.Add(string.Format(Resources.ValidationPptFileNotFoundFormat, PptFilePath));
        }
    }

    private void AddOpenAiValidationIssues(List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(OpenAiApiKey) && string.IsNullOrEmpty(_localDefaultValues?.OpenAiApiKey))
        {
            issues.Add(Resources.ValidationOpenAiApiKey);
        }

        if (string.IsNullOrWhiteSpace(OpenAiEndpoint))
        {
            issues.Add(Resources.ValidationOpenAiEndpointRequired);
        }
        else if (!Uri.TryCreate(OpenAiEndpoint, UriKind.Absolute, out _))
        {
            issues.Add(Resources.ValidationOpenAiEndpointInvalid);
        }

        if (string.IsNullOrWhiteSpace(OpenAiModel))
        {
            issues.Add(Resources.ValidationOpenAiModel);
        }
    }

    private void AddOpenSpeechValidationIssues(List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(OpenSpeechApiKey) && string.IsNullOrEmpty(_localDefaultValues?.OpenSpeechApiKey))
        {
            issues.Add(Resources.ValidationOpenSpeechApiKey);
        }

        if (string.IsNullOrWhiteSpace(ResourceId))
        {
            issues.Add(Resources.ValidationResourceId);
        }

        if (SelectedSpeaker is null)
        {
            issues.Add(Resources.ValidationSpeaker);
        }
    }

    private void AddFfmpegValidationIssues(List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(FfmpegExecutablePath))
        {
            issues.Add(Resources.ValidationFfmpegPathRequired);
            return;
        }

        if (!File.Exists(FfmpegExecutablePath))
        {
            issues.Add(string.Format(Resources.ValidationFfmpegPathNotFoundFormat, FfmpegExecutablePath));
        }
    }

    private void AddWatermarkValidationIssues(List<string> issues)
    {
        if (!EnableWatermark)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(WatermarkText))
        {
            issues.Add(Resources.ValidationWatermarkTextRequired);
            return;
        }

        if (_hasPendingWatermarkChanges)
        {
            issues.Add(Resources.ValidationApplyWatermarkFirst);
        }
    }

    private async Task ApplyWatermarkAsync()
    {
        if (_currentReadResult is null || Slides.Count == 0)
        {
            _hasPendingWatermarkChanges = false;
            UpdateCommandStates();
            return;
        }

        if (_currentWorkingDirectory is null)
        {
            throw new InvalidOperationException(Resources.ValidationSelectPpt);
        }

        if (EnableWatermark && string.IsNullOrWhiteSpace(WatermarkText))
        {
            StatusMessage = Resources.ValidationWatermarkTextRequired;
            return;
        }

        var slideCount = Math.Min(Slides.Count, _currentReadResult.Slides.Count);
        for (var i = 0; i < slideCount; i++)
        {
            var slide = _currentReadResult.Slides[i];
            var previewImagePath = GetSlidePreviewImage(slide.SlideImageFile);
            Slides[i].ImageFilePath = previewImagePath.FullName;

            await Application.Current.Dispatcher.InvokeAsync(
                static () => { },
                DispatcherPriority.Background);
        }

        _hasPendingWatermarkChanges = false;
        UpdateCoursewareSpeechInfoPreviewImages();
        UpdateCommandStates();
    }

    private void RefreshSlidePreviewImages()
    {
        _hasPendingWatermarkChanges = true;
        UpdateCommandStates();
    }

    private System.IO.DirectoryInfo CreatePresentationWorkingDirectory(System.IO.FileInfo sourceFile)
    {
        ArgumentNullException.ThrowIfNull(sourceFile);

        var cacheRoot = GetWorkingCacheRootDirectory();
        var presentationKey = string.Concat(
            Path.GetFileNameWithoutExtension(sourceFile.Name),
            "_",
            sourceFile.LastWriteTimeUtc.Ticks.ToString());
        var safeDirectoryName = string.Join("_", presentationKey.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var directory = new System.IO.DirectoryInfo(Path.Join(cacheRoot.FullName, safeDirectoryName));
        directory.Create();
        return directory;
    }

    private System.IO.DirectoryInfo GetWorkingCacheRootDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_lastGenerationOutputDirectoryPath) && Directory.Exists(_lastGenerationOutputDirectoryPath))
        {
            var workingDirectory = new System.IO.DirectoryInfo(Path.Join(_lastGenerationOutputDirectoryPath, "WorkspaceCache"));
            workingDirectory.Create();
            return workingDirectory;
        }

        if (!string.IsNullOrWhiteSpace(OutputDirectoryPath))
        {
            var configuredDirectory = new System.IO.DirectoryInfo(Path.Join(OutputDirectoryPath, "WorkspaceCache"));
            configuredDirectory.Create();
            return configuredDirectory;
        }

        var fallbackDirectory = new System.IO.DirectoryInfo(Path.Join(Path.GetTempPath(), "JawjeleceeYairlubelhearrene", "WorkspaceCache"));
        fallbackDirectory.Create();
        return fallbackDirectory;
    }

    private System.IO.FileInfo GetSlidePreviewImage(System.IO.FileInfo slideImageFile)
    {
        ArgumentNullException.ThrowIfNull(slideImageFile);

        if (_currentWorkingDirectory is null)
        {
            throw new InvalidOperationException(Resources.ValidationSelectPpt);
        }

        return _slideImageWatermarkService.GetOutputImage(
            slideImageFile,
            _currentWorkingDirectory,
            new SlideWatermarkOptions(EnableWatermark, WatermarkText));
    }

    private void UpdateCoursewareSpeechInfoPreviewImages()
    {
        if (_currentCoursewareSpeechInfo is null)
        {
            return;
        }

        var slideInfoList = new List<CoursewareSpeechSlideInfo>(_currentCoursewareSpeechInfo.SlideInfoList.Count);
        for (var i = 0; i < _currentCoursewareSpeechInfo.SlideInfoList.Count && i < Slides.Count; i++)
        {
            slideInfoList.Add(new CoursewareSpeechSlideInfo(_currentCoursewareSpeechInfo.SlideInfoList[i].PlainScriptText, new System.IO.FileInfo(Slides[i].ImageFilePath)));
        }

        _currentCoursewareSpeechInfo = new CoursewareSpeechInfo(slideInfoList);
    }

    private void AddOutputDirectoryValidationIssues(List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(OutputDirectoryPath))
        {
            issues.Add(Resources.ValidationOutputDirectory);
        }
    }

    private void AddGeneratedScriptsValidationIssues(List<string> issues)
    {
        if (_currentCoursewareSpeechInfo is null || _currentCoursewareSpeechInfo.SlideInfoList.Count == 0)
        {
            issues.Add(Resources.ValidationGenerateScriptsFirst);
        }
    }

    private SpeechVideoGenerationOptions CreateGenerationOptions(bool requireOpenAi, bool requireOpenSpeech, bool requireFfmpeg)
    {
        var openAiApiKey = OpenAiApiKey;
        var openSpeechApiKey = OpenSpeechApiKey;

        if (_localDefaultValues is not null)
        {
            if (string.IsNullOrEmpty(openAiApiKey))
            {
                openAiApiKey = _localDefaultValues.OpenAiApiKey;
            }

            if (string.IsNullOrEmpty(openSpeechApiKey))
            {
                openSpeechApiKey = _localDefaultValues.OpenSpeechApiKey;
            }
        }

        var outputDirectory = GetOrCreateCurrentOutputDirectory();
        return new SpeechVideoGenerationOptions(
            new System.IO.FileInfo(FfmpegExecutablePath),
            EnableWatermark,
            WatermarkText,
            openSpeechApiKey,
            ResourceId,
            SelectedSpeaker?.VoiceType ?? string.Empty,
            openAiApiKey,
            new Uri(OpenAiEndpoint, UriKind.Absolute),
            OpenAiModel,
            outputDirectory);
    }

    private System.IO.DirectoryInfo GetOrCreateCurrentOutputDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_lastGenerationOutputDirectoryPath) && Directory.Exists(_lastGenerationOutputDirectoryPath))
        {
            return new System.IO.DirectoryInfo(_lastGenerationOutputDirectoryPath);
        }

        var outputDirectory = new System.IO.DirectoryInfo(Path.Join(OutputDirectoryPath, DateTime.Now.ToString("yyyyMMdd_HHmmss")));
        outputDirectory.Create();
        _lastGenerationOutputDirectoryPath = outputDirectory.FullName;
        return outputDirectory;
    }

    private void ApplyGeneratedScripts(CoursewareSpeechInfo speechInfo)
    {
        ArgumentNullException.ThrowIfNull(speechInfo);

        for (var i = 0; i < Slides.Count; i++)
        {
            Slides[i].GeneratedScript = i < speechInfo.SlideInfoList.Count
                ? speechInfo.SlideInfoList[i].PlainScriptText
                : string.Empty;
        }

        UpdateCommandStates();
    }

    private void ApplyGeneratedScriptProgress(CoursewareSpeechGenerationProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        var slideIndex = progress.SlideNumber - 1;
        if ((uint)slideIndex >= (uint)Slides.Count)
        {
            return;
        }

        Slides[slideIndex].GeneratedScript = progress.PlainScriptText;
    }

    private void ClearGeneratedScripts()
    {
        foreach (var slide in Slides)
        {
            slide.GeneratedScript = string.Empty;
        }
    }

    private void ApplyGenerationResult(SpeechVideoGenerationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _currentCoursewareSpeechInfo = result.CoursewareSpeechInfo;
        GeneratedVideoPath = result.OutputVideoFile.FullName;
        _lastGenerationOutputDirectoryPath = result.OutputDirectory.FullName;
        ApplyGeneratedScripts(result.CoursewareSpeechInfo);
        StatusMessage = string.Format(Resources.GenerationCompletedFormat, result.OutputVideoFile.FullName);
        AppendLog(StatusMessage);
    }

    private static bool IsSupportedPowerPointFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        return extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase);
    }
}
