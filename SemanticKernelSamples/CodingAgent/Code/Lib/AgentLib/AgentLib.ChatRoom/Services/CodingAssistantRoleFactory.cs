using AgentLib.ChatRoom.Model;

namespace AgentLib.ChatRoom.Services;

/// <summary>
/// 创建编程助手角色定义和运行时模板。
/// </summary>
public sealed class CodingAssistantRoleFactory
{
    /// <summary>
    /// 编程助手运行时模板的固定标识。
    /// </summary>
    public const string TemplateId = "runtime_coding_assistant";

    /// <summary>
    /// 创建可持久化的编程助手角色定义。
    /// </summary>
    /// <returns>新的聊天室角色定义。</returns>
    public ChatRoomRoleDefinition CreateDefinition()
    {
        return new ChatRoomRoleDefinition
        {
            RoleId = Guid.NewGuid().ToString("N"),
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
            RoleName = "编程助手",
            SystemPrompt = string.Empty,
            IsHuman = false,
            ParticipationMode = ChatRoomParticipationMode.MentionOnly,
            IsManagerRole = false,
        };
    }

    /// <summary>
    /// 创建仅在当前进程中存在的编程助手模板。
    /// </summary>
    /// <returns>不会由模板服务写入磁盘的运行时模板。</returns>
    public RoleTemplate CreateRuntimeTemplate()
    {
        ChatRoomRoleDefinition definition = CreateDefinition();
        DateTimeOffset now = DateTimeOffset.Now;
        return new RoleTemplate
        {
            TemplateId = TemplateId,
            Name = definition.RoleName,
            Description = "探索代码、修改文件并运行 .NET 构建与测试",
            Category = "开发",
            Tags = ["开发", "编程", ".NET"],
            CreatedAt = now,
            UpdatedAt = now,
            IsPreset = true,
            Definition = definition,
        };
    }

}
