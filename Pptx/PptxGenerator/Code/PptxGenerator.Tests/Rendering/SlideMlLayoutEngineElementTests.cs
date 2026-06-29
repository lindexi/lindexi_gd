using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlLayoutEngine Rect 与 TextElement 元素测试。
/// 对应测试用例文档第 5 章（Rect 元素）和第 6 章（TextElement 元素）。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineElementTests
{
    private SlideMlLayoutEngine _engine = null!;
    private SlideMlPipelineContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new SlideMlLayoutEngine();
        _context = new SlideMlPipelineContext();
    }

    // ───────── 第 5 章：Rect 元素 ─────────

    [TestMethod]
    public void PreLayout_Rect_DefaultSize()
    {
        var rect = new SlideMlRectElement { Id = "r1" };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, rect.ActualWidth, 0.01, "Rect 默认宽度 0");
        Assert.AreEqual(0, rect.ActualHeight, 0.01, "Rect 默认高度 0");
    }

    [TestMethod]
    public void PreLayout_Rect_ExplicitSize()
    {
        var rect = new SlideMlRectElement { Id = "r1", X = 50, Y = 30, Width = 200, Height = 100 };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(50, rect.LayoutBounds.X, 0.01, "Rect X");
        Assert.AreEqual(30, rect.LayoutBounds.Y, 0.01, "Rect Y");
        Assert.AreEqual(200, rect.LayoutBounds.Width, 0.01, "Rect Width");
        Assert.AreEqual(100, rect.LayoutBounds.Height, 0.01, "Rect Height");
    }

    [TestMethod]
    public void PreLayout_Rect_Alignment_Center()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Width = 400,
            Height = 300,
            Children =
            {
                new SlideMlRectElement
                {
                    Id = "r1",
                    Width = 100,
                    Height = 50,
                    HorizontalAlignment = SlideMlHorizontalAlignment.Center,
                    VerticalAlignment = SlideMlVerticalAlignment.Center,
                },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        Assert.AreEqual((400 - 100) / 2, r1.LayoutBounds.X, 0.01, "Rect Center X");
        Assert.AreEqual((300 - 50) / 2, r1.LayoutBounds.Y, 0.01, "Rect Center Y");
        Assert.AreEqual(100, r1.LayoutBounds.Width, 0.01, "Rect Width");
        Assert.AreEqual(50, r1.LayoutBounds.Height, 0.01, "Rect Height");
    }

    // ───────── 第 6 章：TextElement 元素 ─────────

    [TestMethod]
    public void PreLayout_TextElement_ExplicitSize()
    {
        var text = new SlideMlTextElement { Id = "t1", X = 10, Y = 10, Width = 400, Height = 30, Text = "Hello" };

        var page = CreatePage(text);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(10, text.LayoutBounds.X, 0.01, "Text X");
        Assert.AreEqual(10, text.LayoutBounds.Y, 0.01, "Text Y");
        Assert.AreEqual(400, text.LayoutBounds.Width, 0.01, "Text Width");
        Assert.AreEqual(30, text.LayoutBounds.Height, 0.01, "Text Height");
    }

    [TestMethod]
    public void PreLayout_TextElement_DefaultSize()
    {
        var text = new SlideMlTextElement { Id = "t1", Text = "Hello" };

        var page = CreatePage(text);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, text.ActualWidth, 0.01, "PreLayout 中 TextElement 默认宽度 0");
        Assert.AreEqual(0, text.ActualHeight, 0.01, "PreLayout 中 TextElement 默认高度 0");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
