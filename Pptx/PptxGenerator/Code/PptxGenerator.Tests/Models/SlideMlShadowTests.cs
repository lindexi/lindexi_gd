using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlShadowTests
{
    [TestMethod]
    public void Parse_FourParts_AllProperties()
    {
        var result = SlideMlShadow.Parse("0 4 12 #00000033");

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result!.OffsetX);
        Assert.AreEqual(4, result.OffsetY);
        Assert.AreEqual(12, result.Blur);
        Assert.AreEqual("#00000033", result.Color);
        Assert.AreEqual(1, result.Opacity);
    }

    [TestMethod]
    public void Parse_TwoParts_OffsetOnly()
    {
        var result = SlideMlShadow.Parse("0 4");

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result!.OffsetX);
        Assert.AreEqual(4, result.OffsetY);
        Assert.AreEqual(12, result.Blur);
        Assert.AreEqual("#00000033", result.Color);
        Assert.AreEqual(1, result.Opacity);
    }

    [TestMethod]
    public void Parse_ThreeParts_OffsetAndBlur()
    {
        var result = SlideMlShadow.Parse("0 4 24");

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result!.OffsetX);
        Assert.AreEqual(4, result.OffsetY);
        Assert.AreEqual(24, result.Blur);
        Assert.AreEqual("#00000033", result.Color);
    }

    [TestMethod]
    public void Parse_OnePart_ReturnsNull()
    {
        // 实现要求 parts.Length >= 2，单值返回 null
        var result = SlideMlShadow.Parse("5");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_Null_ReturnsNull()
    {
        var result = SlideMlShadow.Parse(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_EmptyString_ReturnsNull()
    {
        var result = SlideMlShadow.Parse("");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_ColorWithHash_ColorParsed()
    {
        var result = SlideMlShadow.Parse("0 8 24 #FF0000");

        Assert.IsNotNull(result);
        Assert.AreEqual("#FF0000", result!.Color);
    }

    [TestMethod]
    public void Parse_ColorWithoutHash_ColorParsed()
    {
        var result = SlideMlShadow.Parse("0 8 24 FF0000");

        Assert.IsNotNull(result);
        Assert.AreEqual("FF0000", result!.Color);
    }

    [TestMethod]
    public void Parse_NegativeOffset_Parsed()
    {
        var result = SlideMlShadow.Parse("-2 4 12 #00000033");

        Assert.IsNotNull(result);
        Assert.AreEqual(-2, result!.OffsetX);
        Assert.AreEqual(4, result.OffsetY);
    }

    [TestMethod]
    public void Parse_DecimalValues_Parsed()
    {
        var result = SlideMlShadow.Parse("0.5 4.5 12.8 #00000033");

        Assert.IsNotNull(result);
        Assert.AreEqual(0.5, result!.OffsetX);
        Assert.AreEqual(4.5, result.OffsetY);
        Assert.AreEqual(12.8, result.Blur);
    }

    [TestMethod]
    public void Parse_ExtraSpaces_Parsed()
    {
        var result = SlideMlShadow.Parse("  0   4   12   #00000033  ");

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result!.OffsetX);
        Assert.AreEqual(4, result.OffsetY);
        Assert.AreEqual(12, result.Blur);
        Assert.AreEqual("#00000033", result.Color);
    }
}
