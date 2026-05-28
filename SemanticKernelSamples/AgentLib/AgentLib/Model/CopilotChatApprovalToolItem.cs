using System.Threading;

namespace AgentLib.Model;

/// <summary>
/// 表示需要人工审批后才能继续执行的工具片段。
/// </summary>
public sealed class CopilotChatApprovalToolItem : NotifyBase, ICopilotChatMessageItem
{
    private readonly TaskCompletionSource<CopilotToolApprovalState> _approvalTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CopilotChatApprovalToolItem(string callId, string toolName, string? inputText, string? approvalDescription = null)
    {
        CallId = callId;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? "工具" : toolName;
        InputText = inputText ?? string.Empty;
        ApprovalDescription = approvalDescription;
    }

    public string CallId
    {
        get => _callId;
        internal set => _callId = string.IsNullOrWhiteSpace(value) ? _callId : value;
    }

    private string _callId = string.Empty;

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

    public bool HasApprovalDescription => !string.IsNullOrWhiteSpace(ApprovalDescription);

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

    public bool IsPendingApproval => ApprovalState == CopilotToolApprovalState.Pending;

    public bool CanRespondToApproval => IsPendingApproval;

    public string ApprovalStateText => ApprovalState switch
    {
        CopilotToolApprovalState.Pending => "等待审批",
        CopilotToolApprovalState.Approved => "已同意",
        CopilotToolApprovalState.Rejected => "已拒绝",
        CopilotToolApprovalState.Canceled => "已取消",
        _ => string.Empty
    };

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
}