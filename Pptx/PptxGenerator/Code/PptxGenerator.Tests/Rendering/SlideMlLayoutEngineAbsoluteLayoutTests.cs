using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlLayoutEngine 绝对定位（Absolute）布局测试。
/// 对应测试用例文档第 1 章（PreLayout — 绝对定位），不含已有用例 1.1。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineAbsoluteLayoutTests
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
    public void PreLayout_AbsolutePanel_AutoSize_WrapsContent()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Children =
            {
                new SlideMlRectElement { Id = "r1", X = 10, Y = 10, Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", X = 150, Y = 30, Width = 200, Height = 80 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.IsEmpty(_context.Errors, "不应有错误");
        Assert.AreEqual(350, panel.ActualWidth, 0.01, "panel ActualWidth 应为 contentRight=350");
        Assert.AreEqual(110, panel.ActualHeight, 0.01, "panel ActualHeight 应为 contentBottom=110");

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];
        Assert.AreEqual(10, r1.LayoutBounds.X, 0.01, "r1 X");
        Assert.AreEqual(10, r1.LayoutBounds.Y, 0.01, "r1 Y");
        Assert.AreEqual(150, r2.LayoutBounds.X, 0.01, "r2 X");
        Assert.AreEqual(30, r2.LayoutBounds.Y, 0.01, "r2 Y");
    }

    [TestMethod]
    public void PreLayout_AbsolutePanel_FixedSize_SubelementsInside()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Width = 400,
            Height = 300,
            Children =
            {
                new SlideMlRectElement { Id = "r1", X = 50, Y = 50, Width = 200, Height = 100 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(400, panel.ActualWidth, 0.01, "panel ActualWidth");
        Assert.AreEqual(300, panel.ActualHeight, 0.01, "panel ActualHeight");

        var r1 = (SlideMlRectElement)panel.Children[0];
        Assert.AreEqual(50, r1.LayoutBounds.X, 0.01, "r1 X 相对父容器原点");
        Assert.AreEqual(50, r1.LayoutBounds.Y, 0.01, "r1 Y 相对父容器原点");
    }

    [TestMethod]
    public void PreLayout_AbsolutePanel_Padding_OffsetsChildren()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Padding = 16,
            Width = 200,
            Height = 200,
            Children =
            {
                new SlideMlRectElement { Id = "r1", X = 0, Y = 0, Width = 50, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        Assert.AreEqual(16, r1.LayoutBounds.X, 0.01, "r1 X 偏移 Padding");
        Assert.AreEqual(16, r1.LayoutBounds.Y, 0.01, "r1 Y 偏移 Padding");
    }

    [TestMethod]
    public void PreLayout_AbsolutePanel_Padding_AutoSize()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Padding = 16,
            Children =
            {
                new SlideMlRectElement { Id = "r1", X = 0, Y = 0, Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(100 + 16 * 2, panel.ActualWidth, 0.01, "panel ActualWidth 含 Padding");
        Assert.AreEqual(50 + 16 * 2, panel.ActualHeight, 0.01, "panel ActualHeight 含 Padding");

        var r1 = (SlideMlRectElement)panel.Children[0];
        Assert.AreEqual(16, r1.LayoutBounds.X, 0.01, "r1 X 偏移 Padding");
        Assert.AreEqual(16, r1.LayoutBounds.Y, 0.01, "r1 Y 偏移 Padding");
        Assert.AreEqual(100, r1.LayoutBounds.Width, 0.01, "r1 Width");
        Assert.AreEqual(50, r1.LayoutBounds.Height, 0.01, "r1 Height");
    }

    [TestMethod]
    public void PreLayout_NestedAbsolutePanels_LayoutCorrect()
    {
        var innerPanel = new SlideMlPanelElement
        {
            Id = "inner",
            Layout = SlideMlLayoutDirection.Absolute,
            X = 50,
            Y = 50,
            Width = 200,
            Height = 200,
            Children =
            {
                new SlideMlRectElement { Id = "rect", X = 10, Y = 10, Width = 50, Height = 50 },
            },
        };

        var outerPanel = new SlideMlPanelElement
        {
            Id = "outer",
            Layout = SlideMlLayoutDirection.Absolute,
            Width = 300,
            Height = 300,
            Children = { innerPanel },
        };

        var page = CreatePage(outerPanel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(50, innerPanel.LayoutBounds.X, 0.01, "inner panel X");
        Assert.AreEqual(50, innerPanel.LayoutBounds.Y, 0.01, "inner panel Y");
        Assert.AreEqual(200, innerPanel.LayoutBounds.Width, 0.01, "inner panel Width");
        Assert.AreEqual(200, innerPanel.LayoutBounds.Height, 0.01, "inner panel Height");

        var rect = (SlideMlRectElement)innerPanel.Children[0];
        // rect 坐标相对 inner panel 的内容区原点
        Assert.AreEqual(50 + 10, rect.LayoutBounds.X, 0.01, "rect X 相对 inner 内容区");
        Assert.AreEqual(50 + 10, rect.LayoutBounds.Y, 0.01, "rect Y 相对 inner 内容区");
    }

    [TestMethod]
    public void PreLayout_AbsolutePanel_HorizontalAlignment_Center()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Width = 1280,
            Height = 720,
            Children =
            {
                new SlideMlRectElement
                {
                    Id = "r1",
                    Width = 200,
                    Height = 100,
                    HorizontalAlignment = SlideMlHorizontalAlignment.Center,
                },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        // parentBounds.Width = CanvasWidth = 1280, (1280-200)/2 = 540
        Assert.AreEqual(540, r1.LayoutBounds.X, 0.01, "r1 居中 X");
    }

    [TestMethod]
    public void PreLayout_AbsolutePanel_HorizontalAlignment_Right()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Width = 800,
            Children =
            {
                new SlideMlRectElement
                {
                    Id = "r1",
                    Width = 200,
                    Height = 100,
                    HorizontalAlignment = SlideMlHorizontalAlignment.Right,
                },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        // panel Width=800 → provisionalWidth=800, (800-200)=600
        Assert.AreEqual(600, r1.LayoutBounds.X, 0.01, "r1 右对齐 X");
    }

    [TestMethod]
    public void PreLayout_AbsolutePanel_VerticalAlignment_Center()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Height = 600,
            Children =
            {
                new SlideMlRectElement
                {
                    Id = "r1",
                    Width = 100,
                    Height = 200,
                    VerticalAlignment = SlideMlVerticalAlignment.Center,
                },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        // panel Height=600 → provisionalHeight=600, (600-200)/2=200
        Assert.AreEqual(200, r1.LayoutBounds.Y, 0.01, "r1 垂直居中 Y");
    }

    [TestMethod]
    public void PreLayout_AbsolutePanel_ExplicitXY_PrecedesAlignment()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Width = 800,
            Height = 600,
            Children =
            {
                new SlideMlRectElement
                {
                    Id = "r1",
                    X = 10,
                    Y = 20,
                    Width = 200,
                    Height = 100,
                    HorizontalAlignment = SlideMlHorizontalAlignment.Center,
                    VerticalAlignment = SlideMlVerticalAlignment.Bottom,
                },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        Assert.AreEqual(10, r1.LayoutBounds.X, 0.01, "X 优先于 Alignment");
        Assert.AreEqual(20, r1.LayoutBounds.Y, 0.01, "Y 优先于 Alignment");
        Assert.AreEqual(200, r1.LayoutBounds.Width, 0.01, "Width");
        Assert.AreEqual(100, r1.LayoutBounds.Height, 0.01, "Height");
    }

    [TestMethod]
    public void PreLayout_ChildExceedsParent_NoClipWarning_AtPage()
    {
        var rect = new SlideMlRectElement { Id = "r1", X = -100, Y = -100, Width = 2000, Height = 2000 };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        // Page 直接子元素 clipToParent=false，不产生父容器裁剪警告
        Assert.IsFalse(_context.Warnings.Any(w => w.Contains("超出父容器")), "Page 直接子元素不应产生裁剪警告");

        // 但应产生画布边界警告（左/上/右/下均超出）
        Assert.IsTrue(_context.Warnings.Any(w => w.Contains("超出画布宽度")), "应产生右边界超出画布宽度警告");
        Assert.IsTrue(_context.Warnings.Any(w => w.Contains("超出画布高度")), "应产生下边界超出画布高度警告");
        Assert.IsTrue(_context.Warnings.Any(w => w.Contains("超出画布左侧")), "应产生左边界超出警告");
        Assert.IsTrue(_context.Warnings.Any(w => w.Contains("超出画布顶部")), "应产生上边界超出警告");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
