using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using AgentLib.Tests.Fakes;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests;

[TestClass]
public sealed class CopilotChatManagerSessionRestoreTests
{
    [TestMethod(DisplayName = "添加已有会话应加入集合并且不生成额外欢迎消息")]
    [Timeout(10000, CooperativeCancellation = true)]
    public void AddSessionShouldSelectExistingMessagesWithoutWelcomeMessage()
    {
        CopilotChatManager manager = CreateManager();
        var restoredSession = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now);
        restoredSession.AddMessage(new CopilotChatMessage(ChatRole.User, "历史消息"));

        CopilotChatSession addedSession = manager.AddSession(restoredSession, select: true);

        Assert.AreSame(restoredSession, addedSession);
        Assert.AreSame(restoredSession, manager.SelectedSession);
        Assert.HasCount(1, restoredSession.ChatMessages);
    }

    [TestMethod(DisplayName = "添加相同标识会话应复用集合中的已有实例")]
    [Timeout(10000, CooperativeCancellation = true)]
    public void AddSessionWithSameIdShouldReuseExistingInstance()
    {
        CopilotChatManager manager = CreateManager();
        Guid sessionId = Guid.NewGuid();
        var existingSession = new CopilotChatSession(sessionId, DateTimeOffset.Now);
        var duplicateSession = new CopilotChatSession(sessionId, DateTimeOffset.Now.AddMinutes(1));
        manager.AddSession(existingSession);

        CopilotChatSession addedSession = manager.AddSession(duplicateSession, select: true);

        Assert.AreSame(existingSession, addedSession);
        Assert.AreSame(existingSession, manager.SelectedSession);
        Assert.AreEqual(1, manager.ChatSessions.Count(session => session.SessionId == sessionId));
    }

    [TestMethod(DisplayName = "移除未选中会话应按对象实例删除并保留当前选择")]
    [Timeout(10000, CooperativeCancellation = true)]
    public void RemoveUnselectedSessionShouldKeepSelection()
    {
        CopilotChatManager manager = CreateManager();
        CopilotChatSession selectedSession = manager.SelectedSession;
        var removableSession = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now);
        manager.AddSession(removableSession);

        bool removed = manager.RemoveSession(removableSession);

        Assert.IsTrue(removed);
        Assert.DoesNotContain(removableSession, manager.ChatSessions);
        Assert.AreSame(selectedSession, manager.SelectedSession);
    }

    [TestMethod(DisplayName = "相同标识的不同会话实例不可误删集合会话")]
    [Timeout(10000, CooperativeCancellation = true)]
    public void RemoveDifferentInstanceWithSameIdShouldNotRemoveSession()
    {
        CopilotChatManager manager = CreateManager();
        Guid sessionId = Guid.NewGuid();
        var storedSession = new CopilotChatSession(sessionId, DateTimeOffset.Now);
        var differentInstance = new CopilotChatSession(sessionId, DateTimeOffset.Now);
        manager.AddSession(storedSession, select: true);

        bool removed = manager.RemoveSession(differentInstance);

        Assert.IsFalse(removed);
        Assert.Contains(storedSession, manager.ChatSessions);
        Assert.AreSame(storedSession, manager.SelectedSession);
    }

    [TestMethod(DisplayName = "移除选中会话应选择剩余会话")]
    [Timeout(10000, CooperativeCancellation = true)]
    public void RemoveSelectedSessionShouldSelectRemainingSession()
    {
        CopilotChatManager manager = CreateManager();
        CopilotChatSession originalSession = manager.SelectedSession;
        var removableSession = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now);
        manager.AddSession(removableSession, select: true);

        bool removed = manager.RemoveSession(removableSession);

        Assert.IsTrue(removed);
        Assert.AreSame(originalSession, manager.SelectedSession);
    }

    [TestMethod(DisplayName = "移除最后会话应创建新的可用空会话")]
    [Timeout(10000, CooperativeCancellation = true)]
    public void RemoveLastSessionShouldCreateNewEmptySession()
    {
        CopilotChatManager manager = CreateManager();
        CopilotChatSession lastSession = manager.SelectedSession;

        bool removed = manager.RemoveSession(lastSession);

        Assert.IsTrue(removed);
        Assert.HasCount(1, manager.ChatSessions);
        Assert.AreNotSame(lastSession, manager.SelectedSession);
        Assert.AreSame(manager.ChatSessions[0], manager.SelectedSession);
    }

    [TestMethod(DisplayName = "开始聊天作用域应只切换管理器全局状态")]
    [Timeout(10000, CooperativeCancellation = true)]
    public void StartChattingShouldToggleGlobalStateWithoutSession()
    {
        CopilotChatManager manager = CreateManager();
        CopilotChatSession selectedSession = manager.SelectedSession;

        using (manager.StartChatting())
        {
            Assert.IsTrue(manager.IsChatting);
            Assert.AreSame(selectedSession, manager.SelectedSession);
        }

        Assert.IsFalse(manager.IsChatting);
        Assert.AreSame(selectedSession, manager.SelectedSession);
    }

    private static CopilotChatManager CreateManager()
        => CopilotChatManagerTestContext.Create(new FakeChatClient()).ChatManager;
}