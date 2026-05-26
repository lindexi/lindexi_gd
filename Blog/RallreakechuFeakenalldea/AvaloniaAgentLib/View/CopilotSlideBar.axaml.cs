using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using AgentLib.Model;

using AvaloniaAgentLib.ViewModel;

namespace AvaloniaAgentLib.View;

public partial class CopilotSlideBar : UserControl
{
    public static readonly StyledProperty<string?> ChatLogFolderProperty =
        AvaloniaProperty.Register<CopilotSlideBar, string?>(nameof(ChatLogFolder));

    private INotifyCollectionChanged? _currentChatMessages;

    public CopilotSlideBar()
    {
        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        SubscribeChatMessages(ViewModel.ChatMessages);

        InputTextBox.AddHandler(TextBox.KeyDownEvent, InputTextBox_OnKeyDown, handledEventsToo: true, routes: RoutingStrategies.Tunnel);
    }

    public CopilotViewModel ViewModel => (CopilotViewModel) DataContext!;

    public string? ChatLogFolder
    {
        get => GetValue(ChatLogFolderProperty);
        set => SetValue(ChatLogFolderProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ChatLogFolderProperty)
        {
            ViewModel.SetChatLogFolder(ChatLogFolder);
        }
    }

    private async void SendButton_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel.IsChatting)
        {
            ViewModel.CancelCurrentChat();
            return;
        }

        await SendMessageAsync();
    }

    private void InputTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _ = SendMessageAsync();
            e.Handled = true;
        }
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

        try
        {
            await ViewModel.SendMessageAsync(inputText, withHistory: true);
            if (!ViewModel.WasLastChatCanceled)
            {
                InputTextBox.Text = null;
            }

            await ScrollToBottomAsync();
        }
        catch (Exception)
        {
            InputTextBox.Text = originalInputText;
        }
    }

    private void NewSessionButton_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.CreateNewSession();
        InputTextBox.Text = null;
        _ = ScrollToBottomAsync();
    }

    private void ChatMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = ScrollToBottomAsync();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CopilotViewModel.ChatMessages))
        {
            return;
        }

        SubscribeChatMessages(ViewModel.ChatMessages);
        _ = ScrollToBottomAsync();
    }

    private void SubscribeChatMessages(INotifyCollectionChanged chatMessages)
    {
        if (ReferenceEquals(_currentChatMessages, chatMessages))
        {
            return;
        }

        if (_currentChatMessages is not null)
        {
            _currentChatMessages.CollectionChanged -= ChatMessages_CollectionChanged;
        }

        _currentChatMessages = chatMessages;
        _currentChatMessages.CollectionChanged += ChatMessages_CollectionChanged;
    }

    private async Task ScrollToBottomAsync()
    {
        await Task.Yield();
        ChatScrollViewer.ScrollToEnd();
    }

    private async void CopyContentMenuItem_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: CopilotChatMessage message })
        {
            return;
        }

        await SetClipboardTextAsync(message.Content);
    }

    private async void CopyContentAndReasonMenuItem_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: CopilotChatMessage message })
        {
            return;
        }

        await SetClipboardTextAsync(message.FullContent);
    }

    private async void CopyFullMessageMenuItem_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: CopilotChatMessage message })
        {
            return;
        }

        await SetClipboardTextAsync($"{message.Author} {message.TimeText}{Environment.NewLine}{message.FullContent}");
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

    private void SettingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.OpenSetting();
    }

    private void SessionSelector_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _ = ScrollToBottomAsync();
    }
}