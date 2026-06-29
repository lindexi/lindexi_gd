using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlLayoutEngine FinalLayout 测试。
/// 对应测试用例文档第 7 章（FinalLayout），不含已有用例 7.1。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineFinalLayoutTests
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
    public void FinalLayout_TextElement_ActualLineCountBackfill()
    {
        var text = new SlideMlTextElement
        {
            Id = "t1",
            Width = 200,
            Height = 50,
            Text = "多行文本内容",
        };

        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 200, MeasuredHeight = 48, ActualLineCount = 3 },
        });

        var page = CreatePage(text);
        _engine.FinalLayout(page, _context, measurements);

        Assert.AreEqual(3, text.ActualLineCount, "ActualLineCount 应被测量值回填");
    }

    [TestMethod]
    public void FinalLayout_TextHeightOverflow_Warning()
    {
        var text = new SlideMlTextElement
        {
            Id = "t1",
            Width = 400,
            Height = 30,
            Text = "多行文本内容",
        };

        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 400, MeasuredHeight = 80, ActualLineCount = 5 },
        });

        var page = CreatePage(text);
        _engine.FinalLayout(page, _context, measurements);

        Assert.IsTrue(
            _context.Warnings.Any(w => w.Contains("超出容器高度")),
            "文本测量高度超出声明高度应产生 Warning");
    }

    [TestMethod]
    public void FinalLayout_TextHeightExactlyFit_NoWarning()
    {
        var text = new SlideMlTextElement
        {
            Id = "t1",
            Width = 400,
            Height = 48,
            Text = "文本内容",
        };

        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 400, MeasuredHeight = 48, ActualLineCount = 3 },
        });

        var page = CreatePage(text);
        _engine.FinalLayout(page, _context, measurements);

        Assert.IsFalse(
            _context.Warnings.Any(w => w.Contains("超出容器高度")),
            "测量高度刚好等于声明高度时不应产生 Warning");
    }

    [TestMethod]
    public void FinalLayout_Image_MeasuredSize()
    {
        var image = new SlideMlImageElement { Id = "img", Source = "img.png" };

        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["img"] = new() { MeasuredWidth = 800, MeasuredHeight = 600 },
        });

        var page = CreatePage(image);
        _engine.FinalLayout(page, _context, measurements);

        Assert.AreEqual(800, image.ActualWidth, 0.01, "Image 测量宽度");
        Assert.AreEqual(600, image.ActualHeight, 0.01, "Image 测量高度");
    }

    [TestMethod]
    public void FinalLayout_DeclaredWidth_PrecedesMeasured()
    {
        var text = new SlideMlTextElement { Id = "t1", Width = 500, Text = "Hello" };

        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 120, MeasuredHeight = 24 },
        });

        var page = CreatePage(text);
        _engine.FinalLayout(page, _context, measurements);

        // child.Width ?? MeasuredWidth → 500 优先于 120
        Assert.AreEqual(500, text.ActualWidth, 0.01, "声明 Width 优先于测量值");
        // Height 无声明值，使用测量值 24
        Assert.AreEqual(24, text.ActualHeight, 0.01, "无声明 Height 时使用测量值");
    }

    [TestMethod]
    public void FinalLayout_Rect_NoMeasurement_Unchanged()
    {
        var rect = new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 };

        // 测量值字典中无此 Rect 的 Id
        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>());

        var page = CreatePage(rect);
        _engine.FinalLayout(page, _context, measurements);

        Assert.AreEqual(100, rect.ActualWidth, 0.01, "无测量值的 Rect 宽度不变");
        Assert.AreEqual(50, rect.ActualHeight, 0.01, "无测量值的 Rect 高度不变");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
