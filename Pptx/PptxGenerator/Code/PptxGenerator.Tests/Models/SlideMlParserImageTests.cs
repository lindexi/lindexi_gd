using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserImageTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_Image_AllAttributes_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><Image Id=\"img1\" X=\"10\" Y=\"20\" Width=\"400\" Height=\"300\" Source=\"img_001\" Stretch=\"UniformToFill\" Opacity=\"0.8\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Bottom\" Margin=\"0,0,0,8\"></Image></Page>";

        var page = _parser.Parse(xml, context);
        var image = (SlideMlImageElement)page.Children[0];

        Assert.AreEqual("img1", image.Id);
        Assert.AreEqual(10, image.X);
        Assert.AreEqual(20, image.Y);
        Assert.AreEqual(400, image.Width);
        Assert.AreEqual(300, image.Height);
        Assert.AreEqual("img_001", image.Source);
        Assert.AreEqual(SlideMlImageStretch.UniformToFill, image.Stretch);
        Assert.AreEqual(0.8, image.Opacity);
        Assert.AreEqual(SlideMlHorizontalAlignment.Center, image.HorizontalAlignment);
        Assert.AreEqual(SlideMlVerticalAlignment.Bottom, image.VerticalAlignment);
        Assert.IsNotNull(image.Margin);
        Assert.AreEqual(0, image.Margin!.Value.Left);
        Assert.AreEqual(0, image.Margin.Value.Top);
        Assert.AreEqual(0, image.Margin.Value.Right);
        Assert.AreEqual(8, image.Margin.Value.Bottom);
    }

    [TestMethod]
    public void Parse_Image_DefaultValues()
    {
        var context = CreateContext();
        var xml = "<Page><Image Source=\"img_001\"></Image></Page>";

        var page = _parser.Parse(xml, context);
        var image = (SlideMlImageElement)page.Children[0];

        Assert.AreEqual(SlideMlImageStretch.Uniform, image.Stretch);
        Assert.AreEqual(1, image.Opacity);
    }

    [TestMethod]
    public void Parse_Image_NoSource_ThrowsException()
    {
        var context = CreateContext();
        var xml = "<Page><Image></Image></Page>";

        var exception = Assert.ThrowsExactly<SlideMlRequiredAttributeMissingException>(
            () => _parser.Parse(xml, context));

        Assert.AreEqual("Source", exception.AttributeName);
    }

    [TestMethod]
    public void Parse_Image_InvalidStretch_GeneratesError()
    {
        var context = CreateContext();
        var xml = "<Page><Image Source=\"img\" Stretch=\"Cover\"></Image></Page>";

        var page = _parser.Parse(xml, context);
        var image = (SlideMlImageElement)page.Children[0];

        Assert.HasCount(1, context.Errors);
        Assert.IsTrue(
            context.Errors[0].Contains("Stretch") && context.Errors[0].Contains("Cover") && context.Errors[0].Contains("无效"),
            $"错误应包含 Stretch 值 Cover 无效，实际: {context.Errors[0]}");
        Assert.AreEqual(SlideMlImageStretch.Uniform, image.Stretch);
    }
}
