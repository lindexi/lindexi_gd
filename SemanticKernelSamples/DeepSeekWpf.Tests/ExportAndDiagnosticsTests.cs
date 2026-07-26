using System.Text;
using AgentLib.Model;
using DeepSeekWpf.Models;
using DeepSeekWpf.Services;
using DeepSeekWpf.Tests.TestInfrastructure;

namespace DeepSeekWpf.Tests;

[TestClass]
public sealed class ExportAndDiagnosticsTests
{
    [TestMethod]
    public async Task ExportMarkdownAsync_WritesUtf8TitleRolesReasoningAndTextWithoutCredentials()
    {
        using var temp = new TempDirectory();
        var session = new ChatSession { Title = "中文标题" };
        session.Messages.Add(new ChatMessageViewModel(CopilotChatMessage.CreateUser("问题正文")));
        var assistant = CopilotChatMessage.CreateAssistant(string.Empty, false);
        assistant.AppendReasoning("思考内容");
        assistant.AppendText("回答正文");
        session.Messages.Add(new ChatMessageViewModel(assistant));
        var path = temp.GetPath("chat.md");

        await new MarkdownChatExportService().ExportMarkdownAsync(session, path);

        var bytes = await File.ReadAllBytesAsync(path);
        var markdown = Encoding.UTF8.GetString(bytes);
        Assert.IsFalse(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        StringAssert.Contains(markdown, "# 中文标题");
        StringAssert.Contains(markdown, "## 用户");
        StringAssert.Contains(markdown, "## 助手");
        StringAssert.Contains(markdown, "思考内容");
        StringAssert.Contains(markdown, "回答正文");
        Assert.IsFalse(markdown.Contains("ApiKey", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(markdown.Contains("ApiAddress", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task CreateSummaryAsync_ContainsEnvironmentMetadataButNotChatBodyOrApiKey()
    {
        using var temp = new TempDirectory();
        var settings = new FakeSettingsService(temp.Path);
        var model = new AgentModelDescriptor("fake/model", "fake", "model", null, null, null, null);
        await settings.SaveAsync(settings.CurrentSettings with { SelectedModelSpecifier = model.Specifier });
        var modelService = new FakeAgentModelService
        {
            ConfigurationFilePath = temp.GetPath("AgentConfiguration.json"),
            RegisteredModels = [model],
            SelectedModel = model,
        };
        var service = new DiagnosticsService(settings, modelService, new FakeAppLogger());

        var summary = await service.CreateSummaryAsync();

        StringAssert.Contains(summary, "DeepSeekWpf 本地诊断摘要");
        StringAssert.Contains(summary, "已注册模型数: 1");
        StringAssert.Contains(summary, "fake/model");
        Assert.IsFalse(summary.Contains("聊天正文", StringComparison.Ordinal));
        Assert.IsFalse(summary.Contains("ApiKey", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(summary.Contains("secret", StringComparison.OrdinalIgnoreCase));
    }
}