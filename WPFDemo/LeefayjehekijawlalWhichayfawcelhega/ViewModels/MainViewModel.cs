using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using LeefayjehekijawlalWhichayfawcelhega.Models;
using LeefayjehekijawlalWhichayfawcelhega.Services;

namespace LeefayjehekijawlalWhichayfawcelhega.ViewModels;

internal sealed class MainViewModel : ObservableObject
{
    private readonly DoubaoPromptAgentService _doubaoPromptAgentService;
    private readonly DoubaoImageGenerationService _doubaoImageGenerationService;
    private readonly SlideOutlineParser _slideOutlineParser;
    private readonly ImageExportService _imageExportService;

    private string _outlineText = string.Empty;
    private string _exportRootDirectory = string.Empty;
    private string _statusMessage = "请输入每页 PPT 内容，建议使用空行分隔页面。";
    private bool _isBusy;

    public MainViewModel(
        DoubaoPromptAgentService doubaoPromptAgentService,
        DoubaoImageGenerationService doubaoImageGenerationService,
        SlideOutlineParser slideOutlineParser,
        ImageExportService imageExportService)
    {
        _doubaoPromptAgentService = doubaoPromptAgentService;
        _doubaoImageGenerationService = doubaoImageGenerationService;
        _slideOutlineParser = slideOutlineParser;
        _imageExportService = imageExportService;

        GeneratePromptsCommand = new AsyncRelayCommand(GeneratePromptsAsync, CanGeneratePrompts);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
    }

    public ObservableCollection<SlidePageViewModel> Slides { get; } = [];

    public IAsyncRelayCommand GeneratePromptsCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

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
        IReadOnlyList<string> slideContents = _slideOutlineParser.Parse(OutlineText);
        if (slideContents.Count == 0)
        {
            StatusMessage = "没有识别到可用的页面内容。";
            return;
        }

        Slides.Clear();
        for (int index = 0; index < slideContents.Count; index++)
        {
            Slides.Add(new SlidePageViewModel(index + 1, slideContents[index], _doubaoImageGenerationService));
        }

        IsBusy = true;
        StatusMessage = $"正在生成 {slideContents.Count} 页的图片提示词...";

        int successCount = 0;
        try
        {
            foreach (SlidePageViewModel slide in Slides)
            {
                slide.MarkPromptGenerating();

                try
                {
                    string prompt = await _doubaoPromptAgentService.GeneratePromptAsync(slide.OutlineContent, CancellationToken.None);
                    slide.ApplyPrompt(prompt);
                    successCount++;
                }
                catch (Exception exception)
                {
                    slide.MarkFailure($"提示词生成失败：{exception.Message}");
                }
            }

            StatusMessage = $"提示词生成完成，成功 {successCount} 页，共 {Slides.Count} 页。请检查后再逐页生成图片。";
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
        catch (Exception exception)
        {
            StatusMessage = $"导出失败：{exception.Message}";
        }
    }
}
