using PptxGenerator.Models;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlMeasureResultTests
{
    [TestMethod]
    public void Constructor_PropertiesSet()
    {
        var result = new SlideMlMeasureResult
        {
            MeasuredWidth = 100,
            MeasuredHeight = 50,
            ActualLineCount = 2,
        };

        Assert.AreEqual(100, result.MeasuredWidth);
        Assert.AreEqual(50, result.MeasuredHeight);
        Assert.AreEqual(2, result.ActualLineCount);
    }

    [TestMethod]
    public void ActualLineCount_Nullable()
    {
        var result = new SlideMlMeasureResult
        {
            MeasuredWidth = 100,
            MeasuredHeight = 50,
        };

        Assert.IsNull(result.ActualLineCount);
    }
}
