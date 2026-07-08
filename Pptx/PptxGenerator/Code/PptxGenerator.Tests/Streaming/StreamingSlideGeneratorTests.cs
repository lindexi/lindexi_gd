using System.Xml.Linq;

using Microsoft.Extensions.AI;

using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;
using PptxGenerator.Streaming;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// <see cref="StreamingSlideGenerator.GenerateAsync"/> 上层流式生成入口的单元测试。
/// </summary>
[TestClass]
public sealed class StreamingSlideGeneratorTests
{
    /// <summary>
    /// 通过 <see cref="StreamingSlideGenerator.GenerateAsync"/> 上层入口验证首片段为悬空样式元素时，
    /// 第一轮实时渲染失败会触发重试，第二轮 Page 仍可引用第一轮暂存的样式。
    /// </summary>
    [TestMethod(DisplayName = "GenerateAsync 上层链路：首片段悬空样式触发重试后 Page 可引用样式")]
    public async Task GenerateAsync_FirstFragmentDanglingStyleElement_ThenPageCanReferenceStyle()
    {
        // Arrange
        var danglingStyleFragment =
            """<Rect Id="card-template" StyleId="card-style" Fill="#FF0000" Width="100" Height="50"/>""";
        var pageFragment =
            """<Page><Rect Id="card1" StyleFrom="card-style" X="10" Y="20"/></Page>""";

        var capturedMessages = new List<IReadOnlyList<ChatMessage>>();
        var (chatManager, fakeChatClient, recorder) =
            SlideStreamingTestHelper.CreateChatManagerWithSequentialTextsAndRecorder(
                danglingStyleFragment, pageFragment);
        var promptProvider = new SlideMlPromptProvider();
        var dispatcher = new FakeMainThreadDispatcher();
        var streamingState = new SlideStreamingState(
            promptProvider,
            chatManager.SlideMlRenderTool.RenderPipeline,
            dispatcher);
        var generator = new StreamingSlideGenerator(
            chatManager.Pipeline.ChatManager,
            promptProvider,
            chatManager.SlideMlRenderTool,
            dispatcher);

        var originalCallback = fakeChatClient.OnGetStreamingResponseAsync!;
        fakeChatClient.OnGetStreamingResponseAsync = (messages, options, cancellationToken) =>
        {
            capturedMessages.Add(messages.ToList());
            return originalCallback(messages, options, cancellationToken);
        };

        // Act
        await generator.GenerateAsync(
            "生成可复用卡片样式页面",
            isFirstMessage: true,
            streamingState,
            CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(2, recorder.StreamingCallCount, "首个悬空样式片段不可渲染，应触发一次重试");
        Assert.IsTrue(capturedMessages.Count >= 2, "应能捕获首次请求和重试反馈请求");
        var retryText = string.Join("\n", capturedMessages[1].Select(message => message.Text));
        StringAssert.Contains(retryText, "card-template", "重试反馈应包含第一轮已合并的悬空样式片段，便于模型基于当前状态续写");
        StringAssert.Contains(retryText, "仅输出修正和后续片段", "重试反馈应要求模型只输出后续 Page 片段");

        Assert.AreEqual(0, streamingState.Context.Errors.Count, "第二轮 Page 引用悬空样式时不应留下错误");
        var doc = XDocument.Parse(streamingState.Pipeline.CurrentMergedXml);
        Assert.AreEqual("Page", doc.Root!.Name.LocalName, "后续 Page 片段应成为最终可渲染根元素");
        Assert.IsFalse(doc.Root.Elements().Any(element => element.Attribute("Id")?.Value == "card-template"),
            "悬空样式元素不应进入 Page 子树参与渲染");

        var card1 = doc.Root.Elements("Rect").Single(element => element.Attribute("Id")?.Value == "card1");
        Assert.AreEqual("#FF0000", card1.Attribute("Fill")?.Value, "Page 内元素应通过 GenerateAsync 完整链路继承悬空样式 Fill");
        Assert.AreEqual("100", card1.Attribute("Width")?.Value, "Page 内元素应继承悬空样式 Width");
        Assert.AreEqual("50", card1.Attribute("Height")?.Value, "Page 内元素应继承悬空样式 Height");
        Assert.AreEqual("10", card1.Attribute("X")?.Value, "自身显式属性优先保留");
        Assert.AreEqual("20", card1.Attribute("Y")?.Value, "自身显式属性优先保留");
        Assert.IsNull(card1.Attribute("StyleFrom"), "StyleFrom 应在应用后被移除");

        StringAssert.Contains(chatManager.RenderedXml, "card1", "上层渲染工具应接收到最终 Page 渲染结果");
        StringAssert.Contains(chatManager.RenderedXml, "#FF0000", "最终渲染结果应包含继承后的样式属性");
    }
}
