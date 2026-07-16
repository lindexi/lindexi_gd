using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;

using System.Text.Json;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class CodingAssistantRoleFactoryTests
{
    [TestMethod(DisplayName = "创建定义时应标记编程助手种类")]
    public void CreateDefinitionShouldUseCodingAssistantKind()
    {
        var factory = new CodingAssistantRoleFactory();

        ChatRoomRoleDefinition definition = factory.CreateDefinition();

        Assert.AreEqual(ChatRoomRoleKind.CodingAssistant, definition.Kind);
    }

    [TestMethod(DisplayName = "创建运行时模板时应使用固定模板标识")]
    public void CreateRuntimeTemplateShouldUseFixedTemplateId()
    {
        var factory = new CodingAssistantRoleFactory();

        RoleTemplate template = factory.CreateRuntimeTemplate();

        Assert.AreEqual(CodingAssistantRoleFactory.TemplateId, template.TemplateId);
    }

    [TestMethod(DisplayName = "连续创建普通预设时不应共享可变状态")]
    public void CreateDefinitionShouldNotShareMutableState()
    {
        var factory = new CodingAssistantRoleFactory();

        ChatRoomRoleDefinition first = factory.CreateDefinition();
        first.RoleName = "已修改";
        ChatRoomRoleDefinition second = factory.CreateDefinition();

        Assert.AreEqual("编程助手", second.RoleName);
    }

    [TestMethod(DisplayName = "创建编程助手角色时应由代码装配运行时工具")]
    public void CreateRoleShouldAttachRuntimeTools()
    {
        var factory = new CodingAssistantRoleFactory();

        ChatRoomRole role = factory.CreateRole(factory.CreateDefinition());

        Assert.IsNotEmpty(role.RoleTools);
    }

    [TestMethod(DisplayName = "运行时模板应可序列化且不包含来源字段")]
    public void CreateRuntimeTemplateShouldSerializeWithoutSourceField()
    {
        var factory = new CodingAssistantRoleFactory();
        RoleTemplate template = factory.CreateRuntimeTemplate();

        string json = JsonSerializer.Serialize(template, RoleTemplateJsonSerializerContext.Default.RoleTemplate);
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.IsFalse(document.RootElement.TryGetProperty("Source", out _));
        Assert.IsFalse(document.RootElement.GetProperty("Definition").TryGetProperty("Tools", out _));
    }
}
