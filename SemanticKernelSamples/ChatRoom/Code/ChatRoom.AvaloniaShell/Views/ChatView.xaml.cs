using System;
using System.Threading.Tasks;

using AgentLib.Model;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using ChatRoom.AvaloniaShell.ViewModels;

namespace ChatRoom.AvaloniaShell.Views;

/// <summary>
/// 中栏聊天消息区视图。
/// </summary>
public partial class ChatView : UserControl
{
    private ChatViewModel? _subscribedViewModel;

    public ChatView()
    {
        InitializeComponent();
        InputTextBox.AddHandler(KeyDownEvent, InputTextBox_OnKeyDown, RoutingStrategies.Tunnel);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, global::System.EventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.InputFocusRequested -= OnInputFocusRequested;
            _subscribedViewModel = null;
        }

        if (DataContext is ChatViewModel newViewModel)
        {
            _subscribedViewModel = newViewModel;
            _subscribedViewModel.InputFocusRequested += OnInputFocusRequested;
        }
    }

    private void OnInputFocusRequested(object? sender, global::System.EventArgs e)
    {
        InputTextBox.Focus();
        InputTextBox.CaretIndex = InputTextBox.Text?.Length ?? 0;
    }

    private async void CopyContentMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: MessageItemViewModel vm })
        {
            return;
        }

        await SetClipboardTextAsync(vm.Content);
    }

    private async void CopyFullMessageMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: MessageItemViewModel vm })
        {
            return;
        }

        await SetClipboardTextAsync(vm.FullContent);
    }

    private void ApproveToolButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: CopilotChatApprovalToolItem approvalToolItem })
        {
            return;
        }

        if (DataContext is not ChatViewModel vm)
        {
            return;
        }

        vm.ApproveTool(approvalToolItem);
    }

    private void RejectToolButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: CopilotChatApprovalToolItem approvalToolItem })
        {
            return;
        }

        if (DataContext is not ChatViewModel vm)
        {
            return;
        }

        vm.RejectTool(approvalToolItem);
    }

    private void InputTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+Enter 发送消息
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (DataContext is not ChatViewModel vm || !vm.CanSend)
            {
                return;
            }

            vm.SendCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async Task SetClipboardTextAsync(string text)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard is null)
        {
            return;
        }

        var dataTransfer = new DataTransfer();
        dataTransfer.Add(DataTransferItem.CreateText(text));
        await topLevel.Clipboard.SetDataAsync(dataTransfer);
    }
}
