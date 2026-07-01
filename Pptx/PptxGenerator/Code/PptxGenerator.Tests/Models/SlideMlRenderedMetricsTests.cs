using PptxGenerator.Models;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlRenderedMetricsTests
{
    [TestMethod]
    public void Constructor_PropertiesSet()
    {
        var metrics = new SlideMlRenderedMetrics
        {
            RenderSize = "200x100",
            RenderLocation = "10x20",
            ActualLineCount = 3,
        };

        Assert.AreEqual("200x100", metrics.RenderSize);
        Assert.AreEqual("10x20", metrics.RenderLocation);
        Assert.AreEqual(3, metrics.ActualLineCount);
    }

    [TestMethod]
    public void ActualLineCount_Nullable()
    {
        var metrics = new SlideMlRenderedMetrics
        {
            RenderSize = "200x100",
        };

        Assert.IsNull(metrics.ActualLineCount);
    }
}
