using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// 流式生成重新开始回放功能测试。
/// </summary>
[TestClass]
public sealed class SlideStreamingRestartTests
{
    [TestMethod(DisplayName = "从第二条用户消息重新开始时回放第一轮并重新生成第二轮")]
    public async Task RestartFromSecondUserMessage_ReplaysPreviousTurnAndRegeneratesTargetTurn()
    {
        // Arrange
        const string firstTurnXml = """<Page><Rect Id="r1" Width="100" Height="50" Fill="#FF0000"/></Page>""";
        const string oldSecondTurnXml = """<Page><Rect Id="r2-old" Width="100" Height="50" Fill="#00FF00"/></Page>""";
        const string newSecondTurnXml = """<Page><Rect Id="r2-new" Width="100" Height="50" Fill="#0000FF"/></Page>""";

        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(
            firstTurnXml,
            oldSecondTurnXml,
            newSecondTurnXml);

        await chatManager.SendMessageAsync(
            "生成首页",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);
        await chatManager.SendMessageAsync(
            "加一个卡片",
            isFirstMessage: false,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        CopilotChatMessage targetMessage = chatManager.Pipeline.ChatManager.ChatMessages
            .Where(message => message.Role == ChatRole.User && !message.IsPresetInfo)
            .ElementAt(1);
        var originalSession = chatManager.Pipeline.ChatManager.SelectedSession;
        var originalPrimaryModel = chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel;

        // Act
        await chatManager.RestartFromMessageAsync(targetMessage).ConfigureAwait(false);

        // Assert
        Assert.AreSame(originalSession, chatManager.Pipeline.ChatManager.SelectedSession, "重新开始结束后应恢复原会话。");
        Assert.AreSame(originalPrimaryModel, chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel, "重新开始结束后应恢复原模型。");

        var userMessages = chatManager.Pipeline.ChatManager.ChatMessages
            .Where(message => message.Role == ChatRole.User && !message.IsPresetInfo)
            .Select(message => message.Content)
            .ToList();
        Assert.AreEqual(2, userMessages.Count, "原会话应截断后只保留目标前一轮和重新追加的目标用户消息。");
        StringAssert.Contains(userMessages[0], "生成首页", "第一轮用户消息应保留在原会话中。");
        Assert.AreEqual("加一个卡片", userMessages[1], "目标用户消息应被重新追加到原会话。");

        StringAssert.Contains(chatManager.CurrentSlideXml, "r1", "回放后应保留目标消息前的 SlideML 状态。");
        StringAssert.Contains(chatManager.CurrentSlideXml, "r2-new", "目标消息应使用恢复真实模型后的新响应重新生成。");
        Assert.IsFalse(chatManager.CurrentSlideXml.Contains("r2-old", StringComparison.Ordinal), "目标消息之后的旧 SlideML 不应残留。");
    }

    [TestMethod(DisplayName = "从第一条用户消息重新开始时不回放历史并重新生成")]
    public async Task RestartFromFirstUserMessage_RegeneratesFromEmptyStreamingState()
    {
        // Arrange
        const string oldFirstTurnXml = """<Page><Rect Id="old" Width="100" Height="50" Fill="#FF0000"/></Page>""";
        const string newFirstTurnXml = """<Page><Rect Id="new" Width="100" Height="50" Fill="#00FF00"/></Page>""";

        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(
            oldFirstTurnXml,
            newFirstTurnXml);

        await chatManager.SendMessageAsync(
            "生成首页",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        CopilotChatMessage targetMessage = chatManager.Pipeline.ChatManager.ChatMessages
            .Single(message => message.Role == ChatRole.User && !message.IsPresetInfo);
        var originalSession = chatManager.Pipeline.ChatManager.SelectedSession;
        var originalPrimaryModel = chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel;

        // Act
        await chatManager.RestartFromMessageAsync(targetMessage).ConfigureAwait(false);

        // Assert
        Assert.AreSame(originalSession, chatManager.Pipeline.ChatManager.SelectedSession, "重新开始第一条消息后应仍回到原会话。");
        Assert.AreSame(originalPrimaryModel, chatManager.Pipeline.ChatManager.AgentApiEndpointManager.PrimaryModel, "重新开始第一条消息后应恢复原模型。");
        StringAssert.Contains(chatManager.CurrentSlideXml, "new", "第一条消息应从空状态重新生成新结果。");
        Assert.IsFalse(chatManager.CurrentSlideXml.Contains("old", StringComparison.Ordinal), "第一条消息旧结果不应残留。");
    }
}
