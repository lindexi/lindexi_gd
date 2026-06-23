using PptxGenerator.Models;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlElementMeasurementsTests
{
    [TestMethod]
    public void TryGetValue_ExistingId_ReturnsTrue()
    {
        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 100, MeasuredHeight = 50, ActualLineCount = 1 },
        });

        var found = measurements.TryGetValue("t1", out var result);

        Assert.IsTrue(found);
        Assert.IsNotNull(result);
        Assert.AreEqual(100, result!.MeasuredWidth);
        Assert.AreEqual(50, result.MeasuredHeight);
        Assert.AreEqual(1, result.ActualLineCount);
    }

    [TestMethod]
    public void TryGetValue_NonExistingId_ReturnsFalse()
    {
        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>());

        var found = measurements.TryGetValue("nonexist", out var result);

        Assert.IsFalse(found);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Find_ExistingId_ReturnsResult()
    {
        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 100, MeasuredHeight = 50, ActualLineCount = 1 },
        });

        var result = measurements.Find("t1");

        Assert.IsNotNull(result);
        Assert.AreEqual(100, result!.MeasuredWidth);
        Assert.AreEqual(50, result.MeasuredHeight);
    }

    [TestMethod]
    public void Find_NonExistingId_ReturnsNull()
    {
        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>());

        var result = measurements.Find("nonexist");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Constructor_NullDictionary_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new SlideMlElementMeasurements(null!));
    }
}
