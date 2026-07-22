using AgentLib;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using Microsoft.Extensions.AI;
using CoursewarePptxGeneratorWpfDemo.Services;
using PptxGenerator;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class FakeSlideChatManagerFactory : ISlideChatManagerFactory
{
    public Task<SlideChatManager> CreateAsync(
        SlideChatManagerFactoryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateFallback(options));
    }

    public SlideChatManager CreateFallback(SlideChatManagerFactoryOptions? options = null)
    {
        var documentContext = options?.DocumentContext ?? new SlideDocumentContext();
        var fakeChatClient = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, _) => StreamResponseAsync(),
            OnGetResponseAsync = (_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "<Page/>"))),
        };

        var copilotChatManager = new CopilotChatManager();
        copilotChatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider(fakeChatClient));

        var dispatcher = new FakeMainThreadDispatcher();
        var renderTool = new SlideMlRenderTool(new FakeSlideMlRenderPipeline(), dispatcher);
        return new SlideChatManager(copilotChatManager, renderTool, slideDocumentContext: documentContext);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamResponseAsync()
    {
        await Task.Yield();
        yield return new ChatResponseUpdate(ChatRole.Assistant, "<Page/>");
    }
}
