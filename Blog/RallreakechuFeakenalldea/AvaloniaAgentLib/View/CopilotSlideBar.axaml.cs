using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaAgentLib.View;

public partial class CopilotSlideBar : UserControl
{
    public ObservableCollection<CopilotChatMessage> ChatMessages { get; } = [];

    public CopilotSlideBar()
    {
        InitializeComponent();
        DataContext = this;
        AddAssistantWelcomeMessage();
    }

    private void SendButton_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        SendMessagePlaceholder();
    }

    private void InputTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            return;
        }

        SendMessagePlaceholder();
        e.Handled = true;
    }

    private void ClearButton_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        ChatMessages.Clear();
        AddAssistantWelcomeMessage();
    }

    private void SendMessagePlaceholder()
    {
        if (InputTextBox is null)
        {
            return;
        }

        var inputText = InputTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return;
        }

        ChatMessages.Add(CopilotChatMessage.CreateUser(inputText));
        ChatMessages.Add(CopilotChatMessage.CreateAssistant("消息已接收。待接入 Agent 后将返回真实回复。"));
        InputTextBox.Text = string.Empty;
    }

    private void AddAssistantWelcomeMessage()
    {
        ChatMessages.Add(CopilotChatMessage.CreateAssistant("你好，我是 Copilot。请开始输入你的问题。"));
    }
}

public sealed class CopilotChatMessage
{
    private static readonly IBrush UserBackground = new SolidColorBrush(Color.Parse("#FF2F6FED"));
    private static readonly IBrush AssistantBackground = new SolidColorBrush(Color.Parse("#FF2A2A2A"));
    private static readonly IBrush UserForeground = new SolidColorBrush(Color.Parse("#FFFFFFFF"));
    private static readonly IBrush AssistantForeground = new SolidColorBrush(Color.Parse("#FFF0F0F0"));

    private CopilotChatMessage(string author, string content, HorizontalAlignment horizontalAlignment, IBrush bubbleBackground, IBrush bubbleForeground)
    {
        Author = author;
        Content = content;
        HorizontalAlignment = horizontalAlignment;
        BubbleBackground = bubbleBackground;
        BubbleForeground = bubbleForeground;
        TimeText = DateTime.Now.ToString("HH:mm");
    }

    public string Author { get; }

    public string Content { get; }

    public string TimeText { get; }

    public HorizontalAlignment HorizontalAlignment { get; }

    public IBrush BubbleBackground { get; }

    public IBrush BubbleForeground { get; }

    internal static CopilotChatMessage CreateUser(string content)
    {
        return new CopilotChatMessage("你", content, HorizontalAlignment.Right, UserBackground, UserForeground);
    }

    internal static CopilotChatMessage CreateAssistant(string content)
    {
        return new CopilotChatMessage("Copilot", content, HorizontalAlignment.Left, AssistantBackground, AssistantForeground);
    }
}
