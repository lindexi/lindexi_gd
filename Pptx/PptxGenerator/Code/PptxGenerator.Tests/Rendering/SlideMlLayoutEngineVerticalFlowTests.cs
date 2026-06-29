using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlLayoutEngine 垂直流式布局测试。
/// 对应测试用例文档第 3 章（PreLayout — 垂直流式布局），不含已有用例 3.1。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineVerticalFlowTests
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
    public void PreLayout_VerticalFlow_MarginAffectsSpacing()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 4,
            Children =
            {
                new SlideMlRectElement
                {
                    Id = "r1",
                    Width = 200,
                    Height = 40,
                    Margin = new SlideMlThickness { Bottom = 20 },
                },
                new SlideMlRectElement
                {
                    Id = "r2",
                    Width = 200,
                    Height = 60,
                    Margin = new SlideMlThickness { Top = 10 },
                },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];

        Assert.AreEqual(0, r1.LayoutBounds.Y, 0.01, "r1 Y");
        // r2 Y = 40 + max(4, 20+10) = 40 + 30 = 70
        Assert.AreEqual(70, r2.LayoutBounds.Y, 0.01, "r2 Y with margin");
        // panel.H = 70 + 60 = 130
        Assert.AreEqual(130, panel.ActualHeight, 0.01, "panel height");
    }

    [TestMethod]
    public void PreLayout_VerticalFlow_CrossAxisAlignment_Horizontal()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Vertical,
            Width = 300,
            Gap = 8,
            Children =
            {
                new SlideMlRectElement
                {
                    Id = "r1",
                    Width = 100,
                    Height = 40,
                    HorizontalAlignment = SlideMlHorizontalAlignment.Center,
                },
                new SlideMlRectElement
                {
                    Id = "r2",
                    Width = 100,
                    Height = 40,
                    HorizontalAlignment = SlideMlHorizontalAlignment.Right,
                },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];

        // r1: Center → (300-100)/2 = 100
        Assert.AreEqual(100, r1.LayoutBounds.X, 0.01, "r1 Center X");
        // r2: Right → 300-100 = 200
        Assert.AreEqual(200, r2.LayoutBounds.X, 0.01, "r2 Right X");
    }

    [TestMethod]
    public void PreLayout_VerticalFlow_AutoHeight()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 10,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 200, Height = 50 },
                new SlideMlRectElement { Id = "r2", Width = 200, Height = 80 },
                new SlideMlRectElement { Id = "r3", Width = 200, Height = 60 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(50 + 10 + 80 + 10 + 60, panel.ActualHeight, 0.01, "panel 自动高度");
    }

    [TestMethod]
    public void PreLayout_VerticalFlow_FixedHeightOverflow_Warning()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 8,
            Height = 100,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 200, Height = 80 },
                new SlideMlRectElement { Id = "r2", Width = 200, Height = 80 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.IsTrue(
            _context.Warnings.Any(w => w.Contains("流式布局内容高度") && w.Contains("超出")),
            "应产生垂直溢出警告");
    }

    [TestMethod]
    public void PreLayout_VerticalFlow_SingleChild()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Vertical,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 200, Height = 100 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        Assert.AreEqual(0, r1.LayoutBounds.Y, 0.01, "r1 Y");
        Assert.AreEqual(100, panel.ActualHeight, 0.01, "panel height");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
