namespace AgentLib.Coding;

/// <summary>
/// 创建编程助手描述和工作区工具提供器。
/// </summary>
public sealed class CodingAssistantRoleFactory
{
    private readonly string _roslynLanguageServerCommand;

    /// <summary>
    /// 获取编程助手模板的稳定标识。
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
    /// 创建编程助手工厂。
    /// </summary>
    /// <param name="roslynLanguageServerCommand">Roslyn Language Server 启动命令。</param>
    public CodingAssistantRoleFactory(string roslynLanguageServerCommand = "roslyn-language-server")
    {
        if (string.IsNullOrWhiteSpace(roslynLanguageServerCommand))
        {
            throw new ArgumentException("Roslyn Language Server 命令不能为空。", nameof(roslynLanguageServerCommand));
        }

        _roslynLanguageServerCommand = roslynLanguageServerCommand;
    }

    /// <summary>
    /// 创建编程助手角色定义。
    /// </summary>
    /// <returns>新的中立角色定义。</returns>
    public CodingAssistantRoleDefinition CreateDefinition()
    {
        return new CodingAssistantRoleDefinition(
            "编程助手",
            SystemPrompt,
            isHuman: false,
            requiresExplicitMention: true,
            isManagerRole: false);
    }

    /// <summary>
    /// 创建编程助手运行时模板描述。
    /// </summary>
    /// <returns>新的中立模板描述。</returns>
    public CodingAssistantRoleTemplate CreateRuntimeTemplate()
    {
        CodingAssistantRoleDefinition definition = CreateDefinition();
        return new CodingAssistantRoleTemplate(
            TemplateId,
            definition.RoleName,
            "探索代码、修改文件并运行 .NET 构建与测试",
            "开发",
            ["开发", "编程", ".NET"],
            definition);
    }

    /// <summary>
    /// 创建可绑定代码工作区的工具提供器。
    /// </summary>
    /// <returns>新的工作区工具提供器。</returns>
    public CodingWorkspaceToolProvider CreateWorkspaceToolProvider()
    {
        return new CodingWorkspaceToolProvider(_roslynLanguageServerCommand);
    }
}
