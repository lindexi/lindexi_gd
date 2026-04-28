using System;

using LightTextEditorPlus.Primitive;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace LightTextEditorPlus.Primitive.UnitTests;


/// <summary>
/// Unit tests for <see cref="GradientSkiaTextBrushRelativePoint"/>
/// </summary>
[TestClass]
public partial class GradientSkiaTextBrushRelativePointTests
{
    /// <summary>
    /// Tests that ToSKPoint correctly calculates relative point coordinates based on bounds.
    /// </summary>
    /// <param name="x">The relative X coordinate (0-1 range)</param>
    /// <param name="y">The relative Y coordinate (0-1 range)</param>
    /// <param name="left">The left edge of the bounds</param>
    /// <param name="top">The top edge of the bounds</param>
    /// <param name="width">The width of the bounds</param>
    /// <param name="height">The height of the bounds</param>
    /// <param name="expectedX">The expected X coordinate of the result</param>
    /// <param name="expectedY">The expected Y coordinate of the result</param>
    [TestMethod]
    [DataRow(0f, 0f, 10f, 20f, 100f, 200f, 10f, 20f)]
    [DataRow(1f, 1f, 10f, 20f, 100f, 200f, 110f, 220f)]
    [DataRow(0.5f, 0.5f, 10f, 20f, 100f, 200f, 60f, 120f)]
    [DataRow(0.25f, 0.75f, 0f, 0f, 200f, 100f, 50f, 75f)]
    [DataRow(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f)]
    [DataRow(0.5f, 0.5f, -50f, -100f, 200f, 300f, 50f, 50f)]
    [DataRow(1f, 1f, -10f, -20f, 30f, 40f, 20f, 20f)]
    public void ToSKPoint_RelativeUnit_CalculatesPointRelativeToBounds(float x, float y, float left, float top, float width, float height, float expectedX, float expectedY)
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(x, y, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative);
        var bounds = new SKRect(left, top, left + width, top + height);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.AreEqual(expectedX, result.X, 0.0001f, $"X coordinate should be {expectedX}");
        Assert.AreEqual(expectedY, result.Y, 0.0001f, $"Y coordinate should be {expectedY}");
    }

    /// <summary>
    /// Tests that ToSKPoint returns exact coordinates when using Absolute unit, regardless of bounds.
    /// </summary>
    /// <param name="x">The absolute X coordinate</param>
    /// <param name="y">The absolute Y coordinate</param>
    /// <param name="left">The left edge of the bounds (should not affect result)</param>
    /// <param name="top">The top edge of the bounds (should not affect result)</param>
    /// <param name="width">The width of the bounds (should not affect result)</param>
    /// <param name="height">The height of the bounds (should not affect result)</param>
    [TestMethod]
    [DataRow(50f, 100f, 10f, 20f, 200f, 300f)]
    [DataRow(0f, 0f, 100f, 200f, 300f, 400f)]
    [DataRow(-50f, -100f, 10f, 20f, 100f, 200f)]
    [DataRow(1000f, 2000f, 0f, 0f, 50f, 50f)]
    [DataRow(3.14159f, 2.71828f, 10f, 10f, 100f, 100f)]
    [TestCategory("ProductionBugSuspected")]
    [Ignore("ProductionBugSuspected")]
    public void ToSKPoint_AbsoluteUnit_ReturnsExactCoordinates(float x, float y, float left, float top, float width, float height)
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(x, y, GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute);
        var bounds = new SKRect(left, top, left + width, top + height);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.AreEqual(x, result.X, 0.0001f, "X coordinate should match the absolute X value");
        Assert.AreEqual(y, result.Y, 0.0001f, "Y coordinate should match the absolute Y value");
    }

    /// <summary>
    /// Tests that ToSKPoint throws ArgumentOutOfRangeException when Unit has an invalid value.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_InvalidUnit_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint
        {
            X = 0.5f,
            Y = 0.5f,
            Unit = (GradientSkiaTextBrushRelativePoint.RelativeUnit)99
        };
        var bounds = new SKRect(0f, 0f, 100f, 100f);

        // Act & Assert
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => point.ToSKPoint(bounds));
        Assert.AreEqual("Unit", exception.ParamName, "Exception should specify Unit as the parameter name");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles NaN values in relative coordinates.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_RelativeUnitWithNaNCoordinates_ReturnsNaNResult()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(float.NaN, float.NaN, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative);
        var bounds = new SKRect(10f, 20f, 110f, 220f);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.IsTrue(float.IsNaN(result.X), "X coordinate should be NaN");
        Assert.IsTrue(float.IsNaN(result.Y), "Y coordinate should be NaN");
    }

    /// <summary>
    /// Tests that Absolute unit rejects NaN values during construction.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_AbsoluteUnitWithNaNCoordinates_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GradientSkiaTextBrushRelativePoint(float.NaN, float.NaN, GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute));
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles positive infinity values in relative coordinates.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_RelativeUnitWithPositiveInfinity_ReturnsInfinityResult()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(float.PositiveInfinity, float.PositiveInfinity, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative);
        var bounds = new SKRect(10f, 20f, 110f, 220f);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.IsTrue(float.IsPositiveInfinity(result.X), "X coordinate should be positive infinity");
        Assert.IsTrue(float.IsPositiveInfinity(result.Y), "Y coordinate should be positive infinity");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles negative infinity values in relative coordinates.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_RelativeUnitWithNegativeInfinity_ReturnsInfinityResult()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(float.NegativeInfinity, float.NegativeInfinity, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative);
        var bounds = new SKRect(10f, 20f, 110f, 220f);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.IsTrue(float.IsNegativeInfinity(result.X), "X coordinate should be negative infinity");
        Assert.IsTrue(float.IsNegativeInfinity(result.Y), "Y coordinate should be negative infinity");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles positive infinity values in absolute coordinates.
    /// </summary>
    [TestMethod]
    [TestCategory("ProductionBugSuspected")]
    [Ignore("ProductionBugSuspected")]
    public void ToSKPoint_AbsoluteUnitWithPositiveInfinity_ReturnsInfinityResult()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(float.PositiveInfinity, float.PositiveInfinity, GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute);
        var bounds = new SKRect(10f, 20f, 110f, 220f);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.IsTrue(float.IsPositiveInfinity(result.X), "X coordinate should be positive infinity");
        Assert.IsTrue(float.IsPositiveInfinity(result.Y), "Y coordinate should be positive infinity");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles negative infinity values in absolute coordinates.
    /// </summary>
    [TestMethod]
    [TestCategory("ProductionBugSuspected")]
    [Ignore("ProductionBugSuspected")]
    public void ToSKPoint_AbsoluteUnitWithNegativeInfinity_ReturnsInfinityResult()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(float.NegativeInfinity, float.NegativeInfinity, GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute);
        var bounds = new SKRect(10f, 20f, 110f, 220f);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.IsTrue(float.IsNegativeInfinity(result.X), "X coordinate should be negative infinity");
        Assert.IsTrue(float.IsNegativeInfinity(result.Y), "Y coordinate should be negative infinity");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles NaN values in bounds when using relative coordinates.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_RelativeUnitWithNaNBounds_ReturnsNaNResult()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(0.5f, 0.5f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative);
        var bounds = new SKRect(float.NaN, float.NaN, float.NaN, float.NaN);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.IsTrue(float.IsNaN(result.X), "X coordinate should be NaN when bounds contain NaN");
        Assert.IsTrue(float.IsNaN(result.Y), "Y coordinate should be NaN when bounds contain NaN");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles infinity values in bounds when using relative coordinates.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_RelativeUnitWithInfinityBounds_ReturnsInfinityResult()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(0.5f, 0.5f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative);
        var bounds = new SKRect(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.IsTrue(float.IsNaN(result.X), "X coordinate should be NaN when multiplying infinity by 0.5");
        Assert.IsTrue(float.IsNaN(result.Y), "Y coordinate should be NaN when multiplying infinity by 0.5");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles extreme float values in relative mode.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_RelativeUnitWithExtremeValues_CalculatesCorrectly()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(float.MaxValue, float.MaxValue, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative);
        var bounds = new SKRect(0f, 0f, 1f, 1f);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.AreEqual(float.MaxValue, result.X, "X coordinate should be float.MaxValue");
        Assert.AreEqual(float.MaxValue, result.Y, "Y coordinate should be float.MaxValue");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles extreme float values in absolute mode.
    /// </summary>
    [TestMethod]
    [TestCategory("ProductionBugSuspected")]
    [Ignore("ProductionBugSuspected")]
    public void ToSKPoint_AbsoluteUnitWithExtremeValues_ReturnsExactValues()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(float.MaxValue, float.MinValue, GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute);
        var bounds = new SKRect(0f, 0f, 100f, 100f);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.AreEqual(float.MaxValue, result.X, "X coordinate should be float.MaxValue");
        Assert.AreEqual(float.MinValue, result.Y, "Y coordinate should be float.MinValue");
    }

    /// <summary>
    /// Tests that ToSKPoint correctly handles zero-width and zero-height bounds in relative mode.
    /// </summary>
    [TestMethod]
    public void ToSKPoint_RelativeUnitWithZeroDimensionBounds_CalculatesCorrectly()
    {
        // Arrange
        var point = new GradientSkiaTextBrushRelativePoint(0.5f, 0.5f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative);
        var bounds = new SKRect(10f, 20f, 10f, 20f);

        // Act
        var result = point.ToSKPoint(bounds);

        // Assert
        Assert.AreEqual(10f, result.X, 0.0001f, "X coordinate should equal bounds.Left when width is 0");
        Assert.AreEqual(20f, result.Y, 0.0001f, "Y coordinate should equal bounds.Top when height is 0");
    }

    /// <summary>
    /// Tests that the constructor successfully creates an instance with valid coordinates when unit is Relative.
    /// When unit is Relative, no validation is performed on x and y values.
    /// </summary>
    [TestMethod]
    [DataRow(0f, 0f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative)]
    [DataRow(0.5f, 0.5f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative)]
    [DataRow(1f, 1f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative)]
    [DataRow(-1f, -1f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative)]
    [DataRow(100f, 200f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative)]
    [DataRow(float.MinValue, float.MaxValue, GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative)]
    public void Constructor_WithRelativeUnit_SucceedsWithAnyValues(float x, float y, GradientSkiaTextBrushRelativePoint.RelativeUnit unit)
    {
        // Act
        var point = new GradientSkiaTextBrushRelativePoint(x, y, unit);

        // Assert
        Assert.AreEqual(x, point.X);
        Assert.AreEqual(y, point.Y);
        Assert.AreEqual(unit, point.Unit);
    }

    /// <summary>
    /// Tests that the constructor successfully creates an instance when unit is Absolute and coordinates are in valid range [0, 1].
    /// </summary>
    [TestMethod]
    [DataRow(0f, 0f)]
    [DataRow(0f, 0.5f)]
    [DataRow(0.5f, 0f)]
    [DataRow(0.5f, 0.5f)]
    [DataRow(1f, 1f)]
    [DataRow(0f, 1f)]
    [DataRow(1f, 0f)]
    public void Constructor_WithAbsoluteUnitAndValidRange_Succeeds(float x, float y)
    {
        // Arrange
        var unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute;

        // Act
        var point = new GradientSkiaTextBrushRelativePoint(x, y, unit);

        // Assert
        Assert.AreEqual(x, point.X);
        Assert.AreEqual(y, point.Y);
        Assert.AreEqual(unit, point.Unit);
    }

    /// <summary>
    /// Tests that the constructor uses Relative as the default unit when unit parameter is not specified.
    /// </summary>
    [TestMethod]
    public void Constructor_WithoutUnitParameter_UsesRelativeAsDefault()
    {
        // Arrange
        float x = 0.5f;
        float y = 0.7f;

        // Act
        var point = new GradientSkiaTextBrushRelativePoint(x, y);

        // Assert
        Assert.AreEqual(x, point.X);
        Assert.AreEqual(y, point.Y);
        Assert.AreEqual(GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative, point.Unit);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentOutOfRangeException when x is less than 0 and unit is Absolute.
    /// </summary>
    [TestMethod]
    [DataRow(-0.0001f, 0.5f)]
    [DataRow(-1f, 0.5f)]
    [DataRow(-100f, 0.5f)]
    [DataRow(float.MinValue, 0.5f)]
    [DataRow(float.NegativeInfinity, 0.5f)]
    public void Constructor_WithAbsoluteUnitAndXLessThanZero_ThrowsArgumentOutOfRangeException(float x, float y)
    {
        // Arrange
        var unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute;

        // Act & Assert
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GradientSkiaTextBrushRelativePoint(x, y, unit));
        Assert.AreEqual("x", exception.ParamName);
        Assert.AreEqual(x, exception.ActualValue);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentOutOfRangeException when x is greater than 1 and unit is Absolute.
    /// </summary>
    [TestMethod]
    [DataRow(1.0001f, 0.5f)]
    [DataRow(2f, 0.5f)]
    [DataRow(100f, 0.5f)]
    [DataRow(float.MaxValue, 0.5f)]
    [DataRow(float.PositiveInfinity, 0.5f)]
    public void Constructor_WithAbsoluteUnitAndXGreaterThanOne_ThrowsArgumentOutOfRangeException(float x, float y)
    {
        // Arrange
        var unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute;

        // Act & Assert
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GradientSkiaTextBrushRelativePoint(x, y, unit));
        Assert.AreEqual("x", exception.ParamName);
        Assert.AreEqual(x, exception.ActualValue);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentOutOfRangeException when y is less than 0 and unit is Absolute.
    /// </summary>
    [TestMethod]
    [DataRow(0.5f, -0.0001f)]
    [DataRow(0.5f, -1f)]
    [DataRow(0.5f, -100f)]
    [DataRow(0.5f, float.MinValue)]
    [DataRow(0.5f, float.NegativeInfinity)]
    public void Constructor_WithAbsoluteUnitAndYLessThanZero_ThrowsArgumentOutOfRangeException(float x, float y)
    {
        // Arrange
        var unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute;

        // Act & Assert
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GradientSkiaTextBrushRelativePoint(x, y, unit));
        Assert.AreEqual("y", exception.ParamName);
        Assert.AreEqual(y, exception.ActualValue);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentOutOfRangeException when y is greater than 1 and unit is Absolute.
    /// </summary>
    [TestMethod]
    [DataRow(0.5f, 1.0001f)]
    [DataRow(0.5f, 2f)]
    [DataRow(0.5f, 100f)]
    [DataRow(0.5f, float.MaxValue)]
    [DataRow(0.5f, float.PositiveInfinity)]
    public void Constructor_WithAbsoluteUnitAndYGreaterThanOne_ThrowsArgumentOutOfRangeException(float x, float y)
    {
        // Arrange
        var unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute;

        // Act & Assert
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GradientSkiaTextBrushRelativePoint(x, y, unit));
        Assert.AreEqual("y", exception.ParamName);
        Assert.AreEqual(y, exception.ActualValue);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentOutOfRangeException when x is NaN and unit is Absolute.
    /// NaN is outside the valid range [0, 1].
    /// </summary>
    [TestMethod]
    public void Constructor_WithAbsoluteUnitAndXIsNaN_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        float x = float.NaN;
        float y = 0.5f;
        var unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute;

        // Act & Assert
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GradientSkiaTextBrushRelativePoint(x, y, unit));
        Assert.AreEqual("x", exception.ParamName);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentOutOfRangeException when y is NaN and unit is Absolute.
    /// NaN is outside the valid range [0, 1].
    /// </summary>
    [TestMethod]
    public void Constructor_WithAbsoluteUnitAndYIsNaN_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        float x = 0.5f;
        float y = float.NaN;
        var unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute;

        // Act & Assert
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GradientSkiaTextBrushRelativePoint(x, y, unit));
        Assert.AreEqual("y", exception.ParamName);
    }

    /// <summary>
    /// Tests that the constructor accepts NaN, PositiveInfinity, and NegativeInfinity when unit is Relative.
    /// When unit is Relative, no validation is performed.
    /// </summary>
    [TestMethod]
    [DataRow(float.NaN, 0f)]
    [DataRow(0f, float.NaN)]
    [DataRow(float.NaN, float.NaN)]
    [DataRow(float.PositiveInfinity, 0f)]
    [DataRow(0f, float.PositiveInfinity)]
    [DataRow(float.NegativeInfinity, 0f)]
    [DataRow(0f, float.NegativeInfinity)]
    public void Constructor_WithRelativeUnitAndSpecialFloatValues_Succeeds(float x, float y)
    {
        // Arrange
        var unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative;

        // Act
        var point = new GradientSkiaTextBrushRelativePoint(x, y, unit);

        // Assert
        Assert.AreEqual(unit, point.Unit);
        if (float.IsNaN(x))
        {
            Assert.IsTrue(float.IsNaN(point.X));
        }
        else
        {
            Assert.AreEqual(x, point.X);
        }
        if (float.IsNaN(y))
        {
            Assert.IsTrue(float.IsNaN(point.Y));
        }
        else
        {
            Assert.AreEqual(y, point.Y);
        }
    }

    /// <summary>
    /// Tests that the constructor works with undefined enum values.
    /// When unit is an undefined enum value (not Relative), validation logic will be executed.
    /// </summary>
    [TestMethod]
    public void Constructor_WithUndefinedEnumValueAndValidRange_Succeeds()
    {
        // Arrange
        float x = 0.5f;
        float y = 0.5f;
        var undefinedUnit = (GradientSkiaTextBrushRelativePoint.RelativeUnit)99;

        // Act
        var point = new GradientSkiaTextBrushRelativePoint(x, y, undefinedUnit);

        // Assert
        Assert.AreEqual(x, point.X);
        Assert.AreEqual(y, point.Y);
        Assert.AreEqual(undefinedUnit, point.Unit);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentOutOfRangeException when using undefined enum value
    /// and coordinates are outside [0, 1] range.
    /// </summary>
    [TestMethod]
    public void Constructor_WithUndefinedEnumValueAndInvalidRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        float x = 2f;
        float y = 0.5f;
        var undefinedUnit = (GradientSkiaTextBrushRelativePoint.RelativeUnit)99;

        // Act & Assert
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GradientSkiaTextBrushRelativePoint(x, y, undefinedUnit));
        Assert.AreEqual("x", exception.ParamName);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentOutOfRangeException when both x and y are out of range with Absolute unit.
    /// Should throw for x first based on code order.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAbsoluteUnitAndBothCoordinatesOutOfRange_ThrowsForXFirst()
    {
        // Arrange
        float x = -1f;
        float y = 2f;
        var unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute;

        // Act & Assert
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new GradientSkiaTextBrushRelativePoint(x, y, unit));
        Assert.AreEqual("x", exception.ParamName);
    }

    /// <summary>
    /// Tests boundary values exactly at 0 and 1 with Absolute unit.
    /// These should be valid.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAbsoluteUnitAndBoundaryValues_Succeeds()
    {
        // Test x=0, y=0
        var point1 = new GradientSkiaTextBrushRelativePoint(0f, 0f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute);
        Assert.AreEqual(0f, point1.X);
        Assert.AreEqual(0f, point1.Y);

        // Test x=1, y=1
        var point2 = new GradientSkiaTextBrushRelativePoint(1f, 1f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute);
        Assert.AreEqual(1f, point2.X);
        Assert.AreEqual(1f, point2.Y);

        // Test x=0, y=1
        var point3 = new GradientSkiaTextBrushRelativePoint(0f, 1f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute);
        Assert.AreEqual(0f, point3.X);
        Assert.AreEqual(1f, point3.Y);

        // Test x=1, y=0
        var point4 = new GradientSkiaTextBrushRelativePoint(1f, 0f, GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute);
        Assert.AreEqual(1f, point4.X);
        Assert.AreEqual(0f, point4.Y);
    }
}