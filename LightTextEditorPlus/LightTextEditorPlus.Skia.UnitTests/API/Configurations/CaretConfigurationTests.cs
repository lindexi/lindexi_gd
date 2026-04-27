using System;
using LightTextEditorPlus.Configurations;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace LightTextEditorPlus.Configurations.UnitTests;

/// <summary>
/// Unit tests for the <see cref="SkiaCaretConfiguration"/> class.
/// </summary>
[TestClass]
public partial class SkiaCaretConfigurationTests
{
    /// <summary>
    /// Tests that the CaretBlinkTime property returns the default value of 16 milliseconds
    /// when no value has been explicitly set.
    /// </summary>
    [TestMethod]
    public void CaretBlinkTime_WhenNotSet_ReturnsDefaultSixteenMilliseconds()
    {
        // Arrange
        var configuration = new SkiaCaretConfiguration();
        var expectedDefault = TimeSpan.FromMilliseconds(16);

        // Act
        var result = configuration.CaretBlinkTime;

        // Assert
        Assert.AreEqual(expectedDefault, result);
    }

    /// <summary>
    /// Tests that the CaretBlinkTime property correctly stores and retrieves the set value
    /// for various TimeSpan inputs including edge cases.
    /// </summary>
    /// <param name="milliseconds">The milliseconds value to use for creating the TimeSpan.</param>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(1.0)]
    [DataRow(16.0)]
    [DataRow(100.0)]
    [DataRow(500.0)]
    [DataRow(1000.0)]
    [DataRow(-100.0)]
    [DataRow(-1.0)]
    public void CaretBlinkTime_SetValue_ReturnsSetValue(double milliseconds)
    {
        // Arrange
        var configuration = new SkiaCaretConfiguration();
        TimeSpan expectedValue = TimeSpan.FromMilliseconds(milliseconds);

        // Act
        configuration.CaretBlinkTime = expectedValue;
        var result = configuration.CaretBlinkTime;

        // Assert
        Assert.AreEqual(expectedValue, result);
    }

    /// <summary>
    /// Tests that the CaretBlinkTime property correctly stores and retrieves TimeSpan.Zero.
    /// </summary>
    [TestMethod]
    public void CaretBlinkTime_SetToZero_ReturnsZero()
    {
        // Arrange
        var configuration = new SkiaCaretConfiguration();
        var expectedValue = TimeSpan.Zero;

        // Act
        configuration.CaretBlinkTime = expectedValue;
        var result = configuration.CaretBlinkTime;

        // Assert
        Assert.AreEqual(expectedValue, result);
    }

    /// <summary>
    /// Tests that the CaretBlinkTime property correctly stores and retrieves TimeSpan.MinValue.
    /// </summary>
    [TestMethod]
    public void CaretBlinkTime_SetToMinValue_ReturnsMinValue()
    {
        // Arrange
        var configuration = new SkiaCaretConfiguration();
        var expectedValue = TimeSpan.MinValue;

        // Act
        configuration.CaretBlinkTime = expectedValue;
        var result = configuration.CaretBlinkTime;

        // Assert
        Assert.AreEqual(expectedValue, result);
    }

    /// <summary>
    /// Tests that the CaretBlinkTime property correctly stores and retrieves TimeSpan.MaxValue.
    /// </summary>
    [TestMethod]
    public void CaretBlinkTime_SetToMaxValue_ReturnsMaxValue()
    {
        // Arrange
        var configuration = new SkiaCaretConfiguration();
        var expectedValue = TimeSpan.MaxValue;

        // Act
        configuration.CaretBlinkTime = expectedValue;
        var result = configuration.CaretBlinkTime;

        // Assert
        Assert.AreEqual(expectedValue, result);
    }

    /// <summary>
    /// Tests that the CaretBlinkTime property correctly stores and retrieves a negative TimeSpan value.
    /// </summary>
    [TestMethod]
    public void CaretBlinkTime_SetToNegativeValue_ReturnsNegativeValue()
    {
        // Arrange
        var configuration = new SkiaCaretConfiguration();
        var expectedValue = TimeSpan.FromMilliseconds(-500);

        // Act
        configuration.CaretBlinkTime = expectedValue;
        var result = configuration.CaretBlinkTime;

        // Assert
        Assert.AreEqual(expectedValue, result);
    }

    /// <summary>
    /// Tests that the CaretBlinkTime property correctly stores and retrieves a very small positive TimeSpan value.
    /// </summary>
    [TestMethod]
    public void CaretBlinkTime_SetToVerySmallValue_ReturnsVerySmallValue()
    {
        // Arrange
        var configuration = new SkiaCaretConfiguration();
        var expectedValue = TimeSpan.FromTicks(1);

        // Act
        configuration.CaretBlinkTime = expectedValue;
        var result = configuration.CaretBlinkTime;

        // Assert
        Assert.AreEqual(expectedValue, result);
    }

    /// <summary>
    /// Tests that setting the CaretBlinkTime property overrides the default value
    /// and the new value is returned on subsequent access.
    /// </summary>
    [TestMethod]
    public void CaretBlinkTime_SetAfterGettingDefault_ReturnsNewValue()
    {
        // Arrange
        var configuration = new SkiaCaretConfiguration();
        var defaultValue = configuration.CaretBlinkTime;
        var newValue = TimeSpan.FromMilliseconds(100);

        // Act
        configuration.CaretBlinkTime = newValue;
        var result = configuration.CaretBlinkTime;

        // Assert
        Assert.AreEqual(TimeSpan.FromMilliseconds(16), defaultValue);
        Assert.AreEqual(newValue, result);
        Assert.AreNotEqual(defaultValue, result);
    }
}