namespace AgentLib.Model;

/// <summary>
/// 表示工具调用片段。
/// </summary>
public sealed class CopilotChatToolItem : NotifyBase, ICopilotChatMessageItem
{
    /// <summary>
    /// 创建工具调用片段。
    /// </summary>
    /// <param name="callId">调用 ID。</param>
    /// <param name="toolName">工具名称。</param>
    /// <param name="inputText">工具输入文本。</param>
    /// <param name="outputText">工具输出文本。</param>
    public CopilotChatToolItem(string callId, string toolName, string? inputText, string? outputText = null)
    {
        CallId = callId;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? "工具" : toolName;
        InputText = inputText ?? string.Empty;
        OutputText = outputText ?? string.Empty;
    }

    /// <summary>
    /// 工具调用 ID。
    /// </summary>
    public string CallId { get; }

    /// <summary>
    /// 工具名称。
    /// </summary>
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

    /// <summary>
    /// 工具显示名称。
    /// </summary>
    public string DisplayName => ToolName;

    /// <summary>
    /// 工具输入文本。
    /// </summary>
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

    /// <summary>
    /// 是否有输入文本。
    /// </summary>
    public bool HasInputText => !string.IsNullOrEmpty(InputText);

    /// <summary>
    /// 工具输出文本。
    /// </summary>
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

    /// <summary>
    /// 是否有输出文本。
    /// </summary>
    public bool HasOutputText => !string.IsNullOrEmpty(OutputText);

    /// <inheritdoc/>
    ICopilotChatMessageItem ICopilotChatMessageItem.Clone() => new CopilotChatToolItem(CallId, ToolName, InputText, OutputText);
}