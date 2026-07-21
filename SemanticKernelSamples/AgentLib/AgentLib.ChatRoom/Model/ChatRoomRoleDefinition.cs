using System.Collections.Generic;

namespace AgentLib.ChatRoom.Model;

/// <summary>
/// 角色在聊天室中的参与模式。
/// </summary>
public enum ChatRoomParticipationMode
{
    /// <summary>
    /// 默认参与所有对话。在自动循环中正常轮流发言。
    /// </summary>
    AlwaysParticipate,

    /// <summary>
    /// 仅在被 @ 时才发言。不会出现在自动循环中，
    /// 只有当其他角色或人类在消息中明确 @ 了该角色时才会被选中发言。
    /// </summary>
    MentionOnly,
}

/// <summary>
/// 聊天室角色的稳定执行种类，用于选择当前版本的运行引擎。
/// </summary>
public enum ChatRoomRoleExecutionKind
{
    /// <summary>
    /// 使用标准聊天室发送流程。
    /// </summary>
    Standard = 0,

    /// <summary>
    /// 使用专用编程代理流程。
    /// </summary>
    Coding = 1,
}

/// <summary>
/// 聊天室角色定义（可编辑的配置）。包含角色的身份、系统提示词、模型选择和技能。
/// 此定义可序列化到持久化配置文件中。
/// </summary>
public sealed class ChatRoomRoleDefinition
{
    /// <summary>
    /// 角色唯一标识。
    /// </summary>
    public string RoleId { get; init; } = string.Empty;

    /// <summary>
    /// 角色的稳定执行种类。角色工厂根据该值装配对应运行引擎。
    /// </summary>
    public ChatRoomRoleExecutionKind ExecutionKind { get; init; } = ChatRoomRoleExecutionKind.Standard;

    /// <summary>
    /// 角色显示名，如 "代码审查员"。
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色人设 / 系统提示词。注入到 LLM 调用的 System Prompt 中。
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 是否人类角色。人类角色不调用 LLM，其消息通过 <c>HumanInterjectAsync</c> 直接追加。
    /// </summary>
    public bool IsHuman { get; set; }

    /// <summary>
    /// 模型提供商 ID。为 <see langword="null"/> 时使用角色内部 <see cref="AgentLib.AgentApiEndpointManager"/> 的默认 provider。
    /// </summary>
    public string? ModelProviderId { get; set; }

    /// <summary>
    /// 具体模型 ID。为 <see langword="null"/> 时使用提供商的默认模型。
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// 技能文件夹路径列表。每个路径指向一个技能文件夹，角色初始化时加载。
    /// </summary>
    public List<string> SkillFolders { get; init; } = [];

    /// <summary>
    /// 角色记忆内容（可选）。注入到角色首次发言的系统提示词中。
    /// </summary>
    public string? MemoryContent { get; set; }

    /// <summary>
    /// 角色在聊天室中的参与模式。默认为 <see cref="ChatRoomParticipationMode.AlwaysParticipate"/>；
    /// 通过角色管理工具创建的新角色默认为 <see cref="ChatRoomParticipationMode.MentionOnly"/>。
    /// </summary>
    public ChatRoomParticipationMode ParticipationMode { get; set; } = ChatRoomParticipationMode.AlwaysParticipate;

    /// <summary>
    /// 是否为管理者角色。当所有可发言角色都发言完毕且 @ 队列为空时，
    /// 由管理者进行发言。管理者发言后如果 @ 了其他角色则继续链式对话，
    /// 否则真正结束循环。
    /// 支持两种配置方式：
    /// <list type="bullet">
    /// <item><see cref="ChatRoomParticipationMode.MentionOnly"/> + <see cref="IsManagerRole"/>：
    ///   管理者不参与正常轮流，仅在被 @ 或所有人发言完毕后兜底。</item>
    /// <item><see cref="ChatRoomParticipationMode.AlwaysParticipate"/> + <see cref="IsManagerRole"/>：
    ///   管理者正常参与轮流发言，同时在所有非管理者角色发言完毕后兜底。
    ///   当 autoRoles 中仅含管理者角色时（如单助手场景），不会触发兜底，避免重复发言。</item>
    /// </list>
    /// </summary>
    public bool IsManagerRole { get; set; }
}
