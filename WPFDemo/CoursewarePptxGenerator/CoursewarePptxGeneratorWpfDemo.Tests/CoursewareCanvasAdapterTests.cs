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

}
