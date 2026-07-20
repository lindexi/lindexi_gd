using AgentLib.Coding;

namespace AgentLib.Coding.Tests;

/// <summary>
/// <see cref="CodingAssistantRoleFactory"/> 的单元测试。
/// </summary>
[TestClass]
public sealed class CodingAssistantRoleFactoryTests
{
    [TestMethod(DisplayName = "创建定义时应使用编程助手默认配置")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateDefinitionShouldUseCodingAssistantDefaults()
    {
        var factory = new CodingAssistantRoleFactory();

        CodingAssistantRoleDefinition definition = factory.CreateDefinition();

        Assert.AreEqual("编程助手", definition.RoleName);
        Assert.IsTrue(definition.RequiresExplicitMention);
    }

    [TestMethod(DisplayName = "创建运行时模板时应使用固定模板元数据")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateRuntimeTemplateShouldUseStableMetadata()
    {
        var factory = new CodingAssistantRoleFactory();

        CodingAssistantRoleTemplate template = factory.CreateRuntimeTemplate();

        Assert.AreEqual(CodingAssistantRoleFactory.TemplateId, template.TemplateId);
        Assert.AreEqual("开发", template.Category);
        CollectionAssert.AreEquivalent(new[] { "开发", "编程", ".NET" }, template.Tags.ToArray());
    }

    [TestMethod(DisplayName = "连续创建定义时不应共享可变状态")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateDefinitionShouldReturnIndependentValues()
    {
        var factory = new CodingAssistantRoleFactory();

        CodingAssistantRoleDefinition first = factory.CreateDefinition();
        CodingAssistantRoleDefinition second = factory.CreateDefinition();

        Assert.AreNotSame(first, second);
    }

    [TestMethod(DisplayName = "空 Language Server 命令应被拒绝")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void ConstructorShouldRejectEmptyLanguageServerCommand()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new CodingAssistantRoleFactory(" "));
    }

    [TestMethod(DisplayName = "工厂应创建独立的工作区工具提供器")]
    [Timeout(5000, CooperativeCancellation = true)]
    public async Task CreateWorkspaceToolProviderShouldReturnIndependentProvider()
    {
        var factory = new CodingAssistantRoleFactory();
        await using CodingWorkspaceToolProvider first = factory.CreateWorkspaceToolProvider();
        await using CodingWorkspaceToolProvider second = factory.CreateWorkspaceToolProvider();

        Assert.AreNotSame(first, second);
    }
}
