using PptxGenerator;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// 从 SlideChatManager.SendMessageAsync(useStreaming: true) 入口出发的流式错误恢复集成测试。
/// 注入 FakeChatClient 模拟 LLM 逐字符输出 SlideML XML，验证在格式错误、重复 Id、
/// 不完整 XML、空响应等异常场景下管道不崩溃且正确处理有效片段。
/// </summary>
[TestClass]
public sealed class SlideStreamingErrorIntegrationTests
{

    // ───────── 用例 1：格式错误 XML 后继续有效输出 ─────────

    /// <summary>
    /// LLM 先输出有效 XML，然后输出无效文本，最后输出更多有效 XML。
    /// 验证最终渲染结果包含有效部分。
    /// </summary>
    [TestMethod]
    public async Task FullStreaming_MalformedXml_LlmContinuesAfterError()
    {
        // Arrange — 有效片段 + 无效文本 + 有效片段
        var text =
            "<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page>" +
            "<Panel Id=\"bad\"><Rect Id=\"inner\"></Panel></Rect>" +
            "<Page><Rect Id=\"r2\" Width=\"50\" Height=\"30\" Fill=\"#00FF00\"/></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(text);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 有效片段应被渲染
        StringAssert.Contains(chatManager.RenderedXml, "r1", "有效片段 r1 应保留");
        StringAssert.Contains(chatManager.RenderedXml, "r2", "有效片段 r2 应保留");
    }

    // ───────── 用例 2：不完整 XML 只渲染完整片段 ─────────

    /// <summary>
    /// LLM 输出一段不完整的 XML（完整的 Page + 残留的不完整 Panel）。
    /// 验证只有完整的 Page 片段被渲染。
    /// </summary>
    [TestMethod]
    public async Task FullStreaming_LlmOutputsIncompleteXml_OnlyCompletePartsRendered()
    {
        // Arrange — 完整 Page + 不完整 Panel
        var text =
            "<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page>" +
            "<Panel Id=\"incomplete\"";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(text);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 完整 Page 应被渲染
        StringAssert.Contains(chatManager.RenderedXml, "r1", "完整 Page 片段应被渲染");
    }

    // ───────── 用例 3：不同类型元素重复 Id ─────────

    /// <summary>
    /// LLM 输出 Panel 和 Rect 共用 Id="dup"（类型不同）。
    /// 验证管道不崩溃，整个片段被跳过，RenderedXml 为空。
    /// </summary>
    [TestMethod]
    public async Task FullStreaming_LlmOutputsDuplicateIdDifferentTypes_ErrorCollected()
    {
        // Arrange — 整个片段因不同类型重复 Id 被跳过，不会有 Page 入树
        var xml = "<Page><Panel Id=\"dup\"><Rect Id=\"dup\"/></Panel></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act — 不应抛异常
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 片段被跳过，渲染结果为空
        Assert.IsTrue(string.IsNullOrEmpty(chatManager.RenderedXml), "不同类型重复 Id 的片段应被整体跳过");
    }

    // ───────── 用例 4：相同类型元素重复 Id ─────────

    /// <summary>
    /// LLM 输出同一 Panel 内两个 Rect 都叫 Id="dup"（类型相同）。
    /// 验证不应崩溃，最终能渲染。
    /// </summary>
    [TestMethod]
    public async Task FullStreaming_LlmOutputsDuplicateIdSameType_NoError()
    {
        // Arrange
        var xml = "<Page><Panel Id=\"p1\"><Rect Id=\"dup\" Width=\"50\"/><Rect Id=\"dup\" Width=\"100\"/></Panel></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 管道不崩溃，能渲染
        StringAssert.Contains(chatManager.RenderedXml, "Page", "同类型重复 Id 不应阻止渲染");
    }

    // ───────── 用例 5：空 LLM 响应 ─────────

    /// <summary>
    /// LLM 输出空字符串。
    /// 验证不抛异常，RenderedXml 为空或不含 Page。
    /// </summary>
    [TestMethod]
    public async Task FullStreaming_EmptyLlmResponse_NoRender()
    {
        // Arrange
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(string.Empty);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 不抛异常，且不含 Page
        Assert.IsFalse(
            chatManager.RenderedXml.Contains("Page"),
            "空响应不应渲染任何 Page");
    }

    // ───────── 用例 6：纯文本无 XML ─────────

    /// <summary>
    /// LLM 输出纯文本，不包含任何 XML。
    /// 验证不抛异常，RenderedXml 为空。
    /// </summary>
    [TestMethod]
    public async Task FullStreaming_LlmOutputsOnlyText_NoXml_NoRender()
    {
        // Arrange
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager("这是一段文字，没有 XML");

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 不抛异常，且为空
        Assert.IsTrue(
            string.IsNullOrEmpty(chatManager.RenderedXml),
            "纯文本无 XML 时 RenderedXml 应为空");
    }
}
