using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserTextElementTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_TextElement_AllAttributes_Parsed()
    {
        var context = CreateContext();
        var xml = "<Page><TextElement Id=\"t1\" X=\"10\" Y=\"20\" Width=\"400\" Height=\"30\" Text=\"Hello\" FontName=\"Arial\" FontSize=\"24\" Foreground=\"#333333\" TextAlignment=\"Center\" LineHeight=\"1.5\" IsBold=\"True\" IsItalic=\"True\" Opacity=\"0.9\" Margin=\"0,0,0,8\"></TextElement></Page>";

        var page = _parser.Parse(xml, context);
        var textElement = (SlideMlTextElement)page.Children[0];

        Assert.AreEqual("t1", textElement.Id);
        Assert.AreEqual(10, textElement.X);
        Assert.AreEqual(20, textElement.Y);
        Assert.AreEqual(400, textElement.Width);
        Assert.AreEqual(30, textElement.Height);
        Assert.AreEqual("Hello", textElement.Text);
        Assert.AreEqual("Arial", textElement.FontName);
        Assert.AreEqual(24, textElement.FontSize);
        Assert.AreEqual("#333333", textElement.Foreground);
        Assert.AreEqual(SlideMlTextAlignment.Center, textElement.TextAlignment);
        Assert.AreEqual(1.5, textElement.LineHeight);
        Assert.IsTrue(textElement.IsBold);
        Assert.IsTrue(textElement.IsItalic);
        Assert.AreEqual(0.9, textElement.Opacity);
        Assert.IsNotNull(textElement.Margin);
        Assert.AreEqual(0, textElement.Margin!.Value.Left);
        Assert.AreEqual(0, textElement.Margin.Value.Top);
        Assert.AreEqual(0, textElement.Margin.Value.Right);
        Assert.AreEqual(8, textElement.Margin.Value.Bottom);
    }

    [TestMethod]
    public void Parse_TextElement_DefaultValues()
    {
        var context = CreateContext();
        var xml = "<Page><TextElement Text=\"Hi\"></TextElement></Page>";

        var page = _parser.Parse(xml, context);
        var textElement = (SlideMlTextElement)page.Children[0];

        Assert.AreEqual("Microsoft YaHei", textElement.FontName);
        Assert.AreEqual(16, textElement.FontSize);
        Assert.AreEqual("#000000", textElement.Foreground);
        Assert.AreEqual(SlideMlTextAlignment.Left, textElement.TextAlignment);
        Assert.AreEqual(1.2, textElement.LineHeight);
        Assert.IsNull(textElement.IsBold);
        Assert.IsNull(textElement.IsItalic);
        Assert.IsNull(textElement.Spans);
    }

    [TestMethod]
    public void Parse_TextElement_WithSpans_SpansParsed()
    {
        var context = CreateContext();
        var xml = "<Page><TextElement><Span Text=\"标题\" FontSize=\"24\" Foreground=\"#333\" IsBold=\"True\"/><Span Text=\" — 副标题\" FontSize=\"14\" Foreground=\"#666\"/></TextElement></Page>";

        var page = _parser.Parse(xml, context);
        var textElement = (SlideMlTextElement)page.Children[0];

        Assert.IsNotNull(textElement.Spans);
        Assert.AreEqual(2, textElement.Spans!.Count);

        var span0 = textElement.Spans[0];
        Assert.AreEqual("标题", span0.Text);
        Assert.AreEqual(24, span0.FontSize);
        Assert.AreEqual("#333", span0.Foreground);
        Assert.IsTrue(span0.IsBold);

        var span1 = textElement.Spans[1];
        Assert.AreEqual(" — 副标题", span1.Text);
        Assert.AreEqual(14, span1.FontSize);
        Assert.AreEqual("#666", span1.Foreground);

        Assert.AreEqual("标题 — 副标题", textElement.Text);
    }

    [TestMethod]
    public void Parse_TextElement_NoTextNoSpan_ThrowsException()
    {
        var context = CreateContext();
        var xml = "<Page><TextElement></TextElement></Page>";

        var exception = Assert.ThrowsExactly<SlideMlRequiredAttributeMissingException>(
            () => _parser.Parse(xml, context));

        Assert.AreEqual("Text", exception.AttributeName);
        Assert.IsNotNull(exception.ElementId);
    }

    [TestMethod]
    public void Parse_TextElement_SpanOnly_TextConcatenated()
    {
        var context = CreateContext();
        var xml = "<Page><TextElement><Span Text=\"A\"/><Span Text=\"B\"/></TextElement></Page>";

        var page = _parser.Parse(xml, context);
        var textElement = (SlideMlTextElement)page.Children[0];

        Assert.AreEqual("AB", textElement.Text);
        Assert.IsNotNull(textElement.Spans);
        Assert.AreEqual(2, textElement.Spans!.Count);
    }

    [TestMethod]
    public void Parse_Span_NoText_ThrowsException()
    {
        var context = CreateContext();
        var xml = "<Page><TextElement><Span FontSize=\"14\"/></TextElement></Page>";

        Assert.ThrowsExactly<SlideMlRequiredAttributeMissingException>(
            () => _parser.Parse(xml, context));
    }

    [DataTestMethod]
    [DataRow("True", true)]
    [DataRow("true", true)]
    [DataRow("False", false)]
    [DataRow("false", false)]
    public void Parse_TextElement_IsBold_ValidBoolValues_Parsed(string boolValue, bool expected)
    {
        var context = CreateContext();
        var xml = $"<Page><TextElement Text=\"Hi\" IsBold=\"{boolValue}\"></TextElement></Page>";

        var page = _parser.Parse(xml, context);
        var textElement = (SlideMlTextElement)page.Children[0];

        Assert.AreEqual(expected, textElement.IsBold);
    }

    [TestMethod]
    public void Parse_TextElement_IsBold_InvalidValue_ReturnsNull()
    {
        var context = CreateContext();
        var xml = "<Page><TextElement Text=\"Hi\" IsBold=\"yes\"></TextElement></Page>";

        var page = _parser.Parse(xml, context);
        var textElement = (SlideMlTextElement)page.Children[0];

        Assert.IsNull(textElement.IsBold);
    }

    [TestMethod]
    public void Parse_TextElement_IsBold_NotSpecified_ReturnsNull()
    {
        var context = CreateContext();
        var xml = "<Page><TextElement Text=\"Hi\"></TextElement></Page>";

        var page = _parser.Parse(xml, context);
        var textElement = (SlideMlTextElement)page.Children[0];

        Assert.IsNull(textElement.IsBold);
    }

    [TestMethod]
    public void Parse_TextElement_InvalidTextAlignment_GeneratesError()
    {
        var context = CreateContext();
        var xml = "<Page><TextElement Text=\"Hi\" TextAlignment=\"Justified\"></TextElement></Page>";

        var page = _parser.Parse(xml, context);
        var textElement = (SlideMlTextElement)page.Children[0];

        Assert.AreEqual(1, context.Errors.Count);
        Assert.IsTrue(
            context.Errors[0].Contains("TextAlignment") && context.Errors[0].Contains("Justified") && context.Errors[0].Contains("无效"),
            $"错误应包含 TextAlignment 值 Justified 无效，实际: {context.Errors[0]}");
        Assert.AreEqual(SlideMlTextAlignment.Left, textElement.TextAlignment);
    }
}
