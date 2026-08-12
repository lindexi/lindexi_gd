namespace AgentLib.Model;

/// <summary>
/// 表示工具调用片段。
/// </summary>
public sealed class CopilotChatToolItem : NotifyBase, ICopilotChatMessageItem
{
    /// <summary>
    /// 创建工具调用片段。
    /// </summary>
    public CopilotChatToolItem(
        string callId,
        string toolName,
        string? inputText,
        string? outputText = null,
        ToolCallPresentation? presentation = null)
    {
        CallId = callId;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? "工具" : toolName;
        InputText = inputText ?? string.Empty;
        OutputText = outputText ?? string.Empty;
        ApplyPresentation(presentation);
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
    /// 主要操作对象。
    /// </summary>
    public string PrimaryText
    {
        get => _primaryText;
        internal set
        {
            if (!SetField(ref _primaryText, value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(nameof(HasPrimaryText));
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(ToolTipText));
        }
    }

    private string _primaryText = string.Empty;

    /// <summary>
    /// 是否有主要操作对象。
    /// </summary>
    public bool HasPrimaryText => !string.IsNullOrEmpty(PrimaryText);

    /// <summary>
    /// 次要摘要文本。
    /// </summary>
    public string SecondaryText
    {
        get => _secondaryText;
        internal set
        {
            if (!SetField(ref _secondaryText, value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSecondaryText));
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(ToolTipText));
        }
    }

    private string _secondaryText = string.Empty;

    /// <summary>
    /// 是否有次要摘要文本。
    /// </summary>
    public bool HasSecondaryText => !string.IsNullOrEmpty(SecondaryText);

    /// <summary>
    /// 完整目标文本。
    /// </summary>
    public string FullTargetText
    {
        get => _fullTargetText;
        internal set
        {
            if (SetField(ref _fullTargetText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(ToolTipText));
            }
        }
    }

    private string _fullTargetText = string.Empty;

    /// <summary>
    /// 组合摘要文本。
    /// </summary>
    public string SummaryText => string.Join(" · ", new[] { PrimaryText, SecondaryText }.Where(value => !string.IsNullOrWhiteSpace(value)));

    /// <summary>
    /// 标题提示文本。
    /// </summary>
    public string ToolTipText => !string.IsNullOrWhiteSpace(FullTargetText) ? FullTargetText : SummaryText;

    /// <summary>
    /// 工具输入文本。
    /// </summary>
    public string InputText
    {
        get => _inputText;
        internal set
        {
            if (SetField(ref _inputText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasInputText));
            }
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
            if (SetField(ref _outputText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasOutputText));
            }
        }
    }

    private string _outputText = string.Empty;

    /// <summary>
    /// 是否有输出文本。
    /// </summary>
    public bool HasOutputText => !string.IsNullOrEmpty(OutputText);

    internal void ApplyPresentation(ToolCallPresentation? presentation)
    {
        PrimaryText = presentation?.PrimaryText ?? string.Empty;
        SecondaryText = presentation?.SecondaryText ?? string.Empty;
        FullTargetText = presentation?.FullTargetText ?? string.Empty;
    }

    /// <inheritdoc/>
    ICopilotChatMessageItem ICopilotChatMessageItem.Clone() => new CopilotChatToolItem(
        CallId,
        ToolName,
        InputText,
        OutputText,
        new ToolCallPresentation(PrimaryText, SecondaryText, FullTargetText));
}
