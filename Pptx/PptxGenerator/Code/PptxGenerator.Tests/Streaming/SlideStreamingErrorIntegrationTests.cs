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
    /// LLM 先输出有效 XML + 无效 XML，管道检测到异常后取消流并重试。
    /// 状态延续后，第一轮成功合并的 r1 应保留在合并器 DOM 树中。
    /// </summary>
    [TestMethod(DisplayName = "格式错误 XML：检测到异常后取消流并重试，第一轮成功片段保留")]
    public async Task FullStreaming_MalformedXml_LlmContinuesAfterError()
    {
        // Arrange — 第一轮：有效片段 + 无效片段（触发取消）；第二轮：有效片段
        var firstRound =
            "<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page>" +
            "<Panel Id=\"bad\"><Rect Id=\"inner\"></Panel></Rect>";
        var secondRound =
            "<Page><Rect Id=\"r2\" Width=\"50\" Height=\"30\" Fill=\"#00FF00\"/></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(firstRound, secondRound);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 状态延续后，第一轮成功合并的 r1 应保留
        StringAssert.Contains(chatManager.RenderedXml, "r1", "第一轮成功合并的 r1 应保留（状态延续）");
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
        Assert.DoesNotContain(
            "Page",
            chatManager.RenderedXml,
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

    // ───────── 用例 7：未闭合标签 ─────────

    /// <summary>
    /// LLM 输出 Rect 标签既未自闭合也未提供结束标签。
    /// 验证管道不崩溃，片段被跳过，RenderedXml 为空。
    /// </summary>
    [TestMethod(DisplayName = "未闭合标签：缺少自闭合斜杠和结束标签时片段被跳过")]
    public async Task FullStreaming_UnclosedTag_FragmentSkipped()
    {
        // Arrange — Rect 标签未闭合，后面的 </Page> 会被 XElement.Parse 视为不匹配
        var xml = "<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\"></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act — 不应抛异常
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 格式错误的片段被跳过，渲染结果为空
        Assert.IsTrue(string.IsNullOrEmpty(chatManager.RenderedXml), "未闭合标签的片段应被跳过");
    }

    // ───────── 用例 8：属性值缺少引号 ─────────

    /// <summary>
    /// LLM 输出属性值未用引号包裹的 XML。
    /// 验证管道不崩溃，片段被跳过，RenderedXml 为空。
    /// </summary>
    [TestMethod(DisplayName = "属性值缺引号：未加引号的属性值导致片段被跳过")]
    public async Task FullStreaming_AttributeValueWithoutQuotes_FragmentSkipped()
    {
        // Arrange — Id 和 Width 属性值没有引号，XML 格式不合法
        var xml = "<Page><Rect Id=r1 Width=100 Height=50 Fill=#FF0000/></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — XML 解析失败，片段被跳过
        Assert.IsTrue(string.IsNullOrEmpty(chatManager.RenderedXml), "属性值缺引号的片段应被跳过");
    }

    // ───────── 用例 8b：属性值缺引号 + 自然语言 + 有效 XML 混合 ─────────

    /// <summary>
    /// LLM 输出无引号属性的无效 XML，管道检测到异常后取消流并重试。
    /// 重试时输出有效 XML，验证最终渲染结果包含有效片段且不含自然语言。
    /// </summary>
    [TestMethod(DisplayName = "混合输出：无引号属性错误后取消流并重试，最终渲染有效 XML")]
    public async Task FullStreaming_InvalidXmlThenTextThenValid_OnlyValidRendered()
    {
        // Arrange — 第一轮：无引号属性的无效 XML；第二轮：有效 XML
        var firstRound =
            "<Page><Rect Id=r1 Width=100 Height=50 Fill=#FF0000/></Page>\n" +
            "这是一段自然语言，这是一段自然语言\n";
        var secondRound = "<Page><Rect Id=\"r2\" Width=\"300\"/></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(firstRound, secondRound);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 重试后的有效片段被渲染，自然语言不出现在渲染结果中
        StringAssert.Contains(chatManager.RenderedXml, "r2", "重试后的有效片段 r2 应被渲染");
        Assert.DoesNotContain(
            "这是一段自然语言",
            chatManager.RenderedXml,
            "自然语言文本不应出现在渲染结果中");
    }

    // ───────── 用例 8c：未闭合标签 + 自然语言 + 有效 XML 混合 ─────────

    /// <summary>
    /// LLM 输出未闭合标签的无效 XML，管道检测到异常后取消流并重试。
    /// 重试时输出有效 XML，验证最终渲染结果包含有效片段且不含自然语言。
    /// </summary>
    [TestMethod(DisplayName = "混合输出：未闭合标签错误后取消流并重试，最终渲染有效 XML")]
    public async Task FullStreaming_UnclosedTagThenTextThenValid_OnlyValidRendered()
    {
        // Arrange — 第一轮：未闭合标签 + 自然语言；第二轮：有效 XML
        var firstRound =
            "<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\"></Page>\n" +
            "这是说明文字\n";
        var secondRound = "<Page><Rect Id=\"r2\" Width=\"200\" Height=\"80\" Fill=\"#00FF00\"/></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(firstRound, secondRound);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 重试后的有效片段被渲染，自然语言不出现在渲染结果中
        StringAssert.Contains(chatManager.RenderedXml, "r2", "重试后的有效片段 r2 应被渲染");
        Assert.DoesNotContain(
            "这是说明文字",
            chatManager.RenderedXml,
            "自然语言文本不应出现在渲染结果中");
    }

    // ───────── 用例 9：重复属性 ─────────

    /// <summary>
    /// LLM 输出同一标签中出现重复属性名。
    /// 验证管道不崩溃，片段被跳过，RenderedXml 为空。
    /// </summary>
    [TestMethod(DisplayName = "重复属性：同一标签重复属性名导致片段被跳过")]
    public async Task FullStreaming_DuplicateAttribute_FragmentSkipped()
    {
        // Arrange — Rect 标签中 Id 属性出现两次
        var xml = "<Page><Rect Id=\"r1\" Id=\"r2\" Width=\"100\" Height=\"50\"/></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 重复属性导致 XML 解析失败，片段被跳过
        Assert.IsTrue(string.IsNullOrEmpty(chatManager.RenderedXml), "重复属性的片段应被跳过");
    }

    // ───────── 用例 9b：重复属性 + 自然语言 + 有效 XML 混合 ─────────

    /// <summary>
    /// LLM 输出重复属性的无效 XML，管道检测到异常后取消流并重试。
    /// 重试时输出有效 XML，验证最终渲染结果包含有效片段且不含自然语言。
    /// </summary>
    [TestMethod(DisplayName = "混合输出：重复属性错误后取消流并重试，最终渲染有效 XML")]
    public async Task FullStreaming_DuplicateAttributeThenTextThenValid_OnlyValidRendered()
    {
        // Arrange — 第一轮：重复属性 + 自然语言；第二轮：有效 XML
        var firstRound =
            "<Page><Rect Id=\"r1\" Id=\"r2\" Width=\"100\"/></Page>\n" +
            "以上是错误的 XML\n";
        var secondRound = "<Page><Rect Id=\"r3\" Width=\"300\"/></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(firstRound, secondRound);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 重试后的有效片段被渲染，自然语言不出现在渲染结果中
        StringAssert.Contains(chatManager.RenderedXml, "r3", "重试后的有效片段 r3 应被渲染");
        Assert.DoesNotContain(
            "以上是错误的 XML",
            chatManager.RenderedXml,
            "自然语言文本不应出现在渲染结果中");
    }

    // ───────── 用例 10：属性值中包含未转义特殊字符 ─────────

    /// <summary>
    /// LLM 输出属性值中包含未转义的 &lt; 字符。
    /// 验证管道不崩溃，片段被跳过，RenderedXml 为空。
    /// </summary>
    [TestMethod(DisplayName = "属性值含特殊字符：未转义的 < 导致片段被跳过")]
    public async Task FullStreaming_UnescapedSpecialCharInAttribute_FragmentSkipped()
    {
        // Arrange — Fill 属性值中包含未转义的 <
        var xml = "<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"red<blue\"/></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 未转义特殊字符导致 XML 解析失败，片段被跳过
        Assert.IsTrue(string.IsNullOrEmpty(chatManager.RenderedXml), "属性值含未转义特殊字符的片段应被跳过");
    }

    // ───────── 用例 10b：未转义特殊字符 + 自然语言 + 有效 XML 混合 ─────────

    /// <summary>
    /// LLM 输出含未转义 &lt; 的无效 XML，管道检测到异常后取消流并重试。
    /// 重试时输出有效 XML，验证最终渲染结果包含有效片段且不含自然语言。
    /// </summary>
    [TestMethod(DisplayName = "混合输出：未转义特殊字符错误后取消流并重试，最终渲染有效 XML")]
    public async Task FullStreaming_UnescapedCharThenTextThenValid_OnlyValidRendered()
    {
        // Arrange — 第一轮：未转义特殊字符 + 自然语言；第二轮：有效 XML
        var firstRound =
            "<Page><Rect Id=\"r1\" Fill=\"red<blue\"/></Page>\n" +
            "这段文字解释了上面的错误\n";
        var secondRound = "<Page><Rect Id=\"r2\" Width=\"300\" Fill=\"#00FF00\"/></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(firstRound, secondRound);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 重试后的有效片段被渲染，自然语言不出现在渲染结果中
        StringAssert.Contains(chatManager.RenderedXml, "r2", "重试后的有效片段 r2 应被渲染");
        Assert.DoesNotContain(
            "这段文字解释了上面的错误",
            chatManager.RenderedXml,
            "自然语言文本不应出现在渲染结果中");
    }

    // ───────── 用例 11：不完整的 XML 注释 ─────────

    /// <summary>
    /// LLM 输出以不完整注释结尾的 XML。
    /// 验证管道不崩溃，注释之前的完整片段被正常渲染。
    /// </summary>
    [TestMethod(DisplayName = "不完整注释：注释未闭合时之前的完整片段正常渲染")]
    public async Task FullStreaming_IncompleteComment_PreviousFragmentRendered()
    {
        // Arrange — 完整的 Page 片段 + 不完整注释
        var xml = "<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page><!-- 不完整的注释";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 完整的 Page 片段应被渲染
        StringAssert.Contains(chatManager.RenderedXml, "r1", "注释之前的完整片段应被渲染");
    }

    // ───────── 用例 12：多层嵌套未闭合 ─────────

    /// <summary>
    /// LLM 输出多层嵌套 XML 但缺少最外层 Page 的结束标签。
    /// 验证管道不崩溃，不完整片段不被渲染，RenderedXml 为空。
    /// </summary>
    [TestMethod(DisplayName = "多层嵌套未闭合：缺少外层结束标签时片段不渲染")]
    public async Task FullStreaming_MultilayerUnclosed_FragmentNotRendered()
    {
        // Arrange — 缺少 </Page>
        var xml = "<Page><Panel Id=\"p1\"><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Panel>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 不完整片段（深度未归零）不会被提取和渲染
        Assert.IsTrue(string.IsNullOrEmpty(chatManager.RenderedXml), "多层嵌套未闭合的片段不应被渲染");
    }

    // ───────── 用例 13：混合大小写标签名 ─────────

    /// <summary>
    /// LLM 输出使用小写标签名 page 和 rect。
    /// 验证管道不崩溃，但小写子元素 rect 不被解析器识别，渲染结果不包含 r1 的有效渲染。
    /// </summary>
    [TestMethod(DisplayName = "混合大小写标签名：小写子元素名不被解析，有效内容不渲染")]
    public async Task FullStreaming_LowercaseTagName_ChildElementNotRendered()
    {
        // Arrange — 父标签 page 被 merger 不区分大小写接受，但子元素 rect 不被 parser 识别
        var xml = "<page><rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 小写 rect 被解析器视为未知标签忽略，RenderedXml 不应包含被正确渲染的 r1
        Assert.DoesNotContain(
            "Rect",
            chatManager.RenderedXml,
            "小写 rect 标签名不应被解析为有效 Rect 元素");
    }

    // ───────── 用例 14：错误格式 XML 后紧接有效 XML ─────────

    /// <summary>
    /// LLM 先输出重复属性的无效片段，管道检测到异常后取消流并重试。
    /// 重试时输出有效片段，验证最终渲染结果包含有效片段。
    /// </summary>
    [TestMethod(DisplayName = "错误后恢复：重复属性错误后取消流并重试，有效片段正常渲染")]
    public async Task FullStreaming_ErrorThenValid_ValidFragmentRendered()
    {
        // Arrange — 第一轮：无效片段；第二轮：有效片段
        var firstRound = "<Page><Rect Id=\"r1\" Id=\"r2\" Width=\"100\" Height=\"50\"/></Page>";
        var secondRound = "<Page><Rect Id=\"r3\" Width=\"50\" Height=\"30\" Fill=\"#00FF00\"/></Page>";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(firstRound, secondRound);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 重试后的有效片段应被渲染
        StringAssert.Contains(chatManager.RenderedXml, "r3", "重试后的有效片段 r3 应被渲染");
    }

    // ───────── 用例 15：不完整的 CDATA ─────────

    /// <summary>
    /// LLM 输出以不完整 CDATA 结尾的 XML。
    /// 验证管道不崩溃，CDATA 之前的完整片段被正常渲染。
    /// </summary>
    [TestMethod(DisplayName = "不完整 CDATA：CDATA 未闭合时之前的完整片段正常渲染")]
    public async Task FullStreaming_IncompleteCData_PreviousFragmentRendered()
    {
        // Arrange — 完整的 Page 片段 + 不完整 CDATA
        var xml = "<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page><![CDATA[ 不完整的内容";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManager(xml);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 完整的 Page 片段应被渲染
        StringAssert.Contains(chatManager.RenderedXml, "r1", "CDATA 之前的完整片段应被渲染");
    }

    // ───────── 用例 16：Panel Padding 格式错误后重试修正 ─────────

    /// <summary>
    /// LLM 输出 Panel 的 Padding 属性值为非数值 "abc"。
    /// 合并层不报错（XML 结构合法、Id 存在），渲染层检测到 Padding 格式错误后取消流并重试。
    /// 重试时输出正确 Padding 值，验证最终渲染结果不包含格式错误。
    /// 注意：重试时 Panel Id="p1" 相同，MergeChildren 会用第二轮的子元素替换第一轮的子元素。
    /// </summary>
    [TestMethod(DisplayName = "Panel Padding 格式错误：渲染检测到错误后取消流并重试，修正后正常渲染")]
    public async Task FullStreaming_PanelInvalidPadding_RetryWithValid_RendersCorrectly()
    {
        // Arrange — 第一轮：Padding 格式错误；第二轮：修正为有效值
        var firstRound =
            """<Page><Panel Id="p1" Padding="abc"><Rect Id="r1" Width="100" Height="50" Fill="#FF0000"/></Panel></Page>""";
        var secondRound =
            """<Page><Panel Id="p1" Padding="16"><Rect Id="r1" Width="100" Height="50" Fill="#FF0000"/><Rect Id="r2" Width="200" Height="80" Fill="#00FF00"/></Panel></Page>""";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(firstRound, secondRound);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 重试后有效片段被渲染，Padding 被修正
        StringAssert.Contains(chatManager.RenderedXml, "r1", "r1 应保留（第二轮包含 r1）");
        StringAssert.Contains(chatManager.RenderedXml, "r2", "第二轮新增的 r2 应被渲染");
        Assert.DoesNotContain(
            "Padding=\"abc\"",
            chatManager.RenderedXml,
            "修正后渲染结果不应包含错误的 Padding=\"abc\"");
        StringAssert.Contains(chatManager.RenderedXml, "Padding=\"16\"", "Padding 应被修正为 16");
    }

    // ───────── 用例 17：Panel Padding 格式错误（首个片段）─────────

    /// <summary>
    /// 首个片段的 Panel Padding 属性值为非数值 "xyz"。
    /// 合并层不报错，渲染层检测到错误后取消流并重试。
    /// 重试时用同一 Panel Id 覆盖修正 Padding 值，验证最终渲染结果不含格式错误。
    /// </summary>
    [TestMethod(DisplayName = "Panel Padding 格式错误（首个片段）：重试后覆盖修正，正常渲染")]
    public async Task FullStreaming_PanelInvalidPaddingFirstFragment_RetryRecoversFromEmpty()
    {
        // Arrange — 第一轮：首个片段即 Padding 格式错误；第二轮：同 Panel Id 覆盖修正
        var firstRound =
            """<Page><Panel Id="p1" Padding="xyz"><Rect Id="r1" Width="100" Height="50"/></Panel></Page>""";
        var secondRound =
            """<Page><Panel Id="p1" Padding="8"><Rect Id="r1" Width="100" Height="50"/><Rect Id="r2" Width="200" Height="100" Fill="#00FF00"/></Panel></Page>""";
        var (chatManager, _) = SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(firstRound, secondRound);

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 重试后 Padding 被修正，有效片段被渲染
        Assert.DoesNotContain(
            "Padding=\"xyz\"",
            chatManager.RenderedXml,
            "修正后渲染结果不应包含错误的 Padding=\"xyz\"");
        StringAssert.Contains(chatManager.RenderedXml, "r1", "r1 应保留");
        StringAssert.Contains(chatManager.RenderedXml, "r2", "重试后 r2 应被渲染");
    }
}
