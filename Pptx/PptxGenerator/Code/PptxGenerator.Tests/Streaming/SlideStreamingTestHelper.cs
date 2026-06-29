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
        return CreateChatManager(ct => StreamTokensAsync(text, delay, ct));
    }

    /// <summary>
    /// 创建配置好的 SlideChatManager 和对应的 FakeChatClient，
    /// 支持按调用顺序返回不同的流式响应（用于异常检测→重试场景）。
    /// </summary>
    /// <param name="sequentialTexts">按调用顺序依次返回的文本列表。
    /// 第一次调用返回第一段，第二次返回第二段，以此类推。
    /// 超出列表范围时返回最后一段。</param>
    public static (SlideChatManager ChatManager, FakeChatClient FakeChatClient) CreateChatManagerWithSequentialTexts(
        params string[] sequentialTexts)
    {
        var callIndex = 0;
        return CreateChatManager(ct =>
        {
            var text = callIndex < sequentialTexts.Length
                ? sequentialTexts[callIndex]
                : sequentialTexts[^1];
            callIndex++;
            return StreamTokensAsync(text, TimeSpan.Zero, ct);
        });
    }

    /// <summary>
    /// 创建配置好的 SlideChatManager 和对应的 FakeChatClient。
    /// </summary>
    /// <param name="streamingResponseFactory">接收 CancellationToken 的流式响应工厂。
    /// 若为 <see langword="null"/>，默认返回一个空 Page。</param>
    public static (SlideChatManager ChatManager, FakeChatClient FakeChatClient) CreateChatManager(
        Func<CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? streamingResponseFactory = null)
    {
        var fakeChatClient = new FakeChatClient();

        if (streamingResponseFactory is not null)
        {
            fakeChatClient.OnGetStreamingResponseAsync = (_, _, ct) => streamingResponseFactory(ct);
        }
        else
        {
            fakeChatClient.OnGetStreamingResponseAsync = (_, _, ct) =>
                StreamTokensAsync("""<Page Background="#FFFFFF"/>""", TimeSpan.Zero, ct);
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
