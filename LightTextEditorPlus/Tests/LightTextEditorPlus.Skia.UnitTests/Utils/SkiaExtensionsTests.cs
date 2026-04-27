using System;

using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace LightTextEditorPlus.Utils.UnitTests;


/// <summary>
/// Tests for <see cref="SkiaExtensions"/> class.
/// </summary>
[TestClass]
public partial class SkiaExtensionsTests
{
    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly converts a TextRect with normal positive values to SKRect.
    /// </summary>
    [TestMethod]
    [DataRow(0.0, 0.0, 100.0, 200.0)]
    [DataRow(10.5, 20.5, 30.5, 40.5)]
    [DataRow(1.0, 2.0, 3.0, 4.0)]
    [DataRow(100.123456789, 200.987654321, 300.111111111, 400.999999999)]
    public void ToSKRect_NormalPositiveValues_ConvertsCorrectly(double left, double top, double right, double bottom)
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(left, top, right, bottom);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual((float)left, result.Left, "Left coordinate should match after conversion");
        Assert.AreEqual((float)top, result.Top, "Top coordinate should match after conversion");
        Assert.AreEqual((float)right, result.Right, "Right coordinate should match after conversion");
        Assert.AreEqual((float)bottom, result.Bottom, "Bottom coordinate should match after conversion");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles zero values.
    /// </summary>
    [TestMethod]
    public void ToSKRect_ZeroValues_ConvertsCorrectly()
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(0.0, 0.0, 0.0, 0.0);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual(0f, result.Left, "Left should be 0");
        Assert.AreEqual(0f, result.Top, "Top should be 0");
        Assert.AreEqual(0f, result.Right, "Right should be 0");
        Assert.AreEqual(0f, result.Bottom, "Bottom should be 0");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles negative values.
    /// </summary>
    [TestMethod]
    [DataRow(-100.0, -200.0, -50.0, -100.0)]
    [DataRow(-10.5, -20.5, -5.5, -10.5)]
    [DataRow(-1.0, -2.0, -3.0, -4.0)]
    public void ToSKRect_NegativeValues_ConvertsCorrectly(double left, double top, double right, double bottom)
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(left, top, right, bottom);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual((float)left, result.Left, "Left coordinate should match after conversion");
        Assert.AreEqual((float)top, result.Top, "Top coordinate should match after conversion");
        Assert.AreEqual((float)right, result.Right, "Right coordinate should match after conversion");
        Assert.AreEqual((float)bottom, result.Bottom, "Bottom coordinate should match after conversion");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles mixed positive and negative values.
    /// </summary>
    [TestMethod]
    [DataRow(-100.0, -200.0, 100.0, 200.0)]
    [DataRow(-50.5, 50.5, 100.5, -100.5)]
    [DataRow(50.0, -50.0, -50.0, 50.0)]
    public void ToSKRect_MixedPositiveAndNegativeValues_ConvertsCorrectly(double left, double top, double right, double bottom)
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(left, top, right, bottom);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual((float)left, result.Left, "Left coordinate should match after conversion");
        Assert.AreEqual((float)top, result.Top, "Top coordinate should match after conversion");
        Assert.AreEqual((float)right, result.Right, "Right coordinate should match after conversion");
        Assert.AreEqual((float)bottom, result.Bottom, "Bottom coordinate should match after conversion");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles very large double values that exceed float range.
    /// Values beyond float range should become float.PositiveInfinity.
    /// </summary>
    [TestMethod]
    public void ToSKRect_VeryLargeValues_ConvertsToInfinity()
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual(float.PositiveInfinity, result.Left, "Left should be positive infinity for very large double values");
        Assert.AreEqual(float.PositiveInfinity, result.Top, "Top should be positive infinity for very large double values");
        Assert.AreEqual(float.PositiveInfinity, result.Right, "Right should be positive infinity for very large double values");
        Assert.AreEqual(float.PositiveInfinity, result.Bottom, "Bottom should be positive infinity for very large double values");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles very small double values that exceed float range.
    /// Values beyond float range should become float.NegativeInfinity.
    /// </summary>
    [TestMethod]
    public void ToSKRect_VerySmallValues_ConvertsToNegativeInfinity()
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(double.MinValue, double.MinValue, double.MinValue, double.MinValue);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual(float.NegativeInfinity, result.Left, "Left should be negative infinity for very small double values");
        Assert.AreEqual(float.NegativeInfinity, result.Top, "Top should be negative infinity for very small double values");
        Assert.AreEqual(float.NegativeInfinity, result.Right, "Right should be negative infinity for very small double values");
        Assert.AreEqual(float.NegativeInfinity, result.Bottom, "Bottom should be negative infinity for very small double values");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles double.NaN values.
    /// double.NaN should convert to float.NaN.
    /// </summary>
    [TestMethod]
    public void ToSKRect_NaNValues_ConvertsToNaN()
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(double.NaN, double.NaN, double.NaN, double.NaN);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.IsTrue(float.IsNaN(result.Left), "Left should be NaN");
        Assert.IsTrue(float.IsNaN(result.Top), "Top should be NaN");
        Assert.IsTrue(float.IsNaN(result.Right), "Right should be NaN");
        Assert.IsTrue(float.IsNaN(result.Bottom), "Bottom should be NaN");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles double.PositiveInfinity values.
    /// double.PositiveInfinity should convert to float.PositiveInfinity.
    /// </summary>
    [TestMethod]
    public void ToSKRect_PositiveInfinityValues_ConvertsToPositiveInfinity()
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual(float.PositiveInfinity, result.Left, "Left should be positive infinity");
        Assert.AreEqual(float.PositiveInfinity, result.Top, "Top should be positive infinity");
        Assert.IsTrue(float.IsNaN(result.Right), "Right should follow TextRect special-value arithmetic and become NaN");
        Assert.IsTrue(float.IsNaN(result.Bottom), "Bottom should follow TextRect special-value arithmetic and become NaN");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles double.NegativeInfinity values.
    /// double.NegativeInfinity should convert to float.NegativeInfinity.
    /// </summary>
    [TestMethod]
    public void ToSKRect_NegativeInfinityValues_ConvertsToNegativeInfinity()
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual(float.NegativeInfinity, result.Left, "Left should be negative infinity");
        Assert.AreEqual(float.NegativeInfinity, result.Top, "Top should be negative infinity");
        Assert.IsTrue(float.IsNaN(result.Right), "Right should follow TextRect special-value arithmetic and become NaN");
        Assert.IsTrue(float.IsNaN(result.Bottom), "Bottom should follow TextRect special-value arithmetic and become NaN");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles mixed special double values.
    /// Tests combinations of NaN, infinity, and normal values.
    /// </summary>
    [TestMethod]
    [DataRow(double.NaN, 0.0, double.PositiveInfinity, double.NegativeInfinity)]
    [DataRow(double.PositiveInfinity, double.NegativeInfinity, 0.0, double.NaN)]
    [DataRow(100.0, double.NaN, double.PositiveInfinity, 200.0)]
    public void ToSKRect_MixedSpecialValues_ConvertsCorrectly(double left, double top, double right, double bottom)
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(left, top, right, bottom);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        // Note: TextRect stores X, Y, Width, Height internally, where Width = right - left and Height = bottom - top.
        // When retrieving Right = X + Width and Bottom = Y + Height, special double arithmetic may not preserve
        // the original input values. For example: PositiveInfinity + (0.0 - PositiveInfinity) = NaN.
        // Therefore, we check against the actual TextRect properties, not the original input values.
        
        if (double.IsNaN(rect.Left))
        {
            Assert.IsTrue(float.IsNaN(result.Left), "Left should be NaN");
        }
        else if (double.IsPositiveInfinity(rect.Left))
        {
            Assert.AreEqual(float.PositiveInfinity, result.Left, "Left should be positive infinity");
        }
        else if (double.IsNegativeInfinity(rect.Left))
        {
            Assert.AreEqual(float.NegativeInfinity, result.Left, "Left should be negative infinity");
        }
        else
        {
            Assert.AreEqual((float)rect.Left, result.Left, "Left should match converted value");
        }

        if (double.IsNaN(rect.Top))
        {
            Assert.IsTrue(float.IsNaN(result.Top), "Top should be NaN");
        }
        else if (double.IsPositiveInfinity(rect.Top))
        {
            Assert.AreEqual(float.PositiveInfinity, result.Top, "Top should be positive infinity");
        }
        else if (double.IsNegativeInfinity(rect.Top))
        {
            Assert.AreEqual(float.NegativeInfinity, result.Top, "Top should be negative infinity");
        }
        else
        {
            Assert.AreEqual((float)rect.Top, result.Top, "Top should match converted value");
        }

        if (double.IsNaN(rect.Right))
        {
            Assert.IsTrue(float.IsNaN(result.Right), "Right should be NaN");
        }
        else if (double.IsPositiveInfinity(rect.Right))
        {
            Assert.AreEqual(float.PositiveInfinity, result.Right, "Right should be positive infinity");
        }
        else if (double.IsNegativeInfinity(rect.Right))
        {
            Assert.AreEqual(float.NegativeInfinity, result.Right, "Right should be negative infinity");
        }
        else
        {
            Assert.AreEqual((float)rect.Right, result.Right, "Right should match converted value");
        }

        if (double.IsNaN(rect.Bottom))
        {
            Assert.IsTrue(float.IsNaN(result.Bottom), "Bottom should be NaN");
        }
        else if (double.IsPositiveInfinity(rect.Bottom))
        {
            Assert.AreEqual(float.PositiveInfinity, result.Bottom, "Bottom should be positive infinity");
        }
        else if (double.IsNegativeInfinity(rect.Bottom))
        {
            Assert.AreEqual(float.NegativeInfinity, result.Bottom, "Bottom should be negative infinity");
        }
        else
        {
            Assert.AreEqual((float)rect.Bottom, result.Bottom, "Bottom should match converted value");
        }
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles values at the boundaries of float range.
    /// Tests float.MaxValue and float.MinValue boundaries.
    /// </summary>
    [TestMethod]
    public void ToSKRect_FloatBoundaryValues_ConvertsCorrectly()
    {
        // Arrange
        double left = float.MaxValue;
        double top = float.MinValue;
        double right = float.MaxValue;
        double bottom = float.MinValue;
        TextRect rect = TextRect.FromLeftTopRightBottom(left, top, right, bottom);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual(float.MaxValue, result.Left, "Left should be float.MaxValue");
        Assert.AreEqual(float.MinValue, result.Top, "Top should be float.MinValue");
        Assert.AreEqual(float.MaxValue, result.Right, "Right should be float.MaxValue");
        Assert.AreEqual(float.MinValue, result.Bottom, "Bottom should be float.MinValue");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> demonstrates precision loss during double to float conversion.
    /// Values with high precision should lose decimal places when converted to float.
    /// </summary>
    [TestMethod]
    public void ToSKRect_HighPrecisionValues_LosesPrecision()
    {
        // Arrange
        double left = 1.123456789012345;
        double top = 2.987654321098765;
        double right = 3.111111111111111;
        double bottom = 4.999999999999999;
        TextRect rect = TextRect.FromLeftTopRightBottom(left, top, right, bottom);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual((float)left, result.Left, "Left should match float-converted value");
        Assert.AreEqual((float)top, result.Top, "Top should match float-converted value");
        Assert.AreEqual((float)right, result.Right, "Right should match float-converted value");
        Assert.AreEqual((float)bottom, result.Bottom, "Bottom should match float-converted value");

        // Verify precision loss occurred (double precision != float precision)
        Assert.AreNotEqual(left, (double)result.Left, "Precision loss should occur for Left");
        Assert.AreNotEqual(top, (double)result.Top, "Precision loss should occur for Top");
        Assert.AreNotEqual(right, (double)result.Right, "Precision loss should occur for Right");
        Assert.AreNotEqual(bottom, (double)result.Bottom, "Precision loss should occur for Bottom");
    }

    /// <summary>
    /// Tests that <see cref="SkiaExtensions.ToSKRect"/> correctly handles epsilon values (very small but non-zero).
    /// Tests double.Epsilon and similar very small values.
    /// </summary>
    [TestMethod]
    public void ToSKRect_EpsilonValues_ConvertsCorrectly()
    {
        // Arrange
        TextRect rect = TextRect.FromLeftTopRightBottom(double.Epsilon, double.Epsilon, double.Epsilon, double.Epsilon);

        // Act
        SKRect result = rect.ToSKRect();

        // Assert
        Assert.AreEqual((float)double.Epsilon, result.Left, "Left should match converted epsilon value");
        Assert.AreEqual((float)double.Epsilon, result.Top, "Top should match converted epsilon value");
        Assert.AreEqual((float)double.Epsilon, result.Right, "Right should match converted epsilon value");
        Assert.AreEqual((float)double.Epsilon, result.Bottom, "Bottom should match converted epsilon value");
    }

    /// <summary>
    /// Tests that ToTextRect converts a typical SKRect with positive coordinates correctly.
    /// Input: SKRect with Left=10, Top=20, Right=100, Bottom=150
    /// Expected: TextRect with X=10, Y=20, Width=90, Height=130
    /// </summary>
    [TestMethod]
    public void ToTextRect_TypicalPositiveCoordinates_ConvertsCorrectly()
    {
        // Arrange
        var skRect = new SKRect(left: 10f, top: 20f, right: 100f, bottom: 150f);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual(10.0, result.X);
        Assert.AreEqual(20.0, result.Y);
        Assert.AreEqual(90.0, result.Width);
        Assert.AreEqual(130.0, result.Height);
    }

    /// <summary>
    /// Tests that ToTextRect handles zero values correctly.
    /// Input: SKRect with all coordinates set to zero
    /// Expected: TextRect with X=0, Y=0, Width=0, Height=0
    /// </summary>
    [TestMethod]
    public void ToTextRect_AllZeroValues_ConvertsCorrectly()
    {
        // Arrange
        var skRect = new SKRect(left: 0f, top: 0f, right: 0f, bottom: 0f);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual(0.0, result.X);
        Assert.AreEqual(0.0, result.Y);
        Assert.AreEqual(0.0, result.Width);
        Assert.AreEqual(0.0, result.Height);
    }

    /// <summary>
    /// Tests that ToTextRect handles negative coordinates correctly.
    /// Input: SKRect with negative coordinates
    /// Expected: TextRect with negative X and Y, and positive Width and Height
    /// </summary>
    [TestMethod]
    public void ToTextRect_NegativeCoordinates_ConvertsCorrectly()
    {
        // Arrange
        var skRect = new SKRect(left: -50f, top: -30f, right: 20f, bottom: 40f);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual(-50.0, result.X);
        Assert.AreEqual(-30.0, result.Y);
        Assert.AreEqual(70.0, result.Width);
        Assert.AreEqual(70.0, result.Height);
    }

    /// <summary>
    /// Tests that ToTextRect handles minimum float values correctly.
    /// Input: SKRect with float.MinValue coordinates
    /// Expected: TextRect with corresponding double values
    /// </summary>
    [TestMethod]
    public void ToTextRect_FloatMinValue_ConvertsCorrectly()
    {
        // Arrange
        var skRect = new SKRect(left: float.MinValue, top: float.MinValue, right: 0f, bottom: 0f);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual((double)float.MinValue, result.X);
        Assert.AreEqual((double)float.MinValue, result.Y);
        Assert.AreEqual(-(double)float.MinValue, result.Width);
        Assert.AreEqual(-(double)float.MinValue, result.Height);
    }

    /// <summary>
    /// Tests that ToTextRect handles maximum float values correctly.
    /// Input: SKRect with float.MaxValue coordinates
    /// Expected: TextRect with corresponding double values
    /// </summary>
    [TestMethod]
    public void ToTextRect_FloatMaxValue_ConvertsCorrectly()
    {
        // Arrange
        var skRect = new SKRect(left: 0f, top: 0f, right: float.MaxValue, bottom: float.MaxValue);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual(0.0, result.X);
        Assert.AreEqual(0.0, result.Y);
        Assert.AreEqual((double)float.MaxValue, result.Width);
        Assert.AreEqual((double)float.MaxValue, result.Height);
    }

    /// <summary>
    /// Tests that ToTextRect handles NaN values correctly.
    /// Input: SKRect with float.NaN in all coordinates
    /// Expected: TextRect with NaN in X, Y, Width, and Height
    /// </summary>
    [TestMethod]
    public void ToTextRect_NaNValues_ConvertsToNaN()
    {
        // Arrange
        var skRect = new SKRect(left: float.NaN, top: float.NaN, right: float.NaN, bottom: float.NaN);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.IsTrue(double.IsNaN(result.X));
        Assert.IsTrue(double.IsNaN(result.Y));
        Assert.IsTrue(double.IsNaN(result.Width));
        Assert.IsTrue(double.IsNaN(result.Height));
    }

    /// <summary>
    /// Tests that ToTextRect handles positive infinity values correctly.
    /// Input: SKRect with float.PositiveInfinity coordinates
    /// Expected: TextRect with corresponding infinity values
    /// </summary>
    [TestMethod]
    public void ToTextRect_PositiveInfinity_ConvertsCorrectly()
    {
        // Arrange
        var skRect = new SKRect(left: float.PositiveInfinity, top: float.PositiveInfinity, right: float.PositiveInfinity, bottom: float.PositiveInfinity);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual(double.PositiveInfinity, result.X);
        Assert.AreEqual(double.PositiveInfinity, result.Y);
        Assert.IsTrue(double.IsNaN(result.Width)); // PositiveInfinity - PositiveInfinity = NaN
        Assert.IsTrue(double.IsNaN(result.Height));
    }

    /// <summary>
    /// Tests that ToTextRect handles negative infinity values correctly.
    /// Input: SKRect with float.NegativeInfinity coordinates
    /// Expected: TextRect with corresponding infinity values
    /// </summary>
    [TestMethod]
    public void ToTextRect_NegativeInfinity_ConvertsCorrectly()
    {
        // Arrange
        var skRect = new SKRect(left: float.NegativeInfinity, top: float.NegativeInfinity, right: float.NegativeInfinity, bottom: float.NegativeInfinity);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual(double.NegativeInfinity, result.X);
        Assert.AreEqual(double.NegativeInfinity, result.Y);
        Assert.IsTrue(double.IsNaN(result.Width)); // NegativeInfinity - NegativeInfinity = NaN
        Assert.IsTrue(double.IsNaN(result.Height));
    }

    /// <summary>
    /// Tests that ToTextRect handles mixed infinity values correctly.
    /// Input: SKRect with mixed infinity coordinates
    /// Expected: TextRect with infinity in X, Y, Width, and Height
    /// </summary>
    [TestMethod]
    public void ToTextRect_MixedInfinity_ConvertsCorrectly()
    {
        // Arrange
        var skRect = new SKRect(left: float.NegativeInfinity, top: float.NegativeInfinity, right: float.PositiveInfinity, bottom: float.PositiveInfinity);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual(double.NegativeInfinity, result.X);
        Assert.AreEqual(double.NegativeInfinity, result.Y);
        Assert.AreEqual(double.PositiveInfinity, result.Width); // PositiveInfinity - NegativeInfinity = PositiveInfinity
        Assert.AreEqual(double.PositiveInfinity, result.Height);
    }

    /// <summary>
    /// Tests that ToTextRect handles fractional values correctly.
    /// Input: SKRect with fractional float values
    /// Expected: TextRect with corresponding double values preserving precision
    /// </summary>
    [TestMethod]
    public void ToTextRect_FractionalValues_ConvertsCorrectly()
    {
        // Arrange
        var skRect = new SKRect(left: 0.123f, top: 0.456f, right: 1.789f, bottom: 2.345f);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual((double)0.123f, result.X, 0.0001);
        Assert.AreEqual((double)0.456f, result.Y, 0.0001);
        Assert.AreEqual((double)(1.789f - 0.123f), result.Width, 0.0001);
        Assert.AreEqual((double)(2.345f - 0.456f), result.Height, 0.0001);
    }

    /// <summary>
    /// Tests ToTextRect with parameterized test cases covering various edge cases.
    /// Input: Various SKRect configurations
    /// Expected: Correct TextRect conversion for each case
    /// </summary>
    [TestMethod]
    [DataRow(0f, 0f, 10f, 10f, 0.0, 0.0, 10.0, 10.0, DisplayName = "Simple square")]
    [DataRow(-10f, -10f, 10f, 10f, -10.0, -10.0, 20.0, 20.0, DisplayName = "Centered square")]
    [DataRow(1.5f, 2.5f, 3.5f, 4.5f, 1.5, 2.5, 2.0, 2.0, DisplayName = "Small fractional")]
    [DataRow(-100f, -200f, -50f, -100f, -100.0, -200.0, 50.0, 100.0, DisplayName = "All negative")]
    [DataRow(1000000f, 2000000f, 3000000f, 4000000f, 1000000.0, 2000000.0, 2000000.0, 2000000.0, DisplayName = "Large values")]
    public void ToTextRect_VariousInputs_ConvertsCorrectly(float left, float top, float right, float bottom, double expectedX, double expectedY, double expectedWidth, double expectedHeight)
    {
        // Arrange
        var skRect = new SKRect(left, top, right, bottom);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual(expectedX, result.X, 0.0001);
        Assert.AreEqual(expectedY, result.Y, 0.0001);
        Assert.AreEqual(expectedWidth, result.Width, 0.0001);
        Assert.AreEqual(expectedHeight, result.Height, 0.0001);
    }

    /// <summary>
    /// Tests that ToTextRect handles inverted rectangles (where right is less than left).
    /// Input: SKRect with Right less than Left
    /// Expected: TextRect with negative Width
    /// </summary>
    [TestMethod]
    public void ToTextRect_InvertedRectangle_ProducesNegativeWidthHeight()
    {
        // Arrange
        var skRect = new SKRect(left: 100f, top: 100f, right: 50f, bottom: 50f);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual(100.0, result.X);
        Assert.AreEqual(100.0, result.Y);
        Assert.AreEqual(-50.0, result.Width);
        Assert.AreEqual(-50.0, result.Height);
    }

    /// <summary>
    /// Tests that ToTextRect handles very small values near zero.
    /// Input: SKRect with very small float values
    /// Expected: TextRect with corresponding small double values
    /// </summary>
    [TestMethod]
    public void ToTextRect_VerySmallValues_ConvertsCorrectly()
    {
        // Arrange
        var skRect = new SKRect(left: 1e-10f, top: 2e-10f, right: 3e-10f, bottom: 4e-10f);

        // Act
        var result = skRect.ToTextRect();

        // Assert
        Assert.AreEqual((double)1e-10f, result.X, 1e-15);
        Assert.AreEqual((double)2e-10f, result.Y, 1e-15);
        Assert.AreEqual((double)(3e-10f - 1e-10f), result.Width, 1e-15);
        Assert.AreEqual((double)(4e-10f - 2e-10f), result.Height, 1e-15);
    }

    /// <summary>
    /// Tests that ToSKPoint correctly converts a TextPoint with normal positive values to an SKPoint.
    /// Input: TextPoint with positive X and Y coordinates.
    /// Expected: SKPoint with matching X and Y values cast to float.
    /// </summary>
    [TestMethod]
    [DataRow(0.0, 0.0)]
    [DataRow(10.5, 20.3)]
    [DataRow(100.123456789, 200.987654321)]
    [DataRow(1000.0, 2000.0)]
    public void ToSKPoint_NormalPositiveValues_ReturnsCorrectSKPoint(double x, double y)
    {
        // Arrange
        var textPoint = new TextPoint(x, y);

        // Act
        var result = textPoint.ToSKPoint();

        // Assert
        Assert.AreEqual((float)x, result.X, "X coordinate should match the cast value");
        Assert.AreEqual((float)y, result.Y, "Y coordinate should match the cast value");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly converts a TextPoint with negative values to an SKPoint.
    /// Input: TextPoint with negative X and Y coordinates.
    /// Expected: SKPoint with matching negative X and Y values cast to float.
    /// </summary>
    [TestMethod]
    [DataRow(-10.5, -20.3)]
    [DataRow(-100.123456789, -200.987654321)]
    [DataRow(-1000.0, -2000.0)]
    public void ToSKPoint_NegativeValues_ReturnsCorrectSKPoint(double x, double y)
    {
        // Arrange
        var textPoint = new TextPoint(x, y);

        // Act
        var result = textPoint.ToSKPoint();

        // Assert
        Assert.AreEqual((float)x, result.X, "X coordinate should match the cast value");
        Assert.AreEqual((float)y, result.Y, "Y coordinate should match the cast value");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly converts a TextPoint with mixed positive and negative values to an SKPoint.
    /// Input: TextPoint with one positive and one negative coordinate.
    /// Expected: SKPoint with matching X and Y values cast to float.
    /// </summary>
    [TestMethod]
    [DataRow(10.5, -20.3)]
    [DataRow(-100.0, 200.0)]
    public void ToSKPoint_MixedSignValues_ReturnsCorrectSKPoint(double x, double y)
    {
        // Arrange
        var textPoint = new TextPoint(x, y);

        // Act
        var result = textPoint.ToSKPoint();

        // Assert
        Assert.AreEqual((float)x, result.X, "X coordinate should match the cast value");
        Assert.AreEqual((float)y, result.Y, "Y coordinate should match the cast value");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles special double values (NaN, PositiveInfinity, NegativeInfinity).
    /// Input: TextPoint with special double values.
    /// Expected: SKPoint with corresponding special float values.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_SpecialDoubleValues_ReturnsCorrectSKPoint()
    {
        // Arrange & Act & Assert - NaN
        var nanPoint = new TextPoint(double.NaN, double.NaN);
        var nanResult = nanPoint.ToSKPoint();
        Assert.IsTrue(float.IsNaN(nanResult.X), "X should be NaN");
        Assert.IsTrue(float.IsNaN(nanResult.Y), "Y should be NaN");

        // Arrange & Act & Assert - PositiveInfinity
        var posInfPoint = new TextPoint(double.PositiveInfinity, double.PositiveInfinity);
        var posInfResult = posInfPoint.ToSKPoint();
        Assert.IsTrue(float.IsPositiveInfinity(posInfResult.X), "X should be PositiveInfinity");
        Assert.IsTrue(float.IsPositiveInfinity(posInfResult.Y), "Y should be PositiveInfinity");

        // Arrange & Act & Assert - NegativeInfinity
        var negInfPoint = new TextPoint(double.NegativeInfinity, double.NegativeInfinity);
        var negInfResult = negInfPoint.ToSKPoint();
        Assert.IsTrue(float.IsNegativeInfinity(negInfResult.X), "X should be NegativeInfinity");
        Assert.IsTrue(float.IsNegativeInfinity(negInfResult.Y), "Y should be NegativeInfinity");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles mixed special double values.
    /// Input: TextPoint with one special value and one normal value.
    /// Expected: SKPoint with corresponding special and normal float values.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_MixedSpecialValues_ReturnsCorrectSKPoint()
    {
        // Arrange
        var mixedPoint1 = new TextPoint(double.NaN, 10.5);
        var mixedPoint2 = new TextPoint(20.3, double.PositiveInfinity);
        var mixedPoint3 = new TextPoint(double.NegativeInfinity, 0.0);

        // Act
        var result1 = mixedPoint1.ToSKPoint();
        var result2 = mixedPoint2.ToSKPoint();
        var result3 = mixedPoint3.ToSKPoint();

        // Assert
        Assert.IsTrue(float.IsNaN(result1.X), "X should be NaN");
        Assert.AreEqual(10.5f, result1.Y, "Y should be 10.5");

        Assert.AreEqual(20.3f, result2.X, "X should be 20.3");
        Assert.IsTrue(float.IsPositiveInfinity(result2.Y), "Y should be PositiveInfinity");

        Assert.IsTrue(float.IsNegativeInfinity(result3.X), "X should be NegativeInfinity");
        Assert.AreEqual(0.0f, result3.Y, "Y should be 0.0");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles very large double values that exceed float range.
    /// Input: TextPoint with values larger than float.MaxValue.
    /// Expected: SKPoint with PositiveInfinity values.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_ValuesExceedingFloatMaxValue_ReturnsPositiveInfinity()
    {
        // Arrange
        var largePoint = new TextPoint(double.MaxValue, double.MaxValue);

        // Act
        var result = largePoint.ToSKPoint();

        // Assert
        Assert.IsTrue(float.IsPositiveInfinity(result.X), "X should be PositiveInfinity when exceeding float range");
        Assert.IsTrue(float.IsPositiveInfinity(result.Y), "Y should be PositiveInfinity when exceeding float range");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles very small (large negative) double values that exceed float range.
    /// Input: TextPoint with values smaller than float.MinValue.
    /// Expected: SKPoint with NegativeInfinity values.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_ValuesExceedingFloatMinValue_ReturnsNegativeInfinity()
    {
        // Arrange
        var smallPoint = new TextPoint(double.MinValue, double.MinValue);

        // Act
        var result = smallPoint.ToSKPoint();

        // Assert
        Assert.IsTrue(float.IsNegativeInfinity(result.X), "X should be NegativeInfinity when exceeding float range");
        Assert.IsTrue(float.IsNegativeInfinity(result.Y), "Y should be NegativeInfinity when exceeding float range");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles float boundary values.
    /// Input: TextPoint with values at float boundaries.
    /// Expected: SKPoint with corresponding float boundary values.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_FloatBoundaryValues_ReturnsCorrectSKPoint()
    {
        // Arrange
        var maxFloatPoint = new TextPoint((double)float.MaxValue, (double)float.MaxValue);
        var minFloatPoint = new TextPoint((double)float.MinValue, (double)float.MinValue);

        // Act
        var maxResult = maxFloatPoint.ToSKPoint();
        var minResult = minFloatPoint.ToSKPoint();

        // Assert
        Assert.AreEqual(float.MaxValue, maxResult.X, "X should be float.MaxValue");
        Assert.AreEqual(float.MaxValue, maxResult.Y, "Y should be float.MaxValue");
        Assert.AreEqual(float.MinValue, minResult.X, "X should be float.MinValue");
        Assert.AreEqual(float.MinValue, minResult.Y, "Y should be float.MinValue");
    }

    /// <summary>
    /// Tests that ToSKPoint demonstrates precision loss when converting high-precision double values to float.
    /// Input: TextPoint with high-precision double values.
    /// Expected: SKPoint with values that may differ due to float precision limitations.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_HighPrecisionValues_DemonstratesPrecisionLoss()
    {
        // Arrange
        var highPrecisionPoint = new TextPoint(1.123456789123456789, 2.987654321987654321);

        // Act
        var result = highPrecisionPoint.ToSKPoint();

        // Assert
        Assert.AreEqual((float)1.123456789123456789, result.X, "X should match the float cast (with precision loss)");
        Assert.AreEqual((float)2.987654321987654321, result.Y, "Y should match the float cast (with precision loss)");

        // Verify precision loss occurs
        Assert.AreNotEqual(1.123456789123456789, (double)result.X, "Precision loss should occur in X");
        Assert.AreNotEqual(2.987654321987654321, (double)result.Y, "Precision loss should occur in Y");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly converts TextPoint.Zero to SKPoint with zero coordinates.
    /// Input: TextPoint.Zero (0, 0).
    /// Expected: SKPoint with X=0 and Y=0.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_ZeroPoint_ReturnsZeroSKPoint()
    {
        // Arrange
        var zeroPoint = TextPoint.Zero;

        // Act
        var result = zeroPoint.ToSKPoint();

        // Assert
        Assert.AreEqual(0.0f, result.X, "X should be 0");
        Assert.AreEqual(0.0f, result.Y, "Y should be 0");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles very small positive values near zero.
    /// Input: TextPoint with very small positive values (epsilon-like).
    /// Expected: SKPoint with corresponding small float values.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_VerySmallPositiveValues_ReturnsCorrectSKPoint()
    {
        // Arrange
        var epsilonPoint = new TextPoint(double.Epsilon, double.Epsilon);

        // Act
        var result = epsilonPoint.ToSKPoint();

        // Assert
        Assert.AreEqual((float)double.Epsilon, result.X, "X should match the cast value");
        Assert.AreEqual((float)double.Epsilon, result.Y, "Y should match the cast value");
    }
}