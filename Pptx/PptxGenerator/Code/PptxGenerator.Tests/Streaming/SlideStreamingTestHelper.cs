using System.Runtime.CompilerServices;

using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;

using Microsoft.Extensions.AI;

using PptxGenerator;
using PptxGenerator.Pipeline;
using PptxGenerator.Rendering;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// 流式集成测试公共辅助类，提供逐字符流式输出与 SlideChatManager 创建逻辑。
/// </summary>
internal static class SlideStreamingTestHelper
{
    /// <summary>
    /// 将完整文本按逐字符方式通过 ChatResponseUpdate 流式返回，
    /// 可选在每个字符之间加入延迟。
    /// </summary>
    public static async IAsyncEnumerable<ChatResponseUpdate> StreamTokensAsync(
        string text,
        TimeSpan delay = default,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var ch in text)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, ch.ToString());
            if (delay == TimeSpan.Zero)
            {
                await Task.Yield();
            }
            else
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// 创建配置好的 SlideChatManager 和对应的 FakeChatClient，
    /// 使用指定文本逐字符模拟流式输出，可选延迟和取消令牌。
    /// </summary>
    public static (SlideChatManager ChatManager, FakeChatClient FakeChatClient) CreateChatManager(
        string text,
        TimeSpan delay = default,
        CancellationToken cancellationToken = default)
    {
        return CreateChatManager(() => StreamTokensAsync(text, delay, cancellationToken));
    }

    /// <summary>
    /// 创建配置好的 SlideChatManager 和对应的 FakeChatClient。
    /// </summary>
    public static (SlideChatManager ChatManager, FakeChatClient FakeChatClient) CreateChatManager(
        Func<IAsyncEnumerable<ChatResponseUpdate>>? streamingResponseFactory = null)
    {
        var fakeChatClient = new FakeChatClient();

        if (streamingResponseFactory is not null)
        {
            fakeChatClient.OnGetStreamingResponseAsync = (_, _, _) => streamingResponseFactory();
        }
        else
        {
            fakeChatClient.OnGetStreamingResponseAsync = (_, _, _) =>
                StreamTokensAsync("""<Page Background="#FFFFFF"/>""");
        }

        // 非流式也需要配置（工具调用可能用到）
        fakeChatClient.OnGetResponseAsync = (_, _, _) =>
        {
            Assert.Fail("不应该调用到此");
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, """<Page/>""")));
        };

        var copilotChatManager = new CopilotChatManager();
        copilotChatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(
            new FakeLanguageModelProvider(fakeChatClient));

        var layoutEngine = new SlideMlLayoutEngine();
        var renderEngine = new FakeRenderEngine();
        var dispatcher = new FakeMainThreadDispatcher();
        var renderPipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher);
        var renderTool = new SlideMlRenderTool(renderPipeline, dispatcher);
        var chatManager = new SlideChatManager(copilotChatManager, renderTool);

        return (chatManager, fakeChatClient);
    }
}
