using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Rendering;
using LightTextEditorPlus.Rendering.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace LightTextEditorPlus.Rendering.Core.UnitTests;
/// <summary>
/// Tests for the VerticalSkiaTextRenderer class
/// </summary>
[TestClass]
public partial class VerticalSkiaTextRendererTests
{
#region Helper Methods
#endregion
#region Test Helper Class
#endregion
#region Helper Methods and Classes
#endregion
    /// <summary>
    /// Tests that the constructor successfully creates an instance when provided with valid parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var mockRenderManager = new Mock<RenderManager>(Mock.Of<SkiaTextEditor>());
        var mockCanvas = new Mock<SKCanvas>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = mockCanvas.Object,
            RenderInfoProvider = mockRenderInfoProvider.Object,
            RenderBounds = new TextRect(0, 0, 100, 100)
        };
        // Act
        var renderer = new VerticalSkiaTextRenderer(mockRenderManager.Object, in renderArgument);
        // Assert
        Assert.IsNotNull(renderer);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when renderManager is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullRenderManager_ThrowsArgumentNullException()
    {
        // Arrange
        RenderManager? nullRenderManager = null;
        var mockCanvas = new Mock<SKCanvas>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = mockCanvas.Object,
            RenderInfoProvider = mockRenderInfoProvider.Object,
            RenderBounds = new TextRect(0, 0, 100, 100)
        };
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new VerticalSkiaTextRenderer(nullRenderManager!, in renderArgument));
    }

    /// <summary>
    /// Tests that the constructor handles various valid TextRect bounds for RenderBounds.
    /// </summary>
    /// <param name = "x">The X coordinate of the render bounds.</param>
    /// <param name = "y">The Y coordinate of the render bounds.</param>
    /// <param name = "width">The width of the render bounds.</param>
    /// <param name = "height">The height of the render bounds.</param>
    [TestMethod]
    [DataRow(0.0, 0.0, 0.0, 0.0)]
    [DataRow(0.0, 0.0, 100.0, 100.0)]
    [DataRow(-100.0, -100.0, 200.0, 200.0)]
    [DataRow(double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue)]
    [DataRow(double.MinValue, double.MinValue, double.MaxValue, double.MaxValue)]
    public void Constructor_VariousRenderBounds_CreatesInstance(double x, double y, double width, double height)
    {
        // Arrange
        var mockSkiaTextEditor = new Mock<SkiaTextEditor>();
        var mockRenderManager = new Mock<RenderManager>(mockSkiaTextEditor.Object);
        var mockCanvas = new Mock<SKCanvas>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = mockCanvas.Object,
            RenderInfoProvider = mockRenderInfoProvider.Object,
            RenderBounds = new TextRect(x, y, width, height)
        };
        // Act
        var renderer = new VerticalSkiaTextRenderer(mockRenderManager.Object, in renderArgument);
        // Assert
        Assert.IsNotNull(renderer);
    }

    /// <summary>
    /// Tests that the constructor handles RenderArgument with null Viewport.
    /// </summary>
    [TestMethod]
    public void Constructor_NullViewport_CreatesInstance()
    {
        // Arrange
        var mockRenderManager = new Mock<RenderManager>(Mock.Of<SkiaTextEditor>());
        var mockCanvas = new Mock<SKCanvas>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = mockCanvas.Object,
            RenderInfoProvider = mockRenderInfoProvider.Object,
            RenderBounds = new TextRect(0, 0, 100, 100),
            Viewport = null
        };
        // Act
        var renderer = new VerticalSkiaTextRenderer(mockRenderManager.Object, in renderArgument);
        // Assert
        Assert.IsNotNull(renderer);
    }

    /// <summary>
    /// Tests that the constructor handles RenderArgument with non-null Viewport.
    /// </summary>
    [TestMethod]
    public void Constructor_WithViewport_CreatesInstance()
    {
        // Arrange
        var mockRenderManager = new Mock<RenderManager>(Mock.Of<SkiaTextEditor>());
        var mockCanvas = new Mock<SKCanvas>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = mockCanvas.Object,
            RenderInfoProvider = mockRenderInfoProvider.Object,
            RenderBounds = new TextRect(0, 0, 100, 100),
            Viewport = new TextRect(10, 10, 80, 80)
        };
        // Act
        var renderer = new VerticalSkiaTextRenderer(mockRenderManager.Object, in renderArgument);
        // Assert
        Assert.IsNotNull(renderer);
    }

    /// <summary>
    /// Tests that the constructor handles extreme values for Viewport dimensions.
    /// </summary>
    /// <param name = "x">The X coordinate of the viewport.</param>
    /// <param name = "y">The Y coordinate of the viewport.</param>
    /// <param name = "width">The width of the viewport.</param>
    /// <param name = "height">The height of the viewport.</param>
    [TestMethod]
    [DataRow(0.0, 0.0, 1.0, 1.0)]
    [DataRow(-1000.0, -1000.0, 2000.0, 2000.0)]
    [DataRow(double.MinValue, double.MinValue, double.MaxValue, double.MaxValue)]
    public void Constructor_ExtremeViewportValues_CreatesInstance(double x, double y, double width, double height)
    {
        // Arrange
        var mockRenderManager = new Mock<RenderManager>(Mock.Of<SkiaTextEditor>());
        var mockCanvas = new Mock<SKCanvas>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = mockCanvas.Object,
            RenderInfoProvider = mockRenderInfoProvider.Object,
            RenderBounds = new TextRect(0, 0, 100, 100),
            Viewport = new TextRect(x, y, width, height)
        };
        // Act
        var renderer = new VerticalSkiaTextRenderer(mockRenderManager.Object, in renderArgument);
        // Assert
        Assert.IsNotNull(renderer);
    }
}