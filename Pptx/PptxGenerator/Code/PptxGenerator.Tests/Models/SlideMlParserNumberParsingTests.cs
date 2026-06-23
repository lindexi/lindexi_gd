using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserNumberParsingTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_DecimalCoordinates_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect X=\"10.5\" Y=\"20.3\" Width=\"100.7\" Height=\"50.2\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.AreEqual(10.5, rect.X);
        Assert.AreEqual(20.3, rect.Y);
        Assert.AreEqual(100.7, rect.Width);
        Assert.AreEqual(50.2, rect.Height);
    }

    [TestMethod]
    public void Parse_NegativeCoordinates_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect X=\"-10\" Y=\"-20\" Width=\"100\" Height=\"50\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.AreEqual(-10, rect.X);
        Assert.AreEqual(-20, rect.Y);
    }

    [TestMethod]
    public void Parse_ZeroValues_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect X=\"0\" Y=\"0\" Width=\"0\" Height=\"0\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.AreEqual(0, rect.X);
        Assert.AreEqual(0, rect.Y);
        Assert.AreEqual(0, rect.Width);
        Assert.AreEqual(0, rect.Height);
    }

    [TestMethod]
    public void Parse_ValuesWithSpaces_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect X=\" 10 \" Y=\" 20 \" Width=\" 100 \" Height=\" 50 \"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.AreEqual(10, rect.X);
        Assert.AreEqual(20, rect.Y);
        Assert.AreEqual(100, rect.Width);
        Assert.AreEqual(50, rect.Height);
    }

    [TestMethod]
    public void Parse_InvalidNumber_GeneratesError()
    {
        var context = CreateContext();
        var xml = "<Page><Rect X=\"abc\" Width=\"100\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.AreEqual(1, context.Errors.Count);
        Assert.IsTrue(
            context.Errors[0].Contains("X") && context.Errors[0].Contains("abc") && context.Errors[0].Contains("不是有效的数值"),
            $"错误应包含 X 值 abc 不是有效的数值，实际: {context.Errors[0]}");
        Assert.IsNull(rect.X);
    }

    [TestMethod]
    public void Parse_DecimalWithInvariantCulture()
    {
        var context = CreateContext();
        var xml = "<Page><Rect X=\"10.5\" Width=\"100.5\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.AreEqual(10.5, rect.X);
        Assert.AreEqual(100.5, rect.Width);
    }
}
