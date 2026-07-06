using AgentLib.Logging;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

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
    [Description("记录单条消息时应写入日志文件与聊天历史 XML 文件")]
    public async Task LogMessageAsync_WhenCalled_WritesLogAndHistoryFiles()
    {
        string logPath = CreatePath("logs");
        string historyPath = CreatePath("history");
        var logger = new FileCopilotChatLogger(logPath, historyPath);
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
        string historyFile = GetSingleFile(historyPath, "*.xml");
        string logContent = await File.ReadAllTextAsync(logFile);
        XDocument historyDocument = XDocument.Load(historyFile);

        StringAssert.Contains(logContent, $"SessionId: {sessionId}");
        StringAssert.Contains(logContent, "Copilot:");
        StringAssert.Contains(logContent, "测试内容");
        StringAssert.Contains(logContent, "推理内容");
        StringAssert.Contains(logContent, "用量:");
        StringAssert.Contains(logContent, "- 总计: 200");

        XElement root = historyDocument.Root!;
        Assert.AreEqual("CopilotChatSessionHistory", root.Name.LocalName);
        Assert.AreEqual(sessionId.ToString(), root.Attribute("SessionId")?.Value);
        XElement? messageElement = root.Element("Messages")?.Element("Message");
        Assert.IsNotNull(messageElement);
        Assert.AreEqual("Copilot", messageElement.Attribute("Author")?.Value);
        Assert.AreEqual("测试内容", messageElement.Element("Content")?.Value);
        Assert.AreEqual("推理内容", messageElement.Element("Reason")?.Value);
        Assert.IsNull(root.Element("AgentSessionState"));
    }

    [TestMethod]
    [Description("同一会话连续记录多条消息时应追加到同一个日志文件与历史 XML 文件")]
    public async Task LogMessageAsync_WhenCalledTwiceForSameSession_AppendsToExistingFiles()
    {
        string logPath = CreatePath("logs");
        string historyPath = CreatePath("history");
        var logger = new FileCopilotChatLogger(logPath, historyPath);
        Guid sessionId = Guid.NewGuid();
        var firstMessage = new CopilotChatMessage(ChatRole.User, "第一条消息");
        var secondMessage = new CopilotChatMessage(ChatRole.Assistant, "第二条消息");

        await logger.LogMessageAsync(sessionId, firstMessage);
        await logger.LogMessageAsync(sessionId, secondMessage);

        string logFile = GetSingleFile(logPath, "*.log");
        string historyFile = GetSingleFile(historyPath, "*.xml");
        string logContent = await File.ReadAllTextAsync(logFile);
        XDocument historyDocument = XDocument.Load(historyFile);

        Assert.HasCount(1, Directory.GetFiles(logPath, "*.log", SearchOption.AllDirectories));
        Assert.HasCount(1, Directory.GetFiles(historyPath, "*.xml", SearchOption.AllDirectories));
        StringAssert.Contains(logContent, "我:");
        StringAssert.Contains(logContent, "Copilot:");
        StringAssert.Contains(logContent, "第一条消息");
        StringAssert.Contains(logContent, "第二条消息");
        Assert.HasCount(2, historyDocument.Root?.Element("Messages")?.Elements("Message")?.ToArray() ?? []);
    }

    [TestMethod]
    [Description("未配置聊天历史目录时应仅写入日志文件而不生成 XML 历史文件")]
    public async Task LogMessageAsync_WhenHistoryFolderIsNull_OnlyWritesLogFile()
    {
        string logPath = CreatePath("logs");
        var logger = new FileCopilotChatLogger(logPath, null);

        await logger.LogMessageAsync(Guid.NewGuid(), new CopilotChatMessage(ChatRole.User, "只写日志"));

        Assert.HasCount(1, Directory.GetFiles(logPath, "*.log", SearchOption.AllDirectories));
        Assert.IsFalse(Directory.Exists(CreatePath("history")));
    }

    [TestMethod]
    [Description("记录会话状态时应把最新的机器会话状态写入聊天历史 XML 文件")]
    public async Task LogMessageAsync_WhenAgentSessionStateProvided_WritesAgentSessionStateToHistory()
    {
        string logPath = CreatePath("logs");
        string historyPath = CreatePath("history");
        var logger = new FileCopilotChatLogger(logPath, historyPath);
        Guid sessionId = Guid.NewGuid();
        var firstStateProvider = new TestSessionStateProvider("{\"conversationId\":\"conversation-1\"}");
        var secondStateProvider = new TestSessionStateProvider("{\"conversationId\":\"conversation-2\",\"turn\":2}");

        await logger.LogMessageAsync(sessionId, new CopilotChatMessage(ChatRole.User, "第一条消息"), firstStateProvider);
        await logger.LogMessageAsync(sessionId, new CopilotChatMessage(ChatRole.Assistant, "第二条消息"), secondStateProvider);

        string historyFile = GetSingleFile(historyPath, "*.xml");
        XDocument historyDocument = XDocument.Load(historyFile);

        Assert.AreEqual("{" + "\"conversationId\":\"conversation-2\",\"turn\":2}", historyDocument.Root?.Element("AgentSessionState")?.Value);
        Assert.HasCount(2, historyDocument.Root?.Element("Messages")?.Elements("Message")?.ToArray() ?? []);
        Assert.AreEqual(1, firstStateProvider.CallCount);
        Assert.AreEqual(1, secondStateProvider.CallCount);
    }

    [TestMethod]
    [Description("记录多模态和审批消息片段时应写入聊天历史 XML 文件")]
    public async Task LogMessageAsync_WhenMessageContainsSpecialItems_WritesMessageItemsToHistory()
    {
        string logPath = CreatePath("logs");
        string historyPath = CreatePath("history");
        var logger = new FileCopilotChatLogger(logPath, historyPath);
        var message = new CopilotChatMessage(ChatRole.Assistant, "");
        message.MessageItems.Add(new CopilotChatImageItem(BinaryData.FromBytes([1, 2, 3]), "image/png"));
        message.MessageItems.Add(new CopilotChatAudioItem(BinaryData.FromBytes([4, 5, 6]), "audio/wav"));
        var approvalToolItem = new CopilotChatApprovalToolItem("approval-1", "DeleteFile", "demo.txt", "请确认是否删除文件。", "删除文件");
        InvokeApprovalMethod(approvalToolItem, "Reject", "不允许删除");
        approvalToolItem.OutputText = "已拒绝";
        message.MessageItems.Add(approvalToolItem);

        await logger.LogMessageAsync(Guid.NewGuid(), message);

        string historyFile = GetSingleFile(historyPath, "*.xml");
        XDocument historyDocument = XDocument.Load(historyFile);
        XElement messageItemsElement = historyDocument.Root?.Element("Messages")?.Element("Message")?.Element("MessageItems")
                                      ?? throw new AssertFailedException("未找到 MessageItems 元素。");
        XElement imageElement = messageItemsElement.Element("ImageItem") ?? throw new AssertFailedException("未找到 ImageItem 元素。");
        XElement audioElement = messageItemsElement.Element("AudioItem") ?? throw new AssertFailedException("未找到 AudioItem 元素。");
        XElement approvalToolElement = messageItemsElement.Element("ApprovalToolItem") ?? throw new AssertFailedException("未找到 ApprovalToolItem 元素。");

        Assert.AreEqual("image/png", imageElement.Attribute("MimeType")?.Value);
        Assert.AreEqual(Convert.ToBase64String([1, 2, 3]), imageElement.Value);
        Assert.AreEqual("audio/wav", audioElement.Attribute("MimeType")?.Value);
        Assert.AreEqual(Convert.ToBase64String([4, 5, 6]), audioElement.Value);
        Assert.AreEqual("approval-1", approvalToolElement.Attribute("CallId")?.Value);
        Assert.AreEqual("DeleteFile", approvalToolElement.Attribute("ToolName")?.Value);
        Assert.AreEqual("删除文件", approvalToolElement.Attribute("DisplayName")?.Value);
        Assert.AreEqual(CopilotToolApprovalState.Rejected.ToString(), approvalToolElement.Attribute("ApprovalState")?.Value);
        Assert.AreEqual("demo.txt", approvalToolElement.Element("Input")?.Value);
        Assert.AreEqual("已拒绝", approvalToolElement.Element("Output")?.Value);
        Assert.AreEqual("请确认是否删除文件。", approvalToolElement.Element("ApprovalDescription")?.Value);
        Assert.AreEqual("不允许删除", approvalToolElement.Element("DecisionReason")?.Value);
    }

    private sealed class TestSessionStateProvider : ICopilotChatSessionStateProvider
    {
        private readonly JsonElement _sessionState;

        public TestSessionStateProvider(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            _sessionState = document.RootElement.Clone();
        }

        public int CallCount { get; private set; }

        public Task<JsonElement?> GetSerializedSessionStateAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<JsonElement?>(_sessionState);
        }
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

    private static void InvokeApprovalMethod(CopilotChatApprovalToolItem approvalToolItem, string methodName, params object?[] arguments)
    {
        MethodInfo methodInfo = typeof(CopilotChatApprovalToolItem).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                                ?? throw new MissingMethodException(nameof(CopilotChatApprovalToolItem), methodName);

        methodInfo.Invoke(approvalToolItem, arguments);
    }
}
