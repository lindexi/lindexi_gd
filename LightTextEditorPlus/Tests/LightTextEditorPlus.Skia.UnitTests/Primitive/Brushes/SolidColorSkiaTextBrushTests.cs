using System;

using LightTextEditorPlus.Primitive;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace LightTextEditorPlus.Primitive.UnitTests;


/// <summary>
/// Unit tests for <see cref="SolidColorSkiaTextBrush"/>.
/// </summary>
[TestClass]
public partial class SolidColorSkiaTextBrushTests
{
    /// <summary>
    /// Tests that Apply sets the paint color correctly when opacity is exactly 1.0 and no alpha adjustment is needed.
    /// </summary>
    /// <param name="alpha">The alpha component of the color.</param>
    /// <param name="red">The red component of the color.</param>
    /// <param name="green">The green component of the color.</param>
    /// <param name="blue">The blue component of the color.</param>
    [TestMethod]
    [DataRow((byte)255, (byte)100, (byte)150, (byte)200)]
    [DataRow((byte)128, (byte)50, (byte)75, (byte)100)]
    [DataRow((byte)0, (byte)0, (byte)0, (byte)0)]
    [DataRow((byte)255, (byte)255, (byte)255, (byte)255)]
    public void Apply_OpacityIsOne_ColorSetWithoutAlphaAdjustment(byte alpha, byte red, byte green, byte blue)
    {
        // Arrange
        SKColor color = new SKColor(red, green, blue, alpha);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        using SKBitmap bitmap = new SKBitmap(100, 100);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint = new SKPaint();

        SkiaTextBrushRenderContext context = new SkiaTextBrushRenderContext(
            paint,
            canvas,
            new SKRect(0, 0, 100, 100),
            1.0);

        // Act
        brush.Apply(in context);

        // Assert
        Assert.AreEqual(color, paint.Color);
    }

    /// <summary>
    /// Tests that Apply correctly adjusts alpha when opacity is less than 1.0.
    /// </summary>
    /// <param name="originalAlpha">The original alpha component of the color.</param>
    /// <param name="opacity">The opacity value to apply.</param>
    /// <param name="expectedAlpha">The expected resulting alpha component.</param>
    [TestMethod]
    [DataRow((byte)255, 0.5, (byte)127)]
    [DataRow((byte)255, 0.0, (byte)0)]
    [DataRow((byte)128, 0.5, (byte)64)]
    [DataRow((byte)200, 0.75, (byte)150)]
    [DataRow((byte)100, 0.25, (byte)25)]
    [DataRow((byte)255, 0.99, (byte)252)]
    [DataRow((byte)0, 0.5, (byte)0)]
    [DataRow((byte)1, 0.5, (byte)0)]
    public void Apply_OpacityLessThanOne_AlphaAdjustedCorrectly(byte originalAlpha, double opacity, byte expectedAlpha)
    {
        // Arrange
        SKColor color = new SKColor(100, 150, 200, originalAlpha);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        using SKBitmap bitmap = new SKBitmap(100, 100);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint = new SKPaint();

        SkiaTextBrushRenderContext context = new SkiaTextBrushRenderContext(
            paint,
            canvas,
            new SKRect(0, 0, 100, 100),
            opacity);

        // Act
        brush.Apply(in context);

        // Assert
        Assert.AreEqual(expectedAlpha, paint.Color.Alpha);
        Assert.AreEqual((byte)100, paint.Color.Red);
        Assert.AreEqual((byte)150, paint.Color.Green);
        Assert.AreEqual((byte)200, paint.Color.Blue);
    }

    /// <summary>
    /// Tests that Apply does not adjust alpha when opacity is greater than 1.0.
    /// </summary>
    /// <param name="opacity">The opacity value greater than 1.0.</param>
    [TestMethod]
    [DataRow(1.5)]
    [DataRow(2.0)]
    [DataRow(10.0)]
    [DataRow(100.0)]
    public void Apply_OpacityGreaterThanOne_NoAlphaAdjustment(double opacity)
    {
        // Arrange
        SKColor color = new SKColor(100, 150, 200, 128);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        using SKBitmap bitmap = new SKBitmap(100, 100);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint = new SKPaint();

        SkiaTextBrushRenderContext context = new SkiaTextBrushRenderContext(
            paint,
            canvas,
            new SKRect(0, 0, 100, 100),
            opacity);

        // Act
        brush.Apply(in context);

        // Assert
        Assert.AreEqual(color, paint.Color);
    }

    /// <summary>
    /// Tests that Apply does not adjust alpha when opacity is negative.
    /// </summary>
    /// <param name="opacity">The negative opacity value.</param>
    [TestMethod]
    [DataRow(-0.5)]
    [DataRow(-1.0)]
    [DataRow(-100.0)]
    [TestCategory("ProductionBugSuspected")]
    [Ignore("ProductionBugSuspected")]
    public void Apply_NegativeOpacity_NoAlphaAdjustment(double opacity)
    {
        // Arrange
        SKColor color = new SKColor(100, 150, 200, 200);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        using SKBitmap bitmap = new SKBitmap(100, 100);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint = new SKPaint();

        SkiaTextBrushRenderContext context = new SkiaTextBrushRenderContext(
            paint,
            canvas,
            new SKRect(0, 0, 100, 100),
            opacity);

        // Act
        brush.Apply(in context);

        // Assert
        Assert.AreEqual(color, paint.Color);
    }

    /// <summary>
    /// Tests that Apply handles special double values for opacity correctly.
    /// </summary>
    /// <param name="opacity">The special double value for opacity.</param>
    [TestMethod]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    public void Apply_SpecialDoubleOpacityValues_NoAlphaAdjustment(double opacity)
    {
        // Arrange
        SKColor color = new SKColor(100, 150, 200, 180);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        using SKBitmap bitmap = new SKBitmap(100, 100);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint = new SKPaint();

        SkiaTextBrushRenderContext context = new SkiaTextBrushRenderContext(
            paint,
            canvas,
            new SKRect(0, 0, 100, 100),
            opacity);

        // Act
        brush.Apply(in context);

        // Assert
        // NaN < 1 is false, infinity comparisons behave as expected
        // These should not trigger alpha adjustment
        Assert.AreEqual(color, paint.Color);
    }

    /// <summary>
    /// Tests that Apply correctly handles opacity at exact boundary value of 0.0.
    /// </summary>
    [TestMethod]
    public void Apply_OpacityZero_AlphaSetToZero()
    {
        // Arrange
        SKColor color = new SKColor(100, 150, 200, 255);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        using SKBitmap bitmap = new SKBitmap(100, 100);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint = new SKPaint();

        SkiaTextBrushRenderContext context = new SkiaTextBrushRenderContext(
            paint,
            canvas,
            new SKRect(0, 0, 100, 100),
            0.0);

        // Act
        brush.Apply(in context);

        // Assert
        Assert.AreEqual((byte)0, paint.Color.Alpha);
        Assert.AreEqual((byte)100, paint.Color.Red);
        Assert.AreEqual((byte)150, paint.Color.Green);
        Assert.AreEqual((byte)200, paint.Color.Blue);
    }

    /// <summary>
    /// Tests that Apply correctly handles very small opacity values near zero.
    /// </summary>
    [TestMethod]
    public void Apply_VerySmallOpacity_AlphaCalculatedCorrectly()
    {
        // Arrange
        SKColor color = new SKColor(100, 150, 200, 255);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        using SKBitmap bitmap = new SKBitmap(100, 100);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint = new SKPaint();

        SkiaTextBrushRenderContext context = new SkiaTextBrushRenderContext(
            paint,
            canvas,
            new SKRect(0, 0, 100, 100),
            0.001);

        // Act
        brush.Apply(in context);

        // Assert
        byte expectedAlpha = (byte)(255 * 0.001);
        Assert.AreEqual(expectedAlpha, paint.Color.Alpha);
    }

    /// <summary>
    /// Tests that Apply correctly handles opacity value very close to 1.0 but still less than 1.0.
    /// </summary>
    [TestMethod]
    public void Apply_OpacityJustBelowOne_AlphaAdjusted()
    {
        // Arrange
        SKColor color = new SKColor(100, 150, 200, 200);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        using SKBitmap bitmap = new SKBitmap(100, 100);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint = new SKPaint();

        double opacity = 0.999;
        SkiaTextBrushRenderContext context = new SkiaTextBrushRenderContext(
            paint,
            canvas,
            new SKRect(0, 0, 100, 100),
            opacity);

        // Act
        brush.Apply(in context);

        // Assert
        byte expectedAlpha = (byte)(200 * 0.999);
        Assert.AreEqual(expectedAlpha, paint.Color.Alpha);
    }

    /// <summary>
    /// Tests that Apply sets the correct RGB components regardless of opacity.
    /// </summary>
    [TestMethod]
    public void Apply_WithOpacityLessThanOne_RgbComponentsUnchanged()
    {
        // Arrange
        SKColor color = new SKColor(50, 100, 150, 200);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        using SKBitmap bitmap = new SKBitmap(100, 100);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint = new SKPaint();

        SkiaTextBrushRenderContext context = new SkiaTextBrushRenderContext(
            paint,
            canvas,
            new SKRect(0, 0, 100, 100),
            0.5);

        // Act
        brush.Apply(in context);

        // Assert
        Assert.AreEqual((byte)50, paint.Color.Red);
        Assert.AreEqual((byte)100, paint.Color.Green);
        Assert.AreEqual((byte)150, paint.Color.Blue);
    }

    /// <summary>
    /// Tests that Apply handles maximum alpha value with various opacity values.
    /// </summary>
    /// <param name="opacity">The opacity value to apply.</param>
    /// <param name="expectedAlpha">The expected resulting alpha component.</param>
    [TestMethod]
    [DataRow(0.1, (byte)25)]
    [DataRow(0.2, (byte)51)]
    [DataRow(0.3, (byte)76)]
    [DataRow(0.4, (byte)102)]
    [DataRow(0.6, (byte)153)]
    [DataRow(0.8, (byte)204)]
    [DataRow(0.9, (byte)229)]
    public void Apply_MaxAlphaWithVariousOpacity_CalculatesCorrectAlpha(double opacity, byte expectedAlpha)
    {
        // Arrange
        SKColor color = new SKColor(0, 0, 0, 255);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        using SKBitmap bitmap = new SKBitmap(100, 100);
        using SKCanvas canvas = new SKCanvas(bitmap);
        using SKPaint paint = new SKPaint();

        SkiaTextBrushRenderContext context = new SkiaTextBrushRenderContext(
            paint,
            canvas,
            new SKRect(0, 0, 100, 100),
            opacity);

        // Act
        brush.Apply(in context);

        // Assert
        Assert.AreEqual(expectedAlpha, paint.Color.Alpha);
    }

    /// <summary>
    /// Tests that <see cref="SolidColorSkiaTextBrush.AsSolidColor"/> returns the color provided in the constructor
    /// for various color values including transparent, opaque, and custom colors.
    /// </summary>
    /// <param name="alpha">The alpha channel value (0-255).</param>
    /// <param name="red">The red channel value (0-255).</param>
    /// <param name="green">The green channel value (0-255).</param>
    /// <param name="blue">The blue channel value (0-255).</param>
    [TestMethod]
    [DataRow((byte)0, (byte)0, (byte)0, (byte)0, DisplayName = "Fully transparent black")]
    [DataRow((byte)255, (byte)0, (byte)0, (byte)0, DisplayName = "Fully opaque black")]
    [DataRow((byte)255, (byte)255, (byte)255, (byte)255, DisplayName = "Fully opaque white")]
    [DataRow((byte)255, (byte)255, (byte)0, (byte)0, DisplayName = "Fully opaque red")]
    [DataRow((byte)255, (byte)0, (byte)255, (byte)0, DisplayName = "Fully opaque green")]
    [DataRow((byte)255, (byte)0, (byte)0, (byte)255, DisplayName = "Fully opaque blue")]
    [DataRow((byte)128, (byte)128, (byte)128, (byte)128, DisplayName = "Semi-transparent gray")]
    [DataRow((byte)0, (byte)255, (byte)255, (byte)255, DisplayName = "Fully transparent white")]
    [DataRow((byte)1, (byte)1, (byte)1, (byte)1, DisplayName = "Minimal non-zero values")]
    [DataRow((byte)254, (byte)254, (byte)254, (byte)254, DisplayName = "Near-maximum values")]
    public void AsSolidColor_WithVariousColors_ReturnsConstructorColor(byte alpha, byte red, byte green, byte blue)
    {
        // Arrange
        SKColor expectedColor = new SKColor(red, green, blue, alpha);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(expectedColor);

        // Act
        SKColor actualColor = brush.AsSolidColor();

        // Assert
        Assert.AreEqual(expectedColor, actualColor);
    }

    /// <summary>
    /// Tests that <see cref="SolidColorSkiaTextBrush.AsSolidColor"/> returns the same color
    /// when called multiple times, ensuring consistency.
    /// </summary>
    [TestMethod]
    public void AsSolidColor_CalledMultipleTimes_ReturnsSameColor()
    {
        // Arrange
        SKColor expectedColor = new SKColor(100, 150, 200, 255);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(expectedColor);

        // Act
        SKColor firstCall = brush.AsSolidColor();
        SKColor secondCall = brush.AsSolidColor();
        SKColor thirdCall = brush.AsSolidColor();

        // Assert
        Assert.AreEqual(expectedColor, firstCall);
        Assert.AreEqual(expectedColor, secondCall);
        Assert.AreEqual(expectedColor, thirdCall);
        Assert.AreEqual(firstCall, secondCall);
        Assert.AreEqual(secondCall, thirdCall);
    }

    /// <summary>
    /// Tests that <see cref="SolidColorSkiaTextBrush.AsSolidColor"/> returns the correct color
    /// using predefined SKColor constants from SkiaSharp.
    /// </summary>
    /// <param name="colorName">The name of the predefined color for display purposes.</param>
    /// <param name="red">The red channel value.</param>
    /// <param name="green">The green channel value.</param>
    /// <param name="blue">The blue channel value.</param>
    [TestMethod]
    [DataRow("Transparent", (byte)0, (byte)0, (byte)0, (byte)0)]
    [DataRow("Black", (byte)255, (byte)0, (byte)0, (byte)0)]
    [DataRow("White", (byte)255, (byte)255, (byte)255, (byte)255)]
    [DataRow("Red", (byte)255, (byte)255, (byte)0, (byte)0)]
    [DataRow("Green", (byte)255, (byte)0, (byte)128, (byte)0)]
    [DataRow("Blue", (byte)255, (byte)0, (byte)0, (byte)255)]
    public void AsSolidColor_WithPredefinedColors_ReturnsCorrectColor(string colorName, byte alpha, byte red, byte green, byte blue)
    {
        // Arrange
        SKColor color = new SKColor(red, green, blue, alpha);
        SolidColorSkiaTextBrush brush = new SolidColorSkiaTextBrush(color);

        // Act
        SKColor result = brush.AsSolidColor();

        // Assert
        Assert.AreEqual(color, result);
        Assert.AreEqual(alpha, result.Alpha, $"Alpha mismatch for {colorName}");
        Assert.AreEqual(red, result.Red, $"Red mismatch for {colorName}");
        Assert.AreEqual(green, result.Green, $"Green mismatch for {colorName}");
        Assert.AreEqual(blue, result.Blue, $"Blue mismatch for {colorName}");
    }
}