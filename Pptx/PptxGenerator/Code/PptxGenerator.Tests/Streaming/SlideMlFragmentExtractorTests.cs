using PptxGenerator.Streaming;

namespace PptxGenerator.Tests.Streaming;

[TestClass]
public sealed class SlideMlFragmentExtractorTests
{
    [TestMethod(DisplayName = "单个闭合片段返回片段并清空缓冲")]
    public void Append_SingleFragment_ReturnsAfterClose()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page Background=\"#FFFFFF\"></Page>");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page Background=\"#FFFFFF\"></Page>", fragments[0]);
        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod(DisplayName = "片段跨多次追加时补齐后返回片段")]
    public void Append_FragmentSplitAcrossTokens_ReturnsWhenComplete()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Panel Id=\"hea");
        extractor.Append("der\" X=\"0\">");
        extractor.Append("<TextElement Id=\"title\" Text=\"Hi\"/></Panel>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual(
            "<Panel Id=\"header\" X=\"0\"><TextElement Id=\"title\" Text=\"Hi\"/></Panel>",
            fragments[0]);
    }

    [TestMethod(DisplayName = "自闭合标签追加后立即返回片段")]
    public void Append_SelfClosingTag_ReturnsImmediately()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect Id=\"bg\" Width=\"100\"/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Rect Id=\"bg\" Width=\"100\"/>", fragments[0]);
    }

    [TestMethod(DisplayName = "多个完整片段追加后全部返回")]
    public void Append_MultipleFragments_ReturnsAll()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><Panel Id=\"a\"/></Page><Rect Id=\"b\"/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(2, fragments);
        Assert.AreEqual("<Page><Panel Id=\"a\"/></Page>", fragments[0]);
        Assert.AreEqual("<Rect Id=\"b\"/>", fragments[1]);
    }

    [TestMethod(DisplayName = "未闭合片段保留在缓冲区")]
    public void Append_PartialFragment_StaysInBuffer()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Panel Id=\"header\">");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.IsEmpty(fragments);
        Assert.Contains("<Panel Id=\"header\">", remaining);
    }

    [TestMethod(DisplayName = "遇到片段内部不匹配结束标签时返回非法片段供上层报错")]
    public void Append_MismatchedClosingTagInsideFragment_ReturnsInvalidFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></MainContainer>""");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></MainContainer>""", fragments[0]);
        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod(DisplayName = "遇到顶层片段后多余结束标签时提取合法片段并保留剩余内容")]
    public void Append_ExtraClosingTagAfterFragment_ReturnsFragmentAndRemaining()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></Panel></MainContainer>""");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></Panel>""", fragments[0]);
        Assert.AreEqual("""</MainContainer>""", remaining);
    }

    [TestMethod(DisplayName = "遇到顶层片段后多余结束标签和后续片段时跳过多余结束标签并提取两个片段")]
    public void Append_ExtraClosingTagAndFragmentAfterFragment_ReturnsBothFragments()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></Panel></MainContainer><Rect Id="OverlayShape"/>""");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.HasCount(2, fragments);
        Assert.AreEqual("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></Panel>""", fragments[0]);
        Assert.AreEqual("""<Rect Id="OverlayShape"/>""", fragments[1]);
        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod(DisplayName = "遇到嵌套片段不匹配结束标签后恢复外层闭合时返回非法片段")]
    public void Append_MismatchedNestedClosingTagThenOuterClosingTag_ReturnsInvalidFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></MainContainer></Panel>""");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></MainContainer>""", fragments[0]);
        Assert.AreEqual("""</Panel>""", remaining);

        extractor.Append("""<Rect Id="OverlayShape" X="100"/>""");

        fragments = extractor.TryExtractFragments();
        remaining = extractor.GetRemaining();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("""<Rect Id="OverlayShape" X="100"/>""", fragments[0]);
        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod(DisplayName = "遇到非法片段后跟随合法自闭合片段时跳过尾部结束标签并提取两个片段")]
    public void Append_InvalidFragmentFollowedBySelfClosingFragment_ReturnsBothFragments()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></MainContainer></Panel><Rect Id="OverlayShape" X="100"/>""");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.HasCount(2, fragments);
        Assert.AreEqual("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></MainContainer>""", fragments[0]);
        Assert.AreEqual("""<Rect Id="OverlayShape" X="100"/>""", fragments[1]);
        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod(DisplayName = "遇到未闭合内部标签和不匹配结束标签时返回非法片段")]
    public void Append_UnclosedChildAndMismatchedClosingTag_ReturnsInvalidFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("""<Panel Id="MainContainer"><Rect Id="OverlayShape"></MainContainer></Panel>""");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("""<Panel Id="MainContainer"><Rect Id="OverlayShape"></MainContainer>""", fragments[0]);
        Assert.AreEqual("""</Panel>""", remaining);
    }

    [TestMethod(DisplayName = "遇到前导不匹配结束标签和不完整片段时不提取片段并保留全部内容")]
    public void Append_LeadingMismatchedClosingTagAndIncompleteFragment_ReturnsNoFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("""</MainContainer><Panel Id="MainContainer"><Rect Id="OverlayShape"/>""");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.IsEmpty(fragments);
        Assert.AreEqual("""</MainContainer><Panel Id="MainContainer"><Rect Id="OverlayShape"/>""", remaining);
    }

    [TestMethod(DisplayName = "遇到前导不匹配结束标签和后续非法片段时跳过前导结束标签并返回非法片段")]
    public void Append_LeadingMismatchedClosingTagAndInvalidFragment_ReturnsInvalidFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("""</Panel><Panel Id="MainContainer"><Rect Id="OverlayShape"></MainContainer></Panel>""");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("""<Panel Id="MainContainer"><Rect Id="OverlayShape"></MainContainer>""", fragments[0]);
        Assert.AreEqual("""</Panel>""", remaining);
    }

    [TestMethod(DisplayName = "遇到前导不匹配结束标签和后续合法片段时跳过前导结束标签并返回合法片段")]
    public void Append_LeadingMismatchedClosingTagAndValidFragment_ReturnsValidFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("""</MainContainer><Panel Id="MainContainer"><Rect Id="OverlayShape"/></Panel>""");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("""<Panel Id="MainContainer"><Rect Id="OverlayShape"/></Panel>""", fragments[0]);
        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod(DisplayName = "XML 声明不会作为片段并跳过后返回元素")]
    public void Append_XmlDeclaration_NotTreatedAsFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<?xml version=\"1.0\"?><Page/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page/>", fragments[0]);
    }

    [TestMethod(DisplayName = "顶层注释不会作为片段并跳过后返回元素")]
    public void Append_Comment_NotTreatedAsFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<!-- comment --><Page/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page/>", fragments[0]);
    }

    [TestMethod(DisplayName = "嵌套元素在外层闭合后返回外层片段")]
    public void Append_NestedElements_ReturnsWhenOuterCloses()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Panel Id=\"outer\"><Panel Id=\"inner\"/></Panel>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Panel Id=\"outer\"><Panel Id=\"inner\"/></Panel>", fragments[0]);
    }

    [TestMethod(DisplayName = "片段之间的空白被跳过并返回后续片段")]
    public void Append_TextBetweenFragments_SkipsWhitespaceAndReturnsFragments()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page/>\n<Rect Id=\"r\"/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(2, fragments);
        Assert.AreEqual("<Page/>", fragments[0]);
        Assert.AreEqual("<Rect Id=\"r\"/>", fragments[1]);
    }

    [TestMethod(DisplayName = "提取完成后获取残留内容返回空字符串")]
    public void GetRemaining_AfterExtraction_ReturnsEmpty()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page/>");

        extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod(DisplayName = "追加空字符串不会产生片段也不会留下残留")]
    public void Append_EmptyString_NoEffect()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append(string.Empty);

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.IsEmpty(fragments);
        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod(DisplayName = "追加空引用时抛出参数空异常")]
    public void Append_Null_ThrowsArgumentNullException()
    {
        var extractor = new SlideMlFragmentExtractor();

        Assert.ThrowsExactly<ArgumentNullException>(() => extractor.Append(null!));
    }

    [TestMethod(DisplayName = "新实例获取残留内容时返回空字符串")]
    public void GetRemaining_NewInstance_ReturnsEmptyString()
    {
        var extractor = new SlideMlFragmentExtractor();

        var remaining = extractor.GetRemaining();

        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod(DisplayName = "无输入时提取片段返回空列表")]
    public void TryExtractFragments_NoInput_ReturnsEmptyList()
    {
        var extractor = new SlideMlFragmentExtractor();

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.IsEmpty(fragments);
        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod(DisplayName = "完整普通元素返回单个片段")]
    public void TryExtractFragments_CompleteElement_ReturnsSingleFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page></Page>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page></Page>", fragments[0]);
        Assert.AreEqual(string.Empty, extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "元素文本内容不影响片段提取")]
    public void TryExtractFragments_ElementWithText_ReturnsSingleFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<TextElement>Hello</TextElement>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<TextElement>Hello</TextElement>", fragments[0]);
    }

    [TestMethod(DisplayName = "元素属性不影响片段提取")]
    public void TryExtractFragments_ElementWithAttributes_ReturnsSingleFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect Id=\"r1\" Width=\"100\" Height=\"50\"></Rect>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Rect Id=\"r1\" Width=\"100\" Height=\"50\"></Rect>", fragments[0]);
    }

    [TestMethod(DisplayName = "自闭合标签斜杠前有空格时返回单个片段")]
    public void TryExtractFragments_SelfClosingElementWithSpaceBeforeSlash_ReturnsSingleFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect Id=\"r1\" />");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Rect Id=\"r1\" />", fragments[0]);
    }

    [TestMethod(DisplayName = "两个完整片段连续出现时按顺序返回")]
    public void TryExtractFragments_TwoCompleteElements_ReturnsTwoFragments()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect/><TextElement Text=\"A\"/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(2, fragments);
        Assert.AreEqual("<Rect/>", fragments[0]);
        Assert.AreEqual("<TextElement Text=\"A\"/>", fragments[1]);
    }

    [TestMethod(DisplayName = "片段之间的空白被跳过且尾随空白保留在缓冲区")]
    public void TryExtractFragments_ElementsSeparatedByWhitespace_ReturnsFragmentsWithoutWhitespace()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("  <Rect/>\r\n  <TextElement Text=\"A\"/>  ");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(2, fragments);
        Assert.AreEqual("<Rect/>", fragments[0]);
        Assert.AreEqual("<TextElement Text=\"A\"/>", fragments[1]);
        Assert.AreEqual("  ", extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "片段消费后再次提取不会重复返回")]
    public void TryExtractFragments_AfterFragmentsConsumed_DoesNotReturnAgain()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect/>");

        var firstFragments = extractor.TryExtractFragments();
        var secondFragments = extractor.TryExtractFragments();

        Assert.HasCount(1, firstFragments);
        Assert.IsEmpty(secondFragments);
    }

    [TestMethod(DisplayName = "单个元素分两次追加后补齐时返回片段")]
    public void TryExtractFragments_ElementSplitAcrossAppends_ReturnsAfterComplete()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><Rect/>");

        var incompleteFragments = extractor.TryExtractFragments();

        Assert.IsEmpty(incompleteFragments);
        Assert.AreEqual("<Page><Rect/>", extractor.GetRemaining());

        extractor.Append("</Page>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><Rect/></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "标签名被拆分时补齐后返回片段")]
    public void TryExtractFragments_TagNameSplitAcrossAppends_ReturnsAfterComplete()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Pa");

        var incompleteFragments = extractor.TryExtractFragments();

        Assert.IsEmpty(incompleteFragments);
        Assert.AreEqual("<Pa", extractor.GetRemaining());

        extractor.Append("ge></Page>");
        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "属性值被拆分时补齐后返回片段")]
    public void TryExtractFragments_AttributeValueSplitAcrossAppends_ReturnsAfterComplete()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect Text=\"hel");

        var incompleteFragments = extractor.TryExtractFragments();

        Assert.IsEmpty(incompleteFragments);

        extractor.Append("lo\"/>");
        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Rect Text=\"hello\"/>", fragments[0]);
    }

    [TestMethod(DisplayName = "结束标签被拆分时补齐后返回片段")]
    public void TryExtractFragments_EndTagSplitAcrossAppends_ReturnsAfterComplete()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><Rect/></Pa");

        var incompleteFragments = extractor.TryExtractFragments();

        Assert.IsEmpty(incompleteFragments);

        Assert.AreEqual("<Page><Rect/></Pa", extractor.GetRemaining());

        extractor.Append("ge>");
        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><Rect/></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "多个 token 拼出多个片段时先返回已完成片段")]
    public void TryExtractFragments_MultipleFragmentsSplitAcrossAppends_ReturnsAvailableFragments()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect/><Text");

        var firstFragments = extractor.TryExtractFragments();

        Assert.HasCount(1, firstFragments);
        Assert.AreEqual("<Rect/>", firstFragments[0]);
        Assert.AreEqual("<Text", extractor.GetRemaining());

        extractor.Append("Element Text=\"A\"/>");
        var secondFragments = extractor.TryExtractFragments();

        Assert.HasCount(1, secondFragments);
        Assert.AreEqual("<TextElement Text=\"A\"/>", secondFragments[0]);
    }

    [TestMethod(DisplayName = "嵌套元素只返回外层顶级元素")]
    public void TryExtractFragments_NestedElements_ReturnsOuterElementOnly()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><Panel><Rect/></Panel></Page>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><Panel><Rect/></Panel></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "多层同名嵌套返回外层元素")]
    public void TryExtractFragments_NestedSameNameElements_ReturnsOuterElement()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Panel><Panel><Rect/></Panel></Panel>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Panel><Panel><Rect/></Panel></Panel>", fragments[0]);
    }

    [TestMethod(DisplayName = "顶层元素内兄弟子元素不影响提取")]
    public void TryExtractFragments_OuterElementWithSiblingChildren_ReturnsOuterElement()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><Rect/><TextElement Text=\"A\"/><Image Source=\"a.png\"/></Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><Rect/><TextElement Text=\"A\"/><Image Source=\"a.png\"/></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "标签名包含下划线点和短横线时返回片段")]
    public void TryExtractFragments_TagNameWithAllowedSpecialCharacters_ReturnsFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<a_b.c-d></a_b.c-d>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<a_b.c-d></a_b.c-d>", fragments[0]);
    }

    [TestMethod(DisplayName = "双引号属性中大于号不会提前结束标签")]
    public void TryExtractFragments_DoubleQuotedAttributeContainsGreaterThan_DoesNotEndTagEarly()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<TextElement Text=\"1 > 0\"/>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<TextElement Text=\"1 > 0\"/>", fragments[0]);
    }

    [TestMethod(DisplayName = "单引号属性中大于号不会提前结束标签")]
    public void TryExtractFragments_SingleQuotedAttributeContainsGreaterThan_DoesNotEndTagEarly()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<TextElement Text='1 > 0'/>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<TextElement Text='1 > 0'/>", fragments[0]);
    }

    [TestMethod(DisplayName = "属性中斜杠不会被识别为自闭合")]
    public void TryExtractFragments_AttributeContainsSlash_DoesNotTreatAsSelfClosing()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Image Source=\"http://example.com/a/b.png\"></Image>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Image Source=\"http://example.com/a/b.png\"></Image>", fragments[0]);
    }

    [TestMethod(DisplayName = "未闭合双引号属性返回空并保留缓冲")]
    public void TryExtractFragments_UnclosedDoubleQuotedAttribute_ReturnsEmptyAndKeepsRemaining()
    {
        var input = "<TextElement Text=\"abc>";
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append(input);

        var fragments = extractor.TryExtractFragments();
        Assert.IsEmpty(fragments);
        Assert.AreEqual(input, extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "未闭合单引号属性返回空并保留缓冲")]
    public void TryExtractFragments_UnclosedSingleQuotedAttribute_ReturnsEmptyAndKeepsRemaining()
    {
        var input = "<TextElement Text='abc>";
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append(input);

        var fragments = extractor.TryExtractFragments();
        Assert.IsEmpty(fragments);
        Assert.AreEqual(input, extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "元素内部注释不影响片段提取")]
    public void TryExtractFragments_ElementContainsComment_ReturnsFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><!-- comment --><Rect/></Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><!-- comment --><Rect/></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "注释中标签样式文本不参与匹配")]
    public void TryExtractFragments_CommentContainsTagLikeText_IgnoresCommentContent()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><!-- <Rect></Page> --><Rect/></Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><!-- <Rect></Page> --><Rect/></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "注释被流式拆分时补齐后返回片段")]
    public void TryExtractFragments_CommentSplitAcrossAppends_ReturnsAfterComplete()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><!-- comment");

        var incompleteFragments = extractor.TryExtractFragments();
        Assert.IsEmpty(incompleteFragments);
        Assert.AreEqual("<Page><!-- comment", extractor.GetRemaining());

        extractor.Append(" --><Rect/></Page>");
        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><!-- comment --><Rect/></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "顶层元素前处理指令被跳过并返回元素")]
    public void TryExtractFragments_ProcessingInstructionBeforeElement_ReturnsElement()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<?xml version=\"1.0\"?><Page></Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page></Page>", fragments[0]);
        Assert.AreEqual(string.Empty, extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "元素内部处理指令不影响片段提取")]
    public void TryExtractFragments_ElementContainsProcessingInstruction_ReturnsFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><?slide test?><Rect/></Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><?slide test?><Rect/></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "未完成处理指令返回空并保留缓冲")]
    public void TryExtractFragments_IncompleteProcessingInstruction_ReturnsEmptyAndKeepsRemaining()
    {
        var input = "<?xml version=\"1.0\"";
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append(input);

        var fragments = extractor.TryExtractFragments();
        Assert.IsEmpty(fragments);
        Assert.AreEqual(input, extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "CDATA 中标签样式文本不参与匹配")]
    public void TryExtractFragments_CDataContainsTagLikeText_IgnoresCDataContent()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<TextElement><![CDATA[<Rect></Page>]]></TextElement>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<TextElement><![CDATA[<Rect></Page>]]></TextElement>", fragments[0]);
    }

    [TestMethod(DisplayName = "CDATA 被流式拆分时补齐后返回片段")]
    public void TryExtractFragments_CDataSplitAcrossAppends_ReturnsAfterComplete()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<TextElement><![CDATA[abc");

        var incompleteFragments = extractor.TryExtractFragments();
        Assert.IsEmpty(incompleteFragments);
        Assert.AreEqual("<TextElement><![CDATA[abc", extractor.GetRemaining());

        extractor.Append("]]></TextElement>");
        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<TextElement><![CDATA[abc]]></TextElement>", fragments[0]);
    }

    [DataTestMethod(DisplayName = "不完整输入返回空并保留缓冲")]
    [DataRow("<")]
    [DataRow("<Page")]
    [DataRow("<Page></Pa")]
    [DataRow("<Page><Rect/>")]
    public void TryExtractFragments_IncompleteInput_ReturnsEmptyAndKeepsRemaining(string input)
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append(input);

        var fragments = extractor.TryExtractFragments();
        Assert.IsEmpty(fragments);
        Assert.AreEqual(input, extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "前导空白加不完整片段时保留全部残留内容")]
    public void TryExtractFragments_LeadingWhitespaceBeforeIncompleteElement_KeepsAllRemaining()
    {
        var input = "  <Page>";
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append(input);

        var fragments = extractor.TryExtractFragments();
        Assert.IsEmpty(fragments);
        Assert.AreEqual(input, extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "前导自然语言文本被跳过并返回后续元素")]
    public void TryExtractFragments_TextBeforeElement_SkipsTextAndReturnsElement()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("下面是生成内容：<Page></Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "片段之间自然语言文本不会阻止后续提取")]
    public void TryExtractFragments_TextBetweenElements_ReturnsBothElements()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect/>说明文字<TextElement Text=\"A\"/>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(2, fragments);
        Assert.AreEqual("<Rect/>", fragments[0]);
        Assert.AreEqual("<TextElement Text=\"A\"/>", fragments[1]);
    }

    [TestMethod(DisplayName = "只有自然语言文本时不提取片段并保留原始文本")]
    public void TryExtractFragments_TextWithoutElement_ReturnsEmptyAndKeepsOriginalText()
    {
        var input = "这里没有 XML";
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append(input);

        var fragments = extractor.TryExtractFragments();
        Assert.IsEmpty(fragments);
        Assert.AreEqual(input, extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "非法小于号序列不会阻止后续元素提取")]
    public void TryExtractFragments_InvalidLessThanSequenceBeforeElement_SkipsInvalidSequenceAndReturnsElement()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<1 invalid><Page></Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page></Page>", fragments[0]);
        Assert.AreEqual(string.Empty, extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "内层结束标签不匹配时返回非法片段供上层解析")]
    public void TryExtractFragments_MismatchedInnerEndTag_ReturnsInvalidFragmentForParser()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><Panel></Rect>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><Panel></Rect>", fragments[0]);
    }

    [TestMethod(DisplayName = "顶层结束标签不匹配时返回非法片段供上层解析")]
    public void TryExtractFragments_MismatchedTopLevelEndTag_ReturnsInvalidFragmentForParser()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page></Panel>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page></Panel>", fragments[0]);
    }

    [TestMethod(DisplayName = "外层结束标签自动弹出未闭合内层标签并返回片段")]
    public void TryExtractFragments_OuterEndTagAutoPopsUnclosedInnerTags_ReturnsOuterFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><Panel><Rect></Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><Panel><Rect></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "只有非法结束标签时不提取片段并保留原始结束标签")]
    public void TryExtractFragments_OnlyEndTag_ReturnsEmptyAndKeepsOriginalEndTag()
    {
        var input = "</Page>";
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append(input);

        var fragments = extractor.TryExtractFragments();
        Assert.IsEmpty(fragments);
        Assert.AreEqual(input, extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "完整片段后跟不完整片段时返回完整片段并保留不完整片段")]
    public void TryExtractFragments_CompleteThenIncomplete_ReturnsCompleteAndKeepsIncomplete()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect/><Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Rect/>", fragments[0]);
        Assert.AreEqual("<Page>", extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "完整片段后跟半个小于号时保留小于号")]
    public void TryExtractFragments_CompleteThenTrailingLessThan_ReturnsCompleteAndKeepsLessThan()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect/><");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Rect/>", fragments[0]);
        Assert.AreEqual("<", extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "不完整片段补齐后继续提取后续完整片段")]
    public void TryExtractFragments_IncompleteThenCompletedWithNextFragment_ReturnsBothInOrder()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><Rect/>");

        var incompleteFragments = extractor.TryExtractFragments();
        Assert.IsEmpty(incompleteFragments);

        extractor.Append("</Page><TextElement Text=\"A\"/>");
        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(2, fragments);
        Assert.AreEqual("<Page><Rect/></Page>", fragments[0]);
        Assert.AreEqual("<TextElement Text=\"A\"/>", fragments[1]);
    }

    [TestMethod(DisplayName = "片段后跟空白时返回片段并保留尾随空白")]
    public void TryExtractFragments_FragmentFollowedByWhitespace_ReturnsFragmentAndKeepsTrailingWhitespace()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect/>   ");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Rect/>", fragments[0]);
        Assert.AreEqual("   ", extractor.GetRemaining());
    }

    [TestMethod(DisplayName = "开始与结束标签大小写不同也返回片段")]
    public void TryExtractFragments_EndTagDifferentCase_ReturnsFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page></page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page></page>", fragments[0]);
    }

    [TestMethod(DisplayName = "嵌套结束标签大小写不同也返回外层片段")]
    public void TryExtractFragments_NestedEndTagDifferentCase_ReturnsOuterFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><Panel></panel></PAGE>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page><Panel></panel></PAGE>", fragments[0]);
    }

    [TestMethod(DisplayName = "DOCTYPE 样式内容被跳过并返回后续元素")]
    public void TryExtractFragments_DoctypeBeforeElement_SkipsUnsupportedDeclarationAndReturnsElement()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<!DOCTYPE Page><Page></Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "顶层 CDATA 被跳过并返回后续元素")]
    public void TryExtractFragments_TopLevelCDataBeforeElement_SkipsCDataAndReturnsElement()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<![CDATA[text]]><Page></Page>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page></Page>", fragments[0]);
    }

    [TestMethod(DisplayName = "XML 实体文本不影响片段提取")]
    public void TryExtractFragments_ElementContainsXmlEntityText_ReturnsFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<TextElement Text=\"A &amp; B\"></TextElement>");

        var fragments = extractor.TryExtractFragments();
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<TextElement Text=\"A &amp; B\"></TextElement>", fragments[0]);
    }
}
