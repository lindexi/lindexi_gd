using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserRootTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_PageRoot_ReturnsPage()
    {
        var context = CreateContext();
        var xml = "<Page Background=\"#FFFFFF\"></Page>";

        var page = _parser.Parse(xml, context);

        Assert.AreEqual("#FFFFFF", page.Background);
        Assert.IsEmpty(page.Children);
    }

    [TestMethod]
    public void Parse_NonPageRoot_ThrowsRootElementException()
    {
        var context = CreateContext();
        var xml = "<NotPage></NotPage>";

        var exception = Assert.ThrowsExactly<SlideMlRootElementException>(
            () => _parser.Parse(xml, context));

        Assert.Contains("根元素", exception.Message, $"消息应包含\"根元素\"，实际: {exception.Message}");
    }

    [TestMethod]
    public void Parse_EmptyPage_ReturnsEmptyPage()
    {
        var context = CreateContext();
        var xml = "<Page></Page>";

        var page = _parser.Parse(xml, context);

        Assert.AreEqual("#FFFFFF", page.Background);
        Assert.IsEmpty(page.Children);
    }

    [TestMethod]
    public void Parse_PageWithBackground_BackgroundParsed()
    {
        var context = CreateContext();
        var xml = "<Page Background=\"#1A1A2E\"></Page>";

        var page = _parser.Parse(xml, context);

        Assert.AreEqual("#1A1A2E", page.Background);
    }

    [TestMethod]
    public void Parse_MalformedXml_ThrowsXmlException()
    {
        var context = CreateContext();
        var xml = "<Page><Rect></Page>";

        Assert.ThrowsExactly<System.Xml.XmlException>(() => _parser.Parse(xml, context));
    }

    [TestMethod]
    public void Parse_EmptyString_ThrowsXmlException()
    {
        var context = CreateContext();
        var xml = "";

        Assert.ThrowsExactly<System.Xml.XmlException>(() => _parser.Parse(xml, context));
    }

    [TestMethod]
    public void Parse_PageWithUnknownAttribute_GeneratesWarning()
    {
        var context = CreateContext();
        var xml = "<Page Foo=\"bar\"></Page>";

        var page = _parser.Parse(xml, context);

        Assert.HasCount(1, context.Warnings);
        Assert.Contains("未知属性", context.Warnings[0]);
        Assert.Contains("Foo", context.Warnings[0],
            $"警告应包含未知属性 Foo，实际: {context.Warnings[0]}");
    }

    [TestMethod]
    public void Parse_PageWithId_IdPreserved_NoWarnings()
    {
        var context = CreateContext();
        var xml = "<Page Id=\"my-page\" Background=\"#1A1A2E\"></Page>";

        var page = _parser.Parse(xml, context);

        Assert.AreEqual("my-page", page.Id);
        Assert.AreEqual("#1A1A2E", page.Background);
        Assert.IsEmpty(context.Warnings);
        Assert.IsEmpty(context.Errors);
    }

    [TestMethod]
    public void Parse_PageWithoutId_AutoAssignedId()
    {
        var context = CreateContext();
        var xml = "<Page></Page>";

        var page = _parser.Parse(xml, context);

        Assert.IsFalse(string.IsNullOrEmpty(page.Id));
        Assert.IsTrue(
            System.Text.RegularExpressions.Regex.IsMatch(page.Id, @"^elem_\d{3}$"),
            $"Page Id 应匹配 elem_\\d{{3}} 格式，实际: {page.Id}");
    }

    [TestMethod]
    public void Parse_PageWithId_DoesNotConsumeChildIdCounter()
    {
        var context = CreateContext();
        var xml = "<Page Id=\"page-1\"><Rect/><TextElement Text=\"hi\"/></Page>";

        var page = _parser.Parse(xml, context);

        Assert.AreEqual("page-1", page.Id);
        Assert.AreEqual("elem_001", page.Children[0].Id);
        Assert.AreEqual("elem_002", page.Children[1].Id);
    }

    [TestMethod(DisplayName = "解析 Page 下非法 Fill 子元素不生成误导性元素 Id")]
    public void Parse_PageWithFillChild_GeneratesStructuredChildWarningWithoutAutoId()
    {
        var context = CreateContext();
        var xml = "<Page Id=\"page-1\"><Fill><LinearGradient><Stop Offset=\"0\" Color=\"#000000\"/><Stop Offset=\"1\" Color=\"#FFFFFF\"/></LinearGradient></Fill></Page>";

        var page = _parser.Parse(xml, context);

        Assert.IsEmpty(page.Children);
        Assert.HasCount(1, context.Warnings);
        Assert.Contains("Page", context.Warnings[0]);
        Assert.Contains("Fill", context.Warnings[0]);
        Assert.Contains("只能用于 Panel 或 Rect", context.Warnings[0]);
        Assert.DoesNotContain("elem_", context.Warnings[0]);
    }

    [TestMethod(DisplayName = "解析 Page 下非法 Span 子元素不生成误导性元素 Id")]
    public void Parse_PageWithSpanChild_GeneratesStructuredChildWarningWithoutAutoId()
    {
        var context = CreateContext();
        var xml = "<Page Id=\"page-1\"><Span Text=\"A\"/></Page>";

        var page = _parser.Parse(xml, context);

        Assert.IsEmpty(page.Children);
        Assert.HasCount(1, context.Warnings);
        Assert.Contains("Page", context.Warnings[0]);
        Assert.Contains("Span", context.Warnings[0]);
        Assert.Contains("只能用于 TextElement", context.Warnings[0]);
        Assert.DoesNotContain("elem_", context.Warnings[0]);
    }

    [TestMethod]
    public void Parse_PageWithoutId_ConsumesIdCounter()
    {
        var context = CreateContext();
        var xml = "<Page><Rect/></Page>";

        var page = _parser.Parse(xml, context);

        Assert.IsTrue(page.Id.StartsWith("elem_", StringComparison.Ordinal));
        Assert.AreNotEqual(
            page.Id,
            page.Children[0].Id,
            "子元素 Id 不应与 Page 自动分配的 Id 相同");
    }
}
