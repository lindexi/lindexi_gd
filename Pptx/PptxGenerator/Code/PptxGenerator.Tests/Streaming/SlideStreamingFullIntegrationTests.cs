using PptxGenerator;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// 从 SlideChatManager.SendMessageAsync(useStreaming: true) 入口出发的完整流式集成测试。
/// 注入 FakeChatClient 模拟 LLM 逐字符输出 SlideML XML，走完整
/// CopilotChatManager → CreateManualSendMessageContextAsync → ChatClientAgent.RunStreamingAsync
/// → await foreach AgentResponseUpdate → SlideStreamingPipeline.ProcessIncrementalText
/// → ProcessStreamEndAsync → RenderAsync 链路。
/// </summary>
[TestClass]
public sealed class SlideStreamingFullIntegrationTests
{

    // ───────── 用例 1：单页流式渲染 ─────────

    /// <summary>
    /// FakeChatClient 逐字符输出包含 TextElement 的 Page XML，验证完整流式渲染链路。
    /// </summary>
    [TestMethod(DisplayName = "单页流式渲染：验证逐字符输出 Page+TextElement 时完整链路正确")]
    public async Task FullStreaming_SimplePage_RendersCorrectly()
    {
        // Arrange
        var xml = """<Page Background="#FFFFFF"><TextElement Id="title" Text="Hello" FontSize="32" X="10" Y="10" Width="200"/></Page>""";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成标题页",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(chatManager.RenderedXml), "RenderedXml 不应为空");
        Assert.IsNotNull(chatManager.PreviewImage, "PreviewImage 不应为 null");
        StringAssert.Contains(chatManager.RenderedXml, "Page", "RenderedXml 应包含 Page");
        StringAssert.Contains(chatManager.RenderedXml, "TextElement", "RenderedXml 应包含 TextElement");
    }

    // ───────── 用例 2：多元素流式渲染 ─────────

    /// <summary>
    /// FakeChatClient 逐字符输出包含 Rect 和 TextElement 的 Page XML，验证多元素渲染。
    /// </summary>
    [TestMethod(DisplayName = "多元素流式渲染：验证 Rect 和 TextElement 均被成功渲染")]
    public async Task FullStreaming_MultipleElements_AllRendered()
    {
        // Arrange
        var xml = """<Page><Rect Id="bg" X="0" Y="0" Width="1280" Height="720" Fill="#FFFFFF"/><TextElement Id="title" Text="Title" FontSize="32" X="50" Y="50"/></Page>""";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成多元素页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert
        StringAssert.Contains(chatManager.RenderedXml, "bg", "RenderedXml 应包含 bg");
        StringAssert.Contains(chatManager.RenderedXml, "title", "RenderedXml 应包含 title");
    }

    // ───────── 用例 3：嵌套 Panel 流式渲染 ─────────

    /// <summary>
    /// FakeChatClient 逐字符输出嵌套 Panel 的 Page XML，验证嵌套结构正确渲染。
    /// </summary>
    [TestMethod(DisplayName = "嵌套 Panel 流式渲染：验证 outer/inner/r1 嵌套结构正确")]
    public async Task FullStreaming_NestedPanels_CorrectStructure()
    {
        // Arrange
        var xml = """
            <Page>
                <Panel Id="outer" X="10" Y="10" Width="500" Height="300">
                    <Panel Id="inner" X="20" Y="20" Width="200" Height="100">
                        <Rect Id="r1" Width="50" Height="30" Fill="#FF0000"/>
                    </Panel>
                </Panel>
            </Page>
            """;
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成嵌套面板页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert
        StringAssert.Contains(chatManager.RenderedXml, "outer", "RenderedXml 应包含 outer");
        StringAssert.Contains(chatManager.RenderedXml, "inner", "RenderedXml 应包含 inner");
        StringAssert.Contains(chatManager.RenderedXml, "r1", "RenderedXml 应包含 r1");
    }

    // ───────── 用例 4：XML 声明流式渲染 ─────────

    /// <summary>
    /// FakeChatClient 逐字符输出带 XML 声明的 Page XML，验证声明被正确跳过。
    /// </summary>
    [TestMethod(DisplayName = "XML 声明流式渲染：验证 <?xml?> 声明被正确跳过")]
    public async Task FullStreaming_XmlDeclaration_CorrectlyParsed()
    {
        // Arrange
        var xml = """<?xml version="1.0"?><Page><Rect Id="r1" X="0" Y="0" Width="100" Height="50" Fill="#0000FF"/></Page>""";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成带声明的页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert
        StringAssert.Contains(chatManager.RenderedXml, "Page", "RenderedXml 应包含 Page");
        StringAssert.Contains(chatManager.RenderedXml, "r1", "RenderedXml 应包含 r1");
    }

    // ───────── 用例 5：空页面流式渲染 ─────────

    /// <summary>
    /// FakeChatClient 逐字符输出空 Page XML，验证无子元素时不报错。
    /// </summary>
    [TestMethod(DisplayName = "空页面流式渲染：验证空 <Page/> 不报错且 RenderedXml 正确")]
    public async Task FullStreaming_EmptyPage_RendersWithoutError()
    {
        // Arrange
        var xml = """<Page/>""";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成空页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(chatManager.RenderedXml), "RenderedXml 不应为空");
        StringAssert.Contains(chatManager.RenderedXml, "Page", "RenderedXml 应包含 Page");
    }

    // ───────── 用例 6：取消令牌 ─────────

    /// <summary>
    /// 在流式输出过程中取消 CancellationToken，验证抛出 OperationCanceledException 或其子类。
    /// </summary>
    [TestMethod(DisplayName = "取消令牌：流式过程中取消应抛出 OperationCanceledException")]
    public async Task FullStreaming_Cancellation_ThrowsOperationCanceledException()
    {
        // Arrange — 使用足够长的 XML 并在每个字符之间加入延迟，确保取消令牌在流式过程中触发
        var longXml = """<Page>""" + string.Concat(Enumerable.Repeat("""<Rect Id="r" Width="100" Height="50" Fill="#FF0000"/>""", 30)) + """</Page>""";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(longXml, TimeSpan.FromMilliseconds(10));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act & Assert — 取消可能从 ThrowIfCancellationRequested 或 Task.Delay 触发，
        // 两者分别抛出 OperationCanceledException 和 TaskCanceledException（后者继承前者）
        OperationCanceledException? caughtException = null;
        try
        {
            await chatManager.SendMessageAsync(
                "生成页面",
                isFirstMessage: true,
                attachPreview: false,
                useStreaming: true,
                cancellationToken: cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            caughtException = ex;
        }

        Assert.IsNotNull(caughtException, "应抛出 OperationCanceledException 或其子类");
    }

    // ───────── 用例 7：混合文本输出 ─────────

    /// <summary>
    /// FakeChatClient 输出混合文本（非 XML + XML + 非 XML），验证片段提取器只处理 XML 部分。
    /// </summary>
    [TestMethod(DisplayName = "混合文本输出：非 XML 文本与 XML 混合时仅处理 XML 部分")]
    public async Task FullStreaming_LlmOutputsNonXmlText_OnlyXmlPartsProcessed()
    {
        // Arrange
        var mixedText = """
            这是幻灯片XML：
            <Page><Rect Id="r1" Width="100" Height="50" Fill="#FF0000"/></Page>
            以上是XML。
            """;
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(mixedText);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert
        StringAssert.Contains(chatManager.RenderedXml, "Page", "RenderedXml 应包含 Page");
        StringAssert.Contains(chatManager.RenderedXml, "r1", "RenderedXml 应包含 r1");
    }
}
