using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingSubAgentOutputTests
{
    [TestMethod]
    [Timeout(15000)]
    public async Task WhenChildIsStreamingThenParentTreeContainsTextBeforeCompletion()
    {
        var scenario = new Scenario(pause: true);
        await using var agent = new CodingAgent();
        ICodingAgentRunResult run = await scenario.StartAsync(agent);
        await scenario.StreamPaused.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            CopilotChatMessage child = scenario.Message.MessageItems.OfType<CopilotChatToolItem>().Single().Details!;
            Assert.AreEqual("child text", child.MessageItems.OfType<CopilotChatTextItem>().Single().Text);
            Assert.IsFalse(run.CompletionTask.IsCompleted);
        }
        finally
        {
            scenario.Release.TrySetResult();
        }
        await run.CompletionTask;
    }

    [TestMethod]
    public async Task WhenChildCompletesThenReasoningAndToolProcessRemainInChild()
    {
        var scenario = new Scenario();
        await using var agent = new CodingAgent();
        await (await scenario.StartAsync(agent)).CompletionTask;
        CopilotChatMessage child = scenario.Message.MessageItems.OfType<CopilotChatToolItem>().Single().Details!;
        CollectionAssert.AreEqual(new[] { "child reasoning", "ReturnOutputToParent" },
            new[] { child.MessageItems.OfType<CopilotChatReasoningItem>().Single().Text,
                child.MessageItems.OfType<CopilotChatToolItem>().Single().ToolName });
    }

    [TestMethod]
    public async Task WhenSubmissionNeedsSecondRoundThenOutputAndSessionAreReused()
    {
        var scenario = new Scenario(retry: true);
        await using var agent = new CodingAgent();
        await (await scenario.StartAsync(agent)).CompletionTask;
        CopilotChatMessage child = scenario.Message.MessageItems.OfType<CopilotChatToolItem>().Single().Details!;
        Assert.AreEqual("child textchild textchild done", string.Concat(child.MessageItems.OfType<CopilotChatTextItem>().Select(item => item.Text)));
        Assert.IsTrue(scenario.RetryContainsFirstRound);
        Assert.AreEqual("submitted", scenario.ParentResult);
    }

    [TestMethod]
    public async Task WhenParentDelegatesTwiceThenIndependentFragmentsAreCreated()
    {
        var scenario = new Scenario(delegations: 2);
        await using var agent = new CodingAgent();
        await (await scenario.StartAsync(agent)).CompletionTask;
        CopilotChatToolItem[] children = scenario.Message.MessageItems.OfType<CopilotChatToolItem>().ToArray();
        Assert.HasCount(2, children);
        Assert.AreNotSame(children[0].Details!.MessageItems, children[1].Details!.MessageItems);
        Assert.AreNotEqual(children[0].CallId, children[1].CallId);
    }

    [TestMethod]
    public async Task WhenChildIsCanceledThenNoFallbackOrSecondRoundIsDisplayed()
    {
        var scenario = new Scenario(pause: true);
        using var cancellation = new CancellationTokenSource();
        await using var agent = new CodingAgent();
        ICodingAgentRunResult run = await scenario.StartAsync(agent, cancellation.Token);
        await scenario.StreamPaused.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await run.CompletionTask);
        Assert.AreEqual(string.Empty, scenario.Message.MessageItems.OfType<CopilotChatToolItem>().Single().OutputText);
        Assert.AreEqual(1, scenario.ChildRounds);
    }

    [TestMethod]
    public async Task WhenDispatcherIsCapturedThenAllTreeMutationsAreAwaitedOnDispatcher()
    {
        var dispatcher = new RecordingDispatcher();
        var scenario = new Scenario(dispatcher: dispatcher);
        bool outsideDispatcher = false;
        scenario.Message.MessageItems.CollectionChanged += (_, args) =>
        {
            outsideDispatcher |= !dispatcher.CheckAccess();
            foreach (CopilotChatToolItem tool in args.NewItems?.OfType<CopilotChatToolItem>() ?? [])
            {
                tool.PropertyChanged += (_, e) =>
                {
                    outsideDispatcher |= !dispatcher.CheckAccess();
                    if (e.PropertyName == nameof(CopilotChatToolItem.Details) && tool.Details is { } details)
                    {
                        details.MessageItems.CollectionChanged += (_, _) => outsideDispatcher |= !dispatcher.CheckAccess();
                        details.PropertyChanged += (_, _) => outsideDispatcher |= !dispatcher.CheckAccess();
                    }
                };
            }
        };
        await using var agent = new CodingAgent();
        await (await scenario.StartAsync(agent)).CompletionTask;
        Assert.IsFalse(outsideDispatcher);
        Assert.IsGreaterThan(0, dispatcher.InvocationCount);
    }

    [TestMethod]
    public async Task WhenDelegationReturnsThenOnlyOneToolItemContainsDetails()
    {
        var scenario = new Scenario();
        await using var agent = new CodingAgent();
        await (await scenario.StartAsync(agent)).CompletionTask;
        Assert.HasCount(1, scenario.Message.MessageItems.OfType<CopilotChatToolItem>());
        Assert.IsEmpty(scenario.Message.MessageItems.OfType<CopilotChatSubAgentItem>());
    }

    [TestMethod]
    public async Task WhenDelegatingThenChildUsesModelCallId()
    {
        var scenario = new Scenario();
        await using var agent = new CodingAgent();
        await (await scenario.StartAsync(agent)).CompletionTask;
        Assert.AreEqual("parent-1", scenario.Message.MessageItems.OfType<CopilotChatToolItem>().Single().CallId);
    }

    [TestMethod]
    public async Task WhenDelegationReturnsThenOrdinaryToolContainsReturnedOutput()
    {
        var scenario = new Scenario();
        await using var agent = new CodingAgent();
        await (await scenario.StartAsync(agent)).CompletionTask;
        Assert.AreEqual("submitted", scenario.Message.MessageItems.OfType<CopilotChatToolItem>().Single().OutputText);
    }

    [TestMethod]
    public async Task WhenNeitherRoundSubmitsThenFallbackIsReturnedAsToolResult()
    {
        var scenario = new Scenario(retry: true, fallback: true);
        await using var agent = new CodingAgent();
        await (await scenario.StartAsync(agent)).CompletionTask;
        Assert.AreEqual("子智能体没有返回任何内容", scenario.ParentResult);
        Assert.AreEqual(2, scenario.ChildRounds);
    }

    [TestMethod]
    public void WhenOutputIsSpecifiedThenCompatibilityMessageRemainsIndependent()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, false);
        CopilotChatSubAgentItem child = message.CreateSubAgentItem("child", "task");
        var compatibilityMessage = CopilotChatMessage.CreateAssistant(string.Empty, false);
        var context = new CodingRunContext(new FakeChatClient(), compatibilityMessage, output: child);
        context.Output.AppendText("stream");
        Assert.AreEqual(string.Empty, context.AssistantChatMessage.Content);
    }

    [TestMethod]
    public void WhenCodingDelegationIsReportedInsideSubAgentThenOrdinaryCardContainsResult()
    {
        var child = new CopilotChatSubAgentItem("outer", "child", "task");
        child.AppendFunctionCall(new FunctionCallContent("call", "InvokeCodingSubAgent", new Dictionary<string, object?>()));
        child.CreateSubAgentItem("InvokeCodingSubAgent", "task", "fragment");
        child.AppendFunctionResult(new FunctionResultContent("call", "submitted"));
        Assert.AreEqual("submitted", child.MessageItems.OfType<CopilotChatToolItem>().Single().OutputText);
    }

    [TestMethod]
    public async Task WhenChildReportsUsageThenUsageBelongsToChildOutput()
    {
        var scenario = new Scenario();
        await using var agent = new CodingAgent();
        await (await scenario.StartAsync(agent)).CompletionTask;
        Assert.AreEqual(7L, scenario.Message.MessageItems.OfType<CopilotChatToolItem>().Single().Details!.TotalUsageDetails?.InputTokenCount);
    }

    [TestMethod]
    public async Task WhenResponseIsEmptyThenPlaceholderCleanupUsesDispatcher()
    {
        var dispatcher = new RecordingDispatcher();
        var message = CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, false);
        bool outsideDispatcher = false;
        message.MessageItems.CollectionChanged += (_, _) => outsideDispatcher |= !dispatcher.CheckAccess();
        var client = new FakeChatClient { OnGetStreamingResponseAsync = (_, _, _) => EmptyAsync() };
        await using var agent = new CodingAgent();
        ICodingAgentRunResult run = await agent.RunAsync(new CodingRunContext(client, message,
            mainThreadDispatcher: dispatcher), [new TextContent("task")], null);
        await run.CompletionTask;
        Assert.IsFalse(outsideDispatcher);
        Assert.AreEqual(string.Empty, message.Content);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmptyAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    private sealed class Scenario(bool pause = false, bool retry = false, bool fallback = false,
        int delegations = 1, RecordingDispatcher? dispatcher = null)
    {
        public CopilotChatMessage Message { get; } = CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, false);
        public TaskCompletionSource StreamPaused { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int ChildRounds { get; private set; }
        public bool RetryContainsFirstRound { get; private set; }
        public string? ParentResult { get; private set; }
        private int _parentCalls;

        public Task<ICodingAgentRunResult> StartAsync(CodingAgent agent, CancellationToken cancellationToken = default)
        {
            var client = new FakeChatClient { OnGetStreamingResponseAsync = (messages, _, ct) => RespondAsync(messages.ToArray(), ct) };
            return agent.RunAsync(new CodingRunContext(client, Message, mainThreadDispatcher: dispatcher),
                [new TextContent("parent task")], null, new CodingChatRunOptions(false, false, null), cancellationToken);
        }

        private async IAsyncEnumerable<ChatResponseUpdate> RespondAsync(ChatMessage[] messages,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            bool child = messages.Any(message => message.Role == ChatRole.System
                && message.Text.Contains("最终结果必须通过 `ReturnOutputToParent` 提交", StringComparison.Ordinal));
            if (!child)
            {
                ParentResult = messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().LastOrDefault()?.Result?.ToString();
                if (_parentCalls++ < delegations)
                {
                    yield return Call("parent-" + _parentCalls, "InvokeCodingSubAgent", "prompt", "child task " + _parentCalls);
                }
                else
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, "parent done");
                }
                yield break;
            }

            if (messages.Last().Role == ChatRole.Tool)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, "child done");
                yield break;
            }

            ChildRounds++;
            yield return new ChatResponseUpdate(ChatRole.Assistant,
                [new UsageContent(new UsageDetails { InputTokenCount = 7, OutputTokenCount = 3 })]);
            RetryContainsFirstRound = messages.Any(message => message.Role == ChatRole.Assistant && message.Text.Contains("child text", StringComparison.Ordinal));
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextReasoningContent("child reasoning")]);
            yield return new ChatResponseUpdate(ChatRole.Assistant, "child text");
            if (pause)
            {
                StreamPaused.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            if (!fallback && (!retry || ChildRounds == 2))
            {
                yield return Call("submit-" + ChildRounds, "ReturnOutputToParent", "text", "submitted");
            }
        }

        private static ChatResponseUpdate Call(string id, string name, string key, string value) =>
            new(ChatRole.Assistant, [new FunctionCallContent(id, name, new Dictionary<string, object?> { [key] = value })])
            { FinishReason = ChatFinishReason.ToolCalls };
    }

    private sealed class RecordingDispatcher : IMainThreadDispatcher
    {
        private readonly AsyncLocal<bool> _access = new();
        public int InvocationCount { get; private set; }
        public bool CheckAccess() => _access.Value;
        public async Task InvokeAsync(Func<Task> action)
        {
            InvocationCount++;
            await Task.Yield();
            _access.Value = true;
            try { await action(); }
            finally { _access.Value = false; }
        }
        public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
        {
            T? result = default;
            await InvokeAsync(async () => { result = await action(); });
            return result!;
        }
    }
}
