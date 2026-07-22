using AgentLib.ChatRoom.Domain;

namespace AgentLib.ChatRoom.Coordination;

/// <summary>
/// 聊天室状态变化种类。
/// </summary>
public enum ChatRoomChangeKind
{
    /// <summary>
    /// 房间完成初始化或恢复。
    /// </summary>
    Initialized,

    /// <summary>
    /// 房间元数据发生变化。
    /// </summary>
    RoomUpdated,

    /// <summary>
    /// 角色集合发生变化。
    /// </summary>
    RolesChanged,

    /// <summary>
    /// 公开消息已提交。
    /// </summary>
    MessageCommitted,

    /// <summary>
    /// 执行状态发生变化。
    /// </summary>
    ExecutionChanged,

    /// <summary>
    /// 自动循环状态发生变化。
    /// </summary>
    AutoLoopChanged,

    /// <summary>
    /// 持久化健康状态发生变化。
    /// </summary>
    PersistenceChanged,

    /// <summary>
    /// 生命周期状态发生变化。
    /// </summary>
    LifecycleChanged,
}

/// <summary>
/// 核心发布给应用层和 UI 投影层的不可变变化通知。
/// </summary>
public sealed record ChatRoomChange
{
    /// <summary>
    /// 创建变化通知。
    /// </summary>
    public ChatRoomChange(
        ChatRoomChangeKind kind,
        ChatRoomState state,
        long eventSequence,
        DateTimeOffset occurredAt,
        Guid? executionId = null,
        Guid? messageId = null,
        string? roleId = null,
        string? detail = null)
    {
        if (!Enum.IsDefined(typeof(ChatRoomChangeKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
        ArgumentNullException.ThrowIfNull(state);
        if (eventSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eventSequence));
        }

        Kind = kind;
        State = state;
        EventSequence = eventSequence;
        OccurredAt = occurredAt;
        ExecutionId = executionId;
        MessageId = messageId;
        RoleId = NormalizeOptionalValue(roleId);
        Detail = NormalizeOptionalValue(detail);
    }

    /// <summary>
    /// 变化种类。
    /// </summary>
    public ChatRoomChangeKind Kind { get; }

    /// <summary>
    /// 变化后的完整不可变状态。
    /// </summary>
    public ChatRoomState State { get; }

    /// <summary>
    /// 房间稳定标识。
    /// </summary>
    public Guid RoomId => State.RoomId;

    /// <summary>
    /// 当前进程内房间实例标识。
    /// </summary>
    public Guid RoomInstanceId => State.RoomInstanceId;

    /// <summary>
    /// 房间内单调递增的事件序号。
    /// </summary>
    public long EventSequence { get; }

    /// <summary>
    /// 变化对应的状态修订号。
    /// </summary>
    public long StateRevision => State.Revision;

    /// <summary>
    /// 变化发生时间。
    /// </summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// 关联执行标识。
    /// </summary>
    public Guid? ExecutionId { get; }

    /// <summary>
    /// 关联消息标识。
    /// </summary>
    public Guid? MessageId { get; }

    /// <summary>
    /// 关联角色标识。
    /// </summary>
    public string? RoleId { get; }

    /// <summary>
    /// 可选诊断摘要。
    /// </summary>
    public string? Detail { get; }

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// 流式执行增量种类。
/// </summary>
public enum ChatRoomStreamDeltaKind
{
    /// <summary>
    /// 面向用户的公开文本。
    /// </summary>
    PublicText,

    /// <summary>
    /// 可选的推理摘要。
    /// </summary>
    Reasoning,

    /// <summary>
    /// 工具执行状态摘要。
    /// </summary>
    ToolStatus,
}

/// <summary>
/// UI 中立的流式执行增量。
/// </summary>
public sealed record ChatRoomStreamDelta
{
    /// <summary>
    /// 创建流式增量。
    /// </summary>
    public ChatRoomStreamDelta(
        Guid roomId,
        Guid roomInstanceId,
        long eventSequence,
        Guid executionId,
        string roleId,
        ChatRoomStreamDeltaKind kind,
        string content)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("房间标识不能为空。", nameof(roomId));
        }
        if (roomInstanceId == Guid.Empty)
        {
            throw new ArgumentException("房间实例标识不能为空。", nameof(roomInstanceId));
        }
        if (eventSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eventSequence));
        }
        if (executionId == Guid.Empty)
        {
            throw new ArgumentException("执行标识不能为空。", nameof(executionId));
        }
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("角色标识不能为空或空白。", nameof(roleId));
        }
        if (!Enum.IsDefined(typeof(ChatRoomStreamDeltaKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
        ArgumentNullException.ThrowIfNull(content);

        RoomId = roomId;
        RoomInstanceId = roomInstanceId;
        EventSequence = eventSequence;
        ExecutionId = executionId;
        RoleId = roleId.Trim();
        Kind = kind;
        Content = content;
    }

    public Guid RoomId { get; }

    public Guid RoomInstanceId { get; }

    public long EventSequence { get; }

    public Guid ExecutionId { get; }

    public string RoleId { get; }

    public ChatRoomStreamDeltaKind Kind { get; }

    public string Content { get; }
}

/// <summary>
/// UI 中立的审批请求。
/// </summary>
public sealed record ChatRoomApprovalRequest
{
    /// <summary>
    /// 创建审批请求。
    /// </summary>
    public ChatRoomApprovalRequest(
        Guid roomId,
        Guid roomInstanceId,
        long eventSequence,
        Guid executionId,
        string approvalId,
        string roleId,
        string title,
        string? detail = null)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("房间标识不能为空。", nameof(roomId));
        }
        if (roomInstanceId == Guid.Empty)
        {
            throw new ArgumentException("房间实例标识不能为空。", nameof(roomInstanceId));
        }
        if (executionId == Guid.Empty)
        {
            throw new ArgumentException("执行标识不能为空。", nameof(executionId));
        }
        if (eventSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eventSequence));
        }

        RoomId = roomId;
        RoomInstanceId = roomInstanceId;
        EventSequence = eventSequence;
        ExecutionId = executionId;
        ApprovalId = NormalizeRequired(approvalId, nameof(approvalId));
        RoleId = NormalizeRequired(roleId, nameof(roleId));
        Title = NormalizeRequired(title, nameof(title));
        Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim();
    }

    public Guid RoomId { get; }

    public Guid RoomInstanceId { get; }

    public long EventSequence { get; }

    public Guid ExecutionId { get; }

    public string ApprovalId { get; }

    public string RoleId { get; }

    public string Title { get; }

    public string? Detail { get; }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("值不能为空或空白。", parameterName);
        }

        return value.Trim();
    }
}

/// <summary>
/// 审批响应决定。
/// </summary>
public enum ChatRoomApprovalDecision
{
    Approved,
    Rejected,
}

/// <summary>
/// 按房间实例、执行和审批标识精确路由的审批响应。
/// </summary>
public sealed record ChatRoomApprovalResponse
{
    /// <summary>
    /// 创建审批响应。
    /// </summary>
    public ChatRoomApprovalResponse(
        Guid roomId,
        Guid roomInstanceId,
        Guid executionId,
        string approvalId,
        ChatRoomApprovalDecision decision)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("房间标识不能为空。", nameof(roomId));
        }
        if (roomInstanceId == Guid.Empty)
        {
            throw new ArgumentException("房间实例标识不能为空。", nameof(roomInstanceId));
        }
        if (executionId == Guid.Empty)
        {
            throw new ArgumentException("执行标识不能为空。", nameof(executionId));
        }
        if (string.IsNullOrWhiteSpace(approvalId))
        {
            throw new ArgumentException("审批标识不能为空或空白。", nameof(approvalId));
        }
        if (!Enum.IsDefined(typeof(ChatRoomApprovalDecision), decision))
        {
            throw new ArgumentOutOfRangeException(nameof(decision));
        }

        RoomId = roomId;
        RoomInstanceId = roomInstanceId;
        ExecutionId = executionId;
        ApprovalId = approvalId.Trim();
        Decision = decision;
    }

    public Guid RoomId { get; }

    public Guid RoomInstanceId { get; }

    public Guid ExecutionId { get; }

    public string ApprovalId { get; }

    public ChatRoomApprovalDecision Decision { get; }
}

/// <summary>
/// 角色执行期间发布的 UI 中立瞬态事件。
/// </summary>
public abstract record ChatRoomExecutionEvent
{
    private protected ChatRoomExecutionEvent(long eventSequence)
    {
        if (eventSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eventSequence));
        }

        EventSequence = eventSequence;
    }

    public long EventSequence { get; }
}

/// <summary>
/// 流式增量事件。
/// </summary>
public sealed record ChatRoomStreamDeltaEvent : ChatRoomExecutionEvent
{
    public ChatRoomStreamDeltaEvent(ChatRoomStreamDelta delta)
        : base(delta?.EventSequence ?? throw new ArgumentNullException(nameof(delta)))
    {
        Delta = delta;
    }

    public ChatRoomStreamDelta Delta { get; }
}

/// <summary>
/// 审批请求事件。
/// </summary>
public sealed record ChatRoomApprovalRequestedEvent : ChatRoomExecutionEvent
{
    public ChatRoomApprovalRequestedEvent(ChatRoomApprovalRequest request)
        : base(request?.EventSequence ?? throw new ArgumentNullException(nameof(request)))
    {
        Request = request;
    }

    public ChatRoomApprovalRequest Request { get; }
}
