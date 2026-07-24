using AgentLib.Logging;
using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Logging;

[TestClass]
public class FileCopilotChatLoggerTests
{
    private string? _testRootPath;

    [TestCleanup]
    public void Cleanup()
    {
        if (!string.IsNullOrWhiteSpace(_testRootPath) && Directory.Exists(_testRootPath))
        {
            Directory.Delete(_testRootPath, recursive: true);
        }
    }

    [TestMethod]
    [Description("记录单条消息时应只写入人类可读文本日志")]
    public async Task LogMessageAsync_WhenCalled_WritesReadableLogOnly()
    {
        string logPath = CreatePath("logs");
        var logger = new FileCopilotChatLogger(logPath);
        Guid sessionId = Guid.NewGuid();
        var message = new CopilotChatMessage(ChatRole.Assistant, "测试内容");
        message.AppendReasoning("推理内容");
        message.AppendUsageDetails(new UsageDetails
        {
            TotalTokenCount = 200,
            InputTokenCount = 120,
            OutputTokenCount = 80
        });

        await logger.LogMessageAsync(sessionId, message);

        string logFile = GetSingleFile(logPath, "*.log");
        string logContent = await File.ReadAllTextAsync(logFile);

        StringAssert.Contains(logContent, $"SessionId: {sessionId}");
        StringAssert.Contains(logContent, "Copilot:");
        StringAssert.Contains(logContent, "测试内容");
        StringAssert.Contains(logContent, "推理内容");
        StringAssert.Contains(logContent, "用量:");
        StringAssert.Contains(logContent, "- 总计: 200");
        Assert.HasCount(0, Directory.GetFiles(_testRootPath!, "*.xml", SearchOption.AllDirectories));
    }

    [TestMethod]
    [Description("同一会话连续记录多条消息时应追加到同一个日志文件")]
    public async Task LogMessageAsync_WhenCalledTwiceForSameSession_AppendsToExistingFile()
    {
        string logPath = CreatePath("logs");
        var logger = new FileCopilotChatLogger(logPath);
        Guid sessionId = Guid.NewGuid();
        var firstMessage = new CopilotChatMessage(ChatRole.User, "第一条消息");
        var secondMessage = new CopilotChatMessage(ChatRole.Assistant, "第二条消息");

        await logger.LogMessageAsync(sessionId, firstMessage);
        await logger.LogMessageAsync(sessionId, secondMessage);

        string logFile = GetSingleFile(logPath, "*.log");
        string logContent = await File.ReadAllTextAsync(logFile);

        Assert.HasCount(1, Directory.GetFiles(logPath, "*.log", SearchOption.AllDirectories));
        StringAssert.Contains(logContent, "我:");
        StringAssert.Contains(logContent, "Copilot:");
        StringAssert.Contains(logContent, "第一条消息");
        StringAssert.Contains(logContent, "第二条消息");
    }

    private string CreatePath(string name)
    {
        _testRootPath ??= Path.Combine(Path.GetTempPath(), "AgentLib.Tests", Guid.NewGuid().ToString("N"));
        return Path.Combine(_testRootPath, name);
    }

    private static string GetSingleFile(string rootPath, string searchPattern)
    {
        string[] files = Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);
        Assert.HasCount(1, files);
        return files[0];
    }

}
