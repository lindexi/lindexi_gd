namespace AgentLib.Model;

/// <summary>
/// 表示模型思考片段。
/// </summary>
public sealed class CopilotChatReasoningItem : NotifyBase, ICopilotChatMessageItem
{
    /// <summary>
    /// 使用指定文本创建推理片段。
    /// </summary>
    /// <param name="text">推理文本。</param>
    public CopilotChatReasoningItem(string text)
    {
        Text = text;
    }

    /// <summary>
    /// 推理文本内容。
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
    /// 是否有推理文本。
    /// </summary>
    public bool HasText => !string.IsNullOrEmpty(Text);
}