using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Tests.Fakes;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests;

[TestClass]
public class CopilotChatManagerSessionTests
{
    [TestMethod]
    [Description("withHistory 为 true 时应创建并保留当前会话的 AgentSession")]
    public async Task SendMessageAsync_WhenWithHistoryIsTrue_CreatesAndStoresAgentSession()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateStreamingUpdatesAsync(cancellationToken,
            CopilotChatManagerTestContext.AssistantText("助手回复"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        await context.ChatManager.SendMessageAsync(contents: [new TextContent("保留历史")], withHistory: true);

        Assert.IsNotNull(context.ChatManager.SelectedSession.AgentSession);
    }

    [TestMethod]
    [Description("withHistory 为 false 时不应把运行期 AgentSession 保存到当前会话")]
    public async Task SendMessageAsync_WhenWithHistoryIsFalse_DoesNotStoreAgentSession()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateStreamingUpdatesAsync(cancellationToken,
            CopilotChatManagerTestContext.AssistantText("助手回复"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        await context.ChatManager.SendMessageAsync(contents: [new TextContent("不保留历史")], withHistory: false);

        Assert.IsNull(context.ChatManager.SelectedSession.AgentSession);
    }

    [TestMethod]
    [Description("重复带历史发送时应继续复用首次创建的同一个 AgentSession")]
    public async Task SendMessageAsync_WhenWithHistoryIsTrueTwice_ReusesExistingAgentSession()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateStreamingUpdatesAsync(cancellationToken,
            CopilotChatManagerTestContext.AssistantText("助手回复"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        await context.ChatManager.SendMessageAsync(contents: [new TextContent("第一次")], withHistory: true);
        var firstSession = context.ChatManager.SelectedSession.AgentSession;
        await context.ChatManager.SendMessageAsync(contents: [new TextContent("第二次")], withHistory: true);

        Assert.IsNotNull(firstSession);
        Assert.AreSame(firstSession, context.ChatManager.SelectedSession.AgentSession);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateStreamingUpdatesAsync(CancellationToken cancellationToken,
        params ChatResponseUpdate[] updates)
    {
        foreach (ChatResponseUpdate update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }
}