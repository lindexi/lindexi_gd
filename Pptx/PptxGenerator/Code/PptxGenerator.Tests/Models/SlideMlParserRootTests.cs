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
}
