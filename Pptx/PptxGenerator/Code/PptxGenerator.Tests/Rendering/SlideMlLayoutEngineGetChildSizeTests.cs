using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlLayoutEngine.GetChildSize 逻辑测试。
/// 对应测试用例文档第 10 章。
/// GetChildSize 为 private static 方法，通过公共 API（PreLayout/FinalLayout）间接测试。
/// </summary>
[TestClass]
public sealed class SlideMlLayoutEngineGetChildSizeTests
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
    public void GetChildSize_WithMeasurement_ReturnsMeasured()
    {
        var text = new SlideMlTextElement { Id = "t1", Text = "Hello" };

        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 200, MeasuredHeight = 40 },
        });

        var page = CreatePage(text);
        _engine.FinalLayout(page, _context, measurements);

        // 无声明 Width/Height，使用测量值 (200, 40)
        Assert.AreEqual(200, text.MeasuredWidth, 0.01, "测量宽度");
        Assert.AreEqual(40, text.MeasuredHeight, 0.01, "测量高度");
    }

    [TestMethod]
    public void GetChildSize_DeclaredWidth_WithMeasurement()
    {
        var text = new SlideMlTextElement { Id = "t1", Width = 500, Text = "Hello" };

        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 200, MeasuredHeight = 40 },
        });

        var page = CreatePage(text);
        _engine.FinalLayout(page, _context, measurements);

        // Width 使用声明值 500，Height 使用测量值 40（声明 Height 为 null）
        Assert.AreEqual(500, text.MeasuredWidth, 0.01, "声明 Width 优先");
        Assert.AreEqual(40, text.MeasuredHeight, 0.01, "无声明 Height 时使用测量值");
    }

    [TestMethod]
    public void GetChildSize_TextElement_Default()
    {
        var text = new SlideMlTextElement { Id = "t1", Text = "Hello" };

        // PreLayout 中 useMeasured=false，无测量值
        var page = CreatePage(text);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, text.MeasuredWidth, 0.01, "TextElement 默认宽度 0");
        Assert.AreEqual(0, text.MeasuredHeight, 0.01, "TextElement 默认高度 0");
    }

    [TestMethod]
    public void GetChildSize_Image_Default()
    {
        var image = new SlideMlImageElement { Id = "img", Source = "img.png" };

        // PreLayout 中 useMeasured=false，无测量值
        var page = CreatePage(image);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(240, image.MeasuredWidth, 0.01, "Image 默认宽度 240");
        Assert.AreEqual(180, image.MeasuredHeight, 0.01, "Image 默认高度 180");
    }

    [TestMethod]
    public void GetChildSize_Rect_Default()
    {
        var rect = new SlideMlRectElement { Id = "r1" };

        // PreLayout 中 useMeasured=false，无测量值
        var page = CreatePage(rect);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, rect.MeasuredWidth, 0.01, "Rect 默认宽度 0");
        Assert.AreEqual(0, rect.MeasuredHeight, 0.01, "Rect 默认高度 0");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
