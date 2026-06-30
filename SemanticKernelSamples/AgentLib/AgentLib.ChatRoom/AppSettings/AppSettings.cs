using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentLib.ChatRoom.Configuration;

/// <summary>
/// 全局应用设置模型。持久化到 settings.json。
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// 持久化根目录路径。默认为 {AppData}/ChatRoom/Sessions。
    /// </summary>
    public string PersistencePath { get; set; } = string.Empty;

    /// <summary>
    /// 默认最大对话轮次。
    /// </summary>
    public int DefaultMaxRounds { get; set; } = 10;

    /// <summary>
    /// 全局首选模型，格式为 "Provider/ModelName"。
    /// </summary>
    public string? PrimaryModel { get; set; }

    /// <summary>
    /// 模型提供商列表。
    /// </summary>
    public List<ProviderSetting> Providers { get; init; } = [];

    /// <summary>
    /// 角色模板持久化路径。为空时使用默认路径 {AppData}/AgentRoundtable/RoleTemplates。
    /// </summary>
    public string? RoleTemplatesPath { get; set; }

    /// <summary>
    /// 工作区路径。设置后角色的文件系统工具将在此路径下操作文件。
    /// 为空时文件系统工具不可用。
    /// </summary>
    public string? WorkspacePath { get; set; }
}
