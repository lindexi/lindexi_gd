using PptxGenerator.Models;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlRectTests
{
    [TestMethod]
    public void Constructor_SetsProperties()
    {
        var rect = new SlideMlRect(10, 20, 100, 50);

        Assert.AreEqual(10, rect.X);
        Assert.AreEqual(20, rect.Y);
        Assert.AreEqual(100, rect.Width);
        Assert.AreEqual(50, rect.Height);
    }

    [TestMethod]
    public void Right_CalculatedCorrectly()
    {
        var rect = new SlideMlRect(10, 20, 100, 50);

        Assert.AreEqual(110, rect.Right);
    }

    [TestMethod]
    public void Bottom_CalculatedCorrectly()
    {
        var rect = new SlideMlRect(10, 20, 100, 50);

        Assert.AreEqual(70, rect.Bottom);
    }

    [TestMethod]
    public void CenterX_CalculatedCorrectly()
    {
        var rect = new SlideMlRect(10, 20, 100, 50);

        Assert.AreEqual(60, rect.CenterX);
    }

    [TestMethod]
    public void CenterY_CalculatedCorrectly()
    {
        var rect = new SlideMlRect(10, 20, 100, 50);

        Assert.AreEqual(45, rect.CenterY);
    }

    [TestMethod]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new SlideMlRect(10, 20, 100, 50);
        var b = new SlideMlRect(10, 20, 100, 50);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
    }

    [TestMethod]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new SlideMlRect(10, 20, 100, 50);
        var b = new SlideMlRect(10, 20, 200, 50);

        Assert.IsFalse(a.Equals(b));
        Assert.IsTrue(a != b);
    }

    [TestMethod]
    public void ZeroSize_PropertiesCorrect()
    {
        var rect = new SlideMlRect(0, 0, 0, 0);

        Assert.AreEqual(0, rect.X);
        Assert.AreEqual(0, rect.Y);
        Assert.AreEqual(0, rect.Width);
        Assert.AreEqual(0, rect.Height);
        Assert.AreEqual(0, rect.Right);
        Assert.AreEqual(0, rect.Bottom);
        Assert.AreEqual(0, rect.CenterX);
        Assert.AreEqual(0, rect.CenterY);
    }
}
