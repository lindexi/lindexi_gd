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
        Reason = string.Empty;
        Content = content;
        CreatedTime = DateTimeOffset.Now;
        TimeText = CreatedTime.ToString("HH:mm");
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
            OnPropertyChanged(nameof(HasContent));
            OnPropertyChanged(nameof(HasReasonAndContent));
            OnPropertyChanged(nameof(FullContent));
        }
    }

    public string Reason
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasReason));
            OnPropertyChanged(nameof(HasReasonAndContent));
            OnPropertyChanged(nameof(FullContent));
        }
    }

    public bool HasContent => !string.IsNullOrEmpty(Content);

    public bool HasReason => !string.IsNullOrEmpty(Reason);

    public bool HasReasonAndContent => HasReason && HasContent;

    public string FullContent
    {
        get
        {
            if (!HasReason)
            {
                return Content;
            }

            if (!HasContent)
            {
                return $"思考：{Environment.NewLine}{Reason}";
            }

            return $"思考：{Environment.NewLine}{Reason}{Environment.NewLine}--------{Environment.NewLine}{Content}";
        }
    }

    public DateTimeOffset CreatedTime { get; }

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