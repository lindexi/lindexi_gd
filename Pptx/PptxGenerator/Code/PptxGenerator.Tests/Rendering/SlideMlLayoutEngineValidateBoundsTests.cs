using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlLayoutEngine 边界校验（ValidateBounds）测试。
/// 对应测试用例文档第 8 章。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineValidateBoundsTests
{
    private SlideMlLayoutEngine _engine = null!;
    private SlideMlPipelineContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new SlideMlLayoutEngine();
        _context = new SlideMlPipelineContext();
    }

    [TestMethod]
    public void ValidateBounds_ElementRightExceedsCanvas_Warning()
    {
        var rect = new SlideMlRectElement { Id = "r1", X = 1200, Width = 200, Height = 50 };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.IsTrue(
            _context.Warnings.Any(w => w.Contains("右边界") && w.Contains("超出画布宽度")),
            "应产生右边界超出画布宽度的 Warning");
    }

    [TestMethod]
    public void ValidateBounds_ElementBottomExceedsCanvas_Warning()
    {
        var rect = new SlideMlRectElement { Id = "r1", Y = 700, Width = 100, Height = 50 };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.IsTrue(
            _context.Warnings.Any(w => w.Contains("下边界") && w.Contains("超出画布高度")),
            "应产生下边界超出画布高度的 Warning");
    }

    [TestMethod]
    public void ValidateBounds_ElementLeftNegative_Warning()
    {
        var rect = new SlideMlRectElement { Id = "r1", X = -50, Width = 100, Height = 50 };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.IsTrue(
            _context.Warnings.Any(w => w.Contains("左边界") && w.Contains("超出画布左侧")),
            "应产生左边界超出画布左侧的 Warning");
    }

    [TestMethod]
    public void ValidateBounds_ElementTopNegative_Warning()
    {
        var rect = new SlideMlRectElement { Id = "r1", Y = -20, Width = 100, Height = 50 };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.IsTrue(
            _context.Warnings.Any(w => w.Contains("上边界") && w.Contains("超出画布顶部")),
            "应产生上边界超出画布顶部的 Warning");
    }

    [TestMethod]
    public void ValidateBounds_ElementInsideCanvas_NoWarning()
    {
        var rect = new SlideMlRectElement { Id = "r1", X = 100, Y = 100, Width = 400, Height = 300 };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.IsFalse(
            _context.Warnings.Any(w => w.Contains("超出画布")),
            "元素在画布内不应产生画布边界 Warning");
    }

    [TestMethod]
    public void ValidateBounds_ElementExactlyAtEdge_NoWarning()
    {
        // Right = 1180 + 100 = 1280 == CanvasWidth，条件为 > 而非 >=
        var rect = new SlideMlRectElement { Id = "r1", X = 1180, Width = 100, Height = 50 };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.IsFalse(
            _context.Warnings.Any(w => w.Contains("超出画布宽度")),
            "右边界刚好等于画布宽度时不应产生 Warning");
    }

    [TestMethod]
    public void ValidateBounds_ChildExceedsParentContainer_Warning()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            X = 0,
            Y = 0,
            Width = 200,
            Height = 200,
            Children =
            {
                new SlideMlRectElement { Id = "r1", X = 150, Y = 150, Width = 100, Height = 100 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.IsTrue(
            _context.Warnings.Any(w => w.Contains("超出父容器")),
            "子元素超出父容器应产生裁剪 Warning");
    }

    [TestMethod]
    public void ValidateBounds_ChildInsideParent_NoClipWarning()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Width = 300,
            Height = 300,
            Children =
            {
                new SlideMlRectElement { Id = "r1", X = 50, Y = 50, Width = 200, Height = 200 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.IsFalse(
            _context.Warnings.Any(w => w.Contains("超出父容器")),
            "子元素在父容器内不应产生裁剪 Warning");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
