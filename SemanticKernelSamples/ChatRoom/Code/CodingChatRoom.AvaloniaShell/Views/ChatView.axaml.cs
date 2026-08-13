using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using AgentLib.Model;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Views;

/// <summary>
/// 显示当前编程助手会话。
/// </summary>
public partial class ChatView : UserControl
{
    private static readonly FilePickerFileType ImageFileType = new("图片")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp", "*.bmp"],
        MimeTypes = ["image/png", "image/jpeg", "image/gif", "image/webp", "image/bmp"],
    };

    private readonly ChatAutoScrollState _autoScrollState = new();
    private ChatViewModel? _subscribedViewModel;

    /// <summary>
    /// 初始化聊天视图。
    /// </summary>
    public ChatView()
    {
        InitializeComponent();
        MessageInputTextBox.AddHandler(KeyDownEvent, MessageInputTextBox_OnKeyDown, RoutingStrategies.Tunnel);
        MessagesScrollViewer.ScrollChanged += OnMessagesScrollChanged;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _subscribedViewModel = DataContext as ChatViewModel;
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        ResetAndScrollToEnd();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatViewModel.CurrentSessionId))
        {
            ResetAndScrollToEnd();
        }
    }

    private void OnMessagesScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        bool shouldScrollToEnd = _autoScrollState.HandleScrollChanged(
            MessagesScrollViewer.Offset.Y,
            MessagesScrollViewer.Extent.Height,
            MessagesScrollViewer.Viewport.Height,
            e.ExtentDelta.Y,
            e.ViewportDelta.Y,
            e.OffsetDelta.Y);

        if (shouldScrollToEnd)
        {
            ScrollToEndAfterLayout();
        }
    }

    private void ResetAndScrollToEnd()
    {
        _autoScrollState.Reset();
        ScrollToEndAfterLayout();
    }

    private void ScrollToEndAfterLayout()
    {
        Dispatcher.UIThread.Post(
            MessagesScrollViewer.ScrollToEnd,
            DispatcherPriority.Background);
    }

    private async void CopyContentMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: MessageItemViewModel message })
        {
            await SetClipboardTextAsync(message.Content);
        }
    }

    private async void CopyFullMessageMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: MessageItemViewModel message })
        {
            await SetClipboardTextAsync(message.FullContent);
        }
    }

    private void ImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: CopilotChatImageItem imageItem })
        {
            return;
        }

        try
        {
            TemporaryImageViewer.Open(imageItem);
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException
                                          or InvalidOperationException
                                          or System.ComponentModel.Win32Exception)
        {
            Trace.TraceError($"无法使用系统图片查看器打开聊天图片：{exception}");
        }
    }

    private void ApproveToolButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: CopilotChatApprovalToolItem approvalToolItem }
            && DataContext is ChatViewModel viewModel)
        {
            viewModel.ApproveTool(approvalToolItem);
        }
    }

    private void RejectToolButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: CopilotChatApprovalToolItem approvalToolItem }
            && DataContext is ChatViewModel viewModel)
        {
            viewModel.RejectTool(approvalToolItem);
        }
    }

    private async void AddImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ChatViewModel viewModel
            || TopLevel.GetTopLevel(this)?.StorageProvider is not { } storageProvider)
        {
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要附加的图片",
            AllowMultiple = true,
            FileTypeFilter = [ImageFileType],
        });
        foreach (IStorageFile file in files)
        {
            try
            {
                await using Stream stream = await file.OpenReadAsync();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                byte[] data = buffer.ToArray();
                using var validationStream = new MemoryStream(data, writable: false);
                using var bitmap = new Bitmap(validationStream);
                if (!viewModel.TryAddImageAttachment(file.Name, data))
                {
                    await viewModel.AddSystemNoticeAsync($"不支持图片文件：{file.Name}");
                }
            }
            catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or InvalidOperationException
                                              or ArgumentException)
            {
                await viewModel.AddSystemNoticeAsync($"无法附加图片 {file.Name}：{exception.Message}");
            }
        }
    }

    private void RemoveImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: ImageAttachmentViewModel attachment }
            && DataContext is ChatViewModel viewModel)
        {
            viewModel.RemoveImageAttachment(attachment);
        }
    }

    private async void MessageInputTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true;
            if (!await TryPasteClipboardImageAsync() && sender is TextBox textBox)
            {
                textBox.Paste();
            }

            return;
        }

        if (e.Key != Key.Enter
            || !e.KeyModifiers.HasFlag(KeyModifiers.Control)
            || DataContext is not ChatViewModel viewModel
            || !viewModel.SendCommand.CanExecute(null))
        {
            return;
        }

        e.Handled = true;
        viewModel.SendCommand.Execute(null);
    }

    private async Task<bool> TryPasteClipboardImageAsync()
    {
        if (DataContext is not ChatViewModel viewModel
            || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return false;
        }

        IAsyncDataTransfer? dataTransfer = await clipboard.TryGetDataAsync();
        if (dataTransfer is null)
        {
            return false;
        }

        try
        {
            object? clipboardData = await dataTransfer.TryGetValueAsync(DataFormat.Bitmap);
            byte[]? imageData = await GetClipboardImageBytesAsync(clipboardData);
            if (imageData is null)
            {
                return false;
            }

            using var validationStream = new MemoryStream(imageData, writable: false);
            using var bitmap = new Bitmap(validationStream);
            return viewModel.TryAddImageAttachment($"clipboard-{DateTime.Now:yyyyMMdd-HHmmss}.png", imageData);
        }
        catch (Exception exception) when (exception is IOException
                                          or InvalidOperationException
                                          or ArgumentException)
        {
            await viewModel.AddSystemNoticeAsync($"无法粘贴剪贴板图片：{exception.Message}");
            return true;
        }
    }

    private static async Task<byte[]?> GetClipboardImageBytesAsync(object? clipboardData)
    {
        switch (clipboardData)
        {
            case byte[] bytes:
                return bytes;
            case MemoryStream memoryStream:
                return memoryStream.ToArray();
            case Stream stream:
                using (var buffer = new MemoryStream())
                {
                    await stream.CopyToAsync(buffer);
                    return buffer.ToArray();
                }
            case Bitmap bitmap:
                using (var buffer = new MemoryStream())
                {
#pragma warning disable CS0618
                    bitmap.Save(buffer);
#pragma warning restore CS0618
                    return buffer.ToArray();
                }
            default:
                return null;
        }
    }

    private async Task SetClipboardTextAsync(string text)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard is null)
        {
            return;
        }

        var dataTransfer = new DataTransfer();
        dataTransfer.Add(DataTransferItem.CreateText(text));
        await topLevel.Clipboard.SetDataAsync(dataTransfer);
    }
}
