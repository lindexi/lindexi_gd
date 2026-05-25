namespace AgentLib.Model;

/// <summary>
/// 表示工具调用片段。
/// </summary>
public sealed class CopilotChatToolItem : NotifyBase, ICopilotChatMessageItem
{
    public CopilotChatToolItem(string callId, string toolName, string? inputText, string? outputText = null)
    {
        CallId = callId;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? "工具" : toolName;
        InputText = inputText ?? string.Empty;
        OutputText = outputText ?? string.Empty;
    }

    public string CallId { get; }

    public string ToolName
    {
        get => _toolName;
        internal set
        {
            string normalizedValue = string.IsNullOrWhiteSpace(value) ? "工具" : value;
            if (!SetField(ref _toolName, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(DisplayName));
        }
    }

    private string _toolName = string.Empty;

    public string DisplayName => ToolName;

    public string InputText
    {
        get => _inputText;
        internal set
        {
            string normalizedValue = value ?? string.Empty;
            if (!SetField(ref _inputText, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(HasInputText));
        }
    }

    private string _inputText = string.Empty;

    public bool HasInputText => !string.IsNullOrEmpty(InputText);

    public string OutputText
    {
        get => _outputText;
        internal set
        {
            string normalizedValue = value ?? string.Empty;
            if (!SetField(ref _outputText, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(HasOutputText));
        }
    }

    private string _outputText = string.Empty;

    public bool HasOutputText => !string.IsNullOrEmpty(OutputText);
}