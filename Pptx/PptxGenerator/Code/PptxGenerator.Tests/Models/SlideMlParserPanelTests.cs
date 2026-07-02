using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserPanelTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_Panel_AllAttributes_Parsed()
    {
        var context = CreateContext();
        var xml = "<Panel Id=\"p1\" X=\"10\" Y=\"20\" Width=\"100\" Height=\"80\" Padding=\"8\" Background=\"#FF0000\" Layout=\"Horizontal\" Gap=\"12\" Opacity=\"0.5\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Top\" Margin=\"0,0,0,8\"></Panel>";

        var page = _parser.Parse($"<Page>{xml}</Page>", context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.AreEqual("p1", panel.Id);
        Assert.AreEqual(10, panel.X);
        Assert.AreEqual(20, panel.Y);
        Assert.AreEqual(100, panel.Width);
        Assert.AreEqual(80, panel.Height);
        Assert.AreEqual(8, panel.Padding.Left);
        Assert.AreEqual(8, panel.Padding.Top);
        Assert.AreEqual(8, panel.Padding.Right);
        Assert.AreEqual(8, panel.Padding.Bottom);
        Assert.IsInstanceOfType<SlideMlSolidColorBrush>(panel.Background);
        Assert.AreEqual("#FF0000", ((SlideMlSolidColorBrush)panel.Background!).Color);
        Assert.AreEqual(SlideMlLayoutDirection.Horizontal, panel.Layout);
        Assert.AreEqual(12, panel.Gap);
        Assert.AreEqual(0.5, panel.Opacity);
        Assert.AreEqual(SlideMlHorizontalAlignment.Center, panel.HorizontalAlignment);
        Assert.AreEqual(SlideMlVerticalAlignment.Top, panel.VerticalAlignment);
        Assert.IsNotNull(panel.Margin);
        Assert.AreEqual(0, panel.Margin!.Value.Left);
        Assert.AreEqual(0, panel.Margin.Value.Top);
        Assert.AreEqual(0, panel.Margin.Value.Right);
        Assert.AreEqual(8, panel.Margin.Value.Bottom);
    }

    [TestMethod]
    public void Parse_Panel_DefaultValues()
    {
        var context = CreateContext();
        var xml = "<Page><Panel></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.AreEqual(SlideMlLayoutDirection.Absolute, panel.Layout);
        Assert.AreEqual(0, panel.Gap);
        Assert.AreEqual(0, panel.Padding.Left);
        Assert.AreEqual(0, panel.Padding.Top);
        Assert.AreEqual(0, panel.Padding.Right);
        Assert.AreEqual(0, panel.Padding.Bottom);
        Assert.AreEqual(1, panel.Opacity);
        Assert.IsNull(panel.Background);
    }

    [TestMethod]
    public void Parse_Panel_WithGradientFill_BackgroundIsGradient()
    {
        var context = CreateContext();
        var xml = "<Page><Panel><Fill><LinearGradient X1=\"0\" Y1=\"0\" X2=\"1\" Y2=\"1\"><Stop Offset=\"0\" Color=\"#FF0000\"/><Stop Offset=\"1\" Color=\"#00FF00\"/></LinearGradient></Fill></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(panel.Background);
        var gradient = (SlideMlLinearGradientBrush)panel.Background!;
        Assert.HasCount(2, gradient.Stops);
        Assert.AreEqual(0, gradient.Stops[0].Offset);
        Assert.AreEqual("#FF0000", gradient.Stops[0].Color);
        Assert.AreEqual(1, gradient.Stops[1].Offset);
        Assert.AreEqual("#00FF00", gradient.Stops[1].Color);
        Assert.AreEqual(0, gradient.X1);
        Assert.AreEqual(0, gradient.Y1);
        Assert.AreEqual(1, gradient.X2);
        Assert.AreEqual(1, gradient.Y2);
    }

    [TestMethod(DisplayName = "解析 Panel 的 Fill 子元素不会产生未知标签警告")]
    public void Parse_Panel_WithGradientFill_DoesNotGenerateUnknownWarnings()
    {
        var context = CreateContext();
        var xml = "<Page><Panel><Fill><LinearGradient><Stop Offset=\"0\" Color=\"#000000\"/><Stop Offset=\"1\" Color=\"#FFFFFF\"/></LinearGradient></Fill><TextElement Text=\"内容\"/></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.HasCount(1, panel.Children);
        Assert.IsFalse(
            context.Warnings.Any(w => w.Contains("未知标签") || w.Contains("未知属性")),
            $"结构化 Fill 子元素不应产生未知警告，实际: {string.Join("; ", context.Warnings)}");
    }

    [TestMethod]
    public void Parse_Panel_GradientFillOverridesBackgroundAttribute()
    {
        var context = CreateContext();
        var xml = "<Page><Panel Background=\"#FF0000\"><Fill><LinearGradient><Stop Offset=\"0\" Color=\"#000000\"/><Stop Offset=\"1\" Color=\"#FFFFFF\"/></LinearGradient></Fill></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(panel.Background);
    }

    [TestMethod]
    public void Parse_Panel_UnknownAttribute_GeneratesWarning()
    {
        var context = CreateContext();
        var xml = "<Page><Panel Foo=\"bar\"></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.HasCount(1, context.Warnings);
        Assert.IsTrue(
            context.Warnings[0].Contains("未知属性") && context.Warnings[0].Contains("Foo"),
            $"警告应包含未知属性 Foo，实际: {context.Warnings[0]}");
    }

    [TestMethod]
    public void Parse_Panel_UnknownChildTag_GeneratesWarning()
    {
        var context = CreateContext();
        var xml = "<Page><Panel><UnknownTag/></Panel></Page>";

        var page = _parser.Parse(xml, context);

        Assert.IsTrue(
            context.Warnings.Any(w => w.Contains("未知标签") && w.Contains("UnknownTag")),
            $"警告应包含未知标签 UnknownTag，实际: {string.Join("; ", context.Warnings)}");
    }

    [TestMethod]
    public void Parse_Panel_InvalidLayout_GeneratesError()
    {
        var context = CreateContext();
        var xml = "<Page><Panel Layout=\"Diagonal\"></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.HasCount(1, context.Errors);
        Assert.IsTrue(
            context.Errors[0].Contains("Layout") && context.Errors[0].Contains("Diagonal") && context.Errors[0].Contains("无效"),
            $"错误应包含 Layout 值 Diagonal 无效，实际: {context.Errors[0]}");
        Assert.AreEqual(SlideMlLayoutDirection.Absolute, panel.Layout);
    }

    [TestMethod]
    public void Parse_Panel_InvalidPadding_GeneratesError()
    {
        var context = CreateContext();
        var xml = "<Page><Panel Padding=\"abc\"></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.HasCount(1, context.Errors);
        Assert.IsTrue(
            context.Errors[0].Contains("Padding") && context.Errors[0].Contains("abc") && context.Errors[0].Contains("不是有效的间距格式"),
            $"错误应包含 Padding 值 abc 不是有效的间距格式，实际: {context.Errors[0]}");
        Assert.AreEqual(0, panel.Padding.Left);
        Assert.AreEqual(0, panel.Padding.Top);
        Assert.AreEqual(0, panel.Padding.Right);
        Assert.AreEqual(0, panel.Padding.Bottom);
    }

    [TestMethod(DisplayName = "解析 Panel 多值 Padding 逗号分隔")]
    public void Parse_Panel_MultiValuePadding_CommaSeparated()
    {
        var context = CreateContext();
        var xml = "<Page><Panel Padding=\"8,16,24,32\"></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.AreEqual(8, panel.Padding.Left);
        Assert.AreEqual(16, panel.Padding.Top);
        Assert.AreEqual(24, panel.Padding.Right);
        Assert.AreEqual(32, panel.Padding.Bottom);
    }

    [TestMethod(DisplayName = "解析 Panel 多值 Padding 空格分隔")]
    public void Parse_Panel_MultiValuePadding_SpaceSeparated()
    {
        var context = CreateContext();
        var xml = "<Page><Panel Padding=\"8 16 24 32\"></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.AreEqual(8, panel.Padding.Left);
        Assert.AreEqual(16, panel.Padding.Top);
        Assert.AreEqual(24, panel.Padding.Right);
        Assert.AreEqual(32, panel.Padding.Bottom);
    }

    [TestMethod(DisplayName = "解析 Panel 多值 Padding 混合分隔")]
    public void Parse_Panel_MultiValuePadding_MixedSeparator()
    {
        var context = CreateContext();
        var xml = "<Page><Panel Padding=\"8, 16, 24, 32\"></Panel></Page>";

        var page = _parser.Parse(xml, context);
        var panel = (SlideMlPanelElement)page.Children[0];

        Assert.AreEqual(8, panel.Padding.Left);
        Assert.AreEqual(16, panel.Padding.Top);
        Assert.AreEqual(24, panel.Padding.Right);
        Assert.AreEqual(32, panel.Padding.Bottom);
    }
}
