using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserNestingTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_NestedPanels_StructureCorrect()
    {
        var context = CreateContext();
        var xml = "<Page><Panel Id=\"outer\"><Panel Id=\"inner\"><Rect Id=\"leaf\"/></Panel></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var outer = (SlideMlPanelElement)page.Children[0];
        var inner = (SlideMlPanelElement)outer.Children[0];
        var leaf = (SlideMlRectElement)inner.Children[0];

        Assert.AreEqual("outer", outer.Id);
        Assert.AreEqual("inner", inner.Id);
        Assert.AreEqual("leaf", leaf.Id);
    }

    [TestMethod]
    public void Parse_Panel_MixedChildren_AllParsed()
    {
        var context = CreateContext();
        var xml = "<Page><Panel><Rect/><TextElement Text=\"hi\"/><Image Source=\"img\"/><Panel/></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.HasCount(4, panel.Children);
        Assert.IsInstanceOfType<SlideMlRectElement>(panel.Children[0]);
        Assert.IsInstanceOfType<SlideMlTextElement>(panel.Children[1]);
        Assert.IsInstanceOfType<SlideMlImageElement>(panel.Children[2]);
        Assert.IsInstanceOfType<SlideMlPanelElement>(panel.Children[3]);
    }

    [TestMethod]
    public void Parse_DeepNesting_AllParsed()
    {
        var context = CreateContext();
        var xml = "<Page><Panel><Panel><Panel><Panel><Panel><Rect Id=\"deep\" Width=\"42\" Height=\"7\"/></Panel></Panel></Panel></Panel></Panel></Page>";

        var page = _parser.Parse(xml, context);

        var current = page.Children[0];
        for (var depth = 0; depth < 5; depth++)
        {
            Assert.IsInstanceOfType<SlideMlPanelElement>(current);
            current = ((SlideMlPanelElement)current).Children[0];
        }

        var leaf = (SlideMlRectElement)current;
        Assert.AreEqual("deep", leaf.Id);
        Assert.AreEqual(42, leaf.Width);
        Assert.AreEqual(7, leaf.Height);
    }
}
