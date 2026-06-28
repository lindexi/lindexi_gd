using AgentLib.Model;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using ChatRoom.AvaloniaShell.ViewModels;

using System.Threading.Tasks;

namespace ChatRoom.AvaloniaShell.Views;

/// <summary>
/// 中栏聊天消息区视图。
/// </summary>
public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
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
