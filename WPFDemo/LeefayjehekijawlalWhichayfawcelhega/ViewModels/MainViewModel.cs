using System.Collections.ObjectModel;
using System.ClientModel;
using System.IO;
using System.Net.Http;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using LeefayjehekijawlalWhichayfawcelhega.Models;
using LeefayjehekijawlalWhichayfawcelhega.Services;

namespace LeefayjehekijawlalWhichayfawcelhega.ViewModels;

internal sealed class MainViewModel : ObservableObject
{
    private readonly PromptAgentService _promptAgentService;
    private readonly ImageGenerationService _imageGenerationService;
    private readonly ImageExportService _imageExportService;
    private readonly AiProviderOptions _providerOptions;

    private string _outlineText = string.Empty;
    private string _exportRootDirectory = string.Empty;
    private string _statusMessage = "请先配置模型参数，再输入完整 PPT 大纲生成逐页提示词。";
    private bool _isBusy;

    public MainViewModel(
        PromptAgentService promptAgentService,
        ImageGenerationService imageGenerationService,
        ImageExportService imageExportService,
        AiProviderOptions providerOptions)
    {
        _promptAgentService = promptAgentService;
        _imageGenerationService = imageGenerationService;
        _imageExportService = imageExportService;
        _providerOptions = providerOptions;

        GeneratePromptsCommand = new AsyncRelayCommand(GeneratePromptsAsync, CanGeneratePrompts);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        AddSlideCommand = new RelayCommand(AddSlide);

        AddSlide();
    }

    public ObservableCollection<SlidePageViewModel> Slides { get; } = [];

    public IAsyncRelayCommand GeneratePromptsCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public IRelayCommand AddSlideCommand { get; }

    public string ServiceEndpoint
    {
        get => _providerOptions.Endpoint;
        set => SetProviderOption(value, _providerOptions.Endpoint, newValue => _providerOptions.Endpoint = newValue);
    }

    public string ApiKey
    {
        get => _providerOptions.ApiKey;
        set => SetProviderOption(value, _providerOptions.ApiKey, newValue => _providerOptions.ApiKey = newValue);
    }

    public string PromptModelId
    {
        get => _providerOptions.PromptModelId;
        set => SetProviderOption(value, _providerOptions.PromptModelId, newValue => _providerOptions.PromptModelId = newValue);
    }

    public string ImageModelId
    {
        get => _providerOptions.ImageModelId;
        set => SetProviderOption(value, _providerOptions.ImageModelId, newValue => _providerOptions.ImageModelId = newValue);
    }

    public string OutlineText
    {
        get => _outlineText;
        set
        {
            if (SetProperty(ref _outlineText, value))
            {
                GeneratePromptsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ExportRootDirectory
    {
        get => _exportRootDirectory;
        set => SetProperty(ref _exportRootDirectory, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                GeneratePromptsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private bool CanGeneratePrompts()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(OutlineText);
    }

    private async Task GeneratePromptsAsync()
    {
        if (string.IsNullOrWhiteSpace(OutlineText))
        {
            StatusMessage = "没有识别到可用的完整大纲内容。";
            return;
        }

        IsBusy = true;
        StatusMessage = "正在根据完整大纲生成逐页图片提示词...";

        try
        {
            IReadOnlyList<string> prompts = await _promptAgentService.GeneratePromptsAsync(OutlineText, CancellationToken.None);
            ReplaceSlides(prompts);
            StatusMessage = $"提示词生成完成，共 {Slides.Count} 页。请检查后再逐页生成图片。";
        }
        catch (ArgumentException exception)
        {
            StatusMessage = $"提示词生成失败：{exception.Message}";
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage = $"提示词生成失败：{exception.Message}";
        }
        catch (HttpRequestException exception)
        {
            StatusMessage = $"提示词生成失败：{exception.Message}";
        }
        catch (ClientResultException exception)
        {
            StatusMessage = $"提示词生成失败：{exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportAsync()
    {
        List<SlideExportItem> exportItems = Slides
            .Select(slide => slide.TryCreateExportItem())
            .Where(item => item is not null)
            .Cast<SlideExportItem>()
            .ToList();

        if (exportItems.Count == 0)
        {
            StatusMessage = "当前没有可导出的已选图片。";
            return;
        }

        try
        {
            string exportDirectory = await _imageExportService.ExportAsync(exportItems, ExportRootDirectory, CancellationToken.None);
            foreach (SlidePageViewModel slide in Slides.Where(slide => slide.SelectedCandidate is not null))
            {
                slide.MarkExported();
            }

            StatusMessage = $"已导出 {exportItems.Count} 页图片到：{exportDirectory}";
        }
        catch (ArgumentException exception)
        {
            StatusMessage = $"导出失败：{exception.Message}";
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage = $"导出失败：{exception.Message}";
        }
        catch (IOException exception)
        {
            StatusMessage = $"导出失败：{exception.Message}";
        }
    }

    private void AddSlide()
    {
        Slides.Add(CreateSlidePageViewModel(string.Empty));
        RenumberSlides();
    }

    private SlidePageViewModel CreateSlidePageViewModel(string prompt)
    {
        return new SlidePageViewModel(Slides.Count + 1, prompt, _imageGenerationService, RemoveSlide);
    }

    private void RemoveSlide(SlidePageViewModel slide)
    {
        ArgumentNullException.ThrowIfNull(slide);

        Slides.Remove(slide);
        if (Slides.Count == 0)
        {
            Slides.Add(CreateSlidePageViewModel(string.Empty));
        }

        RenumberSlides();
    }

    private void ReplaceSlides(IEnumerable<string> prompts)
    {
        ArgumentNullException.ThrowIfNull(prompts);

        Slides.Clear();
        foreach (string prompt in prompts)
        {
            Slides.Add(CreateSlidePageViewModel(prompt));
        }

        if (Slides.Count == 0)
        {
            Slides.Add(CreateSlidePageViewModel(string.Empty));
        }

        RenumberSlides();
    }

    private void RenumberSlides()
    {
        for (int index = 0; index < Slides.Count; index++)
        {
            Slides[index].UpdatePageNumber(index + 1);
        }
    }

    private void SetProviderOption(string value, string currentValue, Action<string> setter)
    {
        if (string.Equals(value, currentValue, StringComparison.Ordinal))
        {
            return;
        }

        setter(value);
        GeneratePromptsCommand.NotifyCanExecuteChanged();
        OnPropertyChanged();
    }
}
