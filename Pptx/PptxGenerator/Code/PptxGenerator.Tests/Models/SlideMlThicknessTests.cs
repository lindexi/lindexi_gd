using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlThicknessTests
{
    [TestMethod]
    public void Parse_SingleValue_AllSidesEqual()
    {
        var result = SlideMlThickness.Parse("10");

        Assert.IsNotNull(result);
        Assert.AreEqual(10, result!.Value.Left);
        Assert.AreEqual(10, result.Value.Top);
        Assert.AreEqual(10, result.Value.Right);
        Assert.AreEqual(10, result.Value.Bottom);
    }

    [TestMethod]
    public void Parse_TwoValues_VerticalHorizontal()
    {
        var result = SlideMlThickness.Parse("10,20");

        Assert.IsNotNull(result);
        Assert.AreEqual(20, result!.Value.Left);
        Assert.AreEqual(10, result.Value.Top);
        Assert.AreEqual(20, result.Value.Right);
        Assert.AreEqual(10, result.Value.Bottom);
    }

    [TestMethod]
    public void Parse_ThreeValues_Expanded()
    {
        var result = SlideMlThickness.Parse("10,20,30");

        Assert.IsNotNull(result);
        Assert.AreEqual(20, result!.Value.Left);
        Assert.AreEqual(10, result.Value.Top);
        Assert.AreEqual(20, result.Value.Right);
        Assert.AreEqual(30, result.Value.Bottom);
    }

    [TestMethod]
    public void Parse_FourValues_LeftTopRightBottom()
    {
        var result = SlideMlThickness.Parse("10,20,30,40");

        Assert.IsNotNull(result);
        Assert.AreEqual(10, result!.Value.Left);
        Assert.AreEqual(20, result.Value.Top);
        Assert.AreEqual(30, result.Value.Right);
        Assert.AreEqual(40, result.Value.Bottom);
    }

    [TestMethod]
    public void Parse_Null_ReturnsNull()
    {
        var result = SlideMlThickness.Parse(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_EmptyString_ReturnsNull()
    {
        var result = SlideMlThickness.Parse("");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_InvalidNumber_ReturnsNull()
    {
        var result = SlideMlThickness.Parse("abc");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_NegativeValues_Parsed()
    {
        var result = SlideMlThickness.Parse("-10,20,-30,40");

        Assert.IsNotNull(result);
        Assert.AreEqual(-10, result!.Value.Left);
        Assert.AreEqual(20, result.Value.Top);
        Assert.AreEqual(-30, result.Value.Right);
        Assert.AreEqual(40, result.Value.Bottom);
    }

    [TestMethod]
    public void Parse_DecimalValues_Parsed()
    {
        var result = SlideMlThickness.Parse("10.5,20.3");

        Assert.IsNotNull(result);
        Assert.AreEqual(20.3, result!.Value.Left);
        Assert.AreEqual(10.5, result.Value.Top);
        Assert.AreEqual(20.3, result.Value.Right);
        Assert.AreEqual(10.5, result.Value.Bottom);
    }

    [TestMethod]
    public void Parse_MoreThanFourValues_FirstFourUsed()
    {
        var result = SlideMlThickness.Parse("1,2,3,4,5");

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result!.Value.Left);
        Assert.AreEqual(2, result.Value.Top);
        Assert.AreEqual(3, result.Value.Right);
        Assert.AreEqual(4, result.Value.Bottom);
    }
}
