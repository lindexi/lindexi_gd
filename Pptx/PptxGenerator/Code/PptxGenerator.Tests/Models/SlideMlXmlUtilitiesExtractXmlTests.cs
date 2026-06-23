using System.Xml;
using PptxGenerator.Models;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlXmlUtilitiesExtractXmlTests
{
    [TestMethod]
    public void ExtractXml_WithXmlDeclaration_Extracted()
    {
        var input = "Some text\n<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Page></Page>";

        var result = SlideMlXmlUtilities.ExtractXml(input);

        Assert.AreEqual("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Page></Page>", result);
    }

    [TestMethod]
    public void ExtractXml_StartsWithPage_DeclarationAdded()
    {
        var input = "<Page Background=\"#FFF\"></Page>";

        var result = SlideMlXmlUtilities.ExtractXml(input);

        Assert.AreEqual("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Page Background=\"#FFF\"></Page>", result);
    }

    [TestMethod]
    public void ExtractXml_MarkdownCodeBlock_XmlExtracted()
    {
        // 实现从 <Page 开始截取到文本末尾并 Trim，不会主动去除尾部的 markdown 闭合标记
        var input = "```xml\n<Page></Page>\n```";

        var result = SlideMlXmlUtilities.ExtractXml(input);

        Assert.IsTrue(result.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"));
        Assert.IsTrue(result.Contains("<Page></Page>"));
    }

    [TestMethod]
    public void ExtractXml_LeadingTrailingText_Extracted()
    {
        // 实现从 <Page 开始截取到文本末尾并 Trim，不会去除尾部的非 XML 文本
        var input = "Here is the slide:\n<Page><Rect Width=\"100\" Height=\"50\"/></Page>\n---END---";

        var result = SlideMlXmlUtilities.ExtractXml(input);

        Assert.IsTrue(result.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>"));
        Assert.IsTrue(result.Contains("<Page><Rect Width=\"100\" Height=\"50\"/></Page>"));
        // 前导文本被去除
        Assert.IsFalse(result.Contains("Here is the slide"));
    }

    [TestMethod]
    public void ExtractXml_PureXml_Unchanged()
    {
        var input = "<Page></Page>";

        var result = SlideMlXmlUtilities.ExtractXml(input);

        Assert.AreEqual("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Page></Page>", result);
    }

    [TestMethod]
    public void ExtractXml_EmptyString_ReturnsEmpty()
    {
        var result = SlideMlXmlUtilities.ExtractXml("");

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void ExtractXml_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => SlideMlXmlUtilities.ExtractXml(null!));
    }

    [TestMethod]
    public void ExtractXml_PlainText_ReturnsTrimmed()
    {
        var input = "这是一段纯文本";

        var result = SlideMlXmlUtilities.ExtractXml(input);

        Assert.AreEqual("这是一段纯文本", result);
    }
}
