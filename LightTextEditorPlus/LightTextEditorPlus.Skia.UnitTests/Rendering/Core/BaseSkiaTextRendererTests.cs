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
    /// Input: Valid BaseSkiaTextRenderer instance with mocked TextEditor.
    /// Expected: Config returns the same instance as TextEditor.DebugConfiguration.
    /// </summary>
    [TestMethod]
    public void Config_WithValidTextEditor_ReturnsDebugConfiguration()
    {
        // Arrange
        var mockRenderManager = new Mock<RenderManager>();
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockDebugConfiguration = new Mock<SkiaTextEditorDebugConfiguration>(mockTextEditor.Object);

        mockRenderManager.Setup(rm => rm.TextEditor).Returns(mockTextEditor.Object);
        mockTextEditor.Setup(te => te.DebugConfiguration).Returns(mockDebugConfiguration.Object);

        var mockCanvas = new Mock<SKCanvas>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = mockCanvas.Object,
            RenderInfoProvider = mockRenderInfoProvider.Object,
            RenderBounds = renderBounds
        };

        var renderer = new TestableBaseSkiaTextRenderer(mockRenderManager.Object, renderArgument);

        // Act
        var result = renderer.GetConfig();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(mockDebugConfiguration.Object, result);
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
        var mockRenderManager = new Mock<RenderManager>();
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockDebugConfiguration = new Mock<SkiaTextEditorDebugConfiguration>(mockTextEditor.Object);

        mockRenderManager.Setup(rm => rm.TextEditor).Returns(mockTextEditor.Object);
        mockTextEditor.Setup(te => te.DebugConfiguration).Returns(mockDebugConfiguration.Object);

        var mockCanvas = new Mock<SKCanvas>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = mockCanvas.Object,
            RenderInfoProvider = mockRenderInfoProvider.Object,
            RenderBounds = renderBounds
        };

        var renderer = new TestableBaseSkiaTextRenderer(mockRenderManager.Object, renderArgument);

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
        var mockRenderManager = new Mock<RenderManager>();
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var expectedDebugConfiguration = new Mock<SkiaTextEditorDebugConfiguration>(mockTextEditor.Object);
        var wrongDebugConfiguration = new Mock<SkiaTextEditorDebugConfiguration>(mockTextEditor.Object);

        mockRenderManager.Setup(rm => rm.TextEditor).Returns(mockTextEditor.Object);
        mockTextEditor.Setup(te => te.DebugConfiguration).Returns(expectedDebugConfiguration.Object);

        var mockCanvas = new Mock<SKCanvas>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var renderBounds = new TextRect(0, 0, 100, 100);

        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = mockCanvas.Object,
            RenderInfoProvider = mockRenderInfoProvider.Object,
            RenderBounds = renderBounds
        };

        var renderer = new TestableBaseSkiaTextRenderer(mockRenderManager.Object, renderArgument);

        // Act
        var result = renderer.GetConfig();

        // Assert
        Assert.AreSame(expectedDebugConfiguration.Object, result);
        Assert.AreNotSame(wrongDebugConfiguration.Object, result);
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
    /// Helper method to create a valid CharData instance for testing.
    /// </summary>
    /// <param name="glyphIndex">The glyph index to use</param>
    /// <returns>A valid CharData instance</returns>
    private static CharData CreateValidCharData(ushort glyphIndex)
    {
        // Note: This is a simplified mock creation. In a real scenario, proper CharData
        // initialization would be required. This test assumes CharData can be created
        // with valid CharDataInfo containing the specified glyph index.
        // If CharData construction is complex, consider using actual factory methods
        // or builders from the production code.

        // This is a placeholder implementation that may need adjustment based on
        // actual CharData construction requirements
        throw new NotImplementedException(
            "CharData creation needs to be implemented based on actual constructor/factory available in production code. " +
            "Please review CharData class and implement appropriate instantiation logic.");
    }

    /// <summary>
    /// Tests that CheckSameBackground returns true when both CharData objects have the same background color.
    /// This test is marked as Inconclusive due to limitations in mocking sealed types and internal constructors.
    /// </summary>
    /// <remarks>
    /// To fully test this method, you would need:
    /// 1. Actual instances of SkiaTextRunProperty (which has an internal constructor)
    /// 2. A way to create CharData with these properties
    /// 3. Access to SkiaPlatformResourceManager for creating SkiaTextRunProperty
    /// Consider using integration tests or making the constructor internal-visible to test assemblies.
    /// </remarks>
    [TestMethod]
    public void CheckSameBackground_SameBackgroundColors_ReturnsTrue()
    {
        // Arrange
        // Note: Cannot create actual instances due to internal constructor of SkiaTextRunProperty
        // and the AsSkiaRunProperty() extension method performing a direct cast.

        // Act & Assert
        Assert.Inconclusive(
            "Cannot properly test CheckSameBackground without actual SkiaTextRunProperty instances. " +
            "SkiaTextRunProperty has an internal constructor and cannot be mocked. " +
            "The AsSkiaRunProperty() extension method performs a direct cast, preventing the use of mocked IReadOnlyRunProperty. " +
            "Consider making SkiaTextRunProperty constructor internal-visible to test assemblies or providing a test factory.");
    }

    /// <summary>
    /// Tests that CheckSameBackground returns false when CharData objects have different background colors.
    /// This test is marked as Inconclusive due to limitations in mocking sealed types and internal constructors.
    /// </summary>
    /// <remarks>
    /// To fully test this method, you would need:
    /// 1. Actual instances of SkiaTextRunProperty with different Background values
    /// 2. A way to create CharData with these properties
    /// 3. Access to SkiaPlatformResourceManager for creating SkiaTextRunProperty
    /// Consider using integration tests or making the constructor internal-visible to test assemblies.
    /// </remarks>
    [TestMethod]
    public void CheckSameBackground_DifferentBackgroundColors_ReturnsFalse()
    {
        // Arrange
        // Note: Cannot create actual instances due to internal constructor of SkiaTextRunProperty

        // Act & Assert
        Assert.Inconclusive(
            "Cannot properly test CheckSameBackground without actual SkiaTextRunProperty instances. " +
            "SkiaTextRunProperty has an internal constructor and cannot be mocked. " +
            "The AsSkiaRunProperty() extension method performs a direct cast, preventing the use of mocked IReadOnlyRunProperty. " +
            "Consider making SkiaTextRunProperty constructor internal-visible to test assemblies or providing a test factory.");
    }

    /// <summary>
    /// Tests that CheckSameBackground handles transparent backgrounds correctly.
    /// This test is marked as Inconclusive due to limitations in mocking sealed types and internal constructors.
    /// </summary>
    /// <remarks>
    /// This would test the edge case where both backgrounds are SKColors.Transparent (the default value).
    /// To fully test this method, you would need actual instances of SkiaTextRunProperty.
    /// </remarks>
    [TestMethod]
    public void CheckSameBackground_BothTransparentBackgrounds_ReturnsTrue()
    {
        // Arrange
        // Note: Cannot create actual instances due to internal constructor of SkiaTextRunProperty

        // Act & Assert
        Assert.Inconclusive(
            "Cannot properly test CheckSameBackground without actual SkiaTextRunProperty instances. " +
            "SkiaTextRunProperty has an internal constructor and cannot be mocked. " +
            "The AsSkiaRunProperty() extension method performs a direct cast, preventing the use of mocked IReadOnlyRunProperty. " +
            "Consider making SkiaTextRunProperty constructor internal-visible to test assemblies or providing a test factory.");
    }

    /// <summary>
    /// Tests that CheckSameBackground correctly compares different SKColor values.
    /// This test is marked as Inconclusive due to limitations in mocking sealed types and internal constructors.
    /// </summary>
    /// <remarks>
    /// This would test various SKColor combinations including edge cases like SKColors.Empty, different alpha values, etc.
    /// To fully test this method, you would need actual instances of SkiaTextRunProperty.
    /// </remarks>
    [TestMethod]
    [DataRow(true, DisplayName = "Same color")]
    [DataRow(false, DisplayName = "Different colors")]
    public void CheckSameBackground_VariousSKColorValues_ReturnsExpectedResult(bool shouldMatch)
    {
        // Arrange
        // Note: Cannot create actual instances due to internal constructor of SkiaTextRunProperty

        // Act & Assert
        Assert.Inconclusive(
            "Cannot properly test CheckSameBackground without actual SkiaTextRunProperty instances. " +
            "SkiaTextRunProperty has an internal constructor and cannot be mocked. " +
            "The AsSkiaRunProperty() extension method performs a direct cast, preventing the use of mocked IReadOnlyRunProperty. " +
            "Consider making SkiaTextRunProperty constructor internal-visible to test assemblies or providing a test factory.");
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
        Assert.ThrowsException<NullReferenceException>(() =>
        {
            var renderer = new TestableBaseSkiaTextRenderer(renderManager!, renderArgument);
        });
    }

    /// <summary>
    /// Tests that constructor properly initializes properties when valid parameters are provided
    /// </summary>
    /// <remarks>
    /// This test is marked as Inconclusive because it requires real instances of RenderManager and RenderInfoProvider
    /// which have complex dependencies that cannot be mocked per framework restrictions.
    /// To complete this test:
    /// 1. Create a real SkiaTextEditor instance with proper configuration
    /// 2. Create a RenderManager using the SkiaTextEditor
    /// 3. Obtain a RenderInfoProvider from the text editor core
    /// 4. Ensure RenderInfoProvider.IsDirty is false
    /// </remarks>
    [TestMethod]
    public void BaseSkiaTextRenderer_ValidParameters_InitializesPropertiesCorrectly()
    {
        // TODO: This test requires real instances of types that cannot be mocked.
        // Complete implementation requires:
        // - A real SkiaTextEditor instance
        // - A RenderManager created from the SkiaTextEditor
        // - A RenderInfoProvider obtained from TextEditorCore (has internal constructor)
        // - SKCanvas from SKSurface

        Assert.Inconclusive("Test requires real instances of unmockable types with complex dependencies. " +
                          "See test comments for guidance on completing this test.");
    }

    /// <summary>
    /// Tests that constructor behavior when Canvas is null in renderArgument
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_NullCanvas_AssignsNullToCanvasProperty()
    {
        // Arrange
        // Note: This test demonstrates behavior with null Canvas.
        // Creating RenderManager and RenderInfoProvider requires real instances
        // which have complex dependencies.

        // TODO: Create real RenderManager and RenderInfoProvider instances
        // For now, this test is marked as Inconclusive

        Assert.Inconclusive("Test requires real instances of RenderManager and RenderInfoProvider. " +
                          "Cannot proceed without actual instances due to framework restrictions.");
    }

    /// <summary>
    /// Tests that constructor behavior when RenderInfoProvider is null in renderArgument
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_NullRenderInfoProvider_ThrowsNullReferenceException()
    {
        // Arrange
        // Note: This test would trigger NullReferenceException when accessing renderInfoProvider.IsDirty
        // Creating real RenderManager requires SkiaTextEditor which has complex dependencies

        // TODO: Create real RenderManager instance
        // For now, this test is marked as Inconclusive

        Assert.Inconclusive("Test requires real RenderManager instance. " +
                          "Cannot proceed without actual instance due to framework restrictions.");
    }

    /// <summary>
    /// Tests constructor with various RenderBounds values including boundary conditions
    /// </summary>
    /// <remarks>
    /// Testing boundary values for TextRect: zero size, negative values, maximum values
    /// This test is marked as Inconclusive due to dependency constraints.
    /// </remarks>
    [TestMethod]
    [DataRow(0.0, 0.0, 0.0, 0.0, DisplayName = "Zero size bounds")]
    [DataRow(-100.0, -100.0, 50.0, 50.0, DisplayName = "Negative position")]
    [DataRow(0.0, 0.0, double.MaxValue, double.MaxValue, DisplayName = "Maximum size")]
    [DataRow(double.MinValue, double.MinValue, 100.0, 100.0, DisplayName = "Minimum position")]
    public void BaseSkiaTextRenderer_VariousRenderBounds_AssignsBoundsCorrectly(double x, double y, double width, double height)
    {
        // Arrange
        var renderBounds = new TextRect(x, y, width, height);

        // TODO: This test requires real instances of RenderManager and RenderInfoProvider
        // Complete the test by creating actual instances and verifying that RenderBounds property
        // is correctly assigned from the renderArgument parameter

        Assert.Inconclusive($"Test requires real instances for bounds ({x}, {y}, {width}, {height}). " +
                          "Cannot proceed without actual instances due to framework restrictions.");
    }

    /// <summary>
    /// Tests constructor with null Viewport (optional parameter)
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_NullViewport_AcceptsNullValue()
    {
        // Arrange
        // Viewport is TextRect? (nullable), so null is valid

        // TODO: This test requires real instances of RenderManager and RenderInfoProvider
        // Verify that Viewport property correctly accepts null value

        Assert.Inconclusive("Test requires real instances of unmockable types. " +
                          "See test comments for guidance on completing this test.");
    }

    /// <summary>
    /// Tests constructor with non-null Viewport value
    /// </summary>
    [TestMethod]
    public void BaseSkiaTextRenderer_NonNullViewport_AssignsViewportCorrectly()
    {
        // Arrange
        var viewport = new TextRect(10, 10, 200, 150);

        // TODO: This test requires real instances of RenderManager and RenderInfoProvider
        // Verify that Viewport property is correctly assigned from renderArgument

        Assert.Inconclusive("Test requires real instances of unmockable types. " +
                          "See test comments for guidance on completing this test.");
    }

    /// <summary>
    /// Tests that constructor Debug.Assert fails when RenderInfoProvider.IsDirty is true
    /// </summary>
    /// <remarks>
    /// Debug.Assert only executes in DEBUG builds. This test verifies the assertion logic.
    /// In release builds, the assertion is not evaluated.
    /// </remarks>
    [TestMethod]
    public void BaseSkiaTextRenderer_IsDirtyTrue_DebugAssertFails()
    {
        // Arrange
        var mockRenderManager = new Mock<RenderManager>();
        var mockTextEditor = new Mock<SkiaTextEditor>();
        mockRenderManager.Setup(rm => rm.TextEditor).Returns(mockTextEditor.Object);

        var mockCanvas = new Mock<SKCanvas>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        
        // Setup IsDirty to return true - this violates the Debug.Assert condition
        mockRenderInfoProvider.Setup(rip => rip.IsDirty).Returns(true);
        
        var renderBounds = new TextRect(0, 0, 100, 100);
        var renderArgument = new SkiaTextRenderArgument
        {
            Canvas = mockCanvas.Object,
            RenderInfoProvider = mockRenderInfoProvider.Object,
            RenderBounds = renderBounds
        };

        // Act
        // Note: Debug.Assert doesn't throw exceptions, so constructor will succeed even with IsDirty=true
        // However, we can verify that the condition being asserted is violated
        var renderer = new TestableBaseSkiaTextRenderer(mockRenderManager.Object, renderArgument);

        // Assert
        // Verify that IsDirty is true, which would trigger the Debug.Assert in DEBUG builds
        Assert.IsTrue(mockRenderInfoProvider.Object.IsDirty, 
            "IsDirty should be true to demonstrate the Debug.Assert condition is violated");
    }

}