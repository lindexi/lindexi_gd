using System.Collections.ObjectModel;

namespace AgentLib.ChatRoom.Domain;

/// <summary>
/// 聊天室角色的稳定身份。
/// </summary>
/// <param name="RoleId">角色逻辑标识。</param>
/// <param name="Incarnation">角色运行时世代；替换运行时语义时递增。</param>
public sealed record ChatRoomRoleIdentity
{
    /// <summary>
    /// 创建角色身份。
    /// </summary>
    public ChatRoomRoleIdentity(string roleId, long incarnation)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("角色标识不能为空或空白。", nameof(roleId));
        }
        if (incarnation < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(incarnation));
        }

        RoleId = roleId.Trim();
        Incarnation = incarnation;
    }

    /// <summary>
    /// 角色逻辑标识。
    /// </summary>
    public string RoleId { get; }

    /// <summary>
    /// 角色运行时世代。
    /// </summary>
    public long Incarnation { get; }
}

/// <summary>
/// 不可变的聊天室角色定义。
/// </summary>
public sealed record ChatRoomRoleDefinition
{
    private static readonly IReadOnlyList<string> EmptySkillFolders = Array.Empty<string>();

    /// <summary>
    /// 创建角色定义。
    /// </summary>
    public ChatRoomRoleDefinition(
        ChatRoomRoleIdentity identity,
        ChatRoomRoleExecutionKind executionKind,
        string roleName,
        string systemPrompt,
        bool isHuman,
        string? modelProviderId = null,
        string? modelId = null,
        IEnumerable<string>? skillFolders = null,
        string? memoryContent = null,
        ChatRoomParticipationMode participationMode = ChatRoomParticipationMode.AlwaysParticipate,
        bool isManagerRole = false,
        long runtimeVersion = 0)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ThrowIfNullOrWhiteSpace(roleName, nameof(roleName));
        ArgumentNullException.ThrowIfNull(systemPrompt);
        if (!Enum.IsDefined(typeof(ChatRoomRoleExecutionKind), executionKind))
        {
            throw new ArgumentOutOfRangeException(nameof(executionKind));
        }
        if (!Enum.IsDefined(typeof(ChatRoomParticipationMode), participationMode))
        {
            throw new ArgumentOutOfRangeException(nameof(participationMode));
        }
        if (isHuman && executionKind != ChatRoomRoleExecutionKind.Standard)
        {
            throw new ArgumentException("人类角色只能使用 Standard 执行种类。", nameof(executionKind));
        }
        if (runtimeVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(runtimeVersion));
        }

        Identity = identity;
        ExecutionKind = executionKind;
        RoleName = roleName.Trim();
        SystemPrompt = systemPrompt;
        IsHuman = isHuman;
        ModelProviderId = NormalizeOptionalValue(modelProviderId);
        ModelId = NormalizeOptionalValue(modelId);
        SkillFolders = CopySkillFolders(skillFolders);
        MemoryContent = NormalizeOptionalValue(memoryContent);
        ParticipationMode = participationMode;
        IsManagerRole = isManagerRole;
        RuntimeVersion = runtimeVersion;
    }

    /// <summary>
    /// 角色身份。
    /// </summary>
    public ChatRoomRoleIdentity Identity { get; }

    /// <summary>
    /// 角色执行种类。
    /// </summary>
    public ChatRoomRoleExecutionKind ExecutionKind { get; }

    /// <summary>
    /// 角色显示名。
    /// </summary>
    public string RoleName { get; }

    /// <summary>
    /// 角色系统提示词。
    /// </summary>
    public string SystemPrompt { get; }

    /// <summary>
    /// 是否为人类角色。
    /// </summary>
    public bool IsHuman { get; }

    /// <summary>
    /// 模型提供商标识。
    /// </summary>
    public string? ModelProviderId { get; }

    /// <summary>
    /// 模型标识。
    /// </summary>
    public string? ModelId { get; }

    /// <summary>
    /// 技能文件夹快照。
    /// </summary>
    public IReadOnlyList<string> SkillFolders { get; }

    /// <summary>
    /// 角色记忆内容。
    /// </summary>
    public string? MemoryContent { get; }

    /// <summary>
    /// 角色参与模式。
    /// </summary>
    public ChatRoomParticipationMode ParticipationMode { get; }

    /// <summary>
    /// 是否为管理者角色。
    /// </summary>
    public bool IsManagerRole { get; }

    /// <summary>
    /// 当前角色运行时配置版本。
    /// </summary>
    public long RuntimeVersion { get; }

    private static IReadOnlyList<string> CopySkillFolders(IEnumerable<string>? skillFolders)
    {
        if (skillFolders is null)
        {
            return EmptySkillFolders;
        }

        string[] values = skillFolders.Select(folder =>
        {
            ThrowIfNullOrWhiteSpace(folder, nameof(skillFolders));
            return folder.Trim();
        }).ToArray();
        return values.Length == 0
            ? EmptySkillFolders
            : new ReadOnlyCollection<string>(values);
    }

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ThrowIfNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("值不能为空或空白。", parameterName);
        }
    }
}
