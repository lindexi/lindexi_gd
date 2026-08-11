using System.ClientModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OpenAI;
using OpenAI.Images;

namespace ImageGenerationWpf;

/// <summary>
/// OpenAI 图片生成与编辑窗口。
/// </summary>
public partial class MainWindow : Window
{
    private const string KeyFilePath = @"C:\lindexi\Work\Key\ModelLindexi.txt";
    private const string Endpoint = "https://model.server.lindexi.com/v1/";
    private const string Model = "gpt-image-2";

    private readonly ObservableCollection<ImageItem> _images = [];
    private CancellationTokenSource? _cancellationTokenSource;
    private ImageItem? _editSource;

    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        ThumbnailListBox.ItemsSource = _images;
        Closed += MainWindow_OnClosed;
    }

    private async void RunButton_OnClick(object sender, RoutedEventArgs e)
    {
        var prompt = PromptTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            MessageBox.Show(this, "请输入画面描述或编辑要求。", "请检查输入", MessageBoxButton.OK, MessageBoxImage.Warning);
            PromptTextBox.Focus();
            return;
        }

        var isEditMode = _editSource is not null;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        SetBusyState(true);

        try
        {
            var key = await File.ReadAllTextAsync(KeyFilePath, _cancellationTokenSource.Token);
            var openAiClient = new OpenAIClient(new ApiKeyCredential(key.Trim()), new OpenAIClientOptions
            {
                Endpoint = new Uri(Endpoint)
            });
            var imageClient = openAiClient.GetImageClient(Model);

            byte[] bytes;
            if (isEditMode)
            {
                bytes = await GenerateImageEditAsync(
                    key.Trim(),
                    EncodePng(_editSource!.Preview),
                    prompt,
                    _cancellationTokenSource.Token);
            }
            else
            {
                var result = await imageClient.GenerateImageAsync(prompt, cancellationToken: _cancellationTokenSource.Token);
                bytes = result.Value.ImageBytes.ToArray();
            }

            var imageItem = new ImageItem(bytes, CreateBitmapImage(bytes), $"openai-image-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            _images.Add(imageItem);
            ThumbnailListBox.SelectedItem = imageItem;
            ThumbnailListBox.ScrollIntoView(imageItem);
            ResultSummaryTextBlock.Text = isEditMode
                ? $"编辑完成，历史共 {_images.Count} 张图片"
                : $"生成完成，历史共 {_images.Count} 张图片";
            StatusTextBlock.Text = isEditMode ? "图片编辑完成" : "图片生成完成";
            StatusDot.Fill = Brushes.MediumSeaGreen;
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "操作已取消";
            StatusDot.Fill = Brushes.DarkOrange;
        }
        catch (Exception exception) when (exception is ClientResultException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or HttpRequestException)
        {
            StatusTextBlock.Text = "图片处理失败";
            StatusDot.Fill = Brushes.IndianRed;
            MessageBox.Show(this, exception.Message, "图片处理失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void SelectImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择待编辑图片",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.webp|所有文件|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var bytes = File.ReadAllBytes(dialog.FileName);
        SetEditSource(new ImageItem(bytes, CreateBitmapImage(bytes), Path.GetFileName(dialog.FileName)));
    }

    private void ThumbnailListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThumbnailListBox.SelectedItem is not ImageItem selectedImage)
        {
            return;
        }

        PreviewImage.Source = selectedImage.Preview;
        EmptyPreviewTextBlock.Visibility = Visibility.Collapsed;
        SaveButton.IsEnabled = true;

        SetEditSource(selectedImage);
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ThumbnailListBox.SelectedItem is not ImageItem selectedImage)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "保存图片",
            FileName = selectedImage.FileName,
            DefaultExt = ".png",
            Filter = "PNG 图片 (*.png)|*.png|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            await File.WriteAllBytesAsync(dialog.FileName, selectedImage.Bytes);
            StatusTextBlock.Text = $"图片已保存：{dialog.FileName}";
            StatusDot.Fill = Brushes.MediumSeaGreen;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, exception.Message, "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
    }

    private void SetEditSource(ImageItem image)
    {
        _editSource = image;
        EditSourceTitleTextBlock.Text = "已选择待编辑图片";
        EditSourceTextBlock.Text = image.FileName;
        ClearEditSourceButton.Visibility = Visibility.Visible;
        ModeHintTextBlock.Text = "当前将基于所选图片进行编辑";
        RunButton.Content = "编辑图片";
        PreviewImage.Source = image.Preview;
        EmptyPreviewTextBlock.Visibility = Visibility.Collapsed;
    }

    private void ClearEditSourceButton_OnClick(object sender, RoutedEventArgs e)
    {
        _editSource = null;
        ThumbnailListBox.SelectedItem = null;
        EditSourceTitleTextBlock.Text = "未选择图片";
        EditSourceTextBlock.Text = "可选择本地图片，或点击右侧历史图片";
        ClearEditSourceButton.Visibility = Visibility.Collapsed;
        ModeHintTextBlock.Text = "当前将根据文字生成新图片";
        RunButton.Content = "生成图片";
    }

    private void SetBusyState(bool isBusy)
    {
        BusyOverlay.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        RunButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = isBusy;
        EditSourcePanel.IsEnabled = !isBusy;
        ThumbnailListBox.IsEnabled = !isBusy;
        SaveButton.IsEnabled = !isBusy && ThumbnailListBox.SelectedItem is ImageItem;

        if (isBusy)
        {
            StatusTextBlock.Text = _editSource is not null ? "正在编辑图片…" : "正在生成图片…";
            StatusDot.Fill = Brushes.DodgerBlue;
        }
    }

    private static async Task<byte[]> GenerateImageEditAsync(string apiKey, byte[] pngBytes, string prompt, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var content = new MultipartFormDataContent();
        using var imageContent = new ByteArrayContent(pngBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", "image.png");
        content.Add(new StringContent(prompt), "prompt");
        content.Add(new StringContent(Model), "model");
        content.Add(new StringContent("b64_json"), "response_format");

        using var response = await client.PostAsync(new Uri(new Uri(Endpoint), "images/edits"), content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP {(int) response.StatusCode} ({response.ReasonPhrase}){Environment.NewLine}{responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);
        var base64 = document.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new InvalidOperationException("图片编辑服务未返回图片数据。");
        }

        return Convert.FromBase64String(base64);
    }

    private static byte[] EncodePng(BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
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

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }

    private sealed record ImageItem(byte[] Bytes, BitmapImage Preview, string FileName);
}
