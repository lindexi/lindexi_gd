using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Tools;
using AgentLib.ChatRoom.Tools.Coding;
using AgentLib.Core;

namespace AgentLib.ChatRoom.Services;

/// <summary>
/// 集中创建编程助手角色定义和角色大厅运行时模板。
/// </summary>
public sealed class CodingAssistantRoleFactory
{
    private readonly IMainThreadDispatcher? _mainThreadDispatcher;
    private readonly string _roslynLanguageServerCommand;

    /// <summary>
    /// 角色大厅中编程助手运行时模板的固定标识。
    /// </summary>
    public const string TemplateId = "runtime_coding_assistant";

    private const string SystemPrompt = """
        你是聊天室中的编程助手，负责完成用户明确提出的软件开发任务。

        工作规则：
        - 只处理用户要求的范围，不主动修改无关代码，也不把建议误报为已完成修改。
        - 开始工作前确认工作区，先了解解决方案、项目和相关文件结构，不猜测项目或文件路径。
        - 查询代码符号、定义、实现和引用时优先使用代码理解工具；修改文件前必须读取相关内容。
        - 读取并遵守仓库中的编码指令、项目约定和现有代码风格。
        - 使用最小修改完成目标，优先复用已有库、方法和项目模式。
        - 不编辑生成目录或生成文件，不覆盖工作区外文件，不通过禁用检查或删除测试隐藏问题。
        - 修改后运行相关构建或测试；失败时读取相关日志并区分本次引入的问题与已有问题。
        - 无法验证时明确说明阻塞原因和未验证范围。
        - 最终汇报修改文件、主要行为变化、实际执行的构建测试结果以及剩余风险。
        """;

    /// <summary>
    /// 创建编程助手角色工厂。
    /// </summary>
    /// <param name="mainThreadDispatcher">可选的主线程调度器。</param>
    /// <param name="roslynLanguageServerCommand">Roslyn Language Server 启动命令。</param>
    public CodingAssistantRoleFactory(
        IMainThreadDispatcher? mainThreadDispatcher = null,
        string roslynLanguageServerCommand = "roslyn-language-server")
    {
        if (string.IsNullOrWhiteSpace(roslynLanguageServerCommand))
        {
            throw new ArgumentException("Roslyn Language Server 命令不能为空。", nameof(roslynLanguageServerCommand));
        }

        _mainThreadDispatcher = mainThreadDispatcher;
        _roslynLanguageServerCommand = roslynLanguageServerCommand;
    }

    /// <summary>
    /// 创建编程助手角色定义。
    /// </summary>
    /// <returns>新的可持久化角色定义。</returns>
    public ChatRoomRoleDefinition CreateDefinition()
    {
        return new ChatRoomRoleDefinition
        {
            RoleId = Guid.NewGuid().ToString("N"),
            Kind = ChatRoomRoleKind.CodingAssistant,
            RoleName = "编程助手",
            SystemPrompt = SystemPrompt,
            IsHuman = false,
            ParticipationMode = ChatRoomParticipationMode.MentionOnly,
            IsManagerRole = false,
        };
    }

    /// <summary>
    /// 根据编程助手定义创建并装配当前版本工具的运行时角色。
    /// </summary>
    /// <param name="definition">编程助手角色定义。</param>
    /// <returns>尚未初始化的编程助手角色。</returns>
    public ChatRoomRole CreateRole(ChatRoomRoleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Kind != ChatRoomRoleKind.CodingAssistant)
        {
            throw new ArgumentException("角色定义不是编程助手类型。", nameof(definition));
        }

        IReadOnlyList<IChatRoomRoleTool> roleTools =
        [
            new CodingWorkspaceRoleTool(_roslynLanguageServerCommand),
        ];
        return new ChatRoomRole(definition, null, roleTools)
        {
            MainThreadDispatcher = _mainThreadDispatcher,
        };
    }

    /// <summary>
    /// 创建仅在当前进程中存在的角色大厅模板。
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
