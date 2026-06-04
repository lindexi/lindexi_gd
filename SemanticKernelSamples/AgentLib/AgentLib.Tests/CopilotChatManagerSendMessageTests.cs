using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using AgentLib.Tests.Fakes;
using AgentLib.Tools;

using Microsoft.Extensions.AI;

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AgentLib.Tests;

[TestClass]
public class CopilotChatManagerSendMessageTests
{
    [TestMethod]
    [Description("SendMessage 使用纯文本内容时应创建用户消息和助手消息")]
    public async Task SendMessage_WhenTextContentProvided_CreatesUserAndAssistantMessages()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("你好，有什么可以帮你的？"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("你好"));

        Assert.IsNotNull(result.UserChatMessage);
        Assert.IsNotNull(result.AssistantChatMessage);
        Assert.AreEqual(ChatRole.User, result.UserChatMessage.Role);
        Assert.AreEqual(ChatRole.Assistant, result.AssistantChatMessage.Role);
        Assert.AreEqual("你好", result.UserChatMessage.Content);

        await result.RunTask;

        Assert.IsFalse(context.ChatManager.IsChatting);
        Assert.IsTrue(context.ChatManager.ChatMessages.Count >= 2);
    }

    [TestMethod]
    [Description("SendMessage 使用 SystemPrompt 时应注入系统提示词")]
    public async Task SendMessage_WhenSystemPromptProvided_InjectsSystemPrompt()
    {
        var primaryChatClient = new FakeChatClient();
        string? capturedSystemPrompt = null;
        primaryChatClient.OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
        {
            ChatMessage? systemMessage = messages.FirstOrDefault(m => m.Role == ChatRole.System);
            capturedSystemPrompt = systemMessage?.Text;
            return CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("已收到系统提示"));
        };
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest([new TextContent("你好")]) { SystemPrompt = "你是一个友好的助手" });

        await result.RunTask;

        Assert.AreEqual("你是一个友好的助手", capturedSystemPrompt);
    }

    [TestMethod]
    [Description("SendMessage 使用 CreateNewSession=true 时应创建新会话")]
    public async Task SendMessage_WhenCreateNewSessionIsTrue_CreatesNewSession()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        // 先发一条消息让当前会话不再可复用
        await context.ChatManager.SendMessageAsync("占位消息");
        Guid originalSessionId = context.ChatManager.CurrentSessionId;

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest([new TextContent("你好")]) { CreateNewSession = true });

        await result.RunTask;

        Assert.AreNotEqual(originalSessionId, context.ChatManager.CurrentSessionId);
    }

    [TestMethod]
    [Description("SendMessage 使用 WithHistory=true 时应创建并存储 AgentSession")]
    public async Task SendMessage_WhenWithHistoryIsTrue_CreatesAgentSession()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest([new TextContent("你好")]) { WithHistory = true });

        await result.RunTask;

        Assert.IsNotNull(context.ChatManager.SelectedSession.AgentSession);
    }

    [TestMethod]
    [Description("SendMessage 使用 WithHistory=false 时不应存储 AgentSession")]
    public async Task SendMessage_WhenWithHistoryIsFalse_DoesNotStoreAgentSession()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest([new TextContent("你好")]) { WithHistory = false });

        await result.RunTask;

        Assert.IsNull(context.ChatManager.SelectedSession.AgentSession);
    }

    [TestMethod]
    [Description("SendMessage 传入空 Contents 时应抛出 ArgumentException")]
    public void SendMessage_WhenContentsIsEmpty_ThrowsArgumentException()
    {
        var context = CopilotChatManagerTestContext.Create(new FakeChatClient());

        try
        {
            context.ChatManager.SendMessage(new SendMessageRequest([]));
            Assert.Fail("应抛出 ArgumentException");
        }
        catch (ArgumentException exception)
        {
            StringAssert.Contains(exception.Message, "Contents");
        }
    }

    [TestMethod]
    [Description("SendMessage 传入 null Contents 时应抛出 ArgumentNullException")]
    public void SendMessage_WhenContentsIsNull_ThrowsArgumentNullException()
    {
        var context = CopilotChatManagerTestContext.Create(new FakeChatClient());

        try
        {
            context.ChatManager.SendMessage(new SendMessageRequest((IReadOnlyList<AIContent>) null!));
            Assert.Fail("应抛出 ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // 预期行为
        }
    }

    [TestMethod]
    [Description("SendMessage 使用多模态 DataContent 时应创建对应的消息项")]
    public async Task SendMessage_WhenDataContentProvided_CreatesDataMessageItems()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("我看到了图片"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        byte[] imageBytes = [0x89, 0x50, 0x4E, 0x47]; // PNG header
        var contents = new List<AIContent>
        {
            new TextContent("请分析这张图片"),
            new DataContent(imageBytes, "image/png")
        };

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest(contents));

        Assert.AreEqual(2, result.UserChatMessage.MessageItems.Count);
        Assert.IsInstanceOfType<CopilotChatTextItem>(result.UserChatMessage.MessageItems[0]);
        Assert.IsInstanceOfType<CopilotChatImageItem>(result.UserChatMessage.MessageItems[1]);

        var imageItem = (CopilotChatImageItem) result.UserChatMessage.MessageItems[1];
        Assert.AreEqual("image/png", imageItem.MimeType);
        Assert.IsNotNull(imageItem.Data);

        await result.RunTask;
    }

    [TestMethod]
    [Description("SendMessage 使用 DataContent 但 mediaType 为 audio 时应创建 CopilotChatAudioItem")]
    public async Task SendMessage_WhenAudioDataContentProvided_CreatesAudioMessageItem()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("我听到了音频"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        byte[] audioBytes = Encoding.UTF8.GetBytes("fake-audio-data");
        var contents = new List<AIContent>
        {
            new TextContent("请分析这段音频"),
            new DataContent(audioBytes, "audio/wav")
        };

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest(contents));

        Assert.IsInstanceOfType<CopilotChatAudioItem>(result.UserChatMessage.MessageItems[1]);
        var audioItem = (CopilotChatAudioItem) result.UserChatMessage.MessageItems[1];
        Assert.AreEqual("audio/wav", audioItem.MimeType);

        await result.RunTask;
    }

    [TestMethod]
    [Description("SendMessageAsync(string) 便捷重载应正确发送纯文本消息")]
    public async Task SendMessageAsync_WhenStringOverloadCalled_SendsTextMessage()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("收到"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        await context.ChatManager.SendMessageAsync("纯文本消息");

        Assert.IsFalse(context.ChatManager.IsChatting);
        Assert.AreEqual("纯文本消息", context.ChatManager.ChatMessages[^2].Content);
    }

    [TestMethod]
    [Description("SendMessageInNewSessionAsync(string) 便捷重载应创建新会话并发送消息")]
    public async Task SendMessageInNewSessionAsync_WhenStringOverloadCalled_CreatesNewSessionAndSends()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("新会话回复"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        // 先发一条消息让当前会话不再可复用
        await context.ChatManager.SendMessageAsync("占位消息");
        Guid originalSessionId = context.ChatManager.CurrentSessionId;

        await context.ChatManager.SendMessageInNewSessionAsync("新会话消息");

        Assert.AreNotEqual(originalSessionId, context.ChatManager.CurrentSessionId);
        Assert.AreEqual("新会话消息", context.ChatManager.ChatMessages[^2].Content);
    }

    [TestMethod]
    [Description("SendMessage 返回的 SendMessageResult 应包含正确的 ToolList")]
    public async Task SendMessage_WhenNoToolsProvided_ToolListContainsDefaultTools()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("你好"));

        Assert.IsNotNull(result.ToolList);
        Assert.IsTrue(result.ToolList.Count > 0, "应包含默认工具");

        await result.RunTask;
    }

    [TestMethod]
    [Description("SendMessage 完成后 IsChatting 应为 false")]
    public async Task SendMessage_WhenCompleted_IsChattingIsFalse()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("完成"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("测试"));

        Assert.IsTrue(context.ChatManager.IsChatting);

        await result.RunTask;

        Assert.IsFalse(context.ChatManager.IsChatting);
    }

    [TestMethod]
    [Description("SendMessage 使用字符串构造函数重载应正确包装文本内容")]
    public async Task SendMessage_WhenStringConstructorUsed_WrapsTextCorrectly()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("工厂方法测试") { WithHistory = false, CreateNewSession = true });

        Assert.AreEqual("工厂方法测试", result.UserChatMessage.Content);

        await result.RunTask;

        Assert.IsNull(context.ChatManager.SelectedSession.AgentSession);
    }

    [TestMethod]
    [Description("工具抛出异常时，应优雅处理：将错误信息展示给用户，恢复聊天状态，不传播异常")]
    public async Task SendMessage_WhenToolThrowsException_HandlesGracefully()
    {
        var primaryChatClient = new FakeChatClient();
        var throwingTool = new ThrowingTool(new InvalidOperationException("工具执行失败"));
        string toolName = "ThrowingTool";
        primaryChatClient.OnGetStreamingResponseAsync = (_, options, cancellationToken) =>
            CreateToolInvocationAsyncEnumerable(options, "tool-call-1", toolName, null, cancellationToken,
                CopilotChatManagerTestContext.AssistantText("不会到达的结果"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        // 不应抛出异常
        await context.ChatManager.SendMessageAsync(
            contents: [new TextContent("触发工具异常")],
            withHistory: true,
            tools: [throwingTool.CreateTool(toolName, "抛出异常的工具")],
            toolMode: ChatToolMode.RequireAny);

        Assert.IsFalse(context.ChatManager.IsChatting, "聊天状态应恢复");
        CopilotChatMessage lastMessage = context.ChatManager.ChatMessages[^1];
        Assert.IsTrue(lastMessage.Content.Contains("工具执行失败", StringComparison.Ordinal)
                      || lastMessage.Content.Contains("InvalidOperationException", StringComparison.Ordinal),
            $"最后一条消息应包含错误信息，实际内容: {lastMessage.Content}");
        Assert.IsTrue(lastMessage.IsPresetInfo, "错误消息应标记为预设信息");
    }

    [TestMethod]
    [Description("工具抛出异常时，SendMessageResult.RunTask 应返回 IsSuccess=false")]
    public async Task SendMessage_WhenToolThrowsException_RunTaskReturnsFailure()
    {
        var primaryChatClient = new FakeChatClient();
        var throwingTool = new ThrowingTool(new InvalidOperationException("工具执行失败"));
        string toolName = "ThrowingTool";
        primaryChatClient.OnGetStreamingResponseAsync = (_, options, cancellationToken) =>
            CreateToolInvocationAsyncEnumerable(options, "tool-call-1", toolName, null, cancellationToken,
                CopilotChatManagerTestContext.AssistantText("不会到达的结果"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest([new TextContent("触发工具异常")])
            {
                WithHistory = true,
                Tools = [throwingTool.CreateTool(toolName, "抛出异常的工具")],
                ToolMode = ChatToolMode.RequireAny,
            });

        SendMessageRunState runState = await result.RunTask;

        Assert.IsFalse(runState.IsSuccess, "应返回失败状态");
        Assert.IsFalse(runState.WasCanceled, "不应标记为取消");
        Assert.IsFalse(context.ChatManager.IsChatting, "聊天状态应恢复");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateStreamingUpdatesAsync(
        CancellationToken cancellationToken,
        params ChatResponseUpdate[] updates)
    {
        foreach (ChatResponseUpdate update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolInvocationAsyncEnumerable(
        ChatOptions? options,
        string callId,
        string toolName,
        IDictionary<string, object?>? arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        params ChatResponseUpdate[] trailingUpdates)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantFunctionCall(callId, toolName, arguments);

        AITool tool = options?.Tools?.FirstOrDefault(candidate => string.Equals(candidate.Name, toolName, StringComparison.Ordinal))
                      ?? throw new InvalidOperationException($"未找到名为 {toolName} 的工具。");

        if (tool is not AIFunction function)
        {
            throw new InvalidOperationException($"工具 {toolName} 不是可调用函数。");
        }

        object? result = await function.InvokeAsync(new AIFunctionArguments(arguments?.ToDictionary(pair => pair.Key, pair => pair.Value)), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantFunctionResult(callId, NormalizeResult(result));

        foreach (ChatResponseUpdate update in trailingUpdates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    private static object? NormalizeResult(object? result)
    {
        if (result is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                _ => jsonElement.ToString()
            };
        }

        return result;
    }
}
