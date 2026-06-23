using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserGradientTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_LinearGradient_BasicAttributes()
    {
        var context = CreateContext();
        var xml = "<Page><Rect><Fill><LinearGradient X1=\"0\" Y1=\"0\" X2=\"1\" Y2=\"1\"><Stop Offset=\"0\" Color=\"#FF0000\"/><Stop Offset=\"1\" Color=\"#00FF00\"/></LinearGradient></Fill></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(rect.Fill);
        var gradient = (SlideMlLinearGradientBrush)rect.Fill!;
        Assert.AreEqual(0, gradient.X1);
        Assert.AreEqual(0, gradient.Y1);
        Assert.AreEqual(1, gradient.X2);
        Assert.AreEqual(1, gradient.Y2);
        Assert.AreEqual(2, gradient.Stops.Count);
    }

    [TestMethod]
    public void Parse_LinearGradient_DefaultDirection()
    {
        var context = CreateContext();
        var xml = "<Page><Rect><Fill><LinearGradient><Stop Offset=\"0\" Color=\"#000\"/><Stop Offset=\"1\" Color=\"#FFF\"/></LinearGradient></Fill></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(rect.Fill);
        var gradient = (SlideMlLinearGradientBrush)rect.Fill!;
        Assert.AreEqual(0, gradient.X1);
        Assert.AreEqual(0, gradient.Y1);
        Assert.AreEqual(1, gradient.X2);
        Assert.AreEqual(0, gradient.Y2);
    }

    [TestMethod]
    public void Parse_LinearGradient_StopMissingOffset_GeneratesWarning()
    {
        var context = CreateContext();
        var xml = "<Page><Rect><Fill><LinearGradient><Stop Color=\"#000\"/><Stop Offset=\"1\" Color=\"#FFF\"/></LinearGradient></Fill></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsTrue(
            context.Warnings.Any(w => w.Contains("Stop") && w.Contains("缺少 Offset")),
            $"警告应包含 Stop 缺少 Offset，实际: {string.Join("; ", context.Warnings)}");

        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(rect.Fill);
        var gradient = (SlideMlLinearGradientBrush)rect.Fill!;
        Assert.AreEqual(1, gradient.Stops.Count);
    }

    [TestMethod]
    public void Parse_LinearGradient_StopMissingColor_GeneratesWarning()
    {
        var context = CreateContext();
        var xml = "<Page><Rect><Fill><LinearGradient><Stop Offset=\"0\"/><Stop Offset=\"1\" Color=\"#FFF\"/></LinearGradient></Fill></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsTrue(
            context.Warnings.Any(w => w.Contains("Stop") && w.Contains("缺少 Offset 或 Color")),
            $"警告应包含 Stop 缺少 Offset 或 Color，实际: {string.Join("; ", context.Warnings)}");

        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(rect.Fill);
        var gradient = (SlideMlLinearGradientBrush)rect.Fill!;
        Assert.AreEqual(1, gradient.Stops.Count);
    }

    [TestMethod]
    public void Parse_LinearGradient_NoValidStops_GeneratesWarning_ReturnsNull()
    {
        var context = CreateContext();
        var xml = "<Page><Rect><Fill><LinearGradient></LinearGradient></Fill></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsTrue(
            context.Warnings.Any(w => w.Contains("不包含有效 Stop")),
            $"警告应包含不包含有效 Stop，实际: {string.Join("; ", context.Warnings)}");
        Assert.IsNull(rect.Fill);
    }

    [TestMethod]
    public void Parse_LinearGradient_StopOffsetOutOfRange_Clamped()
    {
        var context = CreateContext();
        var xml = "<Page><Rect><Fill><LinearGradient><Stop Offset=\"-0.5\" Color=\"#000\"/><Stop Offset=\"1.5\" Color=\"#FFF\"/></LinearGradient></Fill></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(rect.Fill);
        var gradient = (SlideMlLinearGradientBrush)rect.Fill!;
        Assert.AreEqual(0, gradient.Stops[0].Offset);
        Assert.AreEqual(1, gradient.Stops[1].Offset);
    }
}
