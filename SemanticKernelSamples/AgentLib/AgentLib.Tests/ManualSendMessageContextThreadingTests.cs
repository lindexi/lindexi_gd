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
