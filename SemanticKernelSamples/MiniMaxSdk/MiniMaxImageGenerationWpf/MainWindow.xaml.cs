using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MiniMaxSdk;
using MiniMaxSdk.Images.Models;

namespace MiniMaxImageGenerationWpf;

/// <summary>
/// MiniMax 图片生成器主窗口。
/// </summary>
public partial class MainWindow : Window
{
    private readonly ObservableCollection<GeneratedImageItem> _generatedImages = [];
    private CancellationTokenSource? _generationCancellationTokenSource;
    private GenerationSettings? _lastGenerationSettings;

    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        ThumbnailListBox.ItemsSource = _generatedImages;
        Closed += MainWindow_OnClosed;
    }

    private async void GenerateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryCreateSettings(out var settings))
        {
            return;
        }

        _lastGenerationSettings = settings;
        await GenerateImagesAsync(settings);
    }

    private async void RegenerateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_lastGenerationSettings is not null)
        {
            await GenerateImagesAsync(_lastGenerationSettings);
        }
    }

    private async Task GenerateImagesAsync(GenerationSettings settings)
    {
        _generationCancellationTokenSource?.Cancel();
        _generationCancellationTokenSource?.Dispose();
        _generationCancellationTokenSource = new CancellationTokenSource();

        SetBusyState(true);

        try
        {
            var apiKey = (await File.ReadAllTextAsync(settings.ApiKeyFilePath, _generationCancellationTokenSource.Token)).Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(FindResource("ApiKeyFileEmptyText") as string);
            }

            using var client = new MiniMaxClient(apiKey);
            var style = string.IsNullOrWhiteSpace(settings.StyleType)
                ? null
                : new MiniMaxImageStyle(settings.StyleType, 0.8F);
            var request = new MiniMaxImageGenerationRequest(
                settings.Prompt,
                settings.Model,
                settings.AspectRatio,
                ResponseFormat: MiniMaxImageResponseFormats.Base64,
                Seed: settings.Seed,
                Count: settings.Count,
                PromptOptimizer: settings.PromptOptimizer,
                AigcWatermark: settings.AigcWatermark,
                Style: style);

            var result = await client.ImageGeneration.GenerateAsync(request, _generationCancellationTokenSource.Token);
            var generatedImages = result.Images
                .Where(static image => image.Bytes is { Length: > 0 })
                .Select(static image => new GeneratedImageItem(image, CreateBitmapImage(image.Bytes!)))
                .ToArray();

            if (generatedImages.Length == 0)
            {
                ResultSummaryTextBlock.Text = FindResource("NoImageReturnedText") as string;
                StatusTextBlock.Text = FindResource("NoImageReturnedText") as string;
                StatusDot.Fill = Brushes.DarkOrange;
                return;
            }

            var firstNewImageIndex = _generatedImages.Count;
            foreach (var image in generatedImages)
            {
                _generatedImages.Add(image);
            }

            ThumbnailListBox.SelectedIndex = firstNewImageIndex;
            ThumbnailListBox.ScrollIntoView(_generatedImages[firstNewImageIndex]);
            ResultSummaryTextBlock.Text = $"本次成功生成 {result.SuccessCount} 张，历史共 {_generatedImages.Count} 张";
            StatusTextBlock.Text = $"创作完成，本次新增 {generatedImages.Length} 张图片";
            StatusDot.Fill = Brushes.MediumSeaGreen;
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = FindResource("GenerationCanceledText") as string;
            StatusDot.Fill = Brushes.DarkOrange;
        }
        catch (Exception exception) when (exception is ArgumentException or HttpRequestException or InvalidOperationException)
        {
            StatusTextBlock.Text = FindResource("GenerationFailedTitle") as string;
            StatusDot.Fill = Brushes.IndianRed;
            MessageBox.Show(this, exception.Message, FindResource("GenerationFailedTitle") as string, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private bool TryCreateSettings(out GenerationSettings settings)
    {
        settings = default!;
        var apiKeyFilePath = ApiKeyFilePathTextBox.Text.Trim();
        var prompt = PromptTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(apiKeyFilePath))
        {
            MessageBox.Show(this, FindResource("ApiKeyFileRequiredText") as string, FindResource("ValidationTitle") as string, MessageBoxButton.OK, MessageBoxImage.Warning);
            ApiKeyFilePathTextBox.Focus();
            return false;
        }

        if (!File.Exists(apiKeyFilePath))
        {
            MessageBox.Show(this, FindResource("ApiKeyFileNotFoundText") as string, FindResource("ValidationTitle") as string, MessageBoxButton.OK, MessageBoxImage.Warning);
            ApiKeyFilePathTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            MessageBox.Show(this, FindResource("PromptRequiredText") as string, FindResource("ValidationTitle") as string, MessageBoxButton.OK, MessageBoxImage.Warning);
            PromptTextBox.Focus();
            return false;
        }

        long? seed = null;
        if (!string.IsNullOrWhiteSpace(SeedTextBox.Text))
        {
            if (!long.TryParse(SeedTextBox.Text, out var parsedSeed))
            {
                MessageBox.Show(this, "随机种子必须是整数。", FindResource("ValidationTitle") as string, MessageBoxButton.OK, MessageBoxImage.Warning);
                SeedTextBox.Focus();
                return false;
            }

            seed = parsedSeed;
        }

        var model = GetSelectedTag(ModelComboBox);
        var styleType = model == MiniMaxImageGenerationModels.Image01Live ? GetSelectedTag(StyleComboBox) : null;
        settings = new GenerationSettings(
            apiKeyFilePath,
            prompt,
            model,
            GetSelectedTag(AspectRatioComboBox),
            int.Parse(GetSelectedTag(CountComboBox)),
            seed,
            PromptOptimizerCheckBox.IsChecked == true,
            WatermarkCheckBox.IsChecked == true,
            styleType);
        return true;
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ThumbnailListBox.SelectedItem is not GeneratedImageItem selectedImage)
        {
            return;
        }

        var extension = selectedImage.Image.SuggestedFileExtension;
        var dialog = new SaveFileDialog
        {
            Title = "导出生成的图片",
            FileName = $"minimax-image-{DateTime.Now:yyyyMMdd-HHmmss}{extension}",
            DefaultExt = extension,
            Filter = CreateImageFilter(extension)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            await selectedImage.Image.SaveAsync(new FileInfo(dialog.FileName));
            StatusTextBlock.Text = $"{FindResource("SavedText")}：{dialog.FileName}";
            StatusDot.Fill = Brushes.MediumSeaGreen;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MessageBox.Show(this, exception.Message, FindResource("SaveFailedTitle") as string, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ThumbnailListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThumbnailListBox.SelectedItem is not GeneratedImageItem selectedImage)
        {
            return;
        }

        PreviewImage.Source = selectedImage.Preview;
        EmptyPreviewTextBlock.Visibility = Visibility.Collapsed;
        SaveButton.IsEnabled = true;
    }

    private void BrowseApiKeyFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 MiniMax API Key 文件",
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            FileName = ApiKeyFilePathTextBox.Text
        };

        if (dialog.ShowDialog(this) == true)
        {
            ApiKeyFilePathTextBox.Text = dialog.FileName;
        }
    }

    private void ModelComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || StylePanel is null)
        {
            return;
        }

        StylePanel.IsEnabled = GetSelectedTag(ModelComboBox) == MiniMaxImageGenerationModels.Image01Live;
    }

    private void SetBusyState(bool isBusy)
    {
        BusyOverlay.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        GenerateButton.IsEnabled = !isBusy;
        RegenerateButton.IsEnabled = !isBusy && _lastGenerationSettings is not null;
        SaveButton.IsEnabled = !isBusy && ThumbnailListBox.SelectedItem is GeneratedImageItem;

        if (isBusy)
        {
            StatusTextBlock.Text = FindResource("GeneratingText") as string;
            StatusDot.Fill = Brushes.RoyalBlue;
        }
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        _generationCancellationTokenSource?.Cancel();
        _generationCancellationTokenSource?.Dispose();
    }

    private static string GetSelectedTag(ComboBox comboBox)
    {
        return (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
    }

    private static BitmapImage CreateBitmapImage(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static string CreateImageFilter(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "JPEG 图片 (*.jpg;*.jpeg)|*.jpg;*.jpeg|所有文件 (*.*)|*.*",
            ".webp" => "WebP 图片 (*.webp)|*.webp|所有文件 (*.*)|*.*",
            _ => "PNG 图片 (*.png)|*.png|所有文件 (*.*)|*.*"
        };
    }

    private sealed record GenerationSettings(
        string ApiKeyFilePath,
        string Prompt,
        string Model,
        string AspectRatio,
        int Count,
        long? Seed,
        bool PromptOptimizer,
        bool AigcWatermark,
        string? StyleType);

    private sealed record GeneratedImageItem(MiniMaxGeneratedImage Image, BitmapImage Preview);
}
