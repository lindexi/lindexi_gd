using System.Runtime.CompilerServices;

using AgentLib.Model;

using Microsoft.Extensions.AI;

using PptxGenerator;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// 流式生成重新开始回放功能测试。
/// </summary>
[TestClass]
public sealed class SlideStreamingRestartTests
{
    [TestMethod(DisplayName = "从第一条用户消息重新开始时不回放历史并重新生成")]
    public async Task RestartFromFirstUserMessage_RegeneratesFromEmptyStreamingState()
    {
        const string oldFirstTurnXml = """<Page><Rect Id="old" Width="100" Height="50" Fill="#FF0000"/></Page>""";
        const string newFirstTurnXml = """<Page><Rect Id="new" Width="100" Height="50" Fill="#00FF00"/></Page>""";

        var (chatManager, _, recorder) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTextsAndRecorder(
            oldFirstTurnXml,
            newFirstTurnXml);

        await SendStreamingMessageAsync(chatManager, "生成首页", isFirstMessage: true).ConfigureAwait(false);

        CopilotChatMessage targetMessage = GetUserMessage(chatManager, 0);
        var originalSession = chatManager.Pipeline.ChatManager.SelectedSession;
        var originalPrimaryModel = chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel;

        await chatManager.RestartFromMessageAsync(targetMessage).ConfigureAwait(false);

        Assert.AreSame(originalSession, chatManager.Pipeline.ChatManager.SelectedSession, "重新开始第一条消息后应仍回到原会话。");
        Assert.AreSame(originalPrimaryModel, chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel, "重新开始第一条消息后应恢复原模型。");
        Assert.AreEqual(2, recorder.StreamingCallCount, "第一条消息重启只需要原始生成和目标重新生成两次流式调用。");
        Assert.AreEqual(0, recorder.NonStreamingCallCount, "重新开始不应调用非流式接口。");
        StringAssert.Contains(chatManager.CurrentSlideXml, "new", "第一条消息应从空状态重新生成新结果。");
        Assert.IsFalse(chatManager.CurrentSlideXml.Contains("old", StringComparison.Ordinal), "第一条消息旧结果不应残留。");
    }

    [TestMethod(DisplayName = "从第二条用户消息重新开始时回放第一轮并重新生成第二轮")]
    public async Task RestartFromSecondUserMessage_ReplaysPreviousTurnAndRegeneratesTargetTurn()
    {
        const string firstTurnXml = """<Page><Rect Id="r1" Width="100" Height="50" Fill="#FF0000"/></Page>""";
        const string oldSecondTurnXml = """<Page><Rect Id="r2-old" Width="100" Height="50" Fill="#00FF00"/></Page>""";
        const string newSecondTurnXml = """<Page><Rect Id="r2-new" Width="100" Height="50" Fill="#0000FF"/></Page>""";

        var (chatManager, _, recorder) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTextsAndRecorder(
            firstTurnXml,
            oldSecondTurnXml,
            newSecondTurnXml);

        await SendStreamingMessageAsync(chatManager, "生成首页", isFirstMessage: true).ConfigureAwait(false);
        await SendStreamingMessageAsync(chatManager, "加一个卡片", isFirstMessage: false).ConfigureAwait(false);

        CopilotChatMessage targetMessage = GetUserMessage(chatManager, 1);
        var originalSession = chatManager.Pipeline.ChatManager.SelectedSession;
        var originalPrimaryModel = chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel;

        await chatManager.RestartFromMessageAsync(targetMessage).ConfigureAwait(false);

        Assert.AreSame(originalSession, chatManager.Pipeline.ChatManager.SelectedSession, "重新开始结束后应恢复原会话。");
        Assert.AreSame(originalPrimaryModel, chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel, "重新开始结束后应恢复原模型。");
        Assert.AreEqual(3, recorder.StreamingCallCount, "真实模型只应承担原始两轮和目标重新生成，历史回放由专用回放模型承担。");
        Assert.AreEqual(0, recorder.NonStreamingCallCount, "重新开始不应调用非流式接口。");

        var userMessages = SlideStreamingTestHelper.GetNormalUserMessages(chatManager)
            .Select(message => message.Content)
            .ToList();
        Assert.AreEqual(2, userMessages.Count, "原会话应截断后只保留目标前一轮和重新追加的目标用户消息。");
        StringAssert.Contains(userMessages[0], "生成首页", "第一轮用户消息应保留在原会话中。");
        Assert.AreEqual("加一个卡片", userMessages[1], "目标用户消息应被重新追加到原会话。");

        StringAssert.Contains(chatManager.CurrentSlideXml, "r1", "回放后应保留目标消息前的 SlideML 状态。");
        StringAssert.Contains(chatManager.CurrentSlideXml, "r2-new", "目标消息应使用恢复真实模型后的新响应重新生成。");
        Assert.IsFalse(chatManager.CurrentSlideXml.Contains("r2-old", StringComparison.Ordinal), "目标消息之后的旧 SlideML 不应残留。");
    }

    [TestMethod(DisplayName = "从第三条用户消息重新开始时回放前两轮并重新生成第三轮")]
    public async Task RestartFromThirdUserMessage_ReplaysFirstTwoTurnsAndRegeneratesThirdTurn()
    {
        var (chatManager, _, recorder) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTextsAndRecorder(
            CreateRectPageXml("r1"),
            CreateRectPageXml("r2"),
            CreateRectPageXml("r3-old"),
            CreateRectPageXml("r3-new"));

        await SendStreamingMessageAsync(chatManager, "生成首页", isFirstMessage: true).ConfigureAwait(false);
        await SendStreamingMessageAsync(chatManager, "加一个卡片", isFirstMessage: false).ConfigureAwait(false);
        await SendStreamingMessageAsync(chatManager, "再加一个图形", isFirstMessage: false).ConfigureAwait(false);

        CopilotChatMessage targetMessage = GetUserMessage(chatManager, 2);
        var originalSession = chatManager.Pipeline.ChatManager.SelectedSession;
        var originalPrimaryModel = chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel;

        await chatManager.RestartFromMessageAsync(targetMessage).ConfigureAwait(false);

        Assert.AreSame(originalSession, chatManager.Pipeline.ChatManager.SelectedSession, "重新开始结束后应恢复原会话。");
        Assert.AreSame(originalPrimaryModel, chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel, "重新开始结束后应恢复原模型。");
        Assert.AreEqual(4, recorder.StreamingCallCount, "真实模型应承担三轮原始生成和目标重新生成。");
        StringAssert.Contains(chatManager.CurrentSlideXml, "r1", "第一轮历史应被回放到流式状态中。");
        StringAssert.Contains(chatManager.CurrentSlideXml, "r2", "第二轮历史应被回放到流式状态中。");
        StringAssert.Contains(chatManager.CurrentSlideXml, "r3-new", "第三轮目标消息应重新生成。");
        Assert.IsFalse(chatManager.CurrentSlideXml.Contains("r3-old", StringComparison.Ordinal), "第三轮旧结果不应残留。");
    }

    [TestMethod(DisplayName = "重新开始存在回放历史的消息后恢复原模型和原会话")]
    public async Task RestartFromMessage_WithReplay_RestoresPrimaryModelAndSelectedSession()
    {
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(
            CreateRectPageXml("first"),
            CreateRectPageXml("old-second"),
            CreateRectPageXml("new-second"));

        await SendStreamingMessageAsync(chatManager, "第一轮", isFirstMessage: true).ConfigureAwait(false);
        await SendStreamingMessageAsync(chatManager, "第二轮", isFirstMessage: false).ConfigureAwait(false);

        CopilotChatMessage targetMessage = GetUserMessage(chatManager, 1);
        var originalSession = chatManager.Pipeline.ChatManager.SelectedSession;
        var originalPrimaryModel = chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel;
        int originalSessionCount = chatManager.Pipeline.ChatManager.ChatSessions.Count;
        int originalModelCount = chatManager.Pipeline.ChatManager.AgentApiEndpointManager.GetSupportedModels().Count;

        await chatManager.RestartFromMessageAsync(targetMessage).ConfigureAwait(false);

        Assert.AreSame(originalPrimaryModel, chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel, "回放完成后应恢复真实模型。");
        Assert.AreSame(originalSession, chatManager.Pipeline.ChatManager.SelectedSession, "回放完成后应恢复原会话。");
        Assert.AreEqual(originalSessionCount, chatManager.Pipeline.ChatManager.ChatSessions.Count, "回放完成后不应遗留临时回放会话。");
        Assert.AreEqual(originalModelCount, chatManager.Pipeline.ChatManager.AgentApiEndpointManager.GetSupportedModels().Count, "回放完成后不应遗留临时回放模型。");
    }

    [TestMethod(DisplayName = "重新生成目标消息失败时保持原模型和原会话已恢复")]
    public async Task RestartFromMessage_WhenTargetRegenerationFails_KeepsPrimaryModelAndSelectedSessionRestored()
    {
        var callIndex = 0;
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager((_, ct) =>
        {
            callIndex++;
            if (callIndex == 3)
            {
                return ThrowDuringStreamingAsync(ct);
            }

            string id = callIndex == 1 ? "first" : "old-second";
            return SlideStreamingTestHelper.StreamTokensAsync(CreateRectPageXml(id), cancellationToken: ct);
        }, recorder: null);

        await SendStreamingMessageAsync(chatManager, "第一轮", isFirstMessage: true).ConfigureAwait(false);
        await SendStreamingMessageAsync(chatManager, "第二轮", isFirstMessage: false).ConfigureAwait(false);

        CopilotChatMessage targetMessage = GetUserMessage(chatManager, 1);
        var originalSession = chatManager.Pipeline.ChatManager.SelectedSession;
        var originalPrimaryModel = chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel;

        await AssertThrowsAsync<InvalidOperationException>(() => chatManager.RestartFromMessageAsync(targetMessage)).ConfigureAwait(false);

        Assert.AreSame(originalPrimaryModel, chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel, "目标重新生成失败后应保持真实模型。");
        Assert.AreSame(originalSession, chatManager.Pipeline.ChatManager.SelectedSession, "目标重新生成失败后应保持原会话。");
    }

    [TestMethod(DisplayName = "重新开始被取消时恢复原模型和原会话")]
    public async Task RestartFromMessage_WhenCancellationIsRequested_RestoresPrimaryModelAndSelectedSession()
    {
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(
            CreateRectPageXml("first"),
            CreateRectPageXml("old-second"));

        await SendStreamingMessageAsync(chatManager, "第一轮", isFirstMessage: true).ConfigureAwait(false);
        await SendStreamingMessageAsync(chatManager, "第二轮", isFirstMessage: false).ConfigureAwait(false);

        CopilotChatMessage targetMessage = GetUserMessage(chatManager, 1);
        var originalSession = chatManager.Pipeline.ChatManager.SelectedSession;
        var originalPrimaryModel = chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel;
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync().ConfigureAwait(false);

        await AssertThrowsAsync<OperationCanceledException>(
            () => chatManager.RestartFromMessageAsync(targetMessage, cancellationTokenSource.Token)).ConfigureAwait(false);

        Assert.AreSame(originalPrimaryModel, chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel, "取消后应恢复原模型。");
        Assert.AreSame(originalSession, chatManager.Pipeline.ChatManager.SelectedSession, "取消后应恢复原会话。");
    }

    [TestMethod(DisplayName = "从助手消息重新开始时抛出参数异常")]
    public async Task RestartFromMessage_WithAssistantMessage_ThrowsArgumentException()
    {
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(CreateRectPageXml("first"));
        await SendStreamingMessageAsync(chatManager, "第一轮", isFirstMessage: true).ConfigureAwait(false);
        CopilotChatMessage assistantMessage = SlideStreamingTestHelper.GetNormalAssistantMessages(chatManager).Single();

        await AssertThrowsAsync<ArgumentException>(() => chatManager.RestartFromMessageAsync(assistantMessage)).ConfigureAwait(false);
    }

    [TestMethod(DisplayName = "从预设消息重新开始时抛出参数异常")]
    public async Task RestartFromMessage_WithPresetMessage_ThrowsArgumentException()
    {
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(CreateRectPageXml("first"));
        var presetMessage = CopilotChatMessage.CreateUser("预设消息");
        presetMessage.IsPresetInfo = true;
        await chatManager.Pipeline.ChatManager.AppendMessageAsync(presetMessage).ConfigureAwait(false);

        await AssertThrowsAsync<ArgumentException>(() => chatManager.RestartFromMessageAsync(presetMessage)).ConfigureAwait(false);
    }

    [TestMethod(DisplayName = "目标消息不在当前会话中时抛出无效操作异常")]
    public async Task RestartFromMessage_WithMessageFromAnotherSession_ThrowsInvalidOperationException()
    {
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(CreateRectPageXml("first"));
        await SendStreamingMessageAsync(chatManager, "第一轮", isFirstMessage: true).ConfigureAwait(false);
        CopilotChatMessage targetMessage = GetUserMessage(chatManager, 0);
        chatManager.Pipeline.ChatManager.CreateNewSession();

        await AssertThrowsAsync<InvalidOperationException>(() => chatManager.RestartFromMessageAsync(targetMessage)).ConfigureAwait(false);
    }

    [TestMethod(DisplayName = "构建回放历史时忽略预设消息")]
    public async Task RestartFromMessage_IgnoresPresetMessagesWhenBuildingReplayTurns()
    {
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(
            CreateRectPageXml("first"),
            CreateRectPageXml("old-second"),
            CreateRectPageXml("new-second"));

        await SendStreamingMessageAsync(chatManager, "第一轮", isFirstMessage: true).ConfigureAwait(false);

        var presetMessage = CopilotChatMessage.CreateUser("评估信息");
        presetMessage.IsPresetInfo = true;
        await chatManager.Pipeline.ChatManager.AppendMessageAsync(presetMessage).ConfigureAwait(false);
        await SendStreamingMessageAsync(chatManager, "第二轮", isFirstMessage: false).ConfigureAwait(false);

        CopilotChatMessage targetMessage = GetUserMessage(chatManager, 1);

        await chatManager.RestartFromMessageAsync(targetMessage).ConfigureAwait(false);

        StringAssert.Contains(chatManager.CurrentSlideXml, "first", "预设消息不应阻断第一轮历史回放。");
        StringAssert.Contains(chatManager.CurrentSlideXml, "new-second", "目标消息应重新生成。");
        Assert.IsFalse(chatManager.CurrentSlideXml.Contains("old-second", StringComparison.Ordinal), "目标旧结果不应残留。");
    }

    [TestMethod(DisplayName = "重新开始时历史回放不调用真实模型且目标重新生成使用流式 API")]
    public async Task RestartFromMessage_ReplaysHistoryWithTemporaryFakeModelAndRegeneratesTargetWithStreamingApi()
    {
        var (chatManager, _, recorder) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTextsAndRecorder(
            CreateRectPageXml("first"),
            CreateRectPageXml("old-second"),
            CreateRectPageXml("new-second"));

        await SendStreamingMessageAsync(chatManager, "第一轮", isFirstMessage: true).ConfigureAwait(false);
        await SendStreamingMessageAsync(chatManager, "第二轮", isFirstMessage: false).ConfigureAwait(false);

        await chatManager.RestartFromMessageAsync(GetUserMessage(chatManager, 1)).ConfigureAwait(false);

        Assert.AreEqual(3, recorder.StreamingCallCount, "真实模型应只承担原始两轮生成和目标重新生成，历史回放应由临时 Fake 模型承担。");
        Assert.AreEqual(0, recorder.NonStreamingCallCount, "重新开始链路不应调用非流式接口。");
        Assert.IsTrue(recorder.StreamingMessages.All(messages => messages.Count > 0), "每次流式调用都应携带输入消息。");
    }

    private static Task SendStreamingMessageAsync(SlideChatManager chatManager, string userMessage, bool isFirstMessage)
    {
        return chatManager.SendMessageAsync(
            userMessage,
            isFirstMessage,
            attachPreview: false,
            useStreaming: true);
    }

    private static CopilotChatMessage GetUserMessage(SlideChatManager chatManager, int index)
    {
        return SlideStreamingTestHelper.GetNormalUserMessages(chatManager)[index];
    }

    private static string CreateRectPageXml(string id)
    {
        return $"""<Page><Rect Id="{id}" Width="100" Height="50" Fill="#FF0000"/></Page>""";
    }

    private static async Task AssertThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (TException)
        {
            return;
        }

        Assert.Fail($"应抛出 {typeof(TException).Name}。");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowDuringStreamingAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        throw new InvalidOperationException("测试流式生成失败。");
        #pragma warning disable CS0162
        yield return new ChatResponseUpdate(ChatRole.Assistant, string.Empty);
        #pragma warning restore CS0162
    }
}
