using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Model;

[TestClass]
public class CopilotChatSessionTests
{
    [TestMethod]
    [Description("添加首条用户消息后应根据内容自动生成会话标题")]
    public async Task AddMessage_WhenFirstUserMessageAdded_UpdatesTitleFromContent()
    {
        var session = new CopilotChatSession(Guid.NewGuid(), new DateTimeOffset(2025, 1, 2, 3, 4, 0, TimeSpan.Zero));
        var message = new CopilotChatMessage(ChatRole.User, "   这是   一个  标题   ");

        await session.AddMessageAsync(message);

        Assert.AreEqual("这是 一个 标题", session.Title);
        StringAssert.Contains(session.DisplayText, "01-02 03:04");
    }

    [TestMethod]
    [Description("添加超长用户消息后应将标题截断为二十个字符并追加省略号")]
    public async Task AddMessage_WhenUserMessageIsTooLong_TruncatesTitle()
    {
        var session = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now);
        var message = new CopilotChatMessage(ChatRole.User, "1234567890123456789012345");

        await session.AddMessageAsync(message);

        Assert.AreEqual("12345678901234567890...", session.Title);
    }

    [TestMethod]
    [Description("添加系统消息或预设用户消息时不应覆盖默认标题")]
    public async Task AddMessage_WhenMessageIsNotEligibleForTitle_KeepsDefaultTitle()
    {
        var session = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now);
        var systemMessage = new CopilotChatMessage(ChatRole.System, "系统提示");
        var presetUserMessage = new CopilotChatMessage(ChatRole.User, "用户预设")
        {
            IsPresetInfo = true
        };

        await session.AddMessageAsync(systemMessage);
        await session.AddMessageAsync(presetUserMessage);

        Assert.AreEqual("新会话", session.Title);
    }

    [TestMethod]
    [Description("标题确定后再次添加新的用户消息不应重复覆盖现有标题")]
    public async Task AddMessage_WhenTitleAlreadyGenerated_DoesNotOverwriteTitle()
    {
        var session = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now);

        await session.AddMessageAsync(new CopilotChatMessage(ChatRole.User, "第一个标题"));
        await session.AddMessageAsync(new CopilotChatMessage(ChatRole.User, "第二个标题"));

        Assert.AreEqual("第一个标题", session.Title);
    }

    [TestMethod]
    [Description("后台线程添加消息时应通过主线程调度器修改消息集合")]
    public async Task AddMessage_WhenDispatcherRequiresDispatch_InvokesDispatcher()
    {
        var dispatcher = new RecordingMainThreadDispatcher();
        var session = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now)
        {
            MainThreadDispatcher = dispatcher,
        };

        await session.AddMessageAsync(new CopilotChatMessage(ChatRole.User, "测试消息"));

        Assert.AreEqual(1, dispatcher.InvocationCount);
    }

    private sealed class RecordingMainThreadDispatcher : IMainThreadDispatcher
    {
        public int InvocationCount { get; private set; }

        public bool CheckAccess() => false;

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
