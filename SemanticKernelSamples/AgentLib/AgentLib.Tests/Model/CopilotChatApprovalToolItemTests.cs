using AgentLib.Model;

using System.Reflection;

namespace AgentLib.Tests.Model;

[TestClass]
public class CopilotChatApprovalToolItemTests
{
    [TestMethod]
    [Description("审批工具片段同意后应更新状态并结束等待")]
    public async Task WaitForApprovalAsync_WhenApproved_CompletesWithApprovedState()
    {
        var approvalToolItem = new CopilotChatApprovalToolItem("call-1", "DeleteFile", "demo.txt", "请确认是否删除文件。");

        Task<CopilotToolApprovalState> waitTask = WaitForApprovalAsync(approvalToolItem, CancellationToken.None);
        InvokeApprovalMethod(approvalToolItem, "Approve");
        CopilotToolApprovalState state = await waitTask;

        Assert.AreEqual(CopilotToolApprovalState.Approved, state);
        Assert.IsFalse(approvalToolItem.CanRespondToApproval);
        Assert.AreEqual("已同意", approvalToolItem.ApprovalStateText);
    }

    [TestMethod]
    [Description("审批工具片段拒绝后应记录状态和备注")]
    public async Task WaitForApprovalAsync_WhenRejected_CompletesWithRejectedState()
    {
        var approvalToolItem = new CopilotChatApprovalToolItem("call-1", "DeleteFile", "demo.txt", "请确认是否删除文件。");

        Task<CopilotToolApprovalState> waitTask = WaitForApprovalAsync(approvalToolItem, CancellationToken.None);
        InvokeApprovalMethod(approvalToolItem, "Reject", "不允许删除");
        CopilotToolApprovalState state = await waitTask;

        Assert.AreEqual(CopilotToolApprovalState.Rejected, state);
        Assert.AreEqual("不允许删除", approvalToolItem.DecisionReason);
        Assert.AreEqual("已拒绝", approvalToolItem.ApprovalStateText);
    }

    private static async Task<CopilotToolApprovalState> WaitForApprovalAsync(CopilotChatApprovalToolItem approvalToolItem,
        CancellationToken cancellationToken)
    {
        MethodInfo methodInfo = typeof(CopilotChatApprovalToolItem).GetMethod("WaitForApprovalAsync", BindingFlags.Instance | BindingFlags.NonPublic)
                                ?? throw new InvalidOperationException("未找到 WaitForApprovalAsync 方法。");
        object? result = methodInfo.Invoke(approvalToolItem, [cancellationToken]);
        if (result is not Task<CopilotToolApprovalState> waitTask)
        {
            throw new InvalidOperationException("WaitForApprovalAsync 未返回有效任务。");
        }

        return await waitTask;
    }

    private static void InvokeApprovalMethod(CopilotChatApprovalToolItem approvalToolItem, string methodName, params object?[] arguments)
    {
        MethodInfo methodInfo = typeof(CopilotChatApprovalToolItem).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                                ?? throw new InvalidOperationException($"未找到 {methodName} 方法。");
        _ = methodInfo.Invoke(approvalToolItem, arguments);
    }
}