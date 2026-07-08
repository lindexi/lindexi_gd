using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace PptxGenerator;

public partial class CharUserControl : UserControl
{
    private ScrollViewer? _chatScrollViewer;
    private INotifyCollectionChanged? _chatMessages;
    private bool _isChatAtBottom = true;

    public CharUserControl()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
        DataContextChanged += OnDataContextChanged;
    }

    public MainWindowViewModel ViewModel => DataContext as MainWindowViewModel ?? throw new InvalidCastException("DataContext must be of type MainWindowViewModel.");

    private async void ReduceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.StatusText = "正在总结对话...";
        try
        {
            await ViewModel.SlideChatManager.Pipeline.ChatManager.ReduceSessionAsync();
            ViewModel.StatusText = "总结完成";
        }
        catch (Exception)
        {
            ViewModel.StatusText = "总结失败";
        }
    }

    private async void AttachImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "选择图片文件",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("图片文件")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" }
                }
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);
        if (files is { Count: > 0 })
        {
            var filePaths = files.Select(f => f.Path.LocalPath);
            ViewModel.AddAttachedImageFiles(filePaths);
        }
    }

    private void CloseAttachedImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: FileInfo fileInfo })
        {
            ViewModel.AttachedImageFiles.Remove(fileInfo);
        }
    }

    private void AttachedImageBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button)
        {
            return;
        }

        if (sender is not Control { DataContext: FileInfo fileInfo } || !fileInfo.Exists)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(fileInfo.FullName) { UseShellExecute = true });
        }
        catch (Exception)
        {
            ViewModel.StatusText = "打开图片失败";
        }
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var chatMessagesListBox = this.FindControl<ListBox>("ChatMessagesListBox");
            _chatScrollViewer = chatMessagesListBox?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (_chatScrollViewer is not null)
            {
                _chatScrollViewer.ScrollChanged += ChatScrollViewer_OnScrollChanged;
                UpdateChatBottomState();
            }
        }, DispatcherPriority.Loaded);
        SubscribeChatMessages();
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_chatScrollViewer is not null)
        {
            _chatScrollViewer.ScrollChanged -= ChatScrollViewer_OnScrollChanged;
            _chatScrollViewer = null;
        }

        UnsubscribeChatMessages();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UnsubscribeChatMessages();
        SubscribeChatMessages();
    }

    private void SubscribeChatMessages()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        _chatMessages = viewModel.ChatMessages;
        _chatMessages.CollectionChanged += ChatMessages_OnCollectionChanged;
    }

    private void UnsubscribeChatMessages()
    {
        if (_chatMessages is not null)
        {
            _chatMessages.CollectionChanged -= ChatMessages_OnCollectionChanged;
            _chatMessages = null;
        }
    }

    private void ChatMessages_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isChatAtBottom)
        {
            Dispatcher.UIThread.Post(ScrollChatToBottom, DispatcherPriority.Background);
        }
        else
        {
            SetScrollToBottomButtonVisible(true);
        }
    }

    private void ChatScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateChatBottomState();
    }

    private void ScrollToBottomButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ScrollChatToBottom();
    }

    private void ScrollChatToBottom()
    {
        if (_chatScrollViewer is null)
        {
            return;
        }

        _chatScrollViewer.Offset = new Vector(_chatScrollViewer.Offset.X, _chatScrollViewer.Extent.Height);
        _isChatAtBottom = true;
        SetScrollToBottomButtonVisible(false);
    }

    private void UpdateChatBottomState()
    {
        if (_chatScrollViewer is null)
        {
            return;
        }

        var bottomOffset = Math.Max(0, _chatScrollViewer.Extent.Height - _chatScrollViewer.Viewport.Height);
        _isChatAtBottom = bottomOffset - _chatScrollViewer.Offset.Y < 2;
        SetScrollToBottomButtonVisible(!_isChatAtBottom);
    }

    private void SetScrollToBottomButtonVisible(bool isVisible)
    {
        var button = this.FindControl<Button>("ScrollToBottomButton");
        if (button is not null)
        {
            button.IsVisible = isVisible;
        }
    }
}
