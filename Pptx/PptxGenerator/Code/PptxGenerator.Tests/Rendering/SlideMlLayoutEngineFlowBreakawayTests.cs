using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// 流式布局中主轴显式坐标脱离流的单元测试。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineFlowBreakawayTests
{
    private SlideMlLayoutEngine _engine = null!;
    private SlideMlPipelineContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new SlideMlLayoutEngine();
        _context = new SlideMlPipelineContext();
    }

    /// <summary>
    /// 水平流中，中间元素设置 X 时脱离流，不影响前后流式元素的位置。
    /// </summary>
    [TestMethod]
    public void HorizontalFlow_MiddleChildExplicitX_BreaksAway()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", X = 300, Width = 80, Height = 50 },
                new SlideMlRectElement { Id = "r3", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];
        var r3 = (SlideMlRectElement)panel.Children[2];

        Assert.AreEqual(0, r1.LayoutBounds.X, 0.01, "r1 X 应在流式起始位置");
        Assert.AreEqual(300, r2.LayoutBounds.X, 0.01, "r2 X 应使用显式坐标 300");
        Assert.AreEqual(100 + 8, r3.LayoutBounds.X, 0.01, "r3 X 应紧接 r1，不受 r2 影响");
    }

    /// <summary>
    /// 水平流中，脱离流元素不参与 Panel 自动宽度计算。
    /// </summary>
    [TestMethod]
    public void HorizontalFlow_BreakawayChild_ExcludedFromAutoSize()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", X = 500, Width = 200, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        // Panel 宽度 = r1(100) + Gap(8) + 0（r2 脱离流，不参与）
        // 但 r2 的 trailingMargin 也不计入，所以 totalFlowSize = 100
        Assert.AreEqual(100, panel.ActualWidth, 0.01, "Panel 自动宽度不应包含脱离流元素");
    }

    /// <summary>
    /// 水平流中，所有元素都设置 X 时全部脱离流，Panel 宽度为 0。
    /// </summary>
    [TestMethod]
    public void HorizontalFlow_AllChildrenExplicitX_PanelAutoSizeZero()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideMlRectElement { Id = "r1", X = 0, Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", X = 200, Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, panel.ActualWidth, 0.01, "全部脱离流时 Panel 自动宽度为 0");
    }

    /// <summary>
    /// 垂直流中，中间元素设置 Y 时脱离流，不影响前后流式元素的位置。
    /// </summary>
    [TestMethod]
    public void VerticalFlow_MiddleChildExplicitY_BreaksAway()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 12,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 200, Height = 40 },
                new SlideMlRectElement { Id = "r2", Y = 300, Width = 200, Height = 60 },
                new SlideMlRectElement { Id = "r3", Width = 200, Height = 30 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];
        var r3 = (SlideMlRectElement)panel.Children[2];

        Assert.AreEqual(0, r1.LayoutBounds.Y, 0.01, "r1 Y 应在流式起始位置");
        Assert.AreEqual(300, r2.LayoutBounds.Y, 0.01, "r2 Y 应使用显式坐标 300");
        Assert.AreEqual(40 + 12, r3.LayoutBounds.Y, 0.01, "r3 Y 应紧接 r1，不受 r2 影响");
    }

    /// <summary>
    /// 垂直流中，脱离流元素不参与 Panel 自动高度计算。
    /// </summary>
    [TestMethod]
    public void VerticalFlow_BreakawayChild_ExcludedFromAutoSize()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 12,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 200, Height = 40 },
                new SlideMlRectElement { Id = "r2", Y = 500, Width = 200, Height = 60 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(40, panel.ActualHeight, 0.01, "Panel 自动高度不应包含脱离流元素");
    }

    /// <summary>
    /// 水平流中，脱离流元素的跨轴 Y 仍正常生效。
    /// </summary>
    [TestMethod]
    public void HorizontalFlow_BreakawayChild_CrossAxisY_Respected()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Height = 200,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideMlRectElement
                {
                    Id = "r2",
                    X = 200,
                    Y = 100,
                    Width = 80,
                    Height = 50,
                },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r2 = (SlideMlRectElement)panel.Children[1];
        Assert.AreEqual(200, r2.LayoutBounds.X, 0.01, "r2 X 应使用显式坐标");
        Assert.AreEqual(100, r2.LayoutBounds.Y, 0.01, "r2 跨轴 Y 应使用显式坐标");
    }

    /// <summary>
    /// 水平流中，脱离流元素与流式元素之间的 Gap 不被计算。
    /// </summary>
    [TestMethod]
    public void HorizontalFlow_BreakawayChild_NoGapWithFlow()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 20,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", X = 500, Width = 80, Height = 50 },
                new SlideMlRectElement { Id = "r3", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r3 = (SlideMlRectElement)panel.Children[2];
        // r3 紧接 r1，Gap=20 在 r1 和 r3 之间生效（r2 脱离流不算）
        Assert.AreEqual(100 + 20, r3.LayoutBounds.X, 0.01, "r3 X = r1.Width + Gap，r2 脱离流不影响间距");
    }

    /// <summary>
    /// 流式元素在脱离流元素之后时，间距仍从前一个流式元素正确计算。
    /// </summary>
    [TestMethod]
    public void HorizontalFlow_MultipleBreakaway_FlowContinues()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 10,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", X = 500, Width = 80, Height = 50 },
                new SlideMlRectElement { Id = "r3", X = 600, Width = 80, Height = 50 },
                new SlideMlRectElement { Id = "r4", Width = 120, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r4 = (SlideMlRectElement)panel.Children[3];

        Assert.AreEqual(0, r1.LayoutBounds.X, 0.01, "r1 X 起始为 0");
        // r4 紧接 r1，Gap=10
        Assert.AreEqual(100 + 10, r4.LayoutBounds.X, 0.01, "r4 X = r1.Width + Gap，多个脱离流元素不影响流式序列");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
