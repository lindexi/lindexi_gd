using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserPageStylesTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_PageStyles_TextStyle_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Page.Styles><TextStyle Id=\"title\" FontSize=\"24\" IsBold=\"True\" Foreground=\"#333\" FontName=\"Arial\" TextAlignment=\"Center\"/></Page.Styles></Page>";

        var page = _parser.Parse(xml, context);

        Assert.IsNotNull(page.Styles);
        Assert.HasCount(1, page.Styles!);

        var style = page.Styles[0];
        Assert.AreEqual("title", style.Id);
        Assert.AreEqual(24, style.FontSize);
        Assert.IsTrue(style.IsBold);
        Assert.AreEqual("#333", style.Foreground);
        Assert.AreEqual("Arial", style.FontName);
        Assert.AreEqual(SlideMlTextAlignment.Center, style.TextAlignment);
    }

    [TestMethod]
    public void Parse_PageStyles_TextStyleMissingId_GeneratesWarning()
    {
        var context = CreateContext();
        var xml = "<Page><Page.Styles><TextStyle FontSize=\"24\"/></Page.Styles></Page>";

        var page = _parser.Parse(xml, context);

        Assert.IsTrue(
            context.Warnings.Any(w => w.Contains("TextStyle") && w.Contains("缺少 Id 属性，已忽略")),
            $"警告应包含 TextStyle 缺少 Id 属性，已忽略，实际: {string.Join("; ", context.Warnings)}");
        Assert.IsTrue(page.Styles is null || page.Styles.Count == 0);
    }
}
