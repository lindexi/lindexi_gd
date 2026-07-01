using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlLayoutEngine 特殊场景与幂等性测试。
/// 对应测试用例文档第 11 章（特殊场景）和第 12 章（幂等性/一致性）。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineSpecialScenarioTests
{
    private SlideMlLayoutEngine _engine = null!;
    private SlideMlPipelineContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new SlideMlLayoutEngine();
        _context = new SlideMlPipelineContext();
    }

    // ───────── 第 11 章：特殊场景 ─────────

    [TestMethod]
    public void PreLayout_CustomCanvasSize()
    {
        _context = new SlideMlPipelineContext(canvasWidth: 1920, canvasHeight: 1080);

        var rect = new SlideMlRectElement { Id = "r1", X = 1800, Y = 1000, Width = 200, Height = 200 };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, page.LayoutBounds.X, 0.01, "page X");
        Assert.AreEqual(0, page.LayoutBounds.Y, 0.01, "page Y");
        Assert.AreEqual(1920, page.LayoutBounds.Width, 0.01, "page Width 自定义画布");
        Assert.AreEqual(1080, page.LayoutBounds.Height, 0.01, "page Height 自定义画布");

        // 1800+200=2000 > 1920 → 右边界警告
        Assert.IsTrue(
            _context.Warnings.Any(w => w.Contains("右边界") && w.Contains("超出画布宽度")),
            "自定义画布下右边界超出应产生 Warning");
    }

    [TestMethod]
    public void PreLayout_ZeroSizeElement_NoCrash()
    {
        var rect = new SlideMlRectElement { Id = "r1", Width = 0, Height = 0 };

        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, rect.ActualWidth, 0.01, "零尺寸元素宽度");
        Assert.AreEqual(0, rect.ActualHeight, 0.01, "零尺寸元素高度");
    }

    [TestMethod]
    public void PreLayout_PanelPaddingOnly_NoChildren()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Padding = new SlideMlThickness { Left = 24, Top = 24, Right = 24, Bottom = 24 },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        // 无子元素: contentRight=0, contentBottom=0
        // actualWidth = 0 + 24*2 = 48, actualHeight = 0 + 24*2 = 48
        Assert.AreEqual(48, panel.ActualWidth, 0.01, "无子元素时 Panel 宽度 = Padding*2");
        Assert.AreEqual(48, panel.ActualHeight, 0.01, "无子元素时 Panel 高度 = Padding*2");
    }

    [TestMethod]
    public void PreLayout_FlowChildExplicitAxisPosition()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideMlRectElement { Id = "r1", X = 50, Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];

        // 流式布局中排列轴方向的 X 若设置则脱离流，r1 使用显式坐标 50
        Assert.AreEqual(50, r1.LayoutBounds.X, 0.01, "r1 排列轴 X 设置后脱离流，使用显式坐标");
        // r1 的 LocalBounds.X 保留声明的 X 值
        Assert.AreEqual(50, r1.LocalBounds.X, 0.01, "r1 LocalBounds.X 保留声明值");
        // r2 脱离流后不参与流式计算，r2 从 flowPosition=0 开始（前面无流式元素）
        Assert.AreEqual(0, r2.LayoutBounds.X, 0.01, "r2 X 从流式起始位置开始，r1 已脱离流");
    }

    // ───────── 第 12 章：幂等性/一致性 ─────────

    [TestMethod]
    public void PreLayout_Twice_Idempotent()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);

        _engine.PreLayout(page, _context);

        var r1First = (SlideMlRectElement)panel.Children[0];
        var r2First = (SlideMlRectElement)panel.Children[1];
        var firstR1Bounds = r1First.LayoutBounds;
        var firstR2Bounds = r2First.LayoutBounds;
        var firstPanelWidth = panel.ActualWidth;
        var firstPanelHeight = panel.ActualHeight;

        _engine.PreLayout(page, _context);

        var r1Second = (SlideMlRectElement)panel.Children[0];
        var r2Second = (SlideMlRectElement)panel.Children[1];

        Assert.AreEqual(firstR1Bounds, r1Second.LayoutBounds, "第二次 PreLayout 后 r1 LayoutBounds 应一致");
        Assert.AreEqual(firstR2Bounds, r2Second.LayoutBounds, "第二次 PreLayout 后 r2 LayoutBounds 应一致");
        Assert.AreEqual(firstPanelWidth, panel.ActualWidth, 0.01, "第二次 PreLayout 后 panel width 应一致");
        Assert.AreEqual(firstPanelHeight, panel.ActualHeight, 0.01, "第二次 PreLayout 后 panel height 应一致");
    }

    [TestMethod]
    public void PreLayoutThenFinalLayout_CorrectOverride()
    {
        var text = new SlideMlTextElement { Id = "t1", Width = 500, Text = "Hello" };

        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 120, MeasuredHeight = 24 },
        });

        var page = CreatePage(text);

        // PreLayout: Width=500, Height=0（TextElement 默认）
        _engine.PreLayout(page, _context);
        Assert.AreEqual(500, text.ActualWidth, 0.01, "PreLayout 后 Width");
        Assert.AreEqual(0, text.ActualHeight, 0.01, "PreLayout 后 Height 默认 0");

        // FinalLayout: Width=500（声明优先），Height=24（测量值覆盖）
        _engine.FinalLayout(page, _context, measurements);
        Assert.AreEqual(500, text.ActualWidth, 0.01, "FinalLayout 后 Width 保持声明值");
        Assert.AreEqual(24, text.ActualHeight, 0.01, "FinalLayout 后 Height 被测量值覆盖");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
