using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserRectTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_Rect_AllAttributes_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Id=\"r1\" X=\"10\" Y=\"20\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\" Stroke=\"#000000\" StrokeThickness=\"2\" CornerRadius=\"8\" Shadow=\"0 4 12 #00000033\" Opacity=\"0.8\" StrokeDashArray=\"4,2\" Margin=\"0,0,0,8\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.AreEqual("r1", rect.Id);
        Assert.AreEqual(10, rect.X);
        Assert.AreEqual(20, rect.Y);
        Assert.AreEqual(100, rect.Width);
        Assert.AreEqual(50, rect.Height);
        Assert.IsInstanceOfType<SlideMlSolidColorBrush>(rect.Fill);
        Assert.AreEqual("#FF0000", ((SlideMlSolidColorBrush)rect.Fill!).Color);
        Assert.IsInstanceOfType<SlideMlSolidColorBrush>(rect.Stroke);
        Assert.AreEqual("#000000", ((SlideMlSolidColorBrush)rect.Stroke!).Color);
        Assert.AreEqual(2, rect.StrokeThickness);
        Assert.IsNotNull(rect.CornerRadius);
        Assert.AreEqual(8, rect.CornerRadius!.Value.TopLeft);
        Assert.AreEqual(8, rect.CornerRadius.Value.TopRight);
        Assert.AreEqual(8, rect.CornerRadius.Value.BottomRight);
        Assert.AreEqual(8, rect.CornerRadius.Value.BottomLeft);
        Assert.IsNotNull(rect.Shadow);
        Assert.AreEqual(0, rect.Shadow!.OffsetX);
        Assert.AreEqual(4, rect.Shadow.OffsetY);
        Assert.AreEqual(12, rect.Shadow.Blur);
        Assert.AreEqual("#00000033", rect.Shadow.Color);
        Assert.AreEqual("0 4 12 #00000033", rect.ShadowString);
        Assert.IsNotNull(rect.StrokeDashArray);
        Assert.HasCount(2, rect.StrokeDashArray!);
        Assert.AreEqual(4, rect.StrokeDashArray[0]);
        Assert.AreEqual(2, rect.StrokeDashArray[1]);
        Assert.AreEqual(0.8, rect.Opacity);
        Assert.IsNotNull(rect.Margin);
        Assert.AreEqual(0, rect.Margin!.Value.Left);
        Assert.AreEqual(0, rect.Margin.Value.Top);
        Assert.AreEqual(0, rect.Margin.Value.Right);
        Assert.AreEqual(8, rect.Margin.Value.Bottom);
    }

    [TestMethod]
    public void Parse_Rect_FourCornerRadii_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect CornerRadius=\"8,16,8,16\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.CornerRadius);
        Assert.AreEqual(8, rect.CornerRadius!.Value.TopLeft);
        Assert.AreEqual(16, rect.CornerRadius.Value.TopRight);
        Assert.AreEqual(8, rect.CornerRadius.Value.BottomRight);
        Assert.AreEqual(16, rect.CornerRadius.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_Rect_TwoCornerRadii_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect CornerRadius=\"8,16\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.CornerRadius);
        Assert.AreEqual(8, rect.CornerRadius!.Value.TopLeft);
        Assert.AreEqual(16, rect.CornerRadius.Value.TopRight);
        Assert.AreEqual(8, rect.CornerRadius.Value.BottomRight);
        Assert.AreEqual(16, rect.CornerRadius.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_Rect_ThreeCornerRadii_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect CornerRadius=\"8,16,8\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.CornerRadius);
        Assert.AreEqual(8, rect.CornerRadius!.Value.TopLeft);
        Assert.AreEqual(16, rect.CornerRadius.Value.TopRight);
        Assert.AreEqual(8, rect.CornerRadius.Value.BottomRight);
        Assert.AreEqual(16, rect.CornerRadius.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_Rect_WithGradientFill_FillIsGradient()
    {
        var context = CreateContext();
        var xml = "<Page><Rect><Fill><LinearGradient X1=\"0\" Y1=\"0\" X2=\"1\" Y2=\"0\"><Stop Offset=\"0\" Color=\"#4A7BF7\"/><Stop Offset=\"1\" Color=\"#F4F6FA\"/></LinearGradient></Fill></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(rect.Fill);
        var gradient = (SlideMlLinearGradientBrush)rect.Fill!;
        Assert.HasCount(2, gradient.Stops);
        Assert.AreEqual(0, gradient.X1);
        Assert.AreEqual(0, gradient.Y1);
        Assert.AreEqual(1, gradient.X2);
        Assert.AreEqual(0, gradient.Y2);
    }

    [TestMethod]
    public void Parse_Rect_WithGradientStroke_StrokeIsGradient()
    {
        var context = CreateContext();
        var xml = "<Page><Rect StrokeThickness=\"2\"><Stroke><LinearGradient><Stop Offset=\"0\" Color=\"#4A7BF7\"/><Stop Offset=\"1\" Color=\"#6C5CE7\"/></LinearGradient></Stroke></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(rect.Stroke);
    }

    [TestMethod]
    public void Parse_Rect_WithShadowElement_ShadowParsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect><Shadow OffsetX=\"0\" OffsetY=\"8\" Blur=\"24\" Color=\"#000000\" Opacity=\"0.12\"/></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.Shadow);
        Assert.AreEqual(0, rect.Shadow!.OffsetX);
        Assert.AreEqual(8, rect.Shadow.OffsetY);
        Assert.AreEqual(24, rect.Shadow.Blur);
        Assert.AreEqual("#000000", rect.Shadow.Color);
        Assert.AreEqual(0.12, rect.Shadow.Opacity);
    }

    [TestMethod(DisplayName = "解析 Rect 的 Fill Stroke Shadow 子元素不会产生未知警告")]
    public void Parse_Rect_WithStructuredChildren_DoesNotGenerateUnknownWarnings()
    {
        var context = CreateContext();
        var xml = "<Page><Rect StrokeThickness=\"2\"><Fill><LinearGradient><Stop Offset=\"0\" Color=\"#4A7BF7\"/><Stop Offset=\"1\" Color=\"#F4F6FA\"/></LinearGradient></Fill><Stroke><LinearGradient><Stop Offset=\"0\" Color=\"#4A7BF7\"/><Stop Offset=\"1\" Color=\"#6C5CE7\"/></LinearGradient></Stroke><Shadow OffsetX=\"0\" OffsetY=\"8\" Blur=\"24\" Color=\"#000000\" Opacity=\"0.12\"/></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(rect.Fill);
        Assert.IsInstanceOfType<SlideMlLinearGradientBrush>(rect.Stroke);
        Assert.IsNotNull(rect.Shadow);
        Assert.IsFalse(
            context.Warnings.Any(w => w.Contains("未知标签") || w.Contains("未知子标签") || w.Contains("未知属性")),
            $"结构化 Rect 子元素不应产生未知警告，实际: {string.Join("; ", context.Warnings)}");
    }

    [TestMethod]
    public void Parse_Rect_ShadowElementOverridesShadowAttribute()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Shadow=\"0 4 12 #00000033\"><Shadow OffsetX=\"0\" OffsetY=\"8\" Blur=\"24\" Color=\"#000000\" Opacity=\"0.12\"/></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.Shadow);
        Assert.AreEqual(8, rect.Shadow!.OffsetY);
        Assert.AreEqual("0 4 12 #00000033", rect.ShadowString);
    }

    [TestMethod]
    public void Parse_Rect_StrokeDashArray_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect StrokeDashArray=\"4,2,1,2\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.StrokeDashArray);
        Assert.HasCount(4, rect.StrokeDashArray!);
        Assert.AreEqual(4, rect.StrokeDashArray[0]);
        Assert.AreEqual(2, rect.StrokeDashArray[1]);
        Assert.AreEqual(1, rect.StrokeDashArray[2]);
        Assert.AreEqual(2, rect.StrokeDashArray[3]);
    }

    [TestMethod]
    public void Parse_Rect_StrokeDashArrayInvalidValue_GeneratesError()
    {
        var context = CreateContext();
        var xml = "<Page><Rect StrokeDashArray=\"4,abc,2\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.HasCount(1, context.Errors);
        Assert.Contains(
            "包含无效数值",
            context.Errors[0],
            $"错误应包含\"包含无效数值\"，实际: {context.Errors[0]}");
        Assert.IsNull(rect.StrokeDashArray);
    }

    [TestMethod]
    public void Parse_Rect_CornerRadiusSpaceSeparated_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect CornerRadius=\"8 16 8 16\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.CornerRadius);
        Assert.AreEqual(8, rect.CornerRadius!.Value.TopLeft);
        Assert.AreEqual(16, rect.CornerRadius.Value.TopRight);
        Assert.AreEqual(8, rect.CornerRadius.Value.BottomRight);
        Assert.AreEqual(16, rect.CornerRadius.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_Rect_CornerRadiusMixedCommaAndSpace_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Rect CornerRadius=\"8, 16 8, 16\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.IsNotNull(rect.CornerRadius);
        Assert.AreEqual(8, rect.CornerRadius!.Value.TopLeft);
        Assert.AreEqual(16, rect.CornerRadius.Value.TopRight);
        Assert.AreEqual(8, rect.CornerRadius.Value.BottomRight);
        Assert.AreEqual(16, rect.CornerRadius.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_Rect_CornerRadiusInvalid_GeneratesErrorWithoutIgnored()
    {
        var context = CreateContext();
        var xml = "<Page><Rect Id=\"r1\" CornerRadius=\"abc\"></Rect></Page>";

        var page = _parser.Parse(xml, context);
        var rect = (SlideMlRectElement)page.Children[0];

        Assert.HasCount(1, context.Errors);
        Assert.Contains("CornerRadius", context.Errors[0]);
        Assert.Contains("abc", context.Errors[0]);
        Assert.DoesNotContain("已忽略", context.Errors[0],
            $"错误消息不应包含'已忽略'，实际: {context.Errors[0]}");
        Assert.IsNull(rect.CornerRadius);
    }

    [TestMethod]
    public void Parse_Rect_UnknownChildTag_GeneratesWarning()
    {
        var context = CreateContext();
        var xml = "<Page><Rect><UnknownTag/></Rect></Page>";

        var page = _parser.Parse(xml, context);

        Assert.IsTrue(
            context.Warnings.Any(w => w.Contains("Rect") && w.Contains("未知子标签") && w.Contains("UnknownTag")),
            $"警告应包含 Rect 下未知子标签 UnknownTag，实际: {string.Join("; ", context.Warnings)}");
    }
}
