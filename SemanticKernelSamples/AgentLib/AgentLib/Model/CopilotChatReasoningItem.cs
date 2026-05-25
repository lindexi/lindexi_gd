namespace AgentLib.Model;

/// <summary>
/// 表示模型思考片段。
/// </summary>
public sealed class CopilotChatReasoningItem : NotifyBase, ICopilotChatMessageItem
{
    public CopilotChatReasoningItem(string text)
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