using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserMarginTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_Margin_FourValues_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Margin=\"10,20,30,40\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.Margin);
        Assert.AreEqual(10, rect.Margin!.Value.Left);
        Assert.AreEqual(20, rect.Margin.Value.Top);
        Assert.AreEqual(30, rect.Margin.Value.Right);
        Assert.AreEqual(40, rect.Margin.Value.Bottom);
    }

    [TestMethod]
    public void Parse_Margin_SingleValue_AllSidesEqual()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Margin=\"8\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.Margin);
        Assert.AreEqual(8, rect.Margin!.Value.Left);
        Assert.AreEqual(8, rect.Margin.Value.Top);
        Assert.AreEqual(8, rect.Margin.Value.Right);
        Assert.AreEqual(8, rect.Margin.Value.Bottom);
    }

    [TestMethod]
    public void Parse_Margin_TwoValues_VerticalHorizontal()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Margin=\"10,20\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.Margin);
        Assert.AreEqual(20, rect.Margin!.Value.Left);
        Assert.AreEqual(10, rect.Margin.Value.Top);
        Assert.AreEqual(20, rect.Margin.Value.Right);
        Assert.AreEqual(10, rect.Margin.Value.Bottom);
    }

    [TestMethod]
    public void Parse_Margin_ThreeValues()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Margin=\"10,20,30\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.Margin);
        Assert.AreEqual(20, rect.Margin!.Value.Left);
        Assert.AreEqual(10, rect.Margin.Value.Top);
        Assert.AreEqual(20, rect.Margin.Value.Right);
        Assert.AreEqual(30, rect.Margin.Value.Bottom);
    }

    [TestMethod]
    public void Parse_Margin_InvalidFormat_GeneratesError()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Margin=\"abc\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.AreEqual(1, context.Errors.Count);
        Assert.IsTrue(
            context.Errors[0].Contains("不是有效的间距格式"),
            $"错误应包含\"不是有效的间距格式\"，实际: {context.Errors[0]}");
        Assert.IsNull(rect.Margin);
    }
}
