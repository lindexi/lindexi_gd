using AgentLib.ChatRoom.Domain;
using AgentLib.ChatRoom.Coordination;

using System.Collections.ObjectModel;

namespace AgentLib.ChatRoom.Runtime;

/// <summary>
/// 运行时向协调器报告瞬态执行事件的接缝。
/// </summary>
public interface IChatRoomRoleExecutionEventSink
{
    /// <summary>
    /// 报告流式增量。
    /// </summary>
    ValueTask ReportDeltaAsync(
        ChatRoomStreamDeltaKind kind,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 请求精确路由的人工审批并等待决定。
    /// </summary>
    Task<ChatRoomApprovalDecision> RequestApprovalAsync(
        string approvalId,
        string title,
        string? detail = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 单个角色运行时的工作区切换事务。
/// </summary>
public interface IChatRoomRoleWorkspaceTransaction : IAsyncDisposable
{
    string? WorkspacePath { get; }

    void Apply();

    ValueTask RollbackAsync();

    void CommitAfterPublish();
}

/// <summary>
/// 角色执行请求。
/// </summary>
public sealed record ChatRoomRoleExecutionRequest
{
    /// <summary>
    /// 创建执行请求。
    /// </summary>
    public ChatRoomRoleExecutionRequest(
        Guid executionId,
        ChatRoomRoleDefinition definition,
        IReadOnlyList<ChatRoomMessage> inputMessages,
        long inputThroughSequence,
        ChatRoomRoleCheckpoint? committedCheckpoint,
        string? workspacePath,
        bool omitHumanSenderPrefix)
    {
        if (executionId == Guid.Empty)
        {
            throw new ArgumentException("执行标识不能为空。", nameof(executionId));
        }
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inputMessages);
        if (inputThroughSequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputThroughSequence));
        }
        if (inputMessages.Count > 0 && inputMessages[^1].MessageSequence != inputThroughSequence)
        {
            throw new ArgumentException("输入水位必须与输入消息末尾序号一致。", nameof(inputThroughSequence));
        }
        if (committedCheckpoint is not null
            && committedCheckpoint.RoleIdentity != definition.Identity)
        {
            throw new ArgumentException("Committed checkpoint 与角色身份不一致。", nameof(committedCheckpoint));
        }
        if (committedCheckpoint is not null
            && committedCheckpoint.ExecutionKind != definition.ExecutionKind)
        {
            throw new ArgumentException("Committed checkpoint 与角色执行种类不一致。", nameof(committedCheckpoint));
        }
        if (committedCheckpoint is not null
            && committedCheckpoint.RoleRuntimeVersion != definition.RuntimeVersion)
        {
            throw new ArgumentException("Committed checkpoint 与角色运行时版本不一致。", nameof(committedCheckpoint));
        }
        if (committedCheckpoint is not null
            && committedCheckpoint.ConsumedThroughSequence > inputThroughSequence)
        {
            throw new ArgumentException("Committed checkpoint 的消费水位不能超过本轮输入水位。", nameof(committedCheckpoint));
        }

        ExecutionId = executionId;
        Definition = definition;
        ChatRoomMessage[] inputMessageValues = inputMessages.ToArray();
        InputMessages = inputMessageValues.Length == 0
            ? Array.Empty<ChatRoomMessage>()
            : new ReadOnlyCollection<ChatRoomMessage>(inputMessageValues);
        InputThroughSequence = inputThroughSequence;
        CommittedCheckpoint = committedCheckpoint;
        WorkspacePath = string.IsNullOrWhiteSpace(workspacePath) ? null : workspacePath.Trim();
        OmitHumanSenderPrefix = omitHumanSenderPrefix;
    }

    /// <summary>
    /// 执行标识。
    /// </summary>
    public Guid ExecutionId { get; }

    /// <summary>
    /// 角色定义。
    /// </summary>
    public ChatRoomRoleDefinition Definition { get; }

    /// <summary>
    /// 本轮输入消息快照。
    /// </summary>
    public IReadOnlyList<ChatRoomMessage> InputMessages { get; }

    /// <summary>
    /// 本轮输入水位。
    /// </summary>
    public long InputThroughSequence { get; }

    /// <summary>
    /// 本轮开始前的 committed checkpoint。
    /// </summary>
    public ChatRoomRoleCheckpoint? CommittedCheckpoint { get; }

    /// <summary>
    /// 瞬态工作区路径。
    /// </summary>
    public string? WorkspacePath { get; }

    /// <summary>
    /// 本轮人类消息是否省略发送者前缀。
    /// </summary>
    public bool OmitHumanSenderPrefix { get; }
}

/// <summary>
/// 角色候选执行结果。
/// </summary>
public sealed record ChatRoomRoleExecutionCandidate
{
    /// <summary>
    /// 创建候选结果。
    /// </summary>
    public ChatRoomRoleExecutionCandidate(
        Guid executionId,
        ChatRoomRoleIdentity roleIdentity,
        string? publicContent,
        string? modelDisplayName,
        ChatRoomRoleCheckpointCandidate candidateCheckpoint)
    {
        if (executionId == Guid.Empty)
        {
            throw new ArgumentException("执行标识不能为空。", nameof(executionId));
        }
        ArgumentNullException.ThrowIfNull(roleIdentity);
        ArgumentNullException.ThrowIfNull(candidateCheckpoint);
        if (candidateCheckpoint.RoleIdentity != roleIdentity)
        {
            throw new ArgumentException("候选 checkpoint 与执行角色身份不一致。", nameof(candidateCheckpoint));
        }
        if (candidateCheckpoint.RoleRuntimeVersion < 0)
        {
            throw new ArgumentException("候选 checkpoint 的运行时版本无效。", nameof(candidateCheckpoint));
        }

        ExecutionId = executionId;
        RoleIdentity = roleIdentity;
        PublicContent = publicContent;
        ModelDisplayName = string.IsNullOrWhiteSpace(modelDisplayName)
            ? null
            : modelDisplayName.Trim();
        CandidateCheckpoint = candidateCheckpoint;
    }

    /// <summary>
    /// 执行标识。
    /// </summary>
    public Guid ExecutionId { get; }

    /// <summary>
    /// 角色身份。
    /// </summary>
    public ChatRoomRoleIdentity RoleIdentity { get; }

    /// <summary>
    /// 生成候选的角色运行时版本。
    /// </summary>
    public long RoleRuntimeVersion => CandidateCheckpoint.RoleRuntimeVersion;

    /// <summary>
    /// 候选公开文本。
    /// </summary>
    public string? PublicContent { get; }

    /// <summary>
    /// 模型显示名。
    /// </summary>
    public string? ModelDisplayName { get; }

    /// <summary>
    /// 仅在协调器接受结果后才能成为 committed 的候选 checkpoint。
    /// </summary>
    public ChatRoomRoleCheckpointCandidate CandidateCheckpoint { get; }
}

/// <summary>
/// 角色运行时。
/// </summary>
public interface IChatRoomRoleRuntime : IAsyncDisposable
{
    /// <summary>
    /// 角色身份。
    /// </summary>
    ChatRoomRoleIdentity Identity { get; }

    /// <summary>
    /// 执行种类。
    /// </summary>
    ChatRoomRoleExecutionKind ExecutionKind { get; }

    /// <summary>
    /// 角色运行时版本。
    /// </summary>
    long RuntimeVersion { get; }

    /// <summary>
    /// 从 committed checkpoint 执行并返回隔离的候选结果。
    /// </summary>
    Task<ChatRoomRoleExecutionCandidate> ExecuteAsync(
        ChatRoomRoleExecutionRequest request,
        IChatRoomRoleExecutionEventSink? eventSink,
        CancellationToken cancellationToken);

    /// <summary>
    /// 准备一次工作区切换；准备阶段不得改变公开状态。
    /// </summary>
    Task<IChatRoomRoleWorkspaceTransaction> PrepareWorkspaceChangeAsync(
        string? workspacePath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 角色运行时工厂。
/// </summary>
public interface IChatRoomRoleRuntimeFactory
{
    /// <summary>
    /// 本工厂支持的执行种类。
    /// </summary>
    ChatRoomRoleExecutionKind ExecutionKind { get; }

    /// <summary>
    /// 创建绑定指定角色身份的无状态运行时适配器。
    /// </summary>
    Task<IChatRoomRoleRuntime> CreateAsync(
        ChatRoomRoleDefinition definition,
        CancellationToken cancellationToken);
}
