using AgentLib.Logging;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System.Text.Json;
using System.Xml.Linq;

namespace AgentLib.Tests.Logging;

[TestClass]
public sealed class FileCopilotChatSessionStoreTests
{
    [TestMethod(DisplayName = "版本二会话数据往返应保留元数据消息片段用量和代理状态")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task SaveAndLoadShouldPreserveCompleteSessionSnapshot()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new FileCopilotChatSessionStore(temporaryDirectory.SessionPath, temporaryDirectory.LogPath);
        Guid sessionId = Guid.NewGuid();
        DateTimeOffset startedTime = new(2026, 4, 13, 8, 30, 0, TimeSpan.FromHours(8));
        var session = new CopilotChatSession(sessionId, startedTime);
        session.SetTitle("历史会话标题", TitleSource.UserSet);

        var userMessage = new CopilotChatMessage(ChatRole.User,
        [
            new TextContent("分析图片"),
            new DataContent(BinaryData.FromBytes([1, 2, 3]), "image/png"),
            new DataContent(BinaryData.FromBytes([4, 5, 6]), "audio/wav"),
        ]);
        var assistantMessage = new CopilotChatMessage(ChatRole.Assistant, "回答");
        assistantMessage.AppendReasoning("思考");
        assistantMessage.MessageItems.Add(new CopilotChatToolItem("tool-1", "ReadFile", "demo.cs", "content"));
        var approvalItem = new CopilotChatApprovalToolItem("approval-1", "DeleteFile", "demo.cs", "确认删除", "删除文件");
        approvalItem.Reject("保留文件");
        approvalItem.OutputText = "已拒绝";
        assistantMessage.MessageItems.Add(approvalItem);
        var subAgentItem = new CopilotChatSubAgentItem("sub-1", "Reviewer", "审查代码", "完成");
        subAgentItem.MessageItems.Add(new CopilotChatTextItem("子代理结果"));
        assistantMessage.MessageItems.Add(subAgentItem);
        assistantMessage.AppendUsageDetails(new UsageDetails
        {
            TotalTokenCount = 100,
            InputTokenCount = 60,
            OutputTokenCount = 40,
            ReasoningTokenCount = 20,
            CachedInputTokenCount = 10,
        });
        await session.AddMessageAsync(userMessage);
        await session.AddMessageAsync(assistantMessage);
        using JsonDocument stateDocument = JsonDocument.Parse("{\"conversationId\":\"session-1\",\"turn\":2}");

        await store.SaveSessionAsync(session, stateDocument.RootElement);
        CopilotChatSessionPersistenceData persistenceData = await store.LoadSessionAsync(sessionId);

        Assert.AreEqual(FileCopilotChatSessionStore.CurrentFormatVersion, persistenceData.FormatVersion);
        Assert.AreEqual(sessionId, persistenceData.SessionId);
        Assert.AreEqual(startedTime, persistenceData.StartedTime);
        Assert.AreEqual("历史会话标题", persistenceData.Title);
        Assert.HasCount(2, persistenceData.Messages);
        Assert.AreEqual(stateDocument.RootElement.GetRawText(), persistenceData.AgentSessionState?.GetRawText());
        Assert.HasCount(3, persistenceData.Messages[0].MessageItems);
        Assert.HasCount(5, persistenceData.Messages[1].MessageItems);
        Assert.AreEqual(100L, persistenceData.Messages[1].TotalUsageDetails?.TotalTokenCount);
        Assert.AreEqual(CopilotToolApprovalState.Rejected,
            persistenceData.Messages[1].MessageItems.OfType<CopilotChatApprovalToolItem>().Single().ApprovalState);
    }

    [TestMethod(DisplayName = "历史列表应按最近时间返回摘要并跳过损坏文件")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task ListSessionsShouldReturnSummariesAndSkipCorruptedFiles()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new FileCopilotChatSessionStore(temporaryDirectory.SessionPath, temporaryDirectory.LogPath);
        var olderSession = CreateSession(new DateTimeOffset(2026, 4, 12, 8, 0, 0, TimeSpan.Zero), "较早会话", "一");
        var newerSession = CreateSession(new DateTimeOffset(2026, 4, 13, 8, 0, 0, TimeSpan.Zero), "最近会话", "二");
        await store.SaveSessionAsync(olderSession, agentSessionState: null);
        await store.SaveSessionAsync(newerSession, agentSessionState: null);
        Directory.CreateDirectory(temporaryDirectory.SessionPath);
        await File.WriteAllTextAsync(Path.Join(temporaryDirectory.SessionPath, "corrupted.xml"), "<invalid>");

        IReadOnlyList<CopilotChatSessionSummary> summaries = await store.ListSessionsAsync();

        Assert.HasCount(2, summaries);
        Assert.AreEqual(newerSession.SessionId, summaries[0].SessionId);
        Assert.AreEqual("最近会话", summaries[0].Title);
        Assert.AreEqual(1, summaries[0].MessageCount);
    }

    [TestMethod(DisplayName = "旧版无版本历史应按版本一兼容读取")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task LoadSessionShouldSupportLegacyHistoryWithoutVersion()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new FileCopilotChatSessionStore(temporaryDirectory.SessionPath, temporaryDirectory.LogPath);
        Guid sessionId = Guid.NewGuid();
        Directory.CreateDirectory(temporaryDirectory.SessionPath);
        string filePath = Path.Join(temporaryDirectory.SessionPath, $"20260413_090000_{sessionId:N}.xml");
        await File.WriteAllTextAsync(filePath, $$"""
            <CopilotChatSessionHistory SessionId="{{sessionId}}" CreatedTime="2026-04-13T09:00:00.0000000+08:00">
              <AgentSessionState><![CDATA[{"turn":1}]]></AgentSessionState>
              <Messages>
                <Message Role="user" Author="我" CreatedTime="2026-04-13T09:00:01.0000000+08:00" IsPresetInfo="false">
                  <Content>旧消息</Content>
                  <Reason></Reason>
                  <MessageItems><TextItem Text="旧消息" /></MessageItems>
                </Message>
              </Messages>
            </CopilotChatSessionHistory>
            """);

        CopilotChatSessionPersistenceData persistenceData = await store.LoadSessionAsync(sessionId);

        Assert.AreEqual(1, persistenceData.FormatVersion);
        Assert.AreEqual("旧消息", persistenceData.Title);
        Assert.AreEqual("旧消息", persistenceData.Messages.Single().Content);
        Assert.AreEqual("{\"turn\":1}", persistenceData.AgentSessionState?.GetRawText());
    }

    [TestMethod(DisplayName = "删除会话应同时删除历史文件和对应日志")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task DeleteSessionShouldRemoveHistoryAndLogs()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new FileCopilotChatSessionStore(temporaryDirectory.SessionPath, temporaryDirectory.LogPath);
        CopilotChatSession session = CreateSession(DateTimeOffset.Now, "待删除", "内容");
        await store.SaveSessionAsync(session, agentSessionState: null);
        string dayDirectory = Path.Join(temporaryDirectory.LogPath, session.StartedTime.ToString("yyyyMMdd"));
        Directory.CreateDirectory(dayDirectory);
        string matchingLog = Path.Join(dayDirectory, $"log_{session.SessionId:N}.log");
        string unrelatedLog = Path.Join(dayDirectory, $"log_{Guid.NewGuid():N}.log");
        await File.WriteAllTextAsync(matchingLog, "matching");
        await File.WriteAllTextAsync(unrelatedLog, "unrelated");

        bool deleted = await store.DeleteSessionAsync(session.SessionId);

        Assert.IsTrue(deleted);
        Assert.IsFalse(File.Exists(matchingLog));
        Assert.IsTrue(File.Exists(unrelatedLog));
        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => store.LoadSessionAsync(session.SessionId));
    }

    [TestMethod(DisplayName = "同一会话连续保存应更新同一个历史文件")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task SaveSessionTwiceShouldUpdateSingleHistoryFile()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new FileCopilotChatSessionStore(temporaryDirectory.SessionPath, temporaryDirectory.LogPath);
        CopilotChatSession session = CreateSession(DateTimeOffset.Now, "连续保存", "第一条");
        await store.SaveSessionAsync(session, agentSessionState: null);
        await session.AddMessageAsync(new CopilotChatMessage(ChatRole.Assistant, "第二条"));

        await store.SaveSessionAsync(session, agentSessionState: null);

        Assert.HasCount(1, Directory.GetFiles(temporaryDirectory.SessionPath, "*.xml"));
        CopilotChatSessionPersistenceData persistenceData = await store.LoadSessionAsync(session.SessionId);
        Assert.HasCount(2, persistenceData.Messages);
    }

    [TestMethod(DisplayName = "未来版本历史应被明确拒绝")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task LoadSessionShouldRejectFutureFormatVersion()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new FileCopilotChatSessionStore(temporaryDirectory.SessionPath);
        Guid sessionId = Guid.NewGuid();
        Directory.CreateDirectory(temporaryDirectory.SessionPath);
        string filePath = Path.Join(temporaryDirectory.SessionPath, $"20260413_090000_{sessionId:N}.xml");
        await File.WriteAllTextAsync(filePath, $$"""
            <CopilotChatSessionHistory FormatVersion="{{FileCopilotChatSessionStore.CurrentFormatVersion + 1}}" SessionId="{{sessionId}}" StartedTime="2026-04-13T09:00:00+08:00" Title="未来版本">
              <Messages />
            </CopilotChatSessionHistory>
            """);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => store.LoadSessionAsync(sessionId));
    }

    [TestMethod(DisplayName = "损坏的指定历史应在加载时报告格式错误")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task LoadSessionShouldRejectCorruptedHistory()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new FileCopilotChatSessionStore(temporaryDirectory.SessionPath);
        Guid sessionId = Guid.NewGuid();
        Directory.CreateDirectory(temporaryDirectory.SessionPath);
        await File.WriteAllTextAsync(
            Path.Join(temporaryDirectory.SessionPath, $"20260413_090000_{sessionId:N}.xml"),
            "<invalid>");

        await Assert.ThrowsAsync<System.Xml.XmlException>(() => store.LoadSessionAsync(sessionId));
    }

    [TestMethod(DisplayName = "原子保存取消时应保留旧文件并清理临时文件")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task SaveDocumentAsyncCancellationShouldPreserveExistingFile()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        Directory.CreateDirectory(temporaryDirectory.SessionPath);
        string filePath = Path.Join(temporaryDirectory.SessionPath, "session.xml");
        await File.WriteAllTextAsync(filePath, "原始内容");
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => FileCopilotChatSessionStore.SaveDocumentAsync(
            filePath,
            new XDocument(new XElement("Replacement", new string('x', 10000))),
            cancellationTokenSource.Token));

        Assert.AreEqual("原始内容", await File.ReadAllTextAsync(filePath));
        Assert.IsFalse(Directory.GetFiles(temporaryDirectory.SessionPath, "*.tmp").Any());
    }

    private static CopilotChatSession CreateSession(DateTimeOffset startedTime, string title, string content)
    {
        var session = new CopilotChatSession(Guid.NewGuid(), startedTime);
        session.SetTitle(title, TitleSource.UserSet);
        session.AddMessage(new CopilotChatMessage(ChatRole.User, content));
        return session;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            RootPath = Path.Join(Path.GetTempPath(), "AgentLib.Tests", Guid.NewGuid().ToString("N"));
            SessionPath = Path.Join(RootPath, "Sessions");
            LogPath = Path.Join(RootPath, "Logs");
        }

        public string RootPath { get; }

        public string SessionPath { get; }

        public string LogPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
