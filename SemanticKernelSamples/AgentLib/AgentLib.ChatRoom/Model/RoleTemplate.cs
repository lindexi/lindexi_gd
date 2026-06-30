using System;
using System.Collections.Generic;

namespace AgentLib.ChatRoom.Model;

/// <summary>
/// 角色模板数据模型。作为全局角色模板库的单个条目，独立于会话持久化。
/// 通过 <see cref="RoleTemplateService"/> 管理其生命周期。
/// </summary>
public sealed class RoleTemplate
{
    /// <summary>
    /// 模板唯一标识。
    /// </summary>
    public string TemplateId { get; init; } = string.Empty;

    /// <summary>
    /// 模板显示名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述（一句话说明角色用途）。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 分类（如"开发"、"产品"、"通用"）。
    /// </summary>
    public string Category { get; set; } = "通用";

    /// <summary>
    /// 标签列表（用于搜索和筛选）。
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 最后修改时间。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// 是否为预置模板。预置模板由应用首次启动时自动写入。
    /// </summary>
    public bool IsPreset { get; init; }

    /// <summary>
    /// 对应的角色定义。从模板创建会话角色时，复制此定义并生成新的 RoleId。
    /// </summary>
    public ChatRoomRoleDefinition Definition { get; set; } = new();
}
