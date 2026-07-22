using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;

using System.Text.Json;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class RoleTemplateServiceTests
{
    [TestMethod(DisplayName = "运行时模板应遮蔽磁盘中的同标识模板")]
    public async Task RuntimeTemplateShouldReplaceLoadedTemplateWithSameId()
    {
        string folder = CreateTestFolder();
        var service = new RoleTemplateService(folder);
        const string templateId = "runtime-shared";
        await service.SaveAsync(CreateTemplate(templateId, "磁盘模板"));
        RoleTemplate runtimeTemplate = CreateTemplate(templateId, "运行时模板");
        service.RegisterRuntimeTemplate(runtimeTemplate);

        RoleTemplate loaded = service.LoadAll().Single(template => template.TemplateId == templateId);

        Assert.AreSame(runtimeTemplate, loaded);
    }

    [TestMethod(DisplayName = "删除运行时模板时不应删除磁盘模板文件")]
    public async Task DeleteRuntimeTemplateShouldKeepDiskFile()
    {
        string folder = CreateTestFolder();
        var service = new RoleTemplateService(folder);
        const string templateId = "runtime-shared";
        await service.SaveAsync(CreateTemplate(templateId, "磁盘模板"));
        service.RegisterRuntimeTemplate(CreateTemplate(templateId, "运行时模板"));

        service.Delete(templateId);

        Assert.IsTrue(File.Exists(Path.Join(folder, $"{templateId}.json")));
    }

    [TestMethod(DisplayName = "删除运行时模板后当前服务实例不应显示同标识磁盘模板")]
    public async Task DeleteRuntimeTemplateShouldHideDiskTemplateInCurrentService()
    {
        string folder = CreateTestFolder();
        var service = new RoleTemplateService(folder);
        await service.SaveAsync(CreateTemplate("shared-id", "磁盘模板"));
        service.RegisterRuntimeTemplate(CreateTemplate("shared-id", "运行时模板"));

        service.Delete("shared-id");

        Assert.IsFalse(service.LoadAll().Any(template => template.TemplateId == "shared-id"));
    }

    [TestMethod(DisplayName = "删除运行时模板后新服务实例应重新显示磁盘模板")]
    public async Task DeleteRuntimeTemplateShouldRevealDiskTemplateInNewService()
    {
        string folder = CreateTestFolder();
        var service = new RoleTemplateService(folder);
        await service.SaveAsync(CreateTemplate("shared-id", "磁盘模板"));
        service.RegisterRuntimeTemplate(CreateTemplate("shared-id", "运行时模板"));
        service.Delete("shared-id");

        var newService = new RoleTemplateService(folder);
        RoleTemplate loaded = newService.LoadAll().Single(template => template.TemplateId == "shared-id");

        Assert.AreEqual("磁盘模板", loaded.Name);
    }

    [TestMethod(DisplayName = "保存运行时模板时只应更新内存")]
    public async Task SaveRuntimeTemplateShouldNotCreateDiskFile()
    {
        string folder = CreateTestFolder();
        var service = new RoleTemplateService(folder);
        RoleTemplate runtimeTemplate = CreateTemplate("runtime-only", "运行时模板");
        service.RegisterRuntimeTemplate(runtimeTemplate);
        RoleTemplate replacement = CreateTemplate("runtime-only", "本次进程已编辑");

        await service.SaveAsync(replacement);

        Assert.IsFalse(File.Exists(Path.Join(folder, "runtime-only.json")));
    }

    [TestMethod(DisplayName = "保存同标识运行时模板时应替换内存实例")]
    public async Task SaveRuntimeTemplateShouldReplaceRegisteredInstance()
    {
        string folder = CreateTestFolder();
        var service = new RoleTemplateService(folder);
        service.RegisterRuntimeTemplate(CreateTemplate("runtime-only", "原运行时模板"));
        RoleTemplate replacement = CreateTemplate("runtime-only", "新运行时模板");

        await service.SaveAsync(replacement);

        Assert.AreSame(replacement, service.LoadAll().Single());
    }

    [TestMethod(DisplayName = "角色模板转换为定义时应复制执行种类")]
    [Timeout(5000)]
    public void ToDefinitionShouldCopyExecutionKind()
    {
        string folder = CreateTestFolder();
        var service = new RoleTemplateService(folder);
        RoleTemplate template = CreateTemplate("coding-template", "编程模板");
        template.Definition = new ChatRoomRoleDefinition
        {
            RoleId = "coding-template",
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
            RoleName = "编程模板",
        };

        ChatRoomRoleDefinition definition = service.ToDefinition(template);

        Assert.AreEqual(ChatRoomRoleExecutionKind.Coding, definition.ExecutionKind);
    }

    [TestMethod(DisplayName = "从定义创建模板时应复制执行种类")]
    [Timeout(5000)]
    public void FromDefinitionShouldCopyExecutionKind()
    {
        var service = new RoleTemplateService(CreateTestFolder());
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "coding-role",
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
            RoleName = "编程角色",
        };

        RoleTemplate template = service.FromDefinition(definition, "编程模板", "描述");

        Assert.AreEqual(ChatRoomRoleExecutionKind.Coding, template.Definition.ExecutionKind);
    }

    [TestMethod(DisplayName = "更新模板时应复制执行种类")]
    [Timeout(5000)]
    public void UpdateFromDefinitionShouldCopyExecutionKind()
    {
        var service = new RoleTemplateService(CreateTestFolder());
        RoleTemplate template = CreateTemplate("template", "普通模板");
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "coding-role",
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
            RoleName = "编程角色",
        };

        service.UpdateFromDefinition(template, definition, "编程模板", "描述");

        Assert.AreEqual(ChatRoomRoleExecutionKind.Coding, template.Definition.ExecutionKind);
    }

    [TestMethod(DisplayName = "加载模板时应跳过未知执行种类")]
    [Timeout(5000)]
    public async Task LoadAllShouldSkipUnknownExecutionKind()
    {
        string folder = CreateTestFolder();
        var service = new RoleTemplateService(folder);
        RoleTemplate invalidTemplate = CreateTemplate("invalid", "无效模板");
        invalidTemplate.Definition = new ChatRoomRoleDefinition
        {
            RoleId = "invalid",
            ExecutionKind = (ChatRoomRoleExecutionKind)99,
        };
        string json = JsonSerializer.Serialize(
            invalidTemplate,
            RoleTemplateJsonSerializerContext.Default.RoleTemplate);
        await File.WriteAllTextAsync(Path.Join(folder, "invalid.json"), json);

        Assert.IsEmpty(service.LoadAll());
    }

    [TestMethod(DisplayName = "保存模板时应拒绝人类 Coding 定义")]
    [Timeout(5000)]
    public async Task SaveAsyncShouldRejectHumanCodingDefinition()
    {
        var service = new RoleTemplateService(CreateTestFolder());
        RoleTemplate template = CreateTemplate("invalid", "无效模板");
        template.Definition = new ChatRoomRoleDefinition
        {
            RoleId = "invalid",
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
            IsHuman = true,
        };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => service.SaveAsync(template));
    }

    [TestMethod(DisplayName = "补齐预置模板时不应覆盖用户保存的同标识模板")]
    public async Task EnsurePresetTemplatesShouldNotOverwriteUserTemplateWithSameId()
    {
        string folder = CreateTestFolder();
        var service = new RoleTemplateService(folder);
        RoleTemplate userTemplate = CreateTemplate("preset_assistant", "用户自定义助手");
        await service.SaveAsync(userTemplate);

        await service.EnsurePresetTemplatesAsync();

        RoleTemplate loaded = service.LoadAll().Single(template => template.TemplateId == "preset_assistant");
        Assert.AreEqual("用户自定义助手", loaded.Name);
    }

    [TestMethod(DisplayName = "补齐预置模板时应添加缺失模板")]
    public async Task EnsurePresetTemplatesShouldAddMissingTemplates()
    {
        string folder = CreateTestFolder();
        var service = new RoleTemplateService(folder);

        await service.EnsurePresetTemplatesAsync();

        Assert.IsTrue(service.LoadAll().Any(template => template.TemplateId == "preset_architect"));
    }

    private static RoleTemplate CreateTemplate(string templateId, string name)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        return new RoleTemplate
        {
            TemplateId = templateId,
            Name = name,
            CreatedAt = now,
            UpdatedAt = now,
            Definition = new ChatRoomRoleDefinition
            {
                RoleId = templateId,
                RoleName = name,
            },
        };
    }

    private static string CreateTestFolder()
    {
        string folder = Path.Join(Path.GetTempPath(), $"RoleTemplateServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        return folder;
    }
}
