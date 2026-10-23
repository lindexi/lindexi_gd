using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;

namespace AgentLib.ChatRoom.Tests;

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

        ChatRoomRoleDefinition definition = factory.CreateDefinition();

        Assert.AreEqual("编程助手", definition.RoleName);
        Assert.AreEqual(ChatRoomRoleExecutionKind.Coding, definition.ExecutionKind);
        Assert.AreEqual(ChatRoomParticipationMode.MentionOnly, definition.ParticipationMode);
        Assert.AreEqual(string.Empty, definition.SystemPrompt);
    }

    [TestMethod(DisplayName = "创建运行时模板时应使用固定模板元数据")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateRuntimeTemplateShouldUseStableMetadata()
    {
        var factory = new CodingAssistantRoleFactory();

        RoleTemplate template = factory.CreateRuntimeTemplate();

        Assert.AreEqual(CodingAssistantRoleFactory.TemplateId, template.TemplateId);
        Assert.AreEqual("开发", template.Category);
        CollectionAssert.AreEquivalent(new[] { "开发", "编程", ".NET" }, template.Tags.ToArray());
    }

    [TestMethod(DisplayName = "连续创建定义时不应共享可变状态")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateDefinitionShouldReturnIndependentValues()
    {
        var factory = new CodingAssistantRoleFactory();

        ChatRoomRoleDefinition first = factory.CreateDefinition();
        ChatRoomRoleDefinition second = factory.CreateDefinition();

        Assert.AreNotSame(first, second);
    }

}
