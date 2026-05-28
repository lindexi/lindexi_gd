using AgentLib.AgentExtensions;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Tests.Fakes;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using System.Text;

namespace AgentLib.Tests;

[TestClass]
public class ReasoningStreamingTests
{
    [TestMethod]
    [Description("交错输出思考与内容时应标记重新进入思考和重新进入内容")]
    public async Task RunReasoningStreamingAsync_WhenThinkingAndContentAreInterleaved_SetsReenterFlags()
    {
        var chatClient = new FakeChatClient();
        chatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateStreamingUpdatesAsync(cancellationToken,
            CopilotChatManagerTestContext.AssistantReasoning("思考1"),
            CopilotChatManagerTestContext.AssistantText("内容1"),
            CopilotChatManagerTestContext.AssistantReasoning("思考2"),
            CopilotChatManagerTestContext.AssistantText("内容2"));
        AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions());

        List<ReasoningAgentResponseUpdate> updates = [];
        await foreach (ReasoningAgentResponseUpdate update in agent.RunReasoningStreamingAsync([new ChatMessage(ChatRole.User, "测试交错输出")]))
        {
            updates.Add(update);
        }

        Assert.AreEqual(4, updates.Count);

        Assert.AreEqual("思考1", updates[0].Reasoning);
        Assert.IsTrue(updates[0].IsFirstThinking);
        Assert.IsFalse(updates[0].IsReenterThinking);
        Assert.IsFalse(updates[0].IsFirstOutputContent);
        Assert.IsFalse(updates[0].IsReenterOutputContent);
        Assert.IsFalse(updates[0].IsThinkingEnd);

        Assert.AreEqual("内容1", updates[1].Text);
        Assert.IsTrue(updates[1].IsFirstOutputContent);
        Assert.IsFalse(updates[1].IsReenterOutputContent);
        Assert.IsTrue(updates[1].IsThinkingEnd);

        Assert.AreEqual("思考2", updates[2].Reasoning);
        Assert.IsFalse(updates[2].IsFirstThinking);
        Assert.IsTrue(updates[2].IsReenterThinking);
        Assert.IsFalse(updates[2].IsFirstOutputContent);
        Assert.IsFalse(updates[2].IsReenterOutputContent);
        Assert.IsFalse(updates[2].IsThinkingEnd);

        Assert.AreEqual("内容2", updates[3].Text);
        Assert.IsFalse(updates[3].IsFirstOutputContent);
        Assert.IsTrue(updates[3].IsReenterOutputContent);
        Assert.IsTrue(updates[3].IsThinkingEnd);
    }

    [TestMethod]
    [Description("控制台输出在重新进入思考和内容时应打印分隔与标题")]
    public async Task RunStreamingAndLogToConsoleAsync_WhenThinkingAndContentAreInterleaved_PrintsReenterSections()
    {
        var chatClient = new FakeChatClient();
        chatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateStreamingUpdatesAsync(cancellationToken,
            CopilotChatManagerTestContext.AssistantReasoning("思考1"),
            CopilotChatManagerTestContext.AssistantText("内容1"),
            CopilotChatManagerTestContext.AssistantReasoning("思考2"),
            CopilotChatManagerTestContext.AssistantText("内容2"));
        AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions());

        var output = new StringWriter(new StringBuilder());
        TextWriter original = Console.Out;

        try
        {
            Console.SetOut(output);
            await agent.RunStreamingAndLogToConsoleAsync([new ChatMessage(ChatRole.User, "测试控制台输出")]);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.AreEqual($"思考：{Environment.NewLine}思考1{Environment.NewLine}-------------{Environment.NewLine}内容1{Environment.NewLine}-------------{Environment.NewLine}思考：{Environment.NewLine}思考2{Environment.NewLine}-------------{Environment.NewLine}内容2", output.ToString());
    }

    [TestMethod]
    [Description("控制台输出在流结束后应打印累计用量统计")]
    public async Task RunStreamingAndLogToConsoleAsync_WhenUsageContentExists_PrintsTotalUsageAtEnd()
    {
        var chatClient = new FakeChatClient();
        chatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateStreamingUpdatesAsync(cancellationToken,
            CopilotChatManagerTestContext.AssistantText("内容1"),
            CopilotChatManagerTestContext.AssistantUsage(new UsageDetails
            {
                InputTokenCount = 10,
                OutputTokenCount = 3,
                TotalTokenCount = 13
            }),
            CopilotChatManagerTestContext.AssistantText("内容2"),
            CopilotChatManagerTestContext.AssistantUsage(new UsageDetails
            {
                InputTokenCount = 5,
                OutputTokenCount = 7,
                TotalTokenCount = 12
            }));
        AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions());

        var output = new StringWriter(new StringBuilder());
        TextWriter original = Console.Out;

        try
        {
            Console.SetOut(output);
            await agent.RunStreamingAndLogToConsoleAsync([new ChatMessage(ChatRole.User, "测试用量输出")]);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.AreEqual($"内容1内容2{Environment.NewLine}-------------{Environment.NewLine}用量统计：输入 Token=15，输出 Token=10，总 Token=25{Environment.NewLine}", output.ToString());
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateStreamingUpdatesAsync(CancellationToken cancellationToken, params ChatResponseUpdate[] updates)
    {
        foreach (ChatResponseUpdate update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }
}
