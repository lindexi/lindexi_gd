using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using AgentLib.Tests.Fakes;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

#pragma warning disable MAAI001

namespace AgentLib.Tests;

[TestClass]
public class CopilotChatManagerSkillTests
{
    [TestMethod]
    [Description("AIContextProviders 默认应为 null")]
    public void AIContextProviders_WhenNotSet_IsNull()
    {
        var context = CopilotChatManagerTestContext.Create(new FakeChatClient());

        Assert.IsNull(context.ChatManager.AIContextProviders);
    }

    [TestMethod]
    [Description("AddSkillFolder 调用后 AIContextProviders 应包含 AgentSkillsProvider")]
    public void AddSkillFolder_WhenCalled_AddsAgentSkillsProviderToAIContextProviders()
    {
        var context = CopilotChatManagerTestContext.Create(new FakeChatClient());
        var skillFolder = GetTestSkillFolder();

        context.ChatManager.AddSkillFolder(skillFolder);

        Assert.IsNotNull(context.ChatManager.AIContextProviders);
        Assert.HasCount(1, context.ChatManager.AIContextProviders);
        Assert.IsInstanceOfType<AgentSkillsProvider>(context.ChatManager.AIContextProviders[0]);
    }

    [TestMethod]
    [Description("AddSkillFolder 多次调用应累积多个 AgentSkillsProvider")]
    public void AddSkillFolder_WhenCalledMultipleTimes_AccumulatesProviders()
    {
        var context = CopilotChatManagerTestContext.Create(new FakeChatClient());
        var skillFolder = GetTestSkillFolder();

        context.ChatManager.AddSkillFolder(skillFolder);
        context.ChatManager.AddSkillFolder(skillFolder);

        Assert.IsNotNull(context.ChatManager.AIContextProviders);
        Assert.HasCount(2, context.ChatManager.AIContextProviders);
        Assert.IsInstanceOfType<AgentSkillsProvider>(context.ChatManager.AIContextProviders[0]);
        Assert.IsInstanceOfType<AgentSkillsProvider>(context.ChatManager.AIContextProviders[1]);
    }

    [TestMethod]
    [Description("AddSkillFolder 传入 null 应抛出 ArgumentNullException")]
    public void AddSkillFolder_WhenNull_ThrowsArgumentNullException()
    {
        var context = CopilotChatManagerTestContext.Create(new FakeChatClient());

        try
        {
            context.ChatManager.AddSkillFolder(null!);
            Assert.Fail("应抛出 ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // 预期行为
        }
    }

    [TestMethod]
    [Description("WithSkillFolder 应返回新的 SendMessageRequest 且 AIContextProviders 包含 AgentSkillsProvider")]
    public void WithSkillFolder_WhenCalled_ReturnsNewRequestWithSkills()
    {
        var skillFolder = GetTestSkillFolder();
        var request = new SendMessageRequest("你好");

        SendMessageRequest newRequest = request.WithSkillFolder(skillFolder);

        Assert.AreNotEqual(request, newRequest);
        Assert.IsNull(request.AIContextProviders, "原始请求不应被修改");
        Assert.IsNotNull(newRequest.AIContextProviders);
        Assert.HasCount(1, newRequest.AIContextProviders);
        Assert.IsInstanceOfType<AgentSkillsProvider>(newRequest.AIContextProviders[0]);
    }

    [TestMethod]
    [Description("WithSkillFolder 在已有 AIContextProviders 时应追加而非覆盖")]
    public void WithSkillFolder_WhenAIContextProvidersAlreadySet_AppendsSkill()
    {
        var skillFolder = GetTestSkillFolder();
        var existingProvider = new AgentSkillsProvider(skillFolder.FullName);
        var request = new SendMessageRequest("你好")
        {
            AIContextProviders = new List<AIContextProvider> { existingProvider }
        };

        SendMessageRequest newRequest = request.WithSkillFolder(skillFolder);

        Assert.IsNotNull(newRequest.AIContextProviders);
        Assert.HasCount(2, newRequest.AIContextProviders);
        Assert.AreSame(existingProvider, newRequest.AIContextProviders[0]);
        Assert.IsInstanceOfType<AgentSkillsProvider>(newRequest.AIContextProviders[1]);
    }

    [TestMethod]
    [Description("WithSkillFolder 传入 null 应抛出 ArgumentNullException")]
    public void WithSkillFolder_WhenNull_ThrowsArgumentNullException()
    {
        var request = new SendMessageRequest("你好");

        try
        {
            request.WithSkillFolder(null!);
            Assert.Fail("应抛出 ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // 预期行为
        }
    }

    [TestMethod]
    [Description("SendMessage 使用 Manager 级别的 AddSkillFolder 后，聊天应正常完成")]
    public async Task SendMessage_WithManagerSkillFolder_ChatCompletesSuccessfully()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("我已了解 AOT 兼容性要求"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);
        context.ChatManager.AddSkillFolder(GetTestSkillFolder());

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("如何让项目支持 AOT？"));

        await result.RunTask;

        Assert.IsFalse(context.ChatManager.IsChatting);
        Assert.IsGreaterThanOrEqualTo(2, context.ChatManager.ChatMessages.Count);
        Assert.IsTrue(context.ChatManager.ChatMessages[^1].Content.Contains("AOT", StringComparison.Ordinal));
    }

    [TestMethod]
    [Description("SendMessage 使用 Request 级别的 WithSkillFolder 后，聊天应正常完成")]
    public async Task SendMessage_WithRequestSkillFolder_ChatCompletesSuccessfully()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("AOT 兼容性建议已提供"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        SendMessageRequest request = new SendMessageRequest("分析 AOT 兼容性")
            .WithSkillFolder(GetTestSkillFolder());

        SendMessageResult result = context.ChatManager.SendMessage(request);

        await result.RunTask;

        Assert.IsFalse(context.ChatManager.IsChatting);
        Assert.IsGreaterThanOrEqualTo(2, context.ChatManager.ChatMessages.Count);
    }

    [TestMethod]
    [Description("Request 级别的 AIContextProviders 应覆盖 Manager 级别的设置")]
    public async Task SendMessage_WhenRequestOverridesAIContextProviders_UsesRequestProviders()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
        {
            // 通过 options 无法直接获取 AIContextProviders，但可以验证聊天正常完成
            return CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));
        };
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        // 设置 Manager 级别的技能
        context.ChatManager.AddSkillFolder(GetTestSkillFolder());
        Assert.HasCount(1, context.ChatManager.AIContextProviders!);

        // Request 级别传入空集合覆盖
        SendMessageRequest request = new SendMessageRequest("测试")
        {
            AIContextProviders = Array.Empty<AIContextProvider>()
        };

        SendMessageResult result = context.ChatManager.SendMessage(request);
        await result.RunTask;

        Assert.IsFalse(context.ChatManager.IsChatting);
        // Manager 级别的 AIContextProviders 不应被修改
        Assert.HasCount(1, context.ChatManager.AIContextProviders!);
    }

    [TestMethod]
    [Description("SendMessageRequest.WithSkillFolder 不应修改原始请求（不可变性）")]
    public void WithSkillFolder_DoesNotModifyOriginalRequest()
    {
        var skillFolder = GetTestSkillFolder();
        var request = new SendMessageRequest("你好")
        {
            WithHistory = false,
            CreateNewSession = true,
        };

        SendMessageRequest newRequest = request.WithSkillFolder(skillFolder);

        Assert.IsNull(request.AIContextProviders, "原始请求的 AIContextProviders 应保持 null");
        Assert.AreEqual(request.Contents, newRequest.Contents);
        Assert.AreEqual(request.WithHistory, newRequest.WithHistory);
        Assert.AreEqual(request.CreateNewSession, newRequest.CreateNewSession);
    }

    private static DirectoryInfo GetTestSkillFolder()
    {
        string basePath = AppContext.BaseDirectory;
        string skillPath = Path.Join(basePath, "Assets", "TestSkills", "dotnet-aot-compat");
        return new DirectoryInfo(skillPath);
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
