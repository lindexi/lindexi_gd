using System.Text.RegularExpressions;
using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserIdGenerationTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_NoId_AutoAssigned()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Width=\"100\" Height=\"50\"/></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsTrue(
            Regex.IsMatch(rect.Id, @"^elem_\d{3}$"),
            $"Id 应匹配 elem_\\d{{3}} 格式，实际: {rect.Id}");
    }

    [TestMethod]
    public void Parse_MultipleNoId_IncrementingIds()
    {
        var context = CreateContext();
        var xml = "<Page><Rect/><Rect/><Rect/></Page>";

        var page = _parser.Parse(xml, context);

        Assert.AreEqual("elem_002", page.Children[0].Id);
        Assert.AreEqual("elem_003", page.Children[1].Id);
        Assert.AreEqual("elem_004", page.Children[2].Id);
    }

    [TestMethod]
    public void Parse_WithId_IdPreserved()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Id=\"my-card\"/></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.AreEqual("my-card", rect.Id);
    }

    [TestMethod]
    public void Parse_MixedIdAndNoId_CorrectAssignment()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Id=\"named\"/><Rect/><TextElement Text=\"hi\"/></Page>";

        var page = _parser.Parse(xml, context);

        Assert.AreEqual("named", page.Children[0].Id);
        Assert.AreEqual("elem_002", page.Children[1].Id);
        Assert.AreEqual("elem_003", page.Children[2].Id);
    }
}
