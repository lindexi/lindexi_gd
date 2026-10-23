using AgentLib.ChatRoom.Model;

using System.Text.Json;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class CodingAssistantChatRoomMappingTests
{
    [TestMethod(DisplayName = "创建定义时应标记编程助手种类")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateDefinitionShouldUseCodingAssistantKind()
    {
        var factory = new ChatRoomRoleFactory();

        ChatRoomRoleDefinition definition = factory.CreateCodingAssistantDefinition();

        Assert.AreEqual(ChatRoomRoleExecutionKind.Coding, definition.ExecutionKind);
        Assert.AreEqual(ChatRoomParticipationMode.MentionOnly, definition.ParticipationMode);
        Assert.IsFalse(definition.IsHuman);
    }

    [TestMethod(DisplayName = "创建运行时模板时应使用固定模板标识")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateRuntimeTemplateShouldUseFixedTemplateId()
    {
        var factory = new ChatRoomRoleFactory();

        RoleTemplate template = factory.CreateCodingAssistantRuntimeTemplate();

        Assert.AreEqual("runtime_coding_assistant", template.TemplateId);
        Assert.AreEqual("开发", template.Category);
        CollectionAssert.AreEquivalent(new[] { "开发", "编程", ".NET" }, template.Tags);
    }

    [TestMethod(DisplayName = "连续创建普通预设时不应共享可变状态")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateDefinitionShouldNotShareMutableState()
    {
        var factory = new ChatRoomRoleFactory();

        ChatRoomRoleDefinition first = factory.CreateCodingAssistantDefinition();
        first.RoleName = "已修改";
        ChatRoomRoleDefinition second = factory.CreateCodingAssistantDefinition();

        Assert.AreEqual("编程助手", second.RoleName);
    }

    [TestMethod(DisplayName = "创建编程助手角色时应装配 Coding 执行器")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateRoleShouldAttachRuntimeTools()
    {
        var factory = new ChatRoomRoleFactory();

        ChatRoomRole role = factory.CreateRole(factory.CreateCodingAssistantDefinition());

        Assert.IsInstanceOfType<CodingChatRoomRoleExecutor>(role.Executor);
    }

    [TestMethod(DisplayName = "每个编程助手角色应获得独立的执行器")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateRoleShouldAttachIndependentRuntimeTools()
    {
        var factory = new ChatRoomRoleFactory();

        ChatRoomRole first = factory.CreateRole(factory.CreateCodingAssistantDefinition());
        ChatRoomRole second = factory.CreateRole(factory.CreateCodingAssistantDefinition());

        Assert.AreNotSame(first.Executor, second.Executor);
    }

    [TestMethod(DisplayName = "运行时模板应可序列化且不包含来源字段")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateRuntimeTemplateShouldSerializeWithoutSourceField()
    {
        var factory = new ChatRoomRoleFactory();
        RoleTemplate template = factory.CreateCodingAssistantRuntimeTemplate();

        string json = JsonSerializer.Serialize(template, RoleTemplateJsonSerializerContext.Default.RoleTemplate);
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.IsFalse(document.RootElement.TryGetProperty("Source", out _));
        Assert.IsFalse(document.RootElement.GetProperty("Definition").TryGetProperty("Tools", out _));
        Assert.IsFalse(document.RootElement.GetProperty("Definition").TryGetProperty("CodingAgent", out _));
    }
}
