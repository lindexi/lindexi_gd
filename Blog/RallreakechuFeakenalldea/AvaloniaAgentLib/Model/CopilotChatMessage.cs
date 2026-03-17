using Avalonia.Layout;
using Avalonia.Media;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaAgentLib.Model;

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
