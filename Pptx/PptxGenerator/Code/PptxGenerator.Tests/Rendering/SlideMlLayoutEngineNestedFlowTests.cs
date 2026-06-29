using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlLayoutEngine 嵌套流式布局测试。
/// 对应测试用例文档第 4 章（PreLayout — 嵌套流式布局），不含已有用例 4.1。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineNestedFlowTests
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
    public void PreLayout_NestedVerticalInVertical()
    {
        var innerPanel = new SlideMlPanelElement
        {
            Id = "inner",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 6,
            Children =
            {
                new SlideMlRectElement { Id = "ir1", Width = 100, Height = 20 },
                new SlideMlRectElement { Id = "ir2", Width = 100, Height = 30 },
            },
        };

        var outerPanel = new SlideMlPanelElement
        {
            Id = "outer",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 10,
            Children =
            {
                new SlideMlRectElement { Id = "or1", Width = 200, Height = 30 },
                innerPanel,
            },
        };

        var page = CreatePage(outerPanel);
        _engine.PreLayout(page, _context);

        // or1.Y=0, inner.Y = 30+10 = 40
        var or1 = (SlideMlRectElement)outerPanel.Children[0];
        Assert.AreEqual(0, or1.LayoutBounds.Y, 0.01, "or1 Y");
        Assert.AreEqual(40, innerPanel.LayoutBounds.Y, 0.01, "inner panel Y");

        var ir1 = (SlideMlRectElement)innerPanel.Children[0];
        var ir2 = (SlideMlRectElement)innerPanel.Children[1];
        // LocalBounds.Y 是元素声明的 Y 值（均为 null → 0）
        Assert.AreEqual(0, ir1.LocalBounds.Y, 0.01, "ir1 local Y");
        // ir2 相对 ir1 的间距 = ir1.Height + Gap = 20 + 6 = 26
        Assert.AreEqual(20 + 6, ir2.LayoutBounds.Y - ir1.LayoutBounds.Y, 0.01, "ir2 相对 ir1 的 Y 间距");
    }

    [TestMethod]
    public void PreLayout_HorizontalContainingVertical()
    {
        var innerPanel = new SlideMlPanelElement
        {
            Id = "inner",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 8,
            Width = 100,
            Height = 78,
            Children =
            {
                new SlideMlRectElement { Id = "ir1", Width = 100, Height = 30 },
                new SlideMlRectElement { Id = "ir2", Width = 100, Height = 40 },
            },
        };

        var outerPanel = new SlideMlPanelElement
        {
            Id = "outer",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 10,
            Children =
            {
                innerPanel,
                new SlideMlRectElement { Id = "or1", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(outerPanel);
        _engine.PreLayout(page, _context);

        // inner panel 作为一个整体在外层水平排列
        Assert.AreEqual(0, innerPanel.LayoutBounds.X, 0.01, "inner panel X");
        Assert.AreEqual(100, innerPanel.ActualWidth, 0.01, "inner panel width");

        var or1 = (SlideMlRectElement)outerPanel.Children[1];
        // or1.X = inner.Width + Gap = 100 + 10 = 110
        Assert.AreEqual(110, or1.LayoutBounds.X, 0.01, "or1 X after inner panel");

        // 内层 Panel 的子元素垂直排列
        var ir1 = (SlideMlRectElement)innerPanel.Children[0];
        var ir2 = (SlideMlRectElement)innerPanel.Children[1];
        Assert.AreEqual(30 + 8, ir2.LayoutBounds.Y - ir1.LayoutBounds.Y, 0.01, "ir2 相对 ir1 的 Y 间距");
    }

    [TestMethod]
    public void PreLayout_ThreeLevelNesting()
    {
        var innermostPanel = new SlideMlPanelElement
        {
            Id = "level3",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 4,
            Width = 158,
            Height = 30,
            Children =
            {
                new SlideMlRectElement { Id = "lm1", Width = 50, Height = 30 },
                new SlideMlRectElement { Id = "lm2", Width = 50, Height = 30 },
                new SlideMlRectElement { Id = "lm3", Width = 50, Height = 30 },
            },
        };

        var middlePanel = new SlideMlPanelElement
        {
            Id = "level2",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 6,
            Width = 158,
            Height = 30,
            Children = { innermostPanel },
        };

        var outerPanel = new SlideMlPanelElement
        {
            Id = "level1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Children = { middlePanel },
        };

        var page = CreatePage(outerPanel);
        _engine.PreLayout(page, _context);

        // innermost: 50+4+50+4+50 = 158
        Assert.AreEqual(158, innermostPanel.ActualWidth, 0.01, "level3 width");
        Assert.AreEqual(30, innermostPanel.ActualHeight, 0.01, "level3 height");

        // middle: 声明尺寸
        Assert.AreEqual(158, middlePanel.ActualWidth, 0.01, "level2 width");
        Assert.AreEqual(30, middlePanel.ActualHeight, 0.01, "level2 height");

        // outer: wraps middle (GetChildSize 返回 middle 声明尺寸)
        Assert.AreEqual(158, outerPanel.ActualWidth, 0.01, "level1 width");
        Assert.AreEqual(30, outerPanel.ActualHeight, 0.01, "level1 height");

        // innermost children 相对位置
        var lm1 = (SlideMlRectElement)innermostPanel.Children[0];
        var lm2 = (SlideMlRectElement)innermostPanel.Children[1];
        var lm3 = (SlideMlRectElement)innermostPanel.Children[2];
        Assert.AreEqual(50 + 4, lm2.LayoutBounds.X - lm1.LayoutBounds.X, 0.01, "lm2 相对 lm1 的 X 间距");
        Assert.AreEqual(50 + 4, lm3.LayoutBounds.X - lm2.LayoutBounds.X, 0.01, "lm3 相对 lm2 的 X 间距");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
