using System.Collections.ObjectModel;
using System.ClientModel;
using System.Net.Http;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using LeefayjehekijawlalWhichayfawcelhega.Models;
using LeefayjehekijawlalWhichayfawcelhega.Services;

namespace LeefayjehekijawlalWhichayfawcelhega.ViewModels;

internal sealed class SlidePageViewModel : ObservableObject
{
    private readonly ImageGenerationService _imageGenerationService;
    private readonly Action<SlidePageViewModel> _removeSlideAction;

    private string _promptText = string.Empty;
    private string? _errorMessage;
    private PageGenerationStage _stage = PageGenerationStage.PendingPromptGeneration;
    private bool _isGeneratingImages;
    private ImageCandidateViewModel? _selectedCandidate;

    public SlidePageViewModel(
        int pageNumber,
        string promptText,
        ImageGenerationService imageGenerationService,
        Action<SlidePageViewModel> removeSlideAction)
    {
        _imageGenerationService = imageGenerationService;
        _removeSlideAction = removeSlideAction;
        PageNumber = pageNumber;
        _promptText = promptText;
        _stage = string.IsNullOrWhiteSpace(promptText)
            ? PageGenerationStage.PendingPromptGeneration
            : PageGenerationStage.AwaitingImageGeneration;
        GenerateImagesCommand = new AsyncRelayCommand(GenerateImagesAsync, CanGenerateImages);
        DeleteSlideCommand = new RelayCommand(DeleteSlide);
    }

    public int PageNumber { get; private set; }

    public ObservableCollection<ImageCandidateViewModel> ImageCandidates { get; } = [];

    public IAsyncRelayCommand GenerateImagesCommand { get; }

    public IRelayCommand DeleteSlideCommand { get; }

    public string PromptText
    {
        get => _promptText;
        set
        {
            if (SetProperty(ref _promptText, value))
            {
                if (!IsGeneratingImages)
                {
                    Stage = string.IsNullOrWhiteSpace(value)
                        ? PageGenerationStage.PendingPromptGeneration
                        : PageGenerationStage.AwaitingImageGeneration;
                }

                OnPropertyChanged(nameof(GenerateButtonText));
                GenerateImagesCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public PageGenerationStage Stage
    {
        get => _stage;
        private set
        {
            if (SetProperty(ref _stage, value))
            {
                OnPropertyChanged(nameof(StageText));
            }
        }
    }

    public bool IsGeneratingImages
    {
        get => _isGeneratingImages;
        private set
        {
            if (SetProperty(ref _isGeneratingImages, value))
            {
                OnPropertyChanged(nameof(GenerateButtonText));
                GenerateImagesCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ImageCandidateViewModel? SelectedCandidate
    {
        get => _selectedCandidate;
        set => SetProperty(ref _selectedCandidate, value);
    }

    public bool HasImages => ImageCandidates.Count > 0;

    public string GenerateButtonText => IsGeneratingImages
        ? "生成中..."
        : HasImages
            ? "重新生成图片"
            : "确认提示词并生成图片";

    public string StageText => Stage switch
    {
        PageGenerationStage.PendingPromptGeneration => "等待生成提示词",
        PageGenerationStage.PromptReady => "提示词已生成",
        PageGenerationStage.AwaitingImageGeneration => "等待用户确认生成图片",
        PageGenerationStage.GeneratingImages => "正在生成图片",
        PageGenerationStage.ImagesReady => "候选图片已就绪",
        PageGenerationStage.Exported => "已导出",
        PageGenerationStage.Failed => "当前页处理失败",
        _ => "未知状态",
    };

    public string EmptyImageMessage => HasImages ? string.Empty : "当前页还没有候选图片，请确认提示词后点击生成。";

    public void UpdatePageNumber(int pageNumber)
    {
        PageNumber = pageNumber;
        OnPropertyChanged(nameof(PageNumber));
    }

    public void ApplyPrompt(string promptText)
    {
        if (string.IsNullOrWhiteSpace(promptText))
        {
            throw new ArgumentException("提示词不能为空。", nameof(promptText));
        }

        PromptText = promptText.Trim();
        ErrorMessage = null;
        Stage = PageGenerationStage.AwaitingImageGeneration;
    }

    public void MarkFailure(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("错误信息不能为空。", nameof(message));
        }

        ErrorMessage = message;
        Stage = PageGenerationStage.Failed;
    }

    public void MarkExported()
    {
        Stage = PageGenerationStage.Exported;
    }

    public SlideExportItem? TryCreateExportItem()
    {
        if (SelectedCandidate is null)
        {
            return null;
        }

        return new SlideExportItem
        {
            PageNumber = PageNumber,
            Content = SelectedCandidate.Content,
            FileExtension = SelectedCandidate.FileExtension,
        };
    }

    private bool CanGenerateImages()
    {
        return !IsGeneratingImages && !string.IsNullOrWhiteSpace(PromptText);
    }

    private async Task GenerateImagesAsync()
    {
        ErrorMessage = null;
        IsGeneratingImages = true;
        Stage = PageGenerationStage.GeneratingImages;

        try
        {
            IReadOnlyList<GeneratedImageResult> generatedImages = await _imageGenerationService.GenerateImagesAsync(PromptText, CancellationToken.None);

            ImageCandidates.Clear();
            foreach (GeneratedImageResult generatedImage in generatedImages)
            {
                ImageCandidates.Add(new ImageCandidateViewModel(generatedImage));
            }

            SelectedCandidate = ImageCandidates.FirstOrDefault();
            Stage = PageGenerationStage.ImagesReady;
            OnPropertyChanged(nameof(HasImages));
            OnPropertyChanged(nameof(EmptyImageMessage));
            OnPropertyChanged(nameof(GenerateButtonText));
        }
        catch (ArgumentException exception)
        {
            MarkFailure($"图片生成失败：{exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            MarkFailure($"图片生成失败：{exception.Message}");
        }
        catch (HttpRequestException exception)
        {
            MarkFailure($"图片生成失败：{exception.Message}");
        }
        catch (ClientResultException exception)
        {
            MarkFailure($"图片生成失败：{exception.Message}");
        }
        finally
        {
            IsGeneratingImages = false;
        }
    }

    private void DeleteSlide()
    {
        _removeSlideAction(this);
    }
}
