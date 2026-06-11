using System.Windows;

namespace PptxGenerator.Tests;

[TestClass]
public sealed class SlideLayoutEngineTests
{
    private SlideLayoutEngine _engine = null!;
    private SlidePipelineContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new SlideLayoutEngine();
        _context = new SlidePipelineContext();
    }

    [TestMethod]
    public void HorizontalLayout_ChildrenPlacedSequentially()
    {
        var panel = new SlidePanelElement
        {
            Id = "panel1",
            Layout = SlideLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideRectElement { Id = "r2", Width = 200, Height = 50 },
                new SlideRectElement { Id = "r3", Width = 150, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, _context.Errors.Count, "不应有错误");

        var r1 = (SlideRectElement)panel.Children[0];
        var r2 = (SlideRectElement)panel.Children[1];
        var r3 = (SlideRectElement)panel.Children[2];

        // r1 应在最左侧
        Assert.AreEqual(0, r1.LayoutBounds.X, 0.01, "r1 X");
        // r2 应在 r1 右侧 + Gap
        Assert.AreEqual(100 + 8, r2.LayoutBounds.X, 0.01, "r2 X");
        // r3 应在 r2 右侧 + Gap
        Assert.AreEqual(100 + 8 + 200 + 8, r3.LayoutBounds.X, 0.01, "r3 X");

        // Panel 宽度应包裹所有子元素
        Assert.AreEqual(100 + 8 + 200 + 8 + 150, panel.ActualWidth, 0.01, "panel width");
        Assert.AreEqual(50, panel.ActualHeight, 0.01, "panel height");
    }

    [TestMethod]
    public void VerticalLayout_ChildrenPlacedSequentially()
    {
        var panel = new SlidePanelElement
        {
            Id = "panel1",
            Layout = SlideLayoutDirection.Vertical,
            Gap = 12,
            Children =
            {
                new SlideRectElement { Id = "r1", Width = 200, Height = 40 },
                new SlideRectElement { Id = "r2", Width = 200, Height = 60 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideRectElement)panel.Children[0];
        var r2 = (SlideRectElement)panel.Children[1];

        Assert.AreEqual(0, r1.LayoutBounds.Y, 0.01, "r1 Y");
        Assert.AreEqual(40 + 12, r2.LayoutBounds.Y, 0.01, "r2 Y");
        Assert.AreEqual(40 + 12 + 60, panel.ActualHeight, 0.01, "panel height");
    }

    [TestMethod]
    public void HorizontalLayout_MarginAffectsSpacing()
    {
        var panel = new SlidePanelElement
        {
            Id = "panel1",
            Layout = SlideLayoutDirection.Horizontal,
            Gap = 4,
            Children =
            {
                new SlideRectElement { Id = "r1", Width = 100, Height = 50, Margin = new SlideThickness { Right = 20 } },
                new SlideRectElement { Id = "r2", Width = 100, Height = 50, Margin = new SlideThickness { Left = 10 } },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideRectElement)panel.Children[0];
        var r2 = (SlideRectElement)panel.Children[1];

        // r1 starts at 0
        Assert.AreEqual(0, r1.LayoutBounds.X, 0.01, "r1 X");
        // Gap = max(4, r1.Right(20) + r2.Left(10)) = 30
        // r2.X = r1.X + r1.Width + gap = 0 + 100 + 30 = 130
        Assert.AreEqual(130, r2.LayoutBounds.X, 0.01, "r2 X with margin");
        // total flow = r1.Width + gap + r2.Width + r2.RightMargin(0) = 100 + 30 + 100 + 0 = 230
        Assert.AreEqual(230, panel.ActualWidth, 0.01, "panel width");
    }

    [TestMethod]
    public void HorizontalLayout_CrossAxisAlignment_Respected()
    {
        var panel = new SlidePanelElement
        {
            Id = "panel1",
            Layout = SlideLayoutDirection.Horizontal,
            Gap = 8,
            Height = 200,
            Children =
            {
                new SlideRectElement { Id = "r1", Width = 100, Height = 50, VerticalAlignment = SlideVerticalAlignment.Center },
                new SlideRectElement { Id = "r2", Width = 100, Height = 50, VerticalAlignment = SlideVerticalAlignment.Bottom },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideRectElement)panel.Children[0];
        var r2 = (SlideRectElement)panel.Children[1];

        // Center: (200 - 50) / 2 = 75
        Assert.AreEqual(75, r1.LayoutBounds.Y, 0.01, "r1 center Y");
        // Bottom: 200 - 50 = 150
        Assert.AreEqual(150, r2.LayoutBounds.Y, 0.01, "r2 bottom Y");
    }

    [TestMethod]
    public void FlowLayout_EmptyChildren_DoesNotCrash()
    {
        var panel = new SlidePanelElement
        {
            Id = "panel1",
            Layout = SlideLayoutDirection.Horizontal,
            Gap = 8,
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, panel.ActualWidth, 0.01, "empty panel width");
        Assert.AreEqual(0, panel.ActualHeight, 0.01, "empty panel height");
    }

    [TestMethod]
    public void AbsoluteLayout_BehaviorUnchanged()
    {
        var panel = new SlidePanelElement
        {
            Id = "panel1",
            Layout = SlideLayoutDirection.Absolute,
            Children =
            {
                new SlideRectElement { Id = "r1", X = 50, Y = 30, Width = 100, Height = 50 },
                new SlideRectElement { Id = "r2", X = 200, Y = 100, Width = 150, Height = 80 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideRectElement)panel.Children[0];
        var r2 = (SlideRectElement)panel.Children[1];

        // 绝对定位下 X/Y 应直接使用声明值
        Assert.AreEqual(50, r1.LayoutBounds.X, 0.01, "r1 X");
        Assert.AreEqual(30, r1.LayoutBounds.Y, 0.01, "r1 Y");
        Assert.AreEqual(200, r2.LayoutBounds.X, 0.01, "r2 X");
        Assert.AreEqual(100, r2.LayoutBounds.Y, 0.01, "r2 Y");
    }

    [TestMethod]
    public void HorizontalLayout_WithPadding_OffsetsContent()
    {
        var panel = new SlidePanelElement
        {
            Id = "panel1",
            Layout = SlideLayoutDirection.Horizontal,
            Gap = 8,
            Padding = 16,
            Children =
            {
                new SlideRectElement { Id = "r1", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideRectElement)panel.Children[0];

        // 子元素应从 padding 之后开始
        Assert.AreEqual(16, r1.LayoutBounds.X, 0.01, "r1 X with padding");
        Assert.AreEqual(16, r1.LayoutBounds.Y, 0.01, "r1 Y with padding");

        // Panel 宽度 = 100 + 16*2 = 132
        Assert.AreEqual(132, panel.ActualWidth, 0.01, "panel width with padding");
    }

    [TestMethod]
    public void NestedFlowPanels_LayoutCorrectly()
    {
        var innerPanel = new SlidePanelElement
        {
            Id = "inner",
            Layout = SlideLayoutDirection.Horizontal,
            Gap = 4,
            Children =
            {
                new SlideRectElement { Id = "ir1", Width = 50, Height = 30 },
                new SlideRectElement { Id = "ir2", Width = 50, Height = 30 },
            },
        };

        var outerPanel = new SlidePanelElement
        {
            Id = "outer",
            Layout = SlideLayoutDirection.Vertical,
            Gap = 10,
            Children =
            {
                new SlideRectElement { Id = "or1", Width = 200, Height = 40 },
                innerPanel,
            },
        };

        var page = CreatePage(outerPanel);
        _engine.PreLayout(page, _context);

        var or1 = (SlideRectElement)outerPanel.Children[0];

        Assert.AreEqual(0, or1.LayoutBounds.Y, 0.01, "or1 Y");
        Assert.AreEqual(40 + 10, innerPanel.LayoutBounds.Y, 0.01, "inner panel Y");

        // 内层 Panel 的两个子元素应水平排列
        var ir1 = (SlideRectElement)innerPanel.Children[0];
        var ir2 = (SlideRectElement)innerPanel.Children[1];
        Assert.AreEqual(0, ir1.LayoutBounds.X, 0.01, "ir1 X");
        Assert.AreEqual(50 + 4, ir2.LayoutBounds.X, 0.01, "ir2 X");
    }

    [TestMethod]
    public void FlowLayout_Overflow_GeneratesWarning()
    {
        var panel = new SlidePanelElement
        {
            Id = "panel1",
            Layout = SlideLayoutDirection.Horizontal,
            Gap = 8,
            Width = 150,
            Children =
            {
                new SlideRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideRectElement { Id = "r2", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.IsTrue(_context.Warnings.Count > 0, "应有溢出警告");
        Assert.IsTrue(_context.Warnings.Any(w => w.Contains("超出")), "警告应包含溢出信息");
    }

    [TestMethod]
    public void FinalLayout_UsesMeasuredSizes()
    {
        var panel = new SlidePanelElement
        {
            Id = "panel1",
            Layout = SlideLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideTextElement { Id = "t1", Text = "Hello" },
                new SlideTextElement { Id = "t2", Text = "World" },
            },
        };

        var measurements = new SlideElementMeasurements(new Dictionary<string, SlideMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 120, MeasuredHeight = 24 },
            ["t2"] = new() { MeasuredWidth = 100, MeasuredHeight = 24 },
        });

        var page = CreatePage(panel);
        _engine.FinalLayout(page, _context, measurements);

        var t1 = (SlideTextElement)panel.Children[0];
        var t2 = (SlideTextElement)panel.Children[1];

        Assert.AreEqual(120, t1.ActualWidth, 0.01, "t1 measured width");
        Assert.AreEqual(100, t2.ActualWidth, 0.01, "t2 measured width");
        Assert.AreEqual(120 + 8, t2.LayoutBounds.X, 0.01, "t2 X with measured sizes");
    }

    private static SlidePage CreatePage(SlideElement child)
    {
        var page = new SlidePage();
        page.Children.Add(child);
        return page;
    }
}
