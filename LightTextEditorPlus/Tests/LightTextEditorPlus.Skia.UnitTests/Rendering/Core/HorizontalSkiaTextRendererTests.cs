using System;
using System.Collections.Generic;

using LightTextEditorPlus.Configurations;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Primitive;
using LightTextEditorPlus.Rendering;
using LightTextEditorPlus.Rendering.Core;
using LightTextEditorPlus.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core.UnitTests;


/// <summary>
/// Tests for the HorizontalSkiaTextRenderer class
/// </summary>
[TestClass]
public partial class HorizontalSkiaTextRendererTests
{
    /// <summary>
    /// Test helper class that exposes protected methods for testing
    /// </summary>
    private class TestableHorizontalSkiaTextRenderer : HorizontalSkiaTextRenderer
    {
        public TestableHorizontalSkiaTextRenderer(RenderManager renderManager, in SkiaTextRenderArgument renderArgument)
            : base(renderManager, in renderArgument)
        {
        }

        public void PublicRenderCharList(in TextReadOnlyListSpan<CharData> charList, in ParagraphLineRenderInfo lineInfo)
        {
            RenderCharList(in charList, in lineInfo);
        }

        public TextRect PublicRenderBounds => RenderBounds;
    }

    #region Helper Methods

    private TextReadOnlyListSpan<CharData> CreateCharList(params CharData[] chars)
    {
        var list = new List<CharData>(chars);
        return new TextReadOnlyListSpan<CharData>(list, 0, list.Count);
    }

    private ParagraphLineRenderInfo CreateDefaultLineRenderInfo()
    {
        // ParagraphLineRenderInfo is a struct, so we can create it directly
        // However, we need to ensure it has valid data if required
        return default;
    }

    private static (RenderManager RenderManager, SkiaTextRenderArgument RenderArgument) CreateRenderContext(TextRect renderBounds, TextRect? viewport = null)
    {
        var textEditor = new SkiaTextEditor();
        var renderManager = new RenderManager(textEditor);
        var bitmap = new SKBitmap(100, 100);
        var canvas = new SKCanvas(bitmap);
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds,
            Viewport = viewport
        };

        return (renderManager, renderArgument);
    }

    #endregion

    /// <summary>
    /// Tests that the constructor successfully creates an instance when provided with valid non-null RenderManager and valid SkiaTextRenderArgument.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var (renderManager, renderArgument) = CreateRenderContext(new TextRect(0, 0, 100, 100));

        // Act
        var renderer = new HorizontalSkiaTextRenderer(renderManager, in renderArgument);

        // Assert
        Assert.IsNotNull(renderer);
    }

    /// <summary>
    /// Tests that the constructor throws NullReferenceException when RenderManager parameter is null,
    /// as the base constructor attempts to access renderManager.TextEditor.
    /// </summary>
    [TestMethod]
    public void Constructor_NullRenderManager_ThrowsNullReferenceException()
    {
        // Arrange
        RenderManager renderManager = null!;

        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var textEditor = new SkiaTextEditor();
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds
        };

        // Act
        Assert.ThrowsExactly<NullReferenceException>(() => new HorizontalSkiaTextRenderer(renderManager, in renderArgument));
    }

    /// <summary>
    /// Tests that the constructor successfully creates an instance with viewport specified in render argument.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParametersWithViewport_CreatesInstance()
    {
        // Arrange
        var viewport = new TextRect(10, 10, 50, 50);
        var (renderManager, renderArgument) = CreateRenderContext(new TextRect(0, 0, 100, 100), viewport);

        // Act
        var renderer = new HorizontalSkiaTextRenderer(renderManager, in renderArgument);

        // Assert
        Assert.IsNotNull(renderer);
    }

    /// <summary>
    /// Tests that the constructor successfully creates an instance with extreme boundary values for TextRect dimensions.
    /// </summary>
    [TestMethod]
    [DataRow(double.MinValue, double.MinValue, double.MaxValue, double.MaxValue)]
    [DataRow(0, 0, 0, 0)]
    [DataRow(-100, -100, 200, 200)]
    [DataRow(double.MaxValue, double.MaxValue, 0, 0)]
    public void Constructor_ExtremeBoundaryValues_CreatesInstance(double x, double y, double width, double height)
    {
        // Arrange
        var (renderManager, renderArgument) = CreateRenderContext(new TextRect(x, y, width, height));

        // Act
        var renderer = new HorizontalSkiaTextRenderer(renderManager, in renderArgument);

        // Assert
        Assert.IsNotNull(renderer);
    }
}