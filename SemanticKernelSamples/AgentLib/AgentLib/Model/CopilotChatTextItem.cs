using System;
using System.Collections.Generic;
using System.Linq;

namespace AgentLib.Model;

/// <summary>
/// 表示普通文本输出片段。
/// </summary>
public sealed class CopilotChatTextItem : NotifyBase, ICopilotChatMessageItem
{
    public CopilotChatTextItem(string text)
    {
        Text = text;
    }

    public string Text
    {
        get => _text;
        internal set
        {
            if (!SetField(ref _text, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasText));
        }
    }

    private string _text = string.Empty;

    public bool HasText => !string.IsNullOrEmpty(Text);
}