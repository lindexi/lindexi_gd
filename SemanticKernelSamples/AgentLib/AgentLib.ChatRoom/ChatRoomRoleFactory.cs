using AgentLib.Coding;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Tools;
using AgentLib.Core;

using CodingRoleFactory = AgentLib.Coding.CodingAssistantRoleFactory;

namespace AgentLib.ChatRoom;

/// <summary>
/// 创建聊天室角色。
/// </summary>
public sealed class ChatRoomRoleFactory : IChatRoomRoleFactory
{
    private readonly IMainThreadDispatcher? _mainThreadDispatcher;
    private readonly CodingRoleFactory _codingAssistantRoleFactory;

    /// <summary>
    /// 创建通用角色工厂。
    /// </summary>
    /// <param name="mainThreadDispatcher">可选的主线程调度器。</param>
    public ChatRoomRoleFactory(
        IMainThreadDispatcher? mainThreadDispatcher = null,
        string roslynLanguageServerCommand = "roslyn-language-server")
    {
        _mainThreadDispatcher = mainThreadDispatcher;
        _codingAssistantRoleFactory = new CodingRoleFactory(roslynLanguageServerCommand);
    }

    /// <summary>
    /// 创建可持久化的编程助手角色定义。
    /// </summary>
    /// <returns>新的聊天室角色定义。</returns>
    public ChatRoomRoleDefinition CreateCodingAssistantDefinition()
    {
        return ToChatRoomDefinition(_codingAssistantRoleFactory.CreateDefinition());
    }

    /// <summary>
    /// 创建仅在当前进程中存在的编程助手模板。
    /// </summary>
    /// <returns>不会由模板服务写入磁盘的运行时模板。</returns>
    public RoleTemplate CreateCodingAssistantRuntimeTemplate()
    {
        CodingAssistantRoleTemplate template = _codingAssistantRoleFactory.CreateRuntimeTemplate();
        DateTimeOffset now = DateTimeOffset.Now;
        return new RoleTemplate
        {
            TemplateId = template.TemplateId,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            Tags = [.. template.Tags],
            CreatedAt = now,
            UpdatedAt = now,
            IsPreset = true,
            Definition = ToChatRoomDefinition(template.Definition),
        };
    }

    /// <inheritdoc />
    public ChatRoomRole CreateRole(ChatRoomRoleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Kind == ChatRoomRoleKind.CodingAssistant)
        {
            IReadOnlyList<IChatRoomRoleTool> roleTools =
            [
                new CodingWorkspaceRoleToolAdapter(_codingAssistantRoleFactory.CreateWorkspaceToolProvider()),
            ];
            return new ChatRoomRole(definition, null, roleTools)
            {
                MainThreadDispatcher = _mainThreadDispatcher,
            };
        }

        return new ChatRoomRole(definition)
        {
            MainThreadDispatcher = _mainThreadDispatcher,
        };
    }

    private static ChatRoomRoleDefinition ToChatRoomDefinition(CodingAssistantRoleDefinition definition)
    {
        return new ChatRoomRoleDefinition
        {
            RoleId = Guid.NewGuid().ToString("N"),
            Kind = ChatRoomRoleKind.CodingAssistant,
            RoleName = definition.RoleName,
            SystemPrompt = definition.SystemPrompt,
            IsHuman = definition.IsHuman,
            ParticipationMode = definition.RequiresExplicitMention
                ? ChatRoomParticipationMode.MentionOnly
                : ChatRoomParticipationMode.AlwaysParticipate,
            IsManagerRole = definition.IsManagerRole,
        };
    }
}
