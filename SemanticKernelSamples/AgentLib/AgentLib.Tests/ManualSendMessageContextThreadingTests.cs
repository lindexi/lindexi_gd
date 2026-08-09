using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using AgentLib.Tests.Fakes;

namespace AgentLib.Tests;

[TestClass]
public sealed class ManualSendMessageContextThreadingTests
{
    [TestMethod(DisplayName = "手动发送上下文应原样暴露管理器的主线程调度器")]
    [Timeout(10_000)]
    public async Task CreateManualSendMessageContextAsync_ShouldExposeManagerDispatcher()
    {
        var dispatcher = new RecordingMainThreadDispatcher();
        var context = CopilotChatManagerTestContext.Create(new FakeChatClient());
        var manager = new CopilotChatManager
        {
            MainThreadDispatcher = dispatcher,
        };
        manager.AgentApiEndpointManager.RegisterLanguageModelProvider(
            new FakeLanguageModelProvider([
                new FakeLanguageModel(context.PrimaryChatClient),
            ]));

        IManualSendMessageContext manualContext = await manager.CreateManualSendMessageContextAsync();

        Assert.AreSame(dispatcher, manualContext.MainThreadDispatcher);
        Assert.AreEqual(0, dispatcher.InvocationCount);
    }

    [TestMethod(DisplayName = "管理器的主线程调度器应赋给构造期间创建的初始会话")]
    public void MainThreadDispatcher_ShouldBeAssignedToInitialSession()
    {
        var dispatcher = new RecordingMainThreadDispatcher();
        var manager = new CopilotChatManager
        {
            MainThreadDispatcher = dispatcher,
        };

        Assert.AreSame(dispatcher, manager.SelectedSession.MainThreadDispatcher);
    }

    [TestMethod(DisplayName = "管理器新建的会话应继承主线程调度器")]
    public void CreateNewSession_ShouldAssignMainThreadDispatcher()
    {
        var dispatcher = new RecordingMainThreadDispatcher();
        var manager = new CopilotChatManager
        {
            MainThreadDispatcher = dispatcher,
        };
        manager.SelectedSession.AddMessage(new CopilotChatMessage(Microsoft.Extensions.AI.ChatRole.User, "占用初始会话"));

        manager.CreateNewSession();

        Assert.AreSame(dispatcher, manager.SelectedSession.MainThreadDispatcher);
    }

    private sealed class RecordingMainThreadDispatcher : IMainThreadDispatcher
    {
        public int InvocationCount { get; private set; }

        public bool CheckAccess() => true;

        public Task InvokeAsync(Func<Task> action)
        {
            InvocationCount++;
            return action();
        }

        public Task<T> InvokeAsync<T>(Func<Task<T>> action)
        {
            InvocationCount++;
            return action();
        }
    }
}
