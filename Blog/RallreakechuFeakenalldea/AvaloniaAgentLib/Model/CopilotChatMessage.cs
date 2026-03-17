using Avalonia.Layout;
using Avalonia.Media;

using Microsoft.Extensions.AI;

using ModelContextProtocol.Protocol;

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaAgentLib.Model;

public sealed class CopilotChatMessage: NotifyBase
{
    private static readonly IBrush UserBackground = new SolidColorBrush(Color.Parse("#FF2F6FED"));
    private static readonly IBrush AssistantBackground = new SolidColorBrush(Color.Parse("#FF2A2A2A"));
    private static readonly IBrush UserForeground = new SolidColorBrush(Color.Parse("#FFFFFFFF"));
    private static readonly IBrush AssistantForeground = new SolidColorBrush(Color.Parse("#FFF0F0F0"));

    public CopilotChatMessage(ChatRole role, string content, HorizontalAlignment horizontalAlignment, IBrush bubbleBackground, IBrush bubbleForeground)
    {
        Role = role;
        Content = content;
        HorizontalAlignment = horizontalAlignment;
        BubbleBackground = bubbleBackground;
        BubbleForeground = bubbleForeground;
        TimeText = DateTime.Now.ToString("HH:mm");
    }

    public ChatRole Role { get; }

    /// <summary>
    /// 是否预设的内容，预设内容不参与 GPT 信息
    /// </summary>
    public bool IsPresetInfo { set; get; }

    public string Author
    {
        get
        {
            if (Role == ChatRole.System)
            {
                return "系统";
            }
            else if (Role == ChatRole.User)
            {
                return "我";
            }
            else if (Role == ChatRole.Assistant)
            {
                return "Copilot";
            }

            return "";
        }
    }

    public string Content
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public string TimeText { get; }

    public HorizontalAlignment HorizontalAlignment { get; }

    public IBrush BubbleBackground { get; }

    public IBrush BubbleForeground { get; }

    public static CopilotChatMessage CreateUser(string content)
    {
        return new CopilotChatMessage(ChatRole.User, content, HorizontalAlignment.Right, UserBackground, UserForeground);
    }

    public static CopilotChatMessage CreateAssistant(string content, bool isPresetInfo)
    {
        return new CopilotChatMessage(ChatRole.Assistant, content, HorizontalAlignment.Left, AssistantBackground, AssistantForeground)
        {
            IsPresetInfo = isPresetInfo
        };
    }
}