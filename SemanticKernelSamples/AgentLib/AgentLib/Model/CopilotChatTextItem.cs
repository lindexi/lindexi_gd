using System;
using System.Collections.Generic;
using System.Linq;

namespace AgentLib.Model;

/// <summary>
/// 表示普通文本输出片段。
/// </summary>
public sealed class CopilotChatTextItem : NotifyBase, ICopilotChatMessageItem
{
    /// <summary>
    /// 使用指定文本创建文本片段。
    /// </summary>
    /// <param name="text">文本内容。</param>
    public CopilotChatTextItem(string text)
    {
        Text = text;
    }

    /// <summary>
    /// 文本内容。
    /// </summary>
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

    /// <summary>
    /// 是否有文本内容。
    /// </summary>
    public bool HasText => !string.IsNullOrEmpty(Text);
}