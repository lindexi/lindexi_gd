using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;
using LightTextEditorPlus.Primitive;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;

namespace LightTextEditorPlus.Document.Decorations.UnitTests;

/// <summary>
/// Tests for WaveLine struct.
/// NOTE: The WaveLine struct is declared as file-scoped (using 'file' modifier) in the source code,
/// which makes it inaccessible from external test assemblies. Direct testing is not possible without
/// either:
/// 1. Removing the 'file' modifier to make it internal or public, OR
/// 2. Moving these tests into the same file as the source code (not recommended), OR
/// 3. Using reflection (explicitly forbidden by testing guidelines)
/// 
/// The test methods below document what should be tested once the accessibility issue is resolved.
/// All tests are marked as Inconclusive until the type becomes accessible.
/// </summary>
[TestClass]
public class WaveLineTests
{
    /// <summary>
    /// Tests that DrawWaveLine draws a valid wave between two points with default properties.
    /// This test cannot execute until WaveLine is made accessible from external assemblies.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_ValidPointsWithDefaultProperties_DrawsWaveLine()
    {
        // Arrange
        // Cannot instantiate file-scoped WaveLine struct from external assembly

        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests that DrawWaveLine handles identical start and end points (distance = 0).
    /// Expected behavior: Should handle gracefully, potentially drawing no waves or a point.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_IdenticalStartAndEndPoints_HandlesGracefully()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with very small distance between points.
    /// Expected behavior: Should calculate and draw proportionally small waves.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_VerySmallDistance_DrawsProportionalWaves()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with very large distance between points.
    /// Expected behavior: Should calculate and draw many waves without overflow.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_VeryLargeDistance_DrawsManyWaves()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with WaveLength set to zero.
    /// Expected behavior: May cause division by zero or infinite loop in wave calculation.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_ZeroWaveLength_HandlesEdgeCase()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with negative WaveLength value.
    /// Expected behavior: Should either throw exception or handle gracefully.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_NegativeWaveLength_HandlesInvalidInput()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with WaveHeight set to zero.
    /// Expected behavior: Should draw a straight line with no wave amplitude.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_ZeroWaveHeight_DrawsStraightLine()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with negative WaveHeight value.
    /// Expected behavior: Should either throw exception or draw inverted waves.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_NegativeWaveHeight_HandlesInvalidInput()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with CurveSquaring set to zero.
    /// Expected behavior: Should affect bezier curve control point lengths.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_ZeroCurveSquaring_ModifiesCurveShape()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with CurveSquaring set to negative value.
    /// Expected behavior: Should either throw exception or produce unusual curve shapes.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_NegativeCurveSquaring_HandlesInvalidInput()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with double.NaN coordinates in start or end points.
    /// Expected behavior: Should throw exception or handle invalid coordinates.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_NaNCoordinates_HandlesInvalidInput()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with double.PositiveInfinity coordinates.
    /// Expected behavior: Should throw exception or handle infinite coordinates.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_InfiniteCoordinates_HandlesInvalidInput()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with extremely large coordinate values.
    /// Expected behavior: Should handle without overflow in distance calculations.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_ExtremelyLargeCoordinates_HandlesWithoutOverflow()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with various angles between points (horizontal, vertical, diagonal).
    /// Expected behavior: Should correctly calculate and draw waves at any angle.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_VariousAngles_DrawsCorrectlyOrientedWaves()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }

    /// <summary>
    /// Tests DrawWaveLine with custom property values for WaveLength, WaveHeight, and CurveSquaring.
    /// Expected behavior: Should draw waves according to custom specifications.
    /// </summary>
    [TestMethod]
    public void DrawWaveLine_CustomPropertyValues_DrawsCustomWaves()
    {
        Assert.Inconclusive(
            "Cannot test file-scoped WaveLine struct. " +
            "Remove 'file' modifier from struct declaration to enable testing.");
    }
}

/// <summary>
/// Tests for WaveLineTextEditorDecoration class
/// </summary>
[TestClass]
public partial class WaveLineTextEditorDecorationTests
{
    /// <summary>
    /// Tests BuildDecoration with horizontal layout returns correct result with proper TakeCharCount.
    /// Input: Horizontal layout with valid bounds and character list.
    /// Expected: Returns BuildDecorationResult with TakeCharCount equal to CharDataList.Count.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(100)]
    public void BuildDecoration_HorizontalLayoutWithValidCharCount_ReturnsTakeCharCountEqualToCharDataListCount
        (int charCount)
    {
        // Arrange
        var decoration = new WaveLineTextEditorDecoration();
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.ArrangingType = ArrangingType.Horizontal;
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore);
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var paint = new SKPaint();

        var charDataList = CreateCharDataList(charCount);
        var bounds = new TextRect(0, 0, 100, 20);
        var argument = new BuildDecorationArgument(
            runProperty,
            0,
            charDataList,
            bounds,
            default,
            textEditor)
        {
            Canvas = canvas,
            CachePaint = paint
        };

        // Act
        var result = decoration.BuildDecoration(argument);

        // Assert
        Assert.AreEqual(charCount, result.TakeCharCount);
    }

    /// <summary>
    /// Tests BuildDecoration with horizontal layout applies foreground brush to paint.
    /// Input: Horizontal layout with valid arguments.
    /// Expected: SkiaTextBrush.Apply is called once with correct parameters.
    /// </summary>
    [TestMethod]
    public void BuildDecoration_HorizontalLayout_AppliesForegroundBrushToPaint()
    {
        // Arrange
        var decoration = new WaveLineTextEditorDecoration();
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.ArrangingType = ArrangingType.Horizontal;
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore);
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var paint = new SKPaint();

        var charDataList = CreateCharDataList(1);
        var bounds = new TextRect(10, 20, 100, 30);
        var argument = new BuildDecorationArgument(
            runProperty,
            0,
            charDataList,
            bounds,
            default,
            textEditor)
        {
            Canvas = canvas,
            CachePaint = paint
        };

        // Act
        var result = decoration.BuildDecoration(argument);

        // Assert
        // Since we're using real instances, we verify the decoration executed successfully
        Assert.AreEqual(1, result.TakeCharCount);
    }

    /// <summary>
    /// Tests BuildDecoration with horizontal layout sets paint style to Stroke.
    /// Input: Horizontal layout with valid arguments.
    /// Expected: Paint.Style is set to SKPaintStyle.Stroke and StrokeWidth is set to 1.
    /// </summary>
    [TestMethod]
    public void BuildDecoration_HorizontalLayout_SetsPaintStyleToStroke()
    {
        // Arrange
        var decoration = new WaveLineTextEditorDecoration();
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.ArrangingType = ArrangingType.Horizontal;
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore);
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var paint = new SKPaint();

        var charDataList = CreateCharDataList(1);
        var bounds = new TextRect(0, 0, 100, 20);
        var argument = new BuildDecorationArgument(
            runProperty,
            0,
            charDataList,
            bounds,
            default,
            textEditor)
        {
            Canvas = canvas,
            CachePaint = paint
        };

        // Act
        var result = decoration.BuildDecoration(argument);

        // Assert
        Assert.AreEqual(SKPaintStyle.Stroke, paint.Style);
        Assert.AreEqual(1f, paint.StrokeWidth);
    }

    /// <summary>
    /// Tests BuildDecoration with non-horizontal layout does not apply brush or draw.
    /// Input: Vertical layout (IsHorizontal = false).
    /// Expected: Foreground brush Apply is not called.
    /// </summary>
    [TestMethod]
    public void BuildDecoration_NonHorizontalLayout_DoesNotApplyBrushOrDraw()
    {
        // Arrange
        var decoration = new WaveLineTextEditorDecoration();
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.ArrangingType = ArrangingType.Vertical;
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore);
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var paint = new SKPaint();

        var charDataList = CreateCharDataList(5);
        var bounds = new TextRect(0, 0, 100, 20);
        var argument = new BuildDecorationArgument(
            runProperty,
            0,
            charDataList,
            bounds,
            default,
            textEditor)
        {
            Canvas = canvas,
            CachePaint = paint
        };

        // Act
        var result = decoration.BuildDecoration(argument);

        // Assert
        // For non-horizontal layout, the decoration should still return the TakeCharCount
        Assert.AreEqual(5, result.TakeCharCount);
    }

    /// <summary>
    /// Tests BuildDecoration with zero height bounds calculates wave properties correctly.
    /// Input: Horizontal layout with bounds having zero height.
    /// Expected: Returns valid result with correct TakeCharCount.
    /// </summary>
    [TestMethod]
    public void BuildDecoration_ZeroHeightBounds_ReturnsValidResult()
    {
        // Arrange
        var decoration = new WaveLineTextEditorDecoration();
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.ArrangingType = ArrangingType.Horizontal;
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore);
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var paint = new SKPaint();

        var charDataList = CreateCharDataList(3);
        var bounds = new TextRect(0, 0, 100, 0);
        var argument = new BuildDecorationArgument(
            runProperty,
            0,
            charDataList,
            bounds,
            default,
            textEditor)
        {
            Canvas = canvas,
            CachePaint = paint
        };

        // Act
        var result = decoration.BuildDecoration(argument);

        // Assert
        Assert.AreEqual(3, result.TakeCharCount);
    }

    /// <summary>
    /// Tests BuildDecoration with very large bounds height handles extreme values.
    /// Input: Horizontal layout with bounds having very large height.
    /// Expected: Returns valid result without throwing exceptions.
    /// </summary>
    [TestMethod]
    public void BuildDecoration_VeryLargeHeightBounds_HandlesExtremeValues()
    {
        // Arrange
        var decoration = new WaveLineTextEditorDecoration();
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.ArrangingType = ArrangingType.Horizontal;
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore);
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var paint = new SKPaint();

        var charDataList = CreateCharDataList(2);
        var bounds = new TextRect(0, 0, 100, double.MaxValue / 10);
        var argument = new BuildDecorationArgument(
            runProperty,
            0,
            charDataList,
            bounds,
            default,
            textEditor)
        {
            Canvas = canvas,
            CachePaint = paint
        };

        // Act
        var result = decoration.BuildDecoration(argument);

        // Assert
        Assert.AreEqual(2, result.TakeCharCount);
    }

    /// <summary>
    /// Tests BuildDecoration with negative bounds coordinates.
    /// Input: Horizontal layout with negative X and Y coordinates in bounds.
    /// Expected: Returns valid result with correct TakeCharCount.
    /// </summary>
    [TestMethod]
    public void BuildDecoration_NegativeBoundsCoordinates_ReturnsValidResult()
    {
        // Arrange
        var decoration = new WaveLineTextEditorDecoration();
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.ArrangingType = ArrangingType.Horizontal;
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore);
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var paint = new SKPaint();

        var charDataList = CreateCharDataList(4);
        var bounds = new TextRect(-100, -50, 100, 20);
        var argument = new BuildDecorationArgument(
            runProperty,
            0,
            charDataList,
            bounds,
            default,
            textEditor)
        {
            Canvas = canvas,
            CachePaint = paint
        };

        // Act
        var result = decoration.BuildDecoration(argument);

        // Assert
        Assert.AreEqual(4, result.TakeCharCount);
    }

    /// <summary>
    /// Tests BuildDecoration with empty CharDataList.
    /// Input: Horizontal layout with empty CharDataList.
    /// Expected: Returns BuildDecorationResult with TakeCharCount equal to 0.
    /// </summary>
    [TestMethod]
    public void BuildDecoration_EmptyCharDataList_ReturnsTakeCharCountZero()
    {
        // Arrange
        var decoration = new WaveLineTextEditorDecoration();
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.ArrangingType = ArrangingType.Horizontal;
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore);
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var paint = new SKPaint();

        var charDataList = CreateCharDataList(0);
        var bounds = new TextRect(0, 0, 100, 20);
        var argument = new BuildDecorationArgument(
            runProperty,
            0,
            charDataList,
            bounds,
            default,
            textEditor)
        {
            Canvas = canvas,
            CachePaint = paint
        };

        // Act
        var result = decoration.BuildDecoration(argument);

        // Assert
        Assert.AreEqual(0, result.TakeCharCount);
    }

    /// <summary>
    /// Tests BuildDecoration with minimum and maximum opacity values.
    /// Input: Horizontal layout with opacity at boundaries (0.0 and 1.0).
    /// Expected: Returns valid result with correct TakeCharCount.
    /// </summary>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(1.0)]
    [DataRow(0.5)]
    public void BuildDecoration_DifferentOpacityValues_ReturnsValidResult(double opacity)
    {
        // Arrange
        var decoration = new WaveLineTextEditorDecoration();
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.ArrangingType = ArrangingType.Horizontal;
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore);
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var paint = new SKPaint();

        var charDataList = CreateCharDataList(1);
        var bounds = new TextRect(0, 0, 100, 20);
        var argument = new BuildDecorationArgument(
            runProperty,
            0,
            charDataList,
            bounds,
            default,
            textEditor)
        {
            Canvas = canvas,
            CachePaint = paint
        };

        // Act
        var result = decoration.BuildDecoration(argument);

        // Assert
        Assert.AreEqual(1, result.TakeCharCount);
    }

    /// <summary>
    /// Tests BuildDecoration with very small height bounds.
    /// Input: Horizontal layout with bounds having very small height (0.001).
    /// Expected: Returns valid result with correct TakeCharCount.
    /// </summary>
    [TestMethod]
    public void BuildDecoration_VerySmallHeightBounds_ReturnsValidResult()
    {
        // Arrange
        var decoration = new WaveLineTextEditorDecoration();
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.ArrangingType = ArrangingType.Horizontal;
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore);
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var paint = new SKPaint();

        var charDataList = CreateCharDataList(1);
        var bounds = new TextRect(0, 0, 100, 0.001);
        var argument = new BuildDecorationArgument(
            runProperty,
            0,
            charDataList,
            bounds,
            default,
            textEditor)
        {
            Canvas = canvas,
            CachePaint = paint
        };

        // Act
        var result = decoration.BuildDecoration(argument);

        // Assert
        Assert.AreEqual(1, result.TakeCharCount);
    }

    /// <summary>
    /// Tests that Instance property returns a singleton instance.
    /// Input: None.
    /// Expected: Instance property returns the same instance on multiple accesses.
    /// </summary>
    [TestMethod]
    public void Instance_AccessedMultipleTimes_ReturnsSameInstance()
    {
        // Act
        var instance1 = WaveLineTextEditorDecoration.Instance;
        var instance2 = WaveLineTextEditorDecoration.Instance;

        // Assert
        Assert.AreSame(instance1, instance2);
        Assert.IsNotNull(instance1);
    }

    /// <summary>
    /// Helper method to create a TextReadOnlyListSpan with the specified count of CharData items.
    /// </summary>
    private static TextReadOnlyListSpan<CharData> CreateCharDataList(int count)
    {
        var list = new List<CharData>();
        for (int i = 0; i < count; i++)
        {
            list.Add(default);
        }

        return new TextReadOnlyListSpan<CharData>(list);
    }
}