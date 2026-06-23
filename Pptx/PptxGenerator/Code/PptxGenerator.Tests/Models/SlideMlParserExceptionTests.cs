using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlParserExceptionTests
{
    private readonly SlideMlParser _parser = new();

    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void Parse_NonPageRoot_ThrowsRootElementException()
    {
        var context = CreateContext();
        var xml = "<NotPage></NotPage>";

        Assert.ThrowsExactly<SlideMlRootElementException>(
            () => _parser.Parse(xml, context));
    }

    [TestMethod]
    public void Parse_MissingRequiredAttribute_ExceptionContainsContext()
    {
        var context = CreateContext();
        var xml = "<Page><Image></Image></Page>";

        var exception = Assert.ThrowsExactly<SlideMlRequiredAttributeMissingException>(
            () => _parser.Parse(xml, context));

        Assert.IsFalse(string.IsNullOrEmpty(exception.ElementId));
        Assert.AreEqual("Source", exception.AttributeName);
    }

    [TestMethod]
    public void LayoutEngine_UnknownElementType_ThrowsUnsupportedElementException()
    {
        var engine = new SlideMlLayoutEngine();
        var context = CreateContext();
        var page = new SlideMlPage
        {
            Children = { new CustomUnknownElement() },
        };

        var exception = Assert.ThrowsExactly<SlideMlUnsupportedElementException>(
            () => engine.PreLayout(page, context));

        Assert.IsNotNull(exception.TagName);
        Assert.AreEqual(nameof(CustomUnknownElement), exception.TagName);
    }

    /// <summary>
    /// 自定义元素类型，不被布局引擎识别，用于触发 SlideMlUnsupportedElementException。
    /// </summary>
    private sealed class CustomUnknownElement : SlideMlElement
    {
    }
}
