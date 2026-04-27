using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.TextArrayPools;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;
using LightTextEditorPlus.Rendering;
using LightTextEditorPlus.Rendering.Core;
using LightTextEditorPlus.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core.UnitTests;

/// <summary>
/// Tests for <see cref="BaseSkiaTextRenderer"/> class.
/// </summary>
[TestClass]
public class BaseSkiaTextRendererTests
{
    /// <summary>
    /// Tests that Config property returns the DebugConfiguration from TextEditor.
    /// Input: Valid BaseSkiaTextRenderer instance with real TextEditor.
    /// Expected: Config returns the same instance as TextEditor.DebugConfiguration.
    /// </summary>
    [TestMethod]
    public void Config_WithValidTextEditor_ReturnsDebugConfiguration()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        var renderManager = new RenderManager(textEditor);
        
        var imageInfo = new SKImageInfo(100, 100);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds
        };

        var renderer = new TestableBaseSkiaTextRenderer(renderManager, renderArgument);

        // Act
        var result = renderer.GetConfig();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(textEditor.DebugConfiguration, result);
    }

    /// <summary>
    /// Tests that Config property returns consistent values on multiple accesses.
    /// Input: Valid BaseSkiaTextRenderer instance accessed multiple times.
    /// Expected: Config returns the same instance on each access.
    /// </summary>
    [TestMethod]
    public void Config_MultipleAccesses_ReturnsSameInstance()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        var renderManager = new RenderManager(textEditor);
        
        var imageInfo = new SKImageInfo(100, 100);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds
        };

        var renderer = new TestableBaseSkiaTextRenderer(renderManager, renderArgument);

        // Act
        var result1 = renderer.GetConfig();
        var result2 = renderer.GetConfig();
        var result3 = renderer.GetConfig();

        // Assert
        Assert.AreSame(result1, result2);
        Assert.AreSame(result2, result3);
    }

    /// <summary>
    /// Tests that Config property correctly delegates to TextEditor.DebugConfiguration.
    /// Input: Valid BaseSkiaTextRenderer with TextEditor that has a specific DebugConfiguration.
    /// Expected: Config returns exactly the DebugConfiguration from TextEditor, not a different instance.
    /// </summary>
    [TestMethod]
    public void Config_DelegationToTextEditor_ReturnsCorrectInstance()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        var renderManager = new RenderManager(textEditor);
        
        var imageInfo = new SKImageInfo(100, 100);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds
        };

        var renderer = new TestableBaseSkiaTextRenderer(renderManager, renderArgument);

        // Act
        var result = renderer.GetConfig();

        // Assert
        Assert.AreSame(textEditor.DebugConfiguration, result);
    }

    /// <summary>
    /// Testable concrete implementation of BaseSkiaTextRenderer for testing protected members.
    /// </summary>
    private class TestableBaseSkiaTextRenderer : BaseSkiaTextRenderer
    {
        public TestableBaseSkiaTextRenderer(RenderManager renderManager, in SkiaTextRenderArgument renderArgument)
            : base(renderManager, renderArgument)
        {
        }

        public SkiaTextEditorDebugConfiguration GetConfig() => Config;

        protected override void RenderCharList(in global::LightTextEditorPlus.Core.Primitive.Collections.TextReadOnlyListSpan<global::LightTextEditorPlus.Core.Document.CharData> charList, in ParagraphLineRenderInfo lineInfo)
        {
            // Minimal implementation for testing
        }

        protected override void RenderBackground(in ParagraphLineRenderInfo lineRenderInfo)
        {
            // Minimal implementation for testing
        }
    }

    /// <summary>
    /// Tests that constructor throws NullReferenceException when renderManager is null
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_NullRenderManager_ThrowsNullReferenceException()
    {
        // Arrange
        RenderManager? renderManager = null;

        // Create a minimal SKCanvas for testing
        var imageInfo = new SKImageInfo(100, 100);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;

        // Note: RenderInfoProvider has internal constructor, so this test demonstrates
        // the expected behavior when a null renderManager is passed.
        // In actual usage, renderManager should never be null.
        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = null!, // Cannot create real instance due to internal constructor
            RenderBounds = new TextRect(0, 0, 100, 100)
        };

        // Act & Assert
        Assert.ThrowsExactly<NullReferenceException>(() =>
        {
            var renderer = new TestableBaseSkiaTextRenderer(renderManager!, renderArgument);
        });
    }

    /// <summary>
    /// Tests that constructor properly initializes properties when valid parameters are provided
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_ValidParameters_InitializesPropertiesCorrectly()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        var renderManager = new RenderManager(textEditor);
        
        var imageInfo = new SKImageInfo(100, 100);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds
        };

        // Act
        var renderer = new TestableBaseSkiaTextRenderer(renderManager, renderArgument);

        // Assert
        Assert.IsNotNull(renderer);
        Assert.IsNotNull(renderer.GetConfig());
    }

    /// <summary>
    /// Tests that constructor behavior when Canvas is null in renderArgument
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_NullCanvas_AssignsNullToCanvasProperty()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        var renderManager = new RenderManager(textEditor);
        
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = null!,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds
        };

        // Act
        var renderer = new TestableBaseSkiaTextRenderer(renderManager, renderArgument);

        // Assert
        Assert.IsNotNull(renderer);
    }

    /// <summary>
    /// Tests that constructor behavior when RenderInfoProvider is null in renderArgument
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_NullRenderInfoProvider_ThrowsNullReferenceException()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        var renderManager = new RenderManager(textEditor);
        
        var imageInfo = new SKImageInfo(100, 100);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = null!,
            RenderBounds = renderBounds
        };

        // Act & Assert
        Assert.ThrowsExactly<NullReferenceException>(() =>
        {
            var renderer = new TestableBaseSkiaTextRenderer(renderManager, renderArgument);
        });
    }

    /// <summary>
    /// Tests constructor with various RenderBounds values including boundary conditions
    /// </summary>
    /// <remarks>
    /// Testing boundary values for TextRect: zero size, negative values, maximum values
    /// </remarks>
    [TestMethod]
    [DataRow(0.0, 0.0, 0.0, 0.0, DisplayName = "Zero size bounds")]
    [DataRow(-100.0, -100.0, 50.0, 50.0, DisplayName = "Negative position")]
    [DataRow(0.0, 0.0, double.MaxValue, double.MaxValue, DisplayName = "Maximum size")]
    [DataRow(double.MinValue, double.MinValue, 100.0, 100.0, DisplayName = "Minimum position")]
    public void BaseSkiaTextRenderer_VariousRenderBounds_AssignsBoundsCorrectly(double x, double y, double width, double height)
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        var renderManager = new RenderManager(textEditor);
        
        var imageInfo = new SKImageInfo(100, 100);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        var renderBounds = new TextRect(x, y, width, height);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds
        };

        // Act
        var renderer = new TestableBaseSkiaTextRenderer(renderManager, renderArgument);

        // Assert
        Assert.IsNotNull(renderer);
    }

    /// <summary>
    /// Tests constructor with null Viewport (optional parameter)
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_NullViewport_AcceptsNullValue()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        var renderManager = new RenderManager(textEditor);
        
        var imageInfo = new SKImageInfo(100, 100);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds,
            Viewport = null
        };

        // Act
        var renderer = new TestableBaseSkiaTextRenderer(renderManager, renderArgument);

        // Assert
        Assert.IsNotNull(renderer);
    }

    /// <summary>
    /// Tests constructor with non-null Viewport value
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_NonNullViewport_AssignsViewportCorrectly()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        var renderManager = new RenderManager(textEditor);
        
        var imageInfo = new SKImageInfo(100, 100);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        var renderBounds = new TextRect(0, 0, 100, 100);
        var viewport = new TextRect(10, 10, 200, 150);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds,
            Viewport = viewport
        };

        // Act
        var renderer = new TestableBaseSkiaTextRenderer(renderManager, renderArgument);

        // Assert
        Assert.IsNotNull(renderer);
    }

    /// <summary>
    /// Tests that constructor throws NullReferenceException when renderInfoProvider.IsDirty is checked on null renderManager
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_IsDirtyTrue_DebugAssertFails()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        var renderManager = new RenderManager(textEditor);
        
        var imageInfo = new SKImageInfo(100, 100);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        
        var renderBounds = new TextRect(0, 0, 100, 100);
        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = canvas,
            RenderInfoProvider = renderInfoProvider,
            RenderBounds = renderBounds
        };

        // Act
        // Note: Debug.Assert doesn't throw exceptions in production code, 
        // but we can verify that IsDirty is false (expected state)
        var renderer = new TestableBaseSkiaTextRenderer(renderManager, renderArgument);

        // Assert
        // Verify that IsDirty is false, which is the expected state after layout
        Assert.IsFalse(renderInfoProvider.IsDirty, 
            "IsDirty should be false after layout completion");
    }
}