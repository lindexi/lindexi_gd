using PptxGenerator.Streaming;

namespace PptxGenerator.Tests.Streaming;

[TestClass]
public sealed class SlideMlFragmentExtractorTests
{
    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
    public void Append_SelfClosingTag_ReturnsImmediately()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Rect Id=\"bg\" Width=\"100\"/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Rect Id=\"bg\" Width=\"100\"/>", fragments[0]);
    }

    [TestMethod]
    public void Append_MultipleFragments_ReturnsAll()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page><Panel Id=\"a\"/></Page><Rect Id=\"b\"/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(2, fragments);
        Assert.AreEqual("<Page><Panel Id=\"a\"/></Page>", fragments[0]);
        Assert.AreEqual("<Rect Id=\"b\"/>", fragments[1]);
    }

    [TestMethod]
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
        extractor.Append("<Panel Id=\"MainContainer\"><Rect Id=\"OverlayShape\"/></MainContainer>");

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Panel Id=\"MainContainer\"><Rect Id=\"OverlayShape\"/></MainContainer>", fragments[0]);
        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod]
    public void Append_XmlDeclaration_NotTreatedAsFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<?xml version=\"1.0\"?><Page/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page/>", fragments[0]);
    }

    [TestMethod]
    public void Append_Comment_NotTreatedAsFragment()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<!-- comment --><Page/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page/>", fragments[0]);
    }

    [TestMethod]
    public void Append_NestedElements_ReturnsWhenOuterCloses()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Panel Id=\"outer\"><Panel Id=\"inner\"/></Panel>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Panel Id=\"outer\"><Panel Id=\"inner\"/></Panel>", fragments[0]);
    }

    [TestMethod]
    public void Append_TextBetweenFragments_IgnoredOrPreserved()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page/>\n<Rect Id=\"r\"/>");

        var fragments = extractor.TryExtractFragments();

        Assert.HasCount(2, fragments);
        Assert.AreEqual("<Page/>", fragments[0]);
        Assert.AreEqual("<Rect Id=\"r\"/>", fragments[1]);
    }

    [TestMethod]
    public void GetRemaining_AfterExtraction_ReturnsEmpty()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append("<Page/>");

        extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod]
    public void Append_EmptyString_NoEffect()
    {
        var extractor = new SlideMlFragmentExtractor();
        extractor.Append(string.Empty);

        var fragments = extractor.TryExtractFragments();
        var remaining = extractor.GetRemaining();

        Assert.IsEmpty(fragments);
        Assert.AreEqual(string.Empty, remaining);
    }

    [TestMethod]
    public void Append_Null_ThrowsArgumentNullException()
    {
        var extractor = new SlideMlFragmentExtractor();

        Assert.ThrowsExactly<ArgumentNullException>(() => extractor.Append(null!));
    }
}
