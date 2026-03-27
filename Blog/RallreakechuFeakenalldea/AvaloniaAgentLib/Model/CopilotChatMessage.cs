using Microsoft.Extensions.AI;

using OpenAI.Chat;

using System;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace AvaloniaAgentLib.Model;

public sealed class CopilotChatMessage: NotifyBase
{
    public CopilotChatMessage(ChatRole role, string content)
    {
        Role = role;
        Content = content;
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

    public static CopilotChatMessage CreateUser(string content)
    {
        return new CopilotChatMessage(ChatRole.User, content);
    }

    public static CopilotChatMessage CreateAssistant(string content, bool isPresetInfo)
    {
        return new CopilotChatMessage(ChatRole.Assistant, content)
        {
            IsPresetInfo = isPresetInfo
        };
    }

    public ChatMessage ToChatMessage()
    {
        var chatMessage = this;
        return new ChatMessage(chatMessage.Role, chatMessage.Content);
    }
}