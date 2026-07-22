using AgentLib.ChatRoom.Domain;
using AgentLib.ChatRoom.Runtime;

namespace AgentLib.ChatRoom.Coordination;

/// <summary>
/// 聊天室命令基类。
/// </summary>
public abstract record ChatRoomCommand
{
    /// <summary>
    /// 创建命令。
    /// </summary>
    protected ChatRoomCommand(Guid commandId)
    {
        if (commandId == Guid.Empty)
        {
            throw new ArgumentException("命令标识不能为空。", nameof(commandId));
        }

        CommandId = commandId;
    }

    /// <summary>
    /// 命令标识。
    /// </summary>
    public Guid CommandId { get; }

    /// <summary>
    /// 校验并规范化命令中的必填字符串。
    /// </summary>
    protected static string NormalizeRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("值不能为空或空白。", parameterName);
        }

        return value.Trim();
    }
}

/// <summary>
/// 修改房间标题。
/// </summary>
public sealed record RenameRoomCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建修改标题命令。
    /// </summary>
    public RenameRoomCommand(Guid commandId, string title)
        : base(commandId)
    {
        Title = NormalizeRequired(title, nameof(title));
    }

    /// <summary>
    /// 新标题。
    /// </summary>
    public string Title { get; }
}

/// <summary>
/// 追加人类消息。
/// </summary>
public sealed record AppendHumanMessageCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建人类消息命令。
    /// </summary>
    public AppendHumanMessageCommand(
        Guid commandId,
        string content,
        string humanRoleId,
        string humanRoleName)
        : base(commandId)
    {
        ArgumentNullException.ThrowIfNull(content);
        Content = content;
        HumanRoleId = NormalizeRequired(humanRoleId, nameof(humanRoleId));
        HumanRoleName = NormalizeRequired(humanRoleName, nameof(humanRoleName));
    }

    /// <summary>
    /// 消息内容。
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// 人类角色标识。
    /// </summary>
    public string HumanRoleId { get; }

    /// <summary>
    /// 人类角色名称。
    /// </summary>
    public string HumanRoleName { get; }
}

/// <summary>
/// 添加角色。
/// </summary>
public sealed record AddRoleCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建添加角色命令。
    /// </summary>
    public AddRoleCommand(Guid commandId, ChatRoomRoleDefinition definition)
        : base(commandId)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
    }

    /// <summary>
    /// 待添加的角色定义。
    /// </summary>
    public ChatRoomRoleDefinition Definition { get; }
}

/// <summary>
/// 移除角色。
/// </summary>
public sealed record RemoveRoleCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建移除角色命令。
    /// </summary>
    public RemoveRoleCommand(Guid commandId, string roleId)
        : base(commandId)
    {
        RoleId = NormalizeRequired(roleId, nameof(roleId));
    }

    /// <summary>
    /// 待移除的角色标识。
    /// </summary>
    public string RoleId { get; }
}

/// <summary>
/// 启动自动循环。
/// </summary>
public sealed record StartAutoLoopCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建自动循环命令。
    /// </summary>
    public StartAutoLoopCommand(
        Guid commandId,
        int maxTurns = 20,
        int maxTurnsPerRole = 3)
        : base(commandId)
    {
        if (maxTurns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTurns));
        }
        if (maxTurnsPerRole <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTurnsPerRole));
        }

        MaxTurns = maxTurns;
        MaxTurnsPerRole = maxTurnsPerRole;
    }

    public int MaxTurns { get; }

    public int MaxTurnsPerRole { get; }
}

/// <summary>
/// 停止自动循环和其当前执行。
/// </summary>
public sealed record StopAutoLoopCommand : ChatRoomCommand
{
    public StopAutoLoopCommand(Guid commandId)
        : base(commandId)
    {
    }
}

/// <summary>
/// 在明确接受底层任务可能继续运行的前提下逻辑放弃当前执行并关闭房间。
/// </summary>
public sealed record ForceAbandonRoomCommand : ChatRoomCommand
{
    public ForceAbandonRoomCommand(Guid commandId)
        : base(commandId)
    {
    }
}

/// <summary>
/// 响应当前执行的指定审批请求。
/// </summary>
public sealed record RespondToApprovalCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建审批响应命令。
    /// </summary>
    public RespondToApprovalCommand(Guid commandId, ChatRoomApprovalResponse response)
        : base(commandId)
    {
        ArgumentNullException.ThrowIfNull(response);
        Response = response;
    }

    public ChatRoomApprovalResponse Response { get; }
}

/// <summary>
/// 更新角色定义和运行时事实。
/// </summary>
public sealed record UpdateRoleCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建更新角色命令。
    /// </summary>
    public UpdateRoleCommand(Guid commandId, ChatRoomRoleDefinition definition)
        : base(commandId)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
    }

    /// <summary>
    /// 新角色定义。
    /// </summary>
    public ChatRoomRoleDefinition Definition { get; }
}

/// <summary>
/// 切换瞬态工作区。
/// </summary>
public sealed record ChangeWorkspaceCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建工作区切换命令。
    /// </summary>
    public ChangeWorkspaceCommand(Guid commandId, string? workspacePath)
        : base(commandId)
    {
        WorkspacePath = string.IsNullOrWhiteSpace(workspacePath)
            ? null
            : workspacePath.Trim();
    }

    /// <summary>
    /// 新工作区路径；为空表示清除工作区。
    /// </summary>
    public string? WorkspacePath { get; }
}

/// <summary>
/// 请求指定角色开始一次发言执行。
/// </summary>
public sealed record StartRoleExecutionCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建执行命令。
    /// </summary>
    public StartRoleExecutionCommand(Guid commandId, string roleId)
        : base(commandId)
    {
        RoleId = NormalizeRequired(roleId, nameof(roleId));
    }

    /// <summary>
    /// 执行角色标识。
    /// </summary>
    public string RoleId { get; }
}

/// <summary>
/// 回投角色运行时产生的候选结果。
/// </summary>
public sealed record CompleteRoleExecutionCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建完成执行命令。
    /// </summary>
    public CompleteRoleExecutionCommand(
        Guid commandId,
        Guid roomInstanceId,
        long workspaceVersion,
        ChatRoomRoleExecutionCandidate candidate)
        : base(commandId)
    {
        if (roomInstanceId == Guid.Empty)
        {
            throw new ArgumentException("房间实例标识不能为空。", nameof(roomInstanceId));
        }
        if (workspaceVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workspaceVersion));
        }
        ArgumentNullException.ThrowIfNull(candidate);

        RoomInstanceId = roomInstanceId;
        WorkspaceVersion = workspaceVersion;
        Candidate = candidate;
    }

    /// <summary>
    /// 执行开始时的房间实例标识。
    /// </summary>
    public Guid RoomInstanceId { get; }

    /// <summary>
    /// 执行开始时的工作区版本。
    /// </summary>
    public long WorkspaceVersion { get; }

    /// <summary>
    /// 隔离的候选执行结果。
    /// </summary>
    public ChatRoomRoleExecutionCandidate Candidate { get; }
}

/// <summary>
/// 回投角色运行时失败或取消结果。
/// </summary>
public sealed record FailRoleExecutionCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建失败执行命令。
    /// </summary>
    public FailRoleExecutionCommand(
        Guid commandId,
        Guid roomInstanceId,
        Guid executionId,
        ChatRoomRoleIdentity roleIdentity,
        long roleRuntimeVersion,
        long workspaceVersion,
        bool wasCanceled,
        string? failureMessage = null)
        : base(commandId)
    {
        if (roomInstanceId == Guid.Empty)
        {
            throw new ArgumentException("房间实例标识不能为空。", nameof(roomInstanceId));
        }
        if (executionId == Guid.Empty)
        {
            throw new ArgumentException("执行标识不能为空。", nameof(executionId));
        }
        ArgumentNullException.ThrowIfNull(roleIdentity);
        if (roleRuntimeVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleRuntimeVersion));
        }
        if (workspaceVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workspaceVersion));
        }

        RoomInstanceId = roomInstanceId;
        ExecutionId = executionId;
        RoleIdentity = roleIdentity;
        RoleRuntimeVersion = roleRuntimeVersion;
        WorkspaceVersion = workspaceVersion;
        WasCanceled = wasCanceled;
        FailureMessage = string.IsNullOrWhiteSpace(failureMessage) ? null : failureMessage.Trim();
    }

    public Guid RoomInstanceId { get; }

    public Guid ExecutionId { get; }

    public ChatRoomRoleIdentity RoleIdentity { get; }

    public long RoleRuntimeVersion { get; }

    public long WorkspaceVersion { get; }

    public bool WasCanceled { get; }

    public string? FailureMessage { get; }
}

/// <summary>
/// 停止当前执行或自动循环。
/// </summary>
public sealed record StopRoomCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建停止命令。
    /// </summary>
    public StopRoomCommand(Guid commandId)
        : base(commandId)
    {
    }
}

/// <summary>
/// 关闭房间。
/// </summary>
public sealed record CloseRoomCommand : ChatRoomCommand
{
    /// <summary>
    /// 创建关闭命令。
    /// </summary>
    public CloseRoomCommand(Guid commandId)
        : base(commandId)
    {
    }
}
