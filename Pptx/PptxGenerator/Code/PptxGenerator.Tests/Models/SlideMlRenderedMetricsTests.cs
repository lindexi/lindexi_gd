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
            ActualWidth = 200,
            ActualHeight = 100,
            ActualLineCount = 3,
        };

        Assert.AreEqual(200, metrics.ActualWidth);
        Assert.AreEqual(100, metrics.ActualHeight);
        Assert.AreEqual(3, metrics.ActualLineCount);
    }

    [TestMethod]
    public void ActualLineCount_Nullable()
    {
        var metrics = new SlideMlRenderedMetrics
        {
            ActualWidth = 200,
            ActualHeight = 100,
        };

        Assert.IsNull(metrics.ActualLineCount);
    }
}
