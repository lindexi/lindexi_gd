using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Tests.Fakes;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.Runtime.CompilerServices;

namespace AgentLib.Tests;

[TestClass]
public class CopilotChatManagerChatReducerTests
{
    [TestMethod]
    [Description("ReduceSessionAsync 在没有 AgentSession 时应静默返回，不抛异常")]
    public async Task ReduceSessionAsync_WhenNoAgentSession_ReturnsSilently()
    {
        var primaryChatClient = new FakeChatClient();
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        await context.ChatManager.ReduceSessionAsync();
    }

    [TestMethod]
    [Description("ReduceSessionAsync 在有消息时应触发压缩，reducer 应收到包含历史和系统 prompt 的消息")]
    public async Task ReduceSessionAsync_WhenMessagesExist_TriggersCompression()
    {
        var primaryChatClient = new FakeChatClient();
        List<ChatMessage>? capturedMessages = null;

        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("助手回复"));

        primaryChatClient.OnGetResponseAsync = (messages, _, cancellationToken) =>
        {
            capturedMessages = messages.ToList();
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "这是对话的摘要")]));
        };

        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        await context.ChatManager.SendMessageAsync(contents: [new TextContent("第一轮对话")], withHistory: true);
        await context.ChatManager.SendMessageAsync(contents: [new TextContent("第二轮对话")], withHistory: true);

        await context.ChatManager.ReduceSessionAsync();

        Assert.IsNotNull(capturedMessages, "Reducer 应该收到消息");
        Assert.IsTrue(capturedMessages.Count >= 2, "消息数量应至少包含历史对话");
        Assert.IsTrue(capturedMessages.Any(m => m.Role == ChatRole.System), "应包含系统 prompt");
    }

    [TestMethod]
    [Description("ReduceSessionAsync 压缩后应替换 ChatHistory 为摘要结果")]
    public async Task ReduceSessionAsync_AfterCompression_ReplacesChatHistory()
    {
        var primaryChatClient = new FakeChatClient();
        string summaryText = "用户和助手进行了两轮对话，讨论了测试话题";

        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("助手回复"));

        primaryChatClient.OnGetResponseAsync = (messages, _, cancellationToken) =>
        {
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, summaryText)]));
        };

        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        await context.ChatManager.SendMessageAsync(contents: [new TextContent("第一轮对话")], withHistory: true);
        await context.ChatManager.SendMessageAsync(contents: [new TextContent("第二轮对话")], withHistory: true);

        await context.ChatManager.ReduceSessionAsync();

        var agentSession = context.ChatManager.SelectedSession.AgentSession;
        Assert.IsNotNull(agentSession, "AgentSession 应存在");

        bool hasHistory = agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? compressedMessages);
        Assert.IsTrue(hasHistory, "应能获取压缩后的 ChatHistory");
        Assert.IsNotNull(compressedMessages, "压缩后的消息不应为 null");
        Assert.IsTrue(compressedMessages.Any(m => m.Text?.Contains(summaryText) == true),
            "压缩后的消息应包含摘要内容");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateStreamingUpdatesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
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
