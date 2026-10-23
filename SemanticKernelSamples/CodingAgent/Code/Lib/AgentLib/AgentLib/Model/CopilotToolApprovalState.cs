namespace AgentLib.Model;

/// <summary>
/// 表示工具审批的状态。
/// </summary>
public enum CopilotToolApprovalState
{
    /// <summary>
    /// 等待审批。
    /// </summary>
    Pending,

    /// <summary>
    /// 已同意。
    /// </summary>
    Approved,

    /// <summary>
    /// 已拒绝。
    /// </summary>
    Rejected,

    /// <summary>
    /// 已取消。
    /// </summary>
    Canceled
}