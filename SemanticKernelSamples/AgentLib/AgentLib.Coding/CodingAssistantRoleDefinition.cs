namespace AgentLib.Coding;

/// <summary>
/// 描述编程助手角色的中立配置，不依赖具体宿主的角色模型。
/// </summary>
public sealed record CodingAssistantRoleDefinition
{
    /// <summary>
    /// 创建编程助手角色定义。
    /// </summary>
    /// <param name="roleName">角色显示名。</param>
    /// <param name="systemPrompt">角色系统提示词。</param>
    /// <param name="isHuman">角色是否由人类驱动。</param>
    /// <param name="requiresExplicitMention">角色是否只应在被明确提及时参与。</param>
    /// <param name="isManagerRole">角色是否承担协作流程中的管理职责。</param>
    public CodingAssistantRoleDefinition(
        string roleName,
        string systemPrompt,
        bool isHuman,
        bool requiresExplicitMention,
        bool isManagerRole)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("角色显示名不能为空。", nameof(roleName));
        }

        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            throw new ArgumentException("角色系统提示词不能为空。", nameof(systemPrompt));
        }

        RoleName = roleName;
        SystemPrompt = systemPrompt;
        IsHuman = isHuman;
        RequiresExplicitMention = requiresExplicitMention;
        IsManagerRole = isManagerRole;
    }

    /// <summary>
    /// 获取角色显示名。
    /// </summary>
    public string RoleName { get; }

    /// <summary>
    /// 获取角色系统提示词。
    /// </summary>
    public string SystemPrompt { get; }

    /// <summary>
    /// 获取角色是否由人类驱动。
    /// </summary>
    public bool IsHuman { get; }

    /// <summary>
    /// 获取角色是否只应在被明确提及时参与。
    /// </summary>
    public bool RequiresExplicitMention { get; }

    /// <summary>
    /// 获取角色是否承担协作流程中的管理职责。
    /// </summary>
    public bool IsManagerRole { get; }
}
