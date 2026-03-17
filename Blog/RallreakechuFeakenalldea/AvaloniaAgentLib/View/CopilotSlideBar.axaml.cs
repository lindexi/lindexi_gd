using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaAgentLib.Model;
using AvaloniaAgentLib.ViewModel;

namespace AvaloniaAgentLib.View;

public partial class CopilotSlideBar : UserControl
{
    private CancellationTokenSource? _sendMessageCts;

    public CopilotSlideBar()
    {
        InitializeComponent();

        ViewModel.ChatMessages.CollectionChanged += ChatMessages_CollectionChanged;
    }

    public CopilotViewModel ViewModel => (CopilotViewModel)DataContext!;

    private async void SendButton_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel.IsChatting)
        {
            _sendMessageCts?.Cancel();
            return;
        }

        await SendMessageAsync();
    }

    private void InputTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            return;
        }

        _ = SendMessageAsync();
        e.Handled = true;
    }

    private async Task SendMessageAsync()
    {
        if (ViewModel.IsChatting)
        {
            return;
        }

        var originalInputText = InputTextBox.Text;
        var inputText = originalInputText?.Trim();
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return;
        }

        _sendMessageCts?.Dispose();
        var sendMessageCts = new CancellationTokenSource();
        _sendMessageCts = sendMessageCts;

        try
        {
            await ViewModel.SendMessageAsync(inputText, sendMessageCts.Token);
            InputTextBox.Text = null;
            await ScrollToBottomAsync();
        }
        catch (OperationCanceledException) when (sendMessageCts.IsCancellationRequested)
        {
            InputTextBox.Text = originalInputText;
        }
        catch (Exception)
        {
            InputTextBox.Text = originalInputText;
        }
        finally
        {
            sendMessageCts.Dispose();
            if (ReferenceEquals(_sendMessageCts, sendMessageCts))
            {
                _sendMessageCts = null;
            }
        }
    }

    private void ClearButton_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.Clear();
    }

    private void ChatMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = ScrollToBottomAsync();
    }

    private async Task ScrollToBottomAsync()
    {
        await Task.Yield();
        ChatScrollViewer.ScrollToEnd();
    }

    private async void CopyMessageMenuItem_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: string messageText })
        {
            return;
        }

        await SetClipboardTextAsync(messageText);
    }

    private async void CopyFullMessageMenuItem_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: CopilotChatMessage message })
        {
            return;
        }

        await SetClipboardTextAsync($"{message.Author} {message.TimeText}{Environment.NewLine}{message.Content}");
    }

    private async Task SetClipboardTextAsync(string text)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard is null)
        {
            return;
        }

        await topLevel.Clipboard.SetTextAsync(text);
    }
}