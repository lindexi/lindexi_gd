using AgentLib;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class SlideChatManagerFactoryTests
{
    [TestMethod(DisplayName = "页面运行时工厂应让 Prompt 和 fallback 使用实际页面画布")]
    [Timeout(60_000)]
    public async Task CreateAsyncShouldUsePageDocumentContextForConfiguredAndFallbackManagers()
    {
        var chatManagerFactory = new RecordingCopilotChatManagerFactory();
        var factory = new SlideChatManagerFactory(chatManagerFactory);
        var options = new SlideChatManagerFactoryOptions(new SlideDocumentContext(1920, 1080))
        {
            TryEnableDefaultMcp = false,
        };

        var configuredManager = await factory.CreateAsync(options);
        var fallbackManager = factory.CreateFallback(options);

        StringAssert.Contains(configuredManager.Pipeline.PromptProvider.BuildStreamingSystemPrompt(), "1920");
        StringAssert.Contains(configuredManager.Pipeline.PromptProvider.BuildStreamingSystemPrompt(), "1080");
        StringAssert.Contains(fallbackManager.Pipeline.PromptProvider.BuildInitialUserPrompt("测试"), "1920x1080");
        Assert.AreEqual(2, chatManagerFactory.CreateCount);
    }

    [TestMethod(DisplayName = "页面运行时工厂应在预取消时停止模型初始化")]
    [Timeout(60_000)]
    public async Task CreateAsyncShouldHonorPreCanceledToken()
    {
        var chatManagerFactory = new RecordingCopilotChatManagerFactory();
        var factory = new SlideChatManagerFactory(chatManagerFactory);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => factory.CreateAsync(
            new SlideChatManagerFactoryOptions(new SlideDocumentContext(1280, 720)),
            cancellationTokenSource.Token));

        Assert.AreEqual(0, chatManagerFactory.CreateCount);
    }

    private sealed class RecordingCopilotChatManagerFactory : ICopilotChatManagerFactory
    {
        public int CreateCount { get; private set; }

        public Task<CopilotChatManager> CreateAsync(
            AgentWorkload workload,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateCount++;

            var fakeChatClient = new FakeChatClient
            {
                OnGetResponseAsync = (_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "<Page/>"))),
            };
            var chatManager = new CopilotChatManager();
            chatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider(fakeChatClient));
            return Task.FromResult(chatManager);
        }
    }
}
