using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareCanvasAdapterTests
{
    [TestMethod(DisplayName = "页面画布适配应统一取整并创建文档上下文")]
    public void CreateDocumentContextShouldRoundSourceDimensionsOnce()
    {
        var context = CoursewareCanvasAdapter.CreateDocumentContext(1024.4, 576.6);

        Assert.AreEqual(1024, context.CanvasWidth);
        Assert.AreEqual(577, context.CanvasHeight);
    }

    [TestMethod(DisplayName = "页面画布适配应拒绝非正数和非有限尺寸")]
    public void CreateDocumentContextShouldRejectInvalidDimensions()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CoursewareCanvasAdapter.CreateDocumentContext(0, 720));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CoursewareCanvasAdapter.CreateDocumentContext(1280, double.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CoursewareCanvasAdapter.CreateDocumentContext((double)int.MaxValue + 1, 720));
    }

    [TestMethod(DisplayName = "主题坐标应从分析参考画布缩放到当前页面画布")]
    public void ScaleThemeCoordinatesShouldUseReferenceAndCurrentCanvas()
    {
        var referenceCanvas = new SlideDocumentContext(1280, 720);
        var slideCanvas = new SlideDocumentContext(1920, 1080);
        var safeArea = new CoursewareSafeArea { Left = 60, Top = 40, Right = 80, Bottom = 50 };

        var scaledSafeArea = CoursewareCanvasAdapter.ScaleSafeArea(safeArea, referenceCanvas, slideCanvas);
        var scaledFontSize = CoursewareCanvasAdapter.ScaleFontSize(32, referenceCanvas, slideCanvas);

        Assert.AreEqual(90, scaledSafeArea.Left);
        Assert.AreEqual(60, scaledSafeArea.Top);
        Assert.AreEqual(120, scaledSafeArea.Right);
        Assert.AreEqual(75, scaledSafeArea.Bottom);
        Assert.AreEqual(48, scaledFontSize);
    }
}
