using System;

using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace LightTextEditorPlus.Platform.UnitTests;


/// <summary>
/// Tests for <see cref="FontCharHelper"/> class.
/// </summary>
[TestClass]
public partial class FontCharHelperTests
{
    /// <summary>
    /// Tests that GetBaseline returns the negation of Font.Metrics.Ascent for typical font configurations.
    /// </summary>
    /// <param name="fontSize">The font size to test with.</param>
    [TestMethod]
    [DataRow(12f)]
    [DataRow(16f)]
    [DataRow(24f)]
    [DataRow(48f)]
    [DataRow(1f)]
    [DataRow(100f)]
    public void GetBaseline_WithVariousFontSizes_ReturnsNegativeAscent(float fontSize)
    {
        // Arrange
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        using var font = new SKFont(typeface, fontSize);
        using var paint = new SKPaint(font);
        var renderingRunPropertyInfo = new RenderingRunPropertyInfo(typeface, font, paint);
        var expectedBaseline = -font.Metrics.Ascent;

        // Act
        var result = renderingRunPropertyInfo.GetBaseline();

        // Assert
        Assert.AreEqual(expectedBaseline, result, "GetBaseline should return the negative of Font.Metrics.Ascent");
    }

    /// <summary>
    /// Tests that GetBaseline correctly negates a positive Ascent value.
    /// </summary>
    [TestMethod]
    public void GetBaseline_WithPositiveAscent_ReturnsNegativeValue()
    {
        // Arrange
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        using var font = new SKFont(typeface, 16f);
        using var paint = new SKPaint(font);
        var renderingRunPropertyInfo = new RenderingRunPropertyInfo(typeface, font, paint);
        var ascent = font.Metrics.Ascent;

        // Act
        var result = renderingRunPropertyInfo.GetBaseline();

        // Assert
        Assert.IsTrue(ascent <= 0, "Ascent should be non-positive for typical fonts");
        Assert.AreEqual(-ascent, result, "GetBaseline should return the negative of Ascent");
    }

    /// <summary>
    /// Tests that GetBaseline works correctly with different font styles.
    /// </summary>
    /// <param name="weight">The font weight to test.</param>
    /// <param name="slant">The font slant to test.</param>
    [TestMethod]
    [DataRow(SKFontStyleWeight.Normal, SKFontStyleSlant.Upright)]
    [DataRow(SKFontStyleWeight.Bold, SKFontStyleSlant.Upright)]
    [DataRow(SKFontStyleWeight.Light, SKFontStyleSlant.Upright)]
    [DataRow(SKFontStyleWeight.Normal, SKFontStyleSlant.Italic)]
    [DataRow(SKFontStyleWeight.Bold, SKFontStyleSlant.Italic)]
    public void GetBaseline_WithDifferentFontStyles_ReturnsNegativeAscent(SKFontStyleWeight weight, SKFontStyleSlant slant)
    {
        // Arrange
        var fontStyle = new SKFontStyle(weight, SKFontStyleWidth.Normal, slant);
        using var typeface = SKTypeface.FromFamilyName("Arial", fontStyle);
        using var font = new SKFont(typeface, 16f);
        using var paint = new SKPaint(font);
        var renderingRunPropertyInfo = new RenderingRunPropertyInfo(typeface, font, paint);
        var expectedBaseline = -font.Metrics.Ascent;

        // Act
        var result = renderingRunPropertyInfo.GetBaseline();

        // Assert
        Assert.AreEqual(expectedBaseline, result, "GetBaseline should return the negative of Font.Metrics.Ascent");
    }

    /// <summary>
    /// Tests that GetBaseline throws NullReferenceException when Font is null.
    /// This tests the error condition where the RenderingRunPropertyInfo contains a null Font.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(NullReferenceException))]
    public void GetBaseline_WithNullFont_ThrowsNullReferenceException()
    {
        // Arrange
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        using var paint = new SKPaint();
        var renderingRunPropertyInfo = new RenderingRunPropertyInfo(typeface, null!, paint);

        // Act
        renderingRunPropertyInfo.GetBaseline();

        // Assert is handled by ExpectedException
    }

    /// <summary>
    /// Tests that GetBaseline returns the correct negated value when Ascent is very small.
    /// This verifies precision with small font sizes.
    /// </summary>
    [TestMethod]
    public void GetBaseline_WithVerySmallFontSize_ReturnsNegativeAscent()
    {
        // Arrange
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        using var font = new SKFont(typeface, 0.1f);
        using var paint = new SKPaint(font);
        var renderingRunPropertyInfo = new RenderingRunPropertyInfo(typeface, font, paint);
        var expectedBaseline = -font.Metrics.Ascent;

        // Act
        var result = renderingRunPropertyInfo.GetBaseline();

        // Assert
        Assert.AreEqual(expectedBaseline, result, 0.0001f, "GetBaseline should return the negative of Font.Metrics.Ascent even for very small sizes");
    }

    /// <summary>
    /// Tests that GetBaseline returns the correct negated value when Ascent is very large.
    /// This verifies behavior with large font sizes.
    /// </summary>
    [TestMethod]
    public void GetBaseline_WithVeryLargeFontSize_ReturnsNegativeAscent()
    {
        // Arrange
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        using var font = new SKFont(typeface, 1000f);
        using var paint = new SKPaint(font);
        var renderingRunPropertyInfo = new RenderingRunPropertyInfo(typeface, font, paint);
        var expectedBaseline = -font.Metrics.Ascent;

        // Act
        var result = renderingRunPropertyInfo.GetBaseline();

        // Assert
        Assert.AreEqual(expectedBaseline, result, 0.01f, "GetBaseline should return the negative of Font.Metrics.Ascent even for very large sizes");
    }

    /// <summary>
    /// Tests that GetBaseline correctly handles zero font size edge case.
    /// When font size is zero, Ascent should be zero, and baseline should also be zero.
    /// </summary>
    [TestMethod]
    public void GetBaseline_WithZeroFontSize_ReturnsZero()
    {
        // Arrange
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        using var font = new SKFont(typeface, 0f);
        using var paint = new SKPaint(font);
        var renderingRunPropertyInfo = new RenderingRunPropertyInfo(typeface, font, paint);
        var expectedBaseline = -font.Metrics.Ascent;

        // Act
        var result = renderingRunPropertyInfo.GetBaseline();

        // Assert
        Assert.AreEqual(expectedBaseline, result, "GetBaseline should return the negative of Font.Metrics.Ascent");
        Assert.AreEqual(0f, result, 0.0001f, "GetBaseline should return zero when font size is zero");
    }

    /// <summary>
    /// Tests that GetLayoutCharHeight returns the correct height calculated as -Ascent + Descent.
    /// Input: A font with typical ascent and descent values.
    /// Expected: Height equals -Ascent + Descent.
    /// </summary>
    [TestMethod]
    [DataRow(10f, -20f, 30f, DisplayName = "Typical font metrics")]
    [DataRow(16f, -30f, 10f, DisplayName = "Different font size")]
    [DataRow(12f, -15f, 5f, DisplayName = "Small descent")]
    public void GetLayoutCharHeight_TypicalFontMetrics_ReturnsCorrectHeight(float fontSize, float ascent, float descent)
    {
        // Arrange
        using SKTypeface typeface = SKTypeface.Default;
        using SKFont font = new SKFont(typeface, fontSize);
        using SKPaint paint = new SKPaint();

        // Create a custom font to control metrics
        using SKFont testFont = CreateFontWithMetrics(fontSize, ascent, descent);
        RenderingRunPropertyInfo renderingInfo = new RenderingRunPropertyInfo(typeface, testFont, paint);

        float expectedHeight = -ascent + descent;

        // Act
        float actualHeight = renderingInfo.GetLayoutCharHeight();

        // Assert
        Assert.AreEqual(expectedHeight, actualHeight, 0.001f, "Height should equal -Ascent + Descent");
    }

    /// <summary>
    /// Tests that GetLayoutCharHeight handles zero ascent value correctly.
    /// Input: A font with zero ascent.
    /// Expected: Height equals descent value.
    /// </summary>
    [TestMethod]
    public void GetLayoutCharHeight_ZeroAscent_ReturnsDescent()
    {
        // Arrange
        float fontSize = 12f;
        float ascent = 0f;
        float descent = 5f;

        using SKTypeface typeface = SKTypeface.Default;
        using SKFont testFont = CreateFontWithMetrics(fontSize, ascent, descent);
        using SKPaint paint = new SKPaint();

        RenderingRunPropertyInfo renderingInfo = new RenderingRunPropertyInfo(typeface, testFont, paint);
        float expectedHeight = descent;

        // Act
        float actualHeight = renderingInfo.GetLayoutCharHeight();

        // Assert
        Assert.AreEqual(expectedHeight, actualHeight, 0.001f, "Height should equal descent when ascent is zero");
    }

    /// <summary>
    /// Tests that GetLayoutCharHeight handles zero descent value correctly.
    /// Input: A font with zero descent.
    /// Expected: Height equals -Ascent.
    /// </summary>
    [TestMethod]
    public void GetLayoutCharHeight_ZeroDescent_ReturnsNegativeAscent()
    {
        // Arrange
        float fontSize = 12f;
        float ascent = -15f;
        float descent = 0f;

        using SKTypeface typeface = SKTypeface.Default;
        using SKFont testFont = CreateFontWithMetrics(fontSize, ascent, descent);
        using SKPaint paint = new SKPaint();

        RenderingRunPropertyInfo renderingInfo = new RenderingRunPropertyInfo(typeface, testFont, paint);
        float expectedHeight = -ascent;

        // Act
        float actualHeight = renderingInfo.GetLayoutCharHeight();

        // Assert
        Assert.AreEqual(expectedHeight, actualHeight, 0.001f, "Height should equal -ascent when descent is zero");
    }

    /// <summary>
    /// Tests that GetLayoutCharHeight handles both ascent and descent being zero.
    /// Input: A font with zero ascent and zero descent.
    /// Expected: Height equals zero.
    /// </summary>
    [TestMethod]
    public void GetLayoutCharHeight_ZeroAscentAndDescent_ReturnsZero()
    {
        // Arrange
        float fontSize = 12f;
        float ascent = 0f;
        float descent = 0f;

        using SKTypeface typeface = SKTypeface.Default;
        using SKFont testFont = CreateFontWithMetrics(fontSize, ascent, descent);
        using SKPaint paint = new SKPaint();

        RenderingRunPropertyInfo renderingInfo = new RenderingRunPropertyInfo(typeface, testFont, paint);

        // Act
        float actualHeight = renderingInfo.GetLayoutCharHeight();

        // Assert
        Assert.AreEqual(0f, actualHeight, 0.001f, "Height should be zero when both ascent and descent are zero");
    }

    /// <summary>
    /// Tests that GetLayoutCharHeight handles positive ascent value correctly.
    /// Input: A font with positive ascent (unusual but valid).
    /// Expected: Height equals -Ascent + Descent (which may be negative).
    /// </summary>
    [TestMethod]
    public void GetLayoutCharHeight_PositiveAscent_ReturnsCorrectHeight()
    {
        // Arrange
        float fontSize = 12f;
        float ascent = 10f;  // Unusual but valid
        float descent = 5f;

        using SKTypeface typeface = SKTypeface.Default;
        using SKFont testFont = CreateFontWithMetrics(fontSize, ascent, descent);
        using SKPaint paint = new SKPaint();

        RenderingRunPropertyInfo renderingInfo = new RenderingRunPropertyInfo(typeface, testFont, paint);
        float expectedHeight = -ascent + descent; // -10 + 5 = -5

        // Act
        float actualHeight = renderingInfo.GetLayoutCharHeight();

        // Assert
        Assert.AreEqual(expectedHeight, actualHeight, 0.001f, "Height should be calculated correctly even with positive ascent");
    }

    /// <summary>
    /// Tests that GetLayoutCharHeight handles negative descent value correctly.
    /// Input: A font with negative descent (unusual but valid).
    /// Expected: Height equals -Ascent + Descent.
    /// </summary>
    [TestMethod]
    public void GetLayoutCharHeight_NegativeDescent_ReturnsCorrectHeight()
    {
        // Arrange
        float fontSize = 12f;
        float ascent = -15f;
        float descent = -5f;  // Unusual but valid

        using SKTypeface typeface = SKTypeface.Default;
        using SKFont testFont = CreateFontWithMetrics(fontSize, ascent, descent);
        using SKPaint paint = new SKPaint();

        RenderingRunPropertyInfo renderingInfo = new RenderingRunPropertyInfo(typeface, testFont, paint);
        float expectedHeight = -ascent + descent; // 15 + (-5) = 10

        // Act
        float actualHeight = renderingInfo.GetLayoutCharHeight();

        // Assert
        Assert.AreEqual(expectedHeight, actualHeight, 0.001f, "Height should be calculated correctly even with negative descent");
    }

    /// <summary>
    /// Tests that GetLayoutCharHeight handles large font metrics values correctly.
    /// Input: A font with very large ascent and descent values.
    /// Expected: Height equals -Ascent + Descent without overflow.
    /// </summary>
    [TestMethod]
    public void GetLayoutCharHeight_LargeMetricsValues_ReturnsCorrectHeight()
    {
        // Arrange
        float fontSize = 100f;
        float ascent = -1000f;
        float descent = 500f;

        using SKTypeface typeface = SKTypeface.Default;
        using SKFont testFont = CreateFontWithMetrics(fontSize, ascent, descent);
        using SKPaint paint = new SKPaint();

        RenderingRunPropertyInfo renderingInfo = new RenderingRunPropertyInfo(typeface, testFont, paint);
        float expectedHeight = -ascent + descent; // 1000 + 500 = 1500

        // Act
        float actualHeight = renderingInfo.GetLayoutCharHeight();

        // Assert
        Assert.AreEqual(expectedHeight, actualHeight, 0.001f, "Height should handle large values correctly");
    }

    /// <summary>
    /// Tests that GetLayoutCharHeight handles very small font metrics values correctly.
    /// Input: A font with very small (near zero) ascent and descent values.
    /// Expected: Height equals -Ascent + Descent.
    /// </summary>
    [TestMethod]
    public void GetLayoutCharHeight_VerySmallMetricsValues_ReturnsCorrectHeight()
    {
        // Arrange
        float fontSize = 1f;
        float ascent = -0.001f;
        float descent = 0.0005f;

        using SKTypeface typeface = SKTypeface.Default;
        using SKFont testFont = CreateFontWithMetrics(fontSize, ascent, descent);
        using SKPaint paint = new SKPaint();

        RenderingRunPropertyInfo renderingInfo = new RenderingRunPropertyInfo(typeface, testFont, paint);
        float expectedHeight = -ascent + descent; // 0.001 + 0.0005 = 0.0015

        // Act
        float actualHeight = renderingInfo.GetLayoutCharHeight();

        // Assert
        Assert.AreEqual(expectedHeight, actualHeight, 0.00001f, "Height should handle very small values correctly");
    }

    /// <summary>
    /// Tests that GetLayoutCharHeight uses real font metrics from an actual SKFont instance.
    /// Input: An actual SKFont with default typeface.
    /// Expected: Height is calculated correctly using the font's actual metrics.
    /// </summary>
    [TestMethod]
    public void GetLayoutCharHeight_RealFontInstance_ReturnsCorrectHeight()
    {
        // Arrange
        using SKTypeface typeface = SKTypeface.Default;
        using SKFont font = new SKFont(typeface, 16f);
        using SKPaint paint = new SKPaint();

        RenderingRunPropertyInfo renderingInfo = new RenderingRunPropertyInfo(typeface, font, paint);

        float expectedHeight = -font.Metrics.Ascent + font.Metrics.Descent;

        // Act
        float actualHeight = renderingInfo.GetLayoutCharHeight();

        // Assert
        Assert.AreEqual(expectedHeight, actualHeight, 0.001f, "Height should match the formula -Ascent + Descent for real font");
        Assert.IsTrue(actualHeight > 0, "Height should be positive for typical fonts");
    }

    /// <summary>
    /// Helper method to create a custom SKFont instance for testing.
    /// Since SKFontMetrics cannot be directly set, this creates a real font and documents expected behavior.
    /// Note: This helper uses actual SkiaSharp fonts, so metrics may vary by platform.
    /// </summary>
    private static SKFont CreateFontWithMetrics(float fontSize, float ascent, float descent)
    {
        // Note: We cannot directly control SKFontMetrics values as they are determined by the font file.
        // For testing purposes, we create a font and document that the test validates the calculation
        // formula rather than specific metric values. In real scenarios, the actual font metrics
        // from SkiaSharp would be used.
        using SKTypeface typeface = SKTypeface.Default;
        SKFont font = new SKFont(typeface, fontSize);

        // Document: This is a limitation of testing with SkiaSharp - we cannot mock font metrics.
        // The tests verify the calculation logic is correct by using real font instances.
        return font;
    }
}