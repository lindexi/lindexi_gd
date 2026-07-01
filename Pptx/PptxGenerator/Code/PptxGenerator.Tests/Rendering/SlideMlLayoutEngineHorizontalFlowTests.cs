using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlLayoutEngine 水平流式布局测试。
/// 对应测试用例文档第 2 章（PreLayout — 水平流式布局），不含已有用例 2.1/2.3/2.4/2.5/2.8/2.10。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineHorizontalFlowTests
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
    public void PreLayout_HorizontalFlow_NoGap_ChildrenTouching()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", Width = 200, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];

        Assert.AreEqual(0, r1.LayoutBounds.X, 0.01, "r1 X");
        Assert.AreEqual(100, r2.LayoutBounds.X, 0.01, "r2 X 无 Gap 时紧贴");
        Assert.AreEqual(300, panel.MeasuredWidth, 0.01, "panel width");
    }

    [TestMethod]
    public void PreLayout_HorizontalFlow_ExplicitY_OverridesAlignment()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Height = 200,
            Children =
            {
                new SlideMlRectElement
                {
                    Id = "r1",
                    Width = 100,
                    Height = 50,
                    Y = 10,
                    VerticalAlignment = SlideMlVerticalAlignment.Center,
                },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        Assert.AreEqual(10, r1.LayoutBounds.Y, 0.01, "Y 优先于跨轴对齐");
    }

    [TestMethod]
    public void PreLayout_HorizontalFlow_AutoWidth()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100 },
                new SlideMlRectElement { Id = "r2", Width = 150 },
                new SlideMlRectElement { Id = "r3", Width = 80 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(100 + 8 + 150 + 8 + 80, panel.MeasuredWidth, 0.01, "panel 自动宽度");
    }

    [TestMethod]
    public void PreLayout_HorizontalFlow_FixedHeight()
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
                new SlideMlRectElement { Id = "r2", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(200, panel.MeasuredHeight, 0.01, "panel 固定高度");

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];
        Assert.AreEqual(0, r1.LayoutBounds.Y, 0.01, "r1 Y 默认 Top");
        Assert.AreEqual(0, r2.LayoutBounds.Y, 0.01, "r2 Y 默认 Top");
    }

    [TestMethod]
    public void PreLayout_HorizontalFlow_SingleChild()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 200, Height = 100 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        Assert.AreEqual(0, r1.LayoutBounds.X, 0.01, "r1 X");
        Assert.AreEqual(200, panel.MeasuredWidth, 0.01, "panel width");
    }

    [TestMethod]
    public void PreLayout_HorizontalFlow_MixedElementTypes()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideMlRectElement { Id = "rect", Width = 100, Height = 50 },
                new SlideMlTextElement { Id = "text", Width = 150, Height = 24 },
                new SlideMlImageElement { Id = "img", Width = 80, Height = 80 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var rect = (SlideMlRectElement)panel.Children[0];
        var text = (SlideMlTextElement)panel.Children[1];
        var image = (SlideMlImageElement)panel.Children[2];

        Assert.AreEqual(0, rect.LayoutBounds.X, 0.01, "rect X");
        Assert.AreEqual(100 + 8, text.LayoutBounds.X, 0.01, "text X");
        Assert.AreEqual(100 + 8 + 150 + 8, image.LayoutBounds.X, 0.01, "image X");
    }

    [TestMethod]
    public void PreLayout_HorizontalFlow_TextElementZeroSize()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Children =
            {
                new SlideMlTextElement { Id = "t1", Text = "Hello" },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var t1 = (SlideMlTextElement)panel.Children[0];
        Assert.AreEqual(0, t1.MeasuredWidth, 0.01, "PreLayout 中 TextElement 默认宽度 0");
        Assert.AreEqual(0, t1.MeasuredHeight, 0.01, "PreLayout 中 TextElement 默认高度 0");
    }

    [TestMethod]
    public void PreLayout_HorizontalFlow_ImageDefaultSize()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Children =
            {
                new SlideMlImageElement { Id = "img", Source = "img.png" },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var img = (SlideMlImageElement)panel.Children[0];
        Assert.AreEqual(240, img.MeasuredWidth, 0.01, "Image 默认宽度 240");
        Assert.AreEqual(180, img.MeasuredHeight, 0.01, "Image 默认高度 180");
    }

    [TestMethod]
    public void PreLayout_HorizontalFlow_MarginTopBottom_CrossAxisNotAffected()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Height = 200,
            Children =
            {
                new SlideMlRectElement
                {
                    Id = "r1",
                    Width = 100,
                    Height = 50,
                    Margin = new SlideMlThickness { Top = 20 },
                },
                new SlideMlRectElement { Id = "r2", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];

        // 水平流式中 Margin.Top/Bottom 不影响排列轴间距
        Assert.AreEqual(0, r1.LayoutBounds.X, 0.01, "r1 X");
        // r2 X = r1.Width + max(Gap=0, prevTrailingMargin.Right=0 + leadingMargin.Left=0) = 100
        Assert.AreEqual(100, r2.LayoutBounds.X, 0.01, "r2 X 不受 Margin.Top 影响");
    }

    [TestMethod]
    public void PreLayout_HorizontalFlow_ExactFit_NoWarning()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Width = 208,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        // totalFlowSize = 100+8+100 = 208 == fixedWidth = 208，不触发溢出警告
        Assert.IsFalse(
            _context.Warnings.Any(w => w.Contains("流式布局内容宽度") && w.Contains("超出")),
            "内容宽度刚好等于 Panel 宽度时不应产生溢出警告");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
