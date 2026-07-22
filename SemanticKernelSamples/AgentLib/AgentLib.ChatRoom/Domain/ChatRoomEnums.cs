namespace AgentLib.ChatRoom.Domain;

/// <summary>
/// 角色在聊天室中的参与模式。
/// </summary>
public enum ChatRoomParticipationMode
{
    /// <summary>
    /// 默认参与自动循环。
    /// </summary>
    AlwaysParticipate,

    /// <summary>
    /// 仅在被明确提及时参与。
    /// </summary>
    MentionOnly,
}

/// <summary>
/// 角色运行时的稳定执行种类。
/// </summary>
public enum ChatRoomRoleExecutionKind
{
    /// <summary>
    /// 使用标准聊天角色运行时。
    /// </summary>
    Standard,

    /// <summary>
    /// 使用编程角色运行时。
    /// </summary>
    Coding,
}

/// <summary>
/// 公开消息种类。
/// </summary>
public enum ChatRoomMessageKind
{
    /// <summary>
    /// 人类消息。
    /// </summary>
    Human,

    /// <summary>
    /// AI 助手消息。
    /// </summary>
    Assistant,

    /// <summary>
    /// 系统消息。
    /// </summary>
    System,
}

/// <summary>
/// 单次角色执行的状态。
/// </summary>
public enum ChatRoomExecutionStatus
{
    /// <summary>
    /// 已排队但尚未开始。
    /// </summary>
    Pending,

    /// <summary>
    /// 正在执行。
    /// </summary>
    Running,

    /// <summary>
    /// 正在等待人工审批。
    /// </summary>
    AwaitingApproval,

    /// <summary>
    /// 正在提交候选结果。
    /// </summary>
    Completing,

    /// <summary>
    /// 已成功完成。
    /// </summary>
    Completed,

    /// <summary>
    /// 执行失败。
    /// </summary>
    Failed,

    /// <summary>
    /// 已取消。
    /// </summary>
    Canceled,

    /// <summary>
    /// 调用方已放弃等待，但底层任务仍可能运行。
    /// </summary>
    Abandoned,

    /// <summary>
    /// 底层任务在关闭期限内未能终止。
    /// </summary>
    Stuck,
}

/// <summary>
/// 房间持久化健康状态。
/// </summary>
public enum ChatRoomPersistenceHealth
{
    /// <summary>
    /// 当前内存修订已持久化。
    /// </summary>
    Clean,

    /// <summary>
    /// 存在尚未持久化的内存修订。
    /// </summary>
    Dirty,

    /// <summary>
    /// 最近一次持久化失败。
    /// </summary>
    Faulted,
}

/// <summary>
/// 房间生命周期状态。
/// </summary>
public enum ChatRoomLifecycleStatus
{
    /// <summary>
    /// 房间可接受命令。
    /// </summary>
    Open,

    /// <summary>
    /// 房间正在停止执行并释放资源。
    /// </summary>
    Closing,

    /// <summary>
    /// 房间已关闭。
    /// </summary>
    Closed,

    /// <summary>
    /// 房间关闭过程中发生不可恢复错误。
    /// </summary>
    CloseFaulted,
}

/// <summary>
/// 自动循环运行状态。
/// </summary>
public enum ChatRoomAutoLoopStatus
{
    /// <summary>
    /// 自动循环未运行。
    /// </summary>
    Idle,

    /// <summary>
    /// 自动循环正在调度角色。
    /// </summary>
    Running,

    /// <summary>
    /// 自动循环正在等待当前执行取消。
    /// </summary>
    Stopping,
}
