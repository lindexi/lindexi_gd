using System.Xml;
using PptxGenerator.Models;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlXmlUtilitiesNormalizeXmlTests
{
    [TestMethod]
    public void NormalizeXml_BasicXml_Normalized()
    {
        var input = "<Page><Rect Width=\"100\"/></Page>";

        var result = SlideMlXmlUtilities.NormalizeXml(input);

        Assert.Contains("<Page>", result);
        Assert.Contains("<Rect Width=\"100\" />", result);
        Assert.Contains("</Page>", result);
    }

    [TestMethod]
    public void NormalizeXml_AlreadyNormalized_Unchanged()
    {
        var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Page>\n  <Rect Width=\"100\" />\n</Page>";

        var result = SlideMlXmlUtilities.NormalizeXml(input);

        Assert.Contains("<Page>", result);
        Assert.Contains("<Rect Width=\"100\" />", result);
        Assert.Contains("</Page>", result);
    }

    [TestMethod]
    public void NormalizeXml_WithComment_Preserved()
    {
        var input = "<Page><!-- comment --><Rect Width=\"100\"/></Page>";

        var result = SlideMlXmlUtilities.NormalizeXml(input);

        Assert.Contains("<!-- comment -->", result);
    }

    [TestMethod]
    public void NormalizeXml_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => SlideMlXmlUtilities.NormalizeXml(null!));
    }

    [TestMethod]
    public void NormalizeXml_MalformedXml_ThrowsXmlException()
    {
        var input = "<Page><Rect></Page>";

        Assert.ThrowsExactly<XmlException>(() => SlideMlXmlUtilities.NormalizeXml(input));
    }
}
