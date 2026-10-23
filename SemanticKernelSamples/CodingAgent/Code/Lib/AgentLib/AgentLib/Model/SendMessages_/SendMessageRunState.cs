namespace AgentLib.Model;

/// <summary>
/// 表示一次聊天发送流程执行完成后的状态。
/// </summary>
/// <param name="IsSuccess">是否成功完成流式发送。</param>
/// <param name="WasCanceled">是否因取消而结束。</param>
public record SendMessageRunState(bool IsSuccess, bool WasCanceled);