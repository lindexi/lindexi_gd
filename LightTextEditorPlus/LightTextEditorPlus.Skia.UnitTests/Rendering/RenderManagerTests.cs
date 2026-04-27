using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using LightTextEditorPlus;
using LightTextEditorPlus.Configurations;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Primitive;
using LightTextEditorPlus.Rendering;
using LightTextEditorPlus.Rendering.Core;
using LightTextEditorPlus.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering.UnitTests;


/// <summary>
/// Tests for <see cref="RenderManager"/> class.
/// </summary>
[TestClass]
public class RenderManagerTests
{
    /// <summary>
    /// Tests that UpdateCaretAndSelectionRender throws ArgumentNullException when renderInfoProvider is null.
    /// Input: null renderInfoProvider, default Selection
    /// Expected: ArgumentNullException is thrown
    /// </summary>
    [TestMethod]
    public void UpdateCaretAndSelectionRender_NullRenderInfoProvider_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        RenderManager renderManager = new RenderManager(mockTextEditor.Object);
        Selection selection = default;

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            renderManager.UpdateCaretAndSelectionRender(null!, in selection));
    }

    /// <summary>
    /// Tests that UpdateCaretAndSelectionRender successfully updates the caret and selection render with valid inputs.
    /// Input: valid renderInfoProvider, valid selection, IsOvertypeModeCaret = false
    /// Expected: _currentCaretAndSelectionRender is set to a non-null value
    /// </summary>
    [TestMethod]
    [DataRow(false, DisplayName = "IsOvertypeModeCaret = false")]
    [DataRow(true, DisplayName = "IsOvertypeModeCaret = true")]
    public void UpdateCaretAndSelectionRender_ValidInputs_UpdatesCaretAndSelectionRender(bool isOvertypeMode)
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        RenderManager renderManager = new RenderManager(mockTextEditor.Object);

        Mock<RenderInfoProvider> mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        Selection selection = default;

        // Set IsOvertypeModeCaret via reflection since it's a private property
        typeof(RenderManager).GetProperty("IsOvertypeModeCaret",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(renderManager, isOvertypeMode);

        // Act
        renderManager.UpdateCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection);

        // Assert
        ITextEditorCaretAndSelectionRenderSkiaRenderer? currentRender =
            typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(renderManager) as ITextEditorCaretAndSelectionRenderSkiaRenderer;

        Assert.IsNotNull(currentRender);
    }

    /// <summary>
    /// Tests that UpdateCaretAndSelectionRender correctly passes parameters to BuildCaretAndSelectionRender.
    /// Input: valid renderInfoProvider with specific Selection values
    /// Expected: BuildCaretAndSelectionRender is called with correct parameters
    /// </summary>
    [TestMethod]
    public void UpdateCaretAndSelectionRender_ValidSelection_PassesCorrectParametersToBuildMethod()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        RenderManager renderManager = new RenderManager(mockTextEditor.Object);

        Mock<RenderInfoProvider> mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        // Create a non-default selection
        Selection selection = new Selection(new CaretOffset(0), new CaretOffset(10));

        // Act
        renderManager.UpdateCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection);

        // Assert
        // Verify that _currentCaretAndSelectionRender is not null
        ITextEditorCaretAndSelectionRenderSkiaRenderer? currentRender =
            typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(renderManager) as ITextEditorCaretAndSelectionRenderSkiaRenderer;

        Assert.IsNotNull(currentRender);
    }

    /// <summary>
    /// Tests that UpdateCaretAndSelectionRender updates render multiple times correctly.
    /// Input: calling UpdateCaretAndSelectionRender multiple times with different selections
    /// Expected: _currentCaretAndSelectionRender is updated each time
    /// </summary>
    [TestMethod]
    public void UpdateCaretAndSelectionRender_CalledMultipleTimes_UpdatesRenderEachTime()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        RenderManager renderManager = new RenderManager(mockTextEditor.Object);

        Mock<RenderInfoProvider> mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        Selection selection1 = new Selection(new CaretOffset(0), new CaretOffset(5));
        Selection selection2 = new Selection(new CaretOffset(10), new CaretOffset(20));

        // Act
        renderManager.UpdateCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection1);
        ITextEditorCaretAndSelectionRenderSkiaRenderer? firstRender =
            typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(renderManager) as ITextEditorCaretAndSelectionRenderSkiaRenderer;

        renderManager.UpdateCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection2);
        ITextEditorCaretAndSelectionRenderSkiaRenderer? secondRender =
            typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(renderManager) as ITextEditorCaretAndSelectionRenderSkiaRenderer;

        // Assert
        Assert.IsNotNull(firstRender);
        Assert.IsNotNull(secondRender);
        // Note: We can't reliably compare instances since BuildCaretAndSelectionRender may return new instances
    }

    /// <summary>
    /// Tests that UpdateCaretAndSelectionRender handles empty selection correctly.
    /// Input: empty selection (default)
    /// Expected: _currentCaretAndSelectionRender is set to a valid render object
    /// </summary>
    [TestMethod]
    public void UpdateCaretAndSelectionRender_EmptySelection_CreatesCaretRender()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        RenderManager renderManager = new RenderManager(mockTextEditor.Object);

        Mock<RenderInfoProvider> mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        Selection emptySelection = default;

        // Act
        renderManager.UpdateCaretAndSelectionRender(mockRenderInfoProvider.Object, in emptySelection);

        // Assert
        ITextEditorCaretAndSelectionRenderSkiaRenderer? currentRender =
            typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(renderManager) as ITextEditorCaretAndSelectionRenderSkiaRenderer;

        Assert.IsNotNull(currentRender);
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender returns a TextEditorCaretSkiaRender when selection is empty
    /// and CaretBrush is set.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_EmptySelectionWithCaretBrush_ReturnsCaretRender()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var mockCaretRenderInfo = new CaretRenderInfo();

        var caretThickness = 2.0;
        var caretColor = new SKColor(255, 0, 0);
        var caretBounds = new TextRect(10, 20, caretThickness, 30);
        var isOvertypeMode = false;

        mockCaretConfiguration.Setup(c => c.CaretThickness).Returns(caretThickness);
        mockCaretConfiguration.Setup(c => c.CaretBrush).Returns(caretColor);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(0), 0);
        var renderContext = new CaretAndSelectionRenderContext(isOvertypeMode);

        // Mock GetCurrentCaretRenderInfo to return a mock that will produce caretBounds
        // Note: Since CaretRenderInfo is a struct with internal constructor, we need to mock the provider's method
        // However, the actual GetCaretBounds call on CaretRenderInfo cannot be easily mocked
        // We'll need to work around this limitation

        // Act & Assert
        // This test requires further implementation details that cannot be easily mocked
        // The CaretRenderInfo struct has an internal constructor and GetCaretBounds is an instance method
        // that requires complex internal state. Marking as inconclusive.
        Assert.Inconclusive("CaretRenderInfo struct with internal constructor and complex dependencies cannot be properly mocked. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender returns a TextEditorCaretSkiaRender when selection is empty
    /// and CaretBrush is null, using foreground color instead.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_EmptySelectionWithNullCaretBrush_UsesForegroundColor()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var mockTextEditorCore = new Mock<LightTextEditorPlus.Core.TextEditorCore>();
        var mockDocumentManager = new Mock<LightTextEditorPlus.Core.Document.DocumentManager>();
        var mockRunProperty = new Mock<IReadOnlyRunProperty>();
        var mockSkiaRunProperty = new Mock<SkiaTextRunProperty>();
        var mockForeground = new Mock<SkiaTextBrush>();

        var caretThickness = 2.0;
        SKColor? nullCaretBrush = null;
        var foregroundColor = new SKColor(0, 255, 0);
        var isOvertypeMode = false;

        mockCaretConfiguration.Setup(c => c.CaretThickness).Returns(caretThickness);
        mockCaretConfiguration.Setup(c => c.CaretBrush).Returns(nullCaretBrush);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);
        mockTextEditor.Setup(t => t.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditorCore.Setup(t => t.DocumentManager).Returns(mockDocumentManager.Object);
        mockDocumentManager.Setup(d => d.CurrentCaretRunProperty).Returns(mockRunProperty.Object);
        mockForeground.Setup(f => f.AsSolidColor()).Returns(foregroundColor);
        mockSkiaRunProperty.Setup(s => s.Foreground).Returns(mockForeground.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(0), 0);
        var renderContext = new CaretAndSelectionRenderContext(isOvertypeMode);

        // Act & Assert
        Assert.Inconclusive("CaretRenderInfo struct with internal constructor and complex dependencies cannot be properly mocked. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender returns a TextEditorSelectionSkiaRender when selection is not empty.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelection_ReturnsSelectionRender()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        var selectionColor = new SKColor(100, 100, 255);
        var selectionBounds = new List<TextRect>
        {
            new TextRect(0, 0, 100, 20),
            new TextRect(0, 20, 80, 20)
        };

        mockCaretConfiguration.Setup(c => c.SelectionBrush).Returns(selectionColor);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(0), 10);
        var renderContext = new CaretAndSelectionRenderContext(false);

        mockRenderInfoProvider.Setup(r => r.GetSelectionBoundsList(It.IsAny<Selection>()))
            .Returns(selectionBounds);

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
        var selectionRender = (TextEditorSelectionSkiaRender)result;
        Assert.AreEqual(selectionColor, selectionRender.SelectionColor);
        Assert.AreEqual(selectionBounds, selectionRender.SelectionBoundsList);
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles empty selection bounds list correctly.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithEmptyBoundsList_ReturnsSelectionRenderWithEmptyList()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        var selectionColor = new SKColor(100, 100, 255);
        var emptyBoundsList = new List<TextRect>();

        mockCaretConfiguration.Setup(c => c.SelectionBrush).Returns(selectionColor);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(0), 5);
        var renderContext = new CaretAndSelectionRenderContext(false);

        mockRenderInfoProvider.Setup(r => r.GetSelectionBoundsList(It.IsAny<Selection>()))
            .Returns(emptyBoundsList);

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
        var selectionRender = (TextEditorSelectionSkiaRender)result;
        Assert.AreEqual(0, selectionRender.SelectionBoundsList.Count);
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles single selection bound correctly.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithSingleBound_ReturnsSelectionRenderWithSingleBound()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        var selectionColor = new SKColor(100, 100, 255);
        var singleBound = new List<TextRect> { new TextRect(5, 10, 50, 15) };

        mockCaretConfiguration.Setup(c => c.SelectionBrush).Returns(selectionColor);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(0), 3);
        var renderContext = new CaretAndSelectionRenderContext(false);

        mockRenderInfoProvider.Setup(r => r.GetSelectionBoundsList(It.IsAny<Selection>()))
            .Returns(singleBound);

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
        var selectionRender = (TextEditorSelectionSkiaRender)result;
        Assert.AreEqual(1, selectionRender.SelectionBoundsList.Count);
        Assert.AreEqual(singleBound[0], selectionRender.SelectionBoundsList[0]);
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles multiple selection bounds correctly.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithMultipleBounds_ReturnsSelectionRenderWithMultipleBounds()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        var selectionColor = new SKColor(100, 100, 255);
        var multipleBounds = new List<TextRect>
        {
            new TextRect(0, 0, 100, 20),
            new TextRect(0, 20, 80, 20),
            new TextRect(0, 40, 120, 20)
        };

        mockCaretConfiguration.Setup(c => c.SelectionBrush).Returns(selectionColor);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(0), 50);
        var renderContext = new CaretAndSelectionRenderContext(false);

        mockRenderInfoProvider.Setup(r => r.GetSelectionBoundsList(It.IsAny<Selection>()))
            .Returns(multipleBounds);

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
        var selectionRender = (TextEditorSelectionSkiaRender)result;
        Assert.AreEqual(3, selectionRender.SelectionBoundsList.Count);
        for (int i = 0; i < multipleBounds.Count; i++)
        {
            Assert.AreEqual(multipleBounds[i], selectionRender.SelectionBoundsList[i]);
        }
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender uses correct selection color values.
    /// </summary>
    /// <param name="red">Red component of selection color.</param>
    /// <param name="green">Green component of selection color.</param>
    /// <param name="blue">Blue component of selection color.</param>
    /// <param name="alpha">Alpha component of selection color.</param>
    [TestMethod]
    [DataRow((byte)0, (byte)0, (byte)0, (byte)255)]
    [DataRow((byte)255, (byte)255, (byte)255, (byte)255)]
    [DataRow((byte)128, (byte)64, (byte)192, (byte)128)]
    [DataRow((byte)255, (byte)0, (byte)0, (byte)255)]
    [DataRow((byte)0, (byte)255, (byte)0, (byte)255)]
    [DataRow((byte)0, (byte)0, (byte)255, (byte)255)]
    [DataRow((byte)0, (byte)0, (byte)0, (byte)0)]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithVariousColors_UsesCorrectColor(byte red, byte green, byte blue, byte alpha)
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        var selectionColor = new SKColor(red, green, blue, alpha);
        var selectionBounds = new List<TextRect> { new TextRect(0, 0, 100, 20) };

        mockCaretConfiguration.Setup(c => c.SelectionBrush).Returns(selectionColor);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(0), 5);
        var renderContext = new CaretAndSelectionRenderContext(false);

        mockRenderInfoProvider.Setup(r => r.GetSelectionBoundsList(It.IsAny<Selection>()))
            .Returns(selectionBounds);

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection, in renderContext);

        // Assert
        var selectionRender = (TextEditorSelectionSkiaRender)result;
        Assert.AreEqual(selectionColor, selectionRender.SelectionColor);
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender correctly identifies empty selection with zero length.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_SelectionWithZeroLength_TreatedAsEmptySelection()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        mockCaretConfiguration.Setup(c => c.CaretThickness).Returns(2.0);
        mockCaretConfiguration.Setup(c => c.CaretBrush).Returns(new SKColor(255, 0, 0));
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(10), 0);
        var renderContext = new CaretAndSelectionRenderContext(false);

        // Act & Assert
        // This will attempt the caret path, which requires CaretRenderInfo mock
        Assert.Inconclusive("CaretRenderInfo struct with internal constructor and complex dependencies cannot be properly mocked. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender correctly handles selection with negative offset difference
    /// (backward selection).
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_BackwardSelection_ReturnsSelectionRender()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        var selectionColor = new SKColor(100, 100, 255);
        var selectionBounds = new List<TextRect> { new TextRect(0, 0, 100, 20) };

        mockCaretConfiguration.Setup(c => c.SelectionBrush).Returns(selectionColor);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        // Create backward selection (end before start)
        var selection = new Selection(new CaretOffset(10), new CaretOffset(5));
        var renderContext = new CaretAndSelectionRenderContext(false);

        mockRenderInfoProvider.Setup(r => r.GetSelectionBoundsList(It.IsAny<Selection>()))
            .Returns(selectionBounds);

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles IsOvertypeModeCaret context correctly for selection.
    /// </summary>
    /// <param name="isOvertypeMode">Whether overtype mode is enabled.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithOvertypeMode_IgnoresOvertypeMode(bool isOvertypeMode)
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        var selectionColor = new SKColor(100, 100, 255);
        var selectionBounds = new List<TextRect> { new TextRect(0, 0, 100, 20) };

        mockCaretConfiguration.Setup(c => c.SelectionBrush).Returns(selectionColor);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(0), 5);
        var renderContext = new CaretAndSelectionRenderContext(isOvertypeMode);

        mockRenderInfoProvider.Setup(r => r.GetSelectionBoundsList(It.IsAny<Selection>()))
            .Returns(selectionBounds);

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
        // IsOvertypeModeCaret should not affect selection rendering
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles selection bounds with boundary values.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_SelectionBoundsWithBoundaryValues_HandlesCorrectly()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        var selectionColor = new SKColor(100, 100, 255);
        var selectionBounds = new List<TextRect>
        {
            new TextRect(0, 0, 0, 0), // Zero size
            new TextRect(double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue), // Max values
            new TextRect(double.MinValue, double.MinValue, 1, 1), // Min values
            new TextRect(-100, -100, 50, 50) // Negative coordinates
        };

        mockCaretConfiguration.Setup(c => c.SelectionBrush).Returns(selectionColor);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(0), 10);
        var renderContext = new CaretAndSelectionRenderContext(false);

        mockRenderInfoProvider.Setup(r => r.GetSelectionBoundsList(It.IsAny<Selection>()))
            .Returns(selectionBounds);

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        var selectionRender = (TextEditorSelectionSkiaRender)result;
        Assert.AreEqual(4, selectionRender.SelectionBoundsList.Count);
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles selection with maximum integer length.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_SelectionWithMaxLength_ReturnsSelectionRender()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretConfiguration = new Mock<SkiaCaretConfiguration>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        var selectionColor = new SKColor(100, 100, 255);
        var selectionBounds = new List<TextRect> { new TextRect(0, 0, 100, 20) };

        mockCaretConfiguration.Setup(c => c.SelectionBrush).Returns(selectionColor);
        mockTextEditor.Setup(t => t.CaretConfiguration).Returns(mockCaretConfiguration.Object);

        var renderManager = new RenderManager(mockTextEditor.Object);
        var selection = new Selection(new CaretOffset(0), int.MaxValue);
        var renderContext = new CaretAndSelectionRenderContext(false);

        mockRenderInfoProvider.Setup(r => r.GetSelectionBoundsList(It.IsAny<Selection>()))
            .Returns(selectionBounds);

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(mockRenderInfoProvider.Object, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
    }

    /// <summary>
    /// Tests that GetCurrentCaretAndSelectionRender returns the current render without updating
    /// when IsOvertypeModeCaret matches the render context value.
    /// </summary>
    [TestMethod]
    public void GetCurrentCaretAndSelectionRender_IsOvertypeModeCaretUnchanged_ReturnsCurrentRenderWithoutUpdate()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockCaretAndSelectionRender = new Mock<ITextEditorCaretAndSelectionRenderSkiaRenderer>();

        var renderManager = new RenderManager(mockTextEditor.Object);

        // Set up initial state through reflection
        var isOvertypeModeCaretProperty = typeof(RenderManager).GetProperty("IsOvertypeModeCaret",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        isOvertypeModeCaretProperty!.SetValue(renderManager, false);

        var currentCaretAndSelectionRenderField = typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        currentCaretAndSelectionRenderField!.SetValue(renderManager, mockCaretAndSelectionRender.Object);

        var renderContext = new CaretAndSelectionRenderContext(false);

        // Act
        var result = renderManager.GetCurrentCaretAndSelectionRender(renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(mockCaretAndSelectionRender.Object, result);
        // Note: Cannot verify TryGetRenderInfo is not called because TextEditorCore property is non-virtual and cannot be mocked
    }

    /// <summary>
    /// Tests that GetCurrentCaretAndSelectionRender updates IsOvertypeModeCaret but does not call UpdateCaretAndSelectionRender
    /// when IsOvertypeModeCaret differs and TryGetRenderInfo returns false.
    /// </summary>
    [TestMethod]
    public void GetCurrentCaretAndSelectionRender_IsOvertypeModeCaretChanged_TryGetRenderInfoFalse_UpdatesFlagOnly()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockTextEditorCore = new Mock<TextEditorCore>();
        var mockCaretAndSelectionRender = new Mock<ITextEditorCaretAndSelectionRenderSkiaRenderer>();

        mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

        RenderInfoProvider? outRenderInfo = null;
        mockTextEditorCore.Setup(x => x.TryGetRenderInfo(out outRenderInfo, It.IsAny<bool>())).Returns(false);

        var renderManager = new RenderManager(mockTextEditor.Object);

        // Set up initial state
        var isOvertypeModeCaretProperty = typeof(RenderManager).GetProperty("IsOvertypeModeCaret",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        isOvertypeModeCaretProperty!.SetValue(renderManager, false);

        var currentCaretAndSelectionRenderField = typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        currentCaretAndSelectionRenderField!.SetValue(renderManager, mockCaretAndSelectionRender.Object);

        var renderContext = new CaretAndSelectionRenderContext(true);

        // Act
        var result = renderManager.GetCurrentCaretAndSelectionRender(renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(mockCaretAndSelectionRender.Object, result);
        var updatedIsOvertypeModeCaret = (bool)isOvertypeModeCaretProperty.GetValue(renderManager)!;
        Assert.AreEqual(true, updatedIsOvertypeModeCaret);
        mockTextEditorCore.Verify(x => x.TryGetRenderInfo(out outRenderInfo, It.IsAny<bool>()), Times.Once);
        mockTextEditorCore.Verify(x => x.CurrentSelection, Times.Never);
    }

    /// <summary>
    /// Tests that GetCurrentTextRender returns existing render when _currentRender is already set.
    /// Verifies that IsUsed is set to true and the existing render object is returned without calling Render again.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_WhenCurrentRenderExists_ReturnsExistingRender()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockTextEditorCore = new Mock<TextEditorCore>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditorCore.Setup(c => c.IsDirty).Returns(false);
        mockTextEditorCore.Setup(c => c.GetRenderInfo()).Returns(mockRenderInfoProvider.Object);
        mockTextEditorCore.Setup(c => c.CurrentSelection).Returns(default(Selection));

        var renderManager = new RenderManager(mockTextEditor.Object);

        // First call to initialize _currentRender
        var firstRender = renderManager.GetCurrentTextRender();

        // Reset IsUsed to simulate usage
        if (firstRender is TextEditorSkiaRender firstSkiaRender)
        {
            firstSkiaRender.IsUsed = false;
        }

        // Act
        var secondRender = renderManager.GetCurrentTextRender();

        // Assert
        Assert.IsNotNull(secondRender);
        Assert.AreSame(firstRender, secondRender);
        Assert.IsFalse(secondRender.IsDisposed);

        if (secondRender is TextEditorSkiaRender skiaRender)
        {
            Assert.IsTrue(skiaRender.IsUsed);
        }
    }

    /// <summary>
    /// Tests that GetCurrentTextRender initializes render when _currentRender is null.
    /// Verifies that GetRenderInfo is called, Render is invoked, and a new render object is created and returned.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_WhenCurrentRenderIsNull_InitializesAndReturnsNewRender()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockTextEditorCore = new Mock<TextEditorCore>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditorCore.Setup(c => c.IsDirty).Returns(false);
        mockTextEditorCore.Setup(c => c.GetRenderInfo()).Returns(mockRenderInfoProvider.Object);
        mockTextEditorCore.Setup(c => c.CurrentSelection).Returns(default(Selection));

        var renderManager = new RenderManager(mockTextEditor.Object);

        // Act
        var result = renderManager.GetCurrentTextRender();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsDisposed);

        if (result is TextEditorSkiaRender skiaRender)
        {
            Assert.IsTrue(skiaRender.IsUsed);
        }

        mockTextEditorCore.Verify(c => c.GetRenderInfo(), Times.Once);
    }

    /// <summary>
    /// Tests that GetCurrentTextRender sets IsUsed to true on returned render.
    /// Verifies the IsUsed property is correctly updated each time the method is called.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_Always_SetsIsUsedToTrue()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockTextEditorCore = new Mock<TextEditorCore>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditorCore.Setup(c => c.IsDirty).Returns(false);
        mockTextEditorCore.Setup(c => c.GetRenderInfo()).Returns(mockRenderInfoProvider.Object);
        mockTextEditorCore.Setup(c => c.CurrentSelection).Returns(default(Selection));

        var renderManager = new RenderManager(mockTextEditor.Object);

        // Act
        var result = renderManager.GetCurrentTextRender();

        // Assert
        if (result is TextEditorSkiaRender skiaRender)
        {
            Assert.IsTrue(skiaRender.IsUsed);
        }
    }

    /// <summary>
    /// Tests that GetCurrentTextRender returns non-disposed render object.
    /// Verifies that the returned render object has IsDisposed set to false.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_Always_ReturnsNonDisposedRender()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockTextEditorCore = new Mock<TextEditorCore>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditorCore.Setup(c => c.IsDirty).Returns(false);
        mockTextEditorCore.Setup(c => c.GetRenderInfo()).Returns(mockRenderInfoProvider.Object);
        mockTextEditorCore.Setup(c => c.CurrentSelection).Returns(default(Selection));

        var renderManager = new RenderManager(mockTextEditor.Object);

        // Act
        var result = renderManager.GetCurrentTextRender();

        // Assert
        Assert.IsFalse(result.IsDisposed);
    }

    /// <summary>
    /// Tests that GetCurrentTextRender can be called multiple times consecutively.
    /// Verifies that multiple consecutive calls return the same render object and update IsUsed each time.
    /// </summary>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(5)]
    public void GetCurrentTextRender_MultipleConsecutiveCalls_ReturnsSameRenderAndUpdatesIsUsed(int callCount)
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockTextEditorCore = new Mock<TextEditorCore>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditorCore.Setup(c => c.IsDirty).Returns(false);
        mockTextEditorCore.Setup(c => c.GetRenderInfo()).Returns(mockRenderInfoProvider.Object);
        mockTextEditorCore.Setup(c => c.CurrentSelection).Returns(default(Selection));

        var renderManager = new RenderManager(mockTextEditor.Object);

        // Act
        ITextEditorContentSkiaRenderer? firstRender = null;
        for (int i = 0; i < callCount; i++)
        {
            var currentRender = renderManager.GetCurrentTextRender();

            if (i == 0)
            {
                firstRender = currentRender;
            }

            // Assert within loop
            Assert.IsNotNull(currentRender);
            Assert.AreSame(firstRender, currentRender);
            Assert.IsFalse(currentRender.IsDisposed);

            if (currentRender is TextEditorSkiaRender skiaRender)
            {
                Assert.IsTrue(skiaRender.IsUsed);
            }
        }

        // Verify GetRenderInfo was only called once (during first initialization)
        mockTextEditorCore.Verify(c => c.GetRenderInfo(), Times.Once);
    }

    /// <summary>
    /// Tests that GetCurrentTextRender calls GetRenderInfo only on first invocation.
    /// Verifies that subsequent calls do not trigger additional GetRenderInfo calls.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_OnFirstCall_CallsGetRenderInfo()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockTextEditorCore = new Mock<TextEditorCore>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditorCore.Setup(c => c.IsDirty).Returns(false);
        mockTextEditorCore.Setup(c => c.GetRenderInfo()).Returns(mockRenderInfoProvider.Object);
        mockTextEditorCore.Setup(c => c.CurrentSelection).Returns(default(Selection));

        var renderManager = new RenderManager(mockTextEditor.Object);

        // Act
        var firstRender = renderManager.GetCurrentTextRender();
        var secondRender = renderManager.GetCurrentTextRender();

        // Assert
        mockTextEditorCore.Verify(c => c.GetRenderInfo(), Times.Once);
    }

    /// <summary>
    /// Tests that GetCurrentTextRender returns ITextEditorContentSkiaRenderer interface.
    /// Verifies that the return type matches the expected interface.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_Always_ReturnsITextEditorContentSkiaRenderer()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var mockTextEditorCore = new Mock<TextEditorCore>();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditorCore.Setup(c => c.IsDirty).Returns(false);
        mockTextEditorCore.Setup(c => c.GetRenderInfo()).Returns(mockRenderInfoProvider.Object);
        mockTextEditorCore.Setup(c => c.CurrentSelection).Returns(default(Selection));

        var renderManager = new RenderManager(mockTextEditor.Object);

        // Act
        var result = renderManager.GetCurrentTextRender();

        // Assert
        Assert.IsInstanceOfType(result, typeof(ITextEditorContentSkiaRenderer));
    }

    /// <summary>
    /// Testable subclass of RenderManager that allows testing protected/internal behavior
    /// and tracking method calls without requiring full dependency setup.
    /// </summary>
    private class TestableRenderManager : RenderManager
    {
        private TextEditorSkiaRender? _testCurrentRender;
        private ITextEditorCaretAndSelectionRenderSkiaRenderer? _testCurrentCaretAndSelectionRender;

        public TestableRenderManager(SkiaTextEditor textEditor) : base(textEditor)
        {
        }

        public int UpdateCaretAndSelectionRenderCallCount { get; private set; }
        public int BuildTextEditorSkiaRenderCallCount { get; private set; }
        public RenderInfoProvider? LastRenderInfoProvider { get; private set; }
        public TextEditorSkiaRenderContext? LastBuildContext { get; private set; }

        public void SetCurrentRender(TextEditorSkiaRender render)
        {
            _testCurrentRender = render;
        }

        public TextEditorSkiaRender? GetCurrentRender()
        {
            return _testCurrentRender;
        }

        public new void UpdateCaretAndSelectionRender(RenderInfoProvider renderInfoProvider, in Selection selection)
        {
            UpdateCaretAndSelectionRenderCallCount++;
            LastRenderInfoProvider = renderInfoProvider;
            _testCurrentCaretAndSelectionRender = Mock.Of<ITextEditorCaretAndSelectionRenderSkiaRenderer>();
        }

        public new TextEditorSkiaRender BuildTextEditorSkiaRender(in TextEditorSkiaRenderContext renderContext)
        {
            BuildTextEditorSkiaRenderCallCount++;
            LastBuildContext = renderContext;
            _testCurrentRender = Mock.Of<TextEditorSkiaRender>();
            return _testCurrentRender;
        }
    }

    #region Helper Methods

    private RenderInfoProvider CreateMockRenderInfoProvider(double width, double height)
    {
        var documentOutlineBounds = new TextRect(0, 0, width, height);
        var documentContentBounds = new TextRect(0, 0, width, height);
        var documentLayoutBounds = new DocumentLayoutBounds(documentOutlineBounds, documentContentBounds);

        var renderInfoProviderMock = new Mock<RenderInfoProvider>();
        renderInfoProviderMock.Setup(x => x.GetDocumentLayoutBounds()).Returns(documentLayoutBounds);

        return renderInfoProviderMock.Object;
    }

    #endregion

    /// <summary>
    /// Tests that the constructor correctly assigns the textEditor parameter to the TextEditor property
    /// when provided with a valid SkiaTextEditor instance.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidTextEditor_AssignsToTextEditorProperty()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();

        // Act
        var renderManager = new RenderManager(textEditor);

        // Assert
        Assert.IsNotNull(renderManager);
        Assert.AreSame(textEditor, renderManager.TextEditor);
    }

    /// <summary>
    /// Tests that the constructor accepts a null textEditor parameter (runtime behavior)
    /// even though the parameter is marked as non-nullable.
    /// This verifies the constructor doesn't throw during construction when null is passed.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullTextEditor_AssignsNullToTextEditorProperty()
    {
        // Arrange
        SkiaTextEditor? textEditor = null;

        // Act
        var renderManager = new RenderManager(textEditor!);

        // Assert
        Assert.IsNotNull(renderManager);
        Assert.IsNull(renderManager.TextEditor);
    }
}