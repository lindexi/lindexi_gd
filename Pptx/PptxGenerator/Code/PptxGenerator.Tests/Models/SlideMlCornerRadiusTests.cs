using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlCornerRadiusTests
{
    [TestMethod]
    public void Parse_SingleValue_AllCornersEqual()
    {
        var result = SlideMlCornerRadius.Parse("8");

        Assert.IsNotNull(result);
        Assert.AreEqual(8, result!.Value.TopLeft);
        Assert.AreEqual(8, result.Value.TopRight);
        Assert.AreEqual(8, result.Value.BottomRight);
        Assert.AreEqual(8, result.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_TwoValues_DiagonalPair()
    {
        var result = SlideMlCornerRadius.Parse("8,16");

        Assert.IsNotNull(result);
        Assert.AreEqual(8, result!.Value.TopLeft);
        Assert.AreEqual(16, result.Value.TopRight);
        Assert.AreEqual(8, result.Value.BottomRight);
        Assert.AreEqual(16, result.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_ThreeValues_Expanded()
    {
        var result = SlideMlCornerRadius.Parse("8,16,8");

        Assert.IsNotNull(result);
        Assert.AreEqual(8, result!.Value.TopLeft);
        Assert.AreEqual(16, result.Value.TopRight);
        Assert.AreEqual(8, result.Value.BottomRight);
        Assert.AreEqual(16, result.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_FourValues_AllIndependent()
    {
        var result = SlideMlCornerRadius.Parse("8,16,12,20");

        Assert.IsNotNull(result);
        Assert.AreEqual(8, result!.Value.TopLeft);
        Assert.AreEqual(16, result.Value.TopRight);
        Assert.AreEqual(12, result.Value.BottomRight);
        Assert.AreEqual(20, result.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_Null_ReturnsNull()
    {
        var result = SlideMlCornerRadius.Parse(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_EmptyString_ReturnsNull()
    {
        var result = SlideMlCornerRadius.Parse("");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_Whitespace_ReturnsNull()
    {
        var result = SlideMlCornerRadius.Parse("  ");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_InvalidNumber_ReturnsNull()
    {
        var result = SlideMlCornerRadius.Parse("abc");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_MixedValidInvalid_ReturnsNull()
    {
        var result = SlideMlCornerRadius.Parse("8,abc,8,16");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_NegativeValues_Parsed()
    {
        var result = SlideMlCornerRadius.Parse("-8,16,-8,16");

        Assert.IsNotNull(result);
        Assert.AreEqual(-8, result!.Value.TopLeft);
        Assert.AreEqual(16, result.Value.TopRight);
        Assert.AreEqual(-8, result.Value.BottomRight);
        Assert.AreEqual(16, result.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_ZeroValue_Parsed()
    {
        var result = SlideMlCornerRadius.Parse("0");

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result!.Value.TopLeft);
        Assert.AreEqual(0, result.Value.TopRight);
        Assert.AreEqual(0, result.Value.BottomRight);
        Assert.AreEqual(0, result.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_DecimalValues_Parsed()
    {
        var result = SlideMlCornerRadius.Parse("8.5,16.3,12.7,20.1");

        Assert.IsNotNull(result);
        Assert.AreEqual(8.5, result!.Value.TopLeft);
        Assert.AreEqual(16.3, result.Value.TopRight);
        Assert.AreEqual(12.7, result.Value.BottomRight);
        Assert.AreEqual(20.1, result.Value.BottomLeft);
    }

    [TestMethod]
    public void Parse_MoreThanFourValues_FirstFourUsed()
    {
        var result = SlideMlCornerRadius.Parse("1,2,3,4,5,6");

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result!.Value.TopLeft);
        Assert.AreEqual(2, result.Value.TopRight);
        Assert.AreEqual(3, result.Value.BottomRight);
        Assert.AreEqual(4, result.Value.BottomLeft);
    }

    [TestMethod]
    public void ImplicitConversion_DoubleToCornerRadius()
    {
        SlideMlCornerRadius result = 12.5;

        Assert.AreEqual(12.5, result.TopLeft);
        Assert.AreEqual(12.5, result.TopRight);
        Assert.AreEqual(12.5, result.BottomRight);
        Assert.AreEqual(12.5, result.BottomLeft);
    }
}
