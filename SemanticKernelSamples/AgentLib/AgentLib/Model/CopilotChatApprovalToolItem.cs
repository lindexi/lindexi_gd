using System.Threading;

namespace AgentLib.Model;

/// <summary>
/// 表示需要人工审批后才能继续执行的工具片段。
/// </summary>
public sealed class CopilotChatApprovalToolItem : NotifyBase, ICopilotChatMessageItem
{
    private readonly TaskCompletionSource<CopilotToolApprovalState> _approvalTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// 创建审批工具片段。
    /// </summary>
    /// <param name="callId">调用 ID。</param>
    /// <param name="toolName">工具名称。</param>
    /// <param name="inputText">工具输入文本。</param>
    /// <param name="approvalDescription">审批描述信息。</param>
    public CopilotChatApprovalToolItem(string callId, string toolName, string? inputText, string? approvalDescription = null)
    {
        CallId = callId;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? "工具" : toolName;
        InputText = inputText ?? string.Empty;
        ApprovalDescription = approvalDescription;
    }

    /// <summary>
        /// 工具调用 ID。
        /// </summary>
        public string CallId
    {
        get => _callId;
        internal set => _callId = string.IsNullOrWhiteSpace(value) ? _callId : value;
    }

    private string _callId = string.Empty;

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

    /// <summary>
    /// 审批描述信息。
    /// </summary>
    public string? ApprovalDescription
    {
        get => _approvalDescription;
        internal set
        {
            string? normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value;
            if (!SetField(ref _approvalDescription, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(HasApprovalDescription));
        }
    }

    private string? _approvalDescription;

    /// <summary>
    /// 是否有审批描述。
    /// </summary>
    public bool HasApprovalDescription => !string.IsNullOrWhiteSpace(ApprovalDescription);

    /// <summary>
    /// 当前审批状态。
    /// </summary>
    public CopilotToolApprovalState ApprovalState
    {
        get => _approvalState;
        private set
        {
            if (!SetField(ref _approvalState, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsPendingApproval));
            OnPropertyChanged(nameof(CanRespondToApproval));
            OnPropertyChanged(nameof(ApprovalStateText));
        }
    }

    private CopilotToolApprovalState _approvalState = CopilotToolApprovalState.Pending;

    /// <summary>
    /// 是否正在等待审批。
    /// </summary>
    public bool IsPendingApproval => ApprovalState == CopilotToolApprovalState.Pending;

    /// <summary>
    /// 是否可以响应审批（即是否处于待审批状态）。
    /// </summary>
    public bool CanRespondToApproval => IsPendingApproval;

    /// <summary>
    /// 审批状态的显示文本。
    /// </summary>
    public string ApprovalStateText => ApprovalState switch
    {
        CopilotToolApprovalState.Pending => "等待审批",
        CopilotToolApprovalState.Approved => "已同意",
        CopilotToolApprovalState.Rejected => "已拒绝",
        CopilotToolApprovalState.Canceled => "已取消",
        _ => string.Empty
    };

    /// <summary>
        /// 审批决策原因（拒绝时填写）。
        /// </summary>
        public string DecisionReason
    {
        get => _decisionReason;
        private set
        {
            string normalizedValue = value ?? string.Empty;
            if (!SetField(ref _decisionReason, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(HasDecisionReason));
        }
    }

    private string _decisionReason = string.Empty;

    /// <summary>
        /// 是否有决策原因。
        /// </summary>
        public bool HasDecisionReason => !string.IsNullOrEmpty(DecisionReason);

    internal async Task<CopilotToolApprovalState> WaitForApprovalAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _approvalTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Cancel();
            throw;
        }
    }

    internal void Approve()
    {
        if (!CanRespondToApproval)
        {
            return;
        }

        DecisionReason = string.Empty;
        ApprovalState = CopilotToolApprovalState.Approved;
        _approvalTaskCompletionSource.TrySetResult(CopilotToolApprovalState.Approved);
    }

    internal void Reject(string? reason)
    {
        if (!CanRespondToApproval)
        {
            return;
        }

        DecisionReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        ApprovalState = CopilotToolApprovalState.Rejected;
        _approvalTaskCompletionSource.TrySetResult(CopilotToolApprovalState.Rejected);
    }

    internal void Cancel()
    {
        if (!CanRespondToApproval)
        {
            return;
        }

        DecisionReason = string.Empty;
        ApprovalState = CopilotToolApprovalState.Canceled;
        _approvalTaskCompletionSource.TrySetCanceled();
    }

    /// <inheritdoc/>
    ICopilotChatMessageItem ICopilotChatMessageItem.Clone()
    {
        var clone = new CopilotChatApprovalToolItem(CallId, ToolName, InputText, ApprovalDescription);
        // 复制静态展示属性，但不复制运行时 TCS（展示用克隆不需要审批交互）
        clone.OutputText = OutputText;
        clone.ApprovalState = ApprovalState;
        clone.DecisionReason = DecisionReason;
        return clone;
    }
}