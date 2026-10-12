using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingRunEntryTests
{
    [TestMethod]
    public async Task RunAsyncWhenContentsAreEmptyShouldRejectBeforeWorkspaceInitialization()
    {
        await using var agent = new CodingAgent();
        var context = new CodingRunContext(new FakeChatClient(),
            CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false));

        ArgumentException exception = await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            agent.RunAsync(context, [], Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));

        Assert.AreEqual("contents", exception.ParamName);
    }

    [TestMethod]
    public async Task RunAsyncWhenOptionsAreOmittedShouldCompleteConversation()
    {
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, cancellationToken) => RespondAsync(cancellationToken),
        };
        await using var agent = new CodingAgent();
        var context = new CodingRunContext(client,
            CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false));

        ICodingAgentRunResult run = await agent.RunAsync(context, [new TextContent("task")], null);

        Assert.AreEqual("done", await run.CompletionTask);
    }

    [TestMethod]
    public void CodingRunContextWhenCreatedShouldUseAssistantMessageAsOutput()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false);
        var context = new CodingRunContext(new FakeChatClient(), message);

        Assert.AreSame(message, context.Output);
    }

    [TestMethod]
    public async Task RunAsyncWhenMessageHasPlaceholderShouldReplaceItWithOutput()
    {
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, cancellationToken) => RespondAsync(cancellationToken),
        };
        await using var agent = new CodingAgent();
        var message = CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, isPresetInfo: false);
        var context = new CodingRunContext(client, message);

        ICodingAgentRunResult run = await agent.RunAsync(context, [new TextContent("task")], null);
        await run.CompletionTask;

        Assert.AreEqual("done", message.Content);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> RespondAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("done")]);
        await Task.CompletedTask;
    }
}
