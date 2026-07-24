using System;
using System.ComponentModel;
using System.Threading.Tasks;

using AgentLib.Model;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Views;

/// <summary>
/// 显示当前编程助手会话。
/// </summary>
public partial class ChatView : UserControl
{
    private readonly ChatAutoScrollState _autoScrollState = new();
    private ChatViewModel? _subscribedViewModel;

    /// <summary>
    /// 初始化聊天视图。
    /// </summary>
    public ChatView()
    {
        InitializeComponent();
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

    private void MessageInputTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
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
