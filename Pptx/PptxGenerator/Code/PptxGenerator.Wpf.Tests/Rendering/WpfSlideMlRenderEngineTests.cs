using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;
using System.Windows;

namespace PptxGenerator.Wpf.Tests.Rendering;

[TestClass]
public sealed class WpfSlideMlRenderEngineTests
{
    [TestMethod(DisplayName = "按 Uniform 布局图片时应居中并保持比例")]
    public void CalculateImageDestination_WhenUniformThenCentersAndPreservesRatio()
    {
        var destination = new Rect(10, 20, 200, 200);
        var source = new Rect(0, 0, 400, 200);

        var result = WpfSlideMlRenderEngine.CalculateImageDestination(destination, source, SlideMlImageStretch.Uniform);

        Assert.AreEqual(new Rect(10, 70, 200, 100), result);
    }

    [TestMethod(DisplayName = "按 UniformToFill 布局图片时应居中并填满区域")]
    public void CalculateImageDestination_WhenUniformToFillThenCentersAndCoversArea()
    {
        var destination = new Rect(10, 20, 200, 200);
        var source = new Rect(0, 0, 400, 200);

        var result = WpfSlideMlRenderEngine.CalculateImageDestination(destination, source, SlideMlImageStretch.UniformToFill);

        Assert.AreEqual(new Rect(-90, 20, 400, 200), result);
    }

    [STATestMethod(DisplayName = "渲染简单页面后应能保存 PNG 数据")]
    public void Render_WhenSimplePageThenSavesPngData()
    {
        var page = new SlideMlPage
        {
            Background = "#FFFFFFFF",
        };
        page.Children.Add(new SlideMlTextElement
        {
            Id = "title",
            Text = "Hello SlideML",
            X = 10,
            Y = 10,
            Width = 180,
            Height = 40,
            FontSize = 20,
            LayoutBounds = new SlideMlRect(10, 10, 180, 40),
        });
        var context = new SlideMlPipelineContext(240, 120);
        var engine = new WpfSlideMlRenderEngine();

        _ = engine.PreMeasure(page, context);
        var image = engine.Render(page, context);
        using var stream = new MemoryStream();
        image.Save(stream);

        Assert.IsTrue(stream.Length > 8);
        var bytes = stream.ToArray();
        CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes.Take(4).ToArray());
    }
}
