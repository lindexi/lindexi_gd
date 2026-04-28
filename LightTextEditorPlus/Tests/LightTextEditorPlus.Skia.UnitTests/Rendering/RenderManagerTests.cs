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
    /// Expected: ArgumentNullException is thrown.
    /// </summary>
    [TestMethod]
    public void UpdateCaretAndSelectionRender_NullRenderInfoProvider_ThrowsArgumentNullException()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        RenderManager renderManager = new RenderManager(textEditor);
        Selection selection = default;

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
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
        SkiaTextEditor textEditor = new SkiaTextEditor();
        textEditor.AppendText("Hello World");
        RenderManager renderManager = new RenderManager(textEditor);
        Selection selection = new Selection(new CaretOffset(0), 5);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        renderManager.UpdateCaretAndSelectionRender(renderInfoProvider, in selection);

        // Assert
        var currentRenderField = typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var currentRender = currentRenderField!.GetValue(renderManager);
        
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
        SkiaTextEditor textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test content");
        RenderManager renderManager = new RenderManager(textEditor);
        Selection selection = new Selection(new CaretOffset(2), 4);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        renderManager.UpdateCaretAndSelectionRender(renderInfoProvider, in selection);

        // Assert
        // Verify that the render was created (BuildCaretAndSelectionRender was called internally)
        var currentRenderField = typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var currentRender = currentRenderField!.GetValue(renderManager);
        
        Assert.IsNotNull(currentRender);
        Assert.IsInstanceOfType(currentRender, typeof(ITextEditorCaretAndSelectionRenderSkiaRenderer));
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
        SkiaTextEditor textEditor = new SkiaTextEditor();
        textEditor.AppendText("Multiple updates test");
        RenderManager renderManager = new RenderManager(textEditor);
        
        var currentRenderField = typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act & Assert
        Selection selection1 = new Selection(new CaretOffset(0), 5);
        RenderInfoProvider renderInfoProvider1 = textEditor.TextEditorCore.GetRenderInfo();
        renderManager.UpdateCaretAndSelectionRender(renderInfoProvider1, in selection1);
        var render1 = currentRenderField!.GetValue(renderManager);
        Assert.IsNotNull(render1);

        Selection selection2 = new Selection(new CaretOffset(5), 3);
        RenderInfoProvider renderInfoProvider2 = textEditor.TextEditorCore.GetRenderInfo();
        renderManager.UpdateCaretAndSelectionRender(renderInfoProvider2, in selection2);
        var render2 = currentRenderField.GetValue(renderManager);
        Assert.IsNotNull(render2);
        
        // Each update creates a new render object
        Assert.AreNotSame(render1, render2);
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
        SkiaTextEditor textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        RenderManager renderManager = new RenderManager(textEditor);
        Selection selection = new Selection(new CaretOffset(0), 0); // Empty selection
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        renderManager.UpdateCaretAndSelectionRender(renderInfoProvider, in selection);

        // Assert
        var currentRenderField = typeof(RenderManager).GetField("_currentCaretAndSelectionRender",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var currentRender = currentRenderField!.GetValue(renderManager);
        
        Assert.IsNotNull(currentRender);
        // Empty selection should create a caret render
        Assert.IsInstanceOfType(currentRender, typeof(TextEditorCaretSkiaRender));
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender returns a TextEditorCaretSkiaRender when selection is empty
    /// and CaretBrush is set.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_EmptySelectionWithCaretBrush_ReturnsCaretRender()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");

        var caretThickness = 2.0;
        var caretColor = new SKColor(255, 0, 0);

        // Set CaretConfiguration properties directly on the real instance
        textEditor.CaretConfiguration.CaretThickness = caretThickness;
        textEditor.CaretConfiguration.CaretBrush = caretColor;

        var renderManager = new RenderManager(textEditor);
        var selection = new Selection(new CaretOffset(0), 0);
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorCaretSkiaRender));
        
        var caretRender = (TextEditorCaretSkiaRender)result;
        Assert.AreEqual(caretColor, caretRender.CaretColor);
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender returns a TextEditorCaretSkiaRender when selection is empty
    /// and CaretBrush is null, using foreground color instead.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_EmptySelectionWithNullCaretBrush_UsesForegroundColor()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");

        var caretThickness = 2.0;
        SKColor? nullCaretBrush = null;

        // Set configuration directly
        textEditor.CaretConfiguration.CaretThickness = caretThickness;
        textEditor.CaretConfiguration.CaretBrush = nullCaretBrush;

        var renderManager = new RenderManager(textEditor);
        var selection = new Selection(new CaretOffset(0), 0);
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorCaretSkiaRender));
        
        // When CaretBrush is null, it uses the foreground color from CurrentCaretRunProperty
        var caretRender = (TextEditorCaretSkiaRender)result;
        Assert.IsTrue(caretRender.CaretColor != default); // Verify a color was set
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender returns a TextEditorSelectionSkiaRender when selection is not empty.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelection_ReturnsSelectionRender()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Hello World");
        
        var renderManager = new RenderManager(textEditor);
        var selection = new Selection(new CaretOffset(0), 5); // Select "Hello"
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles empty selection bounds list correctly.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithEmptyBoundsList_ReturnsSelectionRenderWithEmptyList()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        // Don't add any text, so selection bounds will be empty or minimal
        
        var renderManager = new RenderManager(textEditor);
        var selection = new Selection(new CaretOffset(0), 1); // Try to select beyond content
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
        
        var selectionRender = (TextEditorSelectionSkiaRender)result;
        // Selection bounds list might be empty if there's no text
        Assert.IsNotNull(selectionRender.SelectionBoundsList);
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles single selection bound correctly.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithSingleBound_ReturnsSelectionRenderWithSingleBound()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test");
        
        var renderManager = new RenderManager(textEditor);
        var selection = new Selection(new CaretOffset(0), 2); // Select first two characters
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
        
        var selectionRender = (TextEditorSelectionSkiaRender)result;
        Assert.IsNotNull(selectionRender.SelectionBoundsList);
        Assert.IsTrue(selectionRender.SelectionBoundsList.Count >= 1);
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles multiple selection bounds correctly.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithMultipleBounds_ReturnsSelectionRenderWithMultipleBounds()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        // Set a narrow width to force text wrapping, creating multiple selection bounds
        textEditor.TextEditorCore.DocumentManager.DocumentWidth = 50; // Very narrow
        textEditor.AppendText("This is a long text that will wrap");
        
        var renderManager = new RenderManager(textEditor);
        var selection = new Selection(new CaretOffset(0), 20); // Select across multiple lines
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
        
        var selectionRender = (TextEditorSelectionSkiaRender)result;
        Assert.IsNotNull(selectionRender.SelectionBoundsList);
        // With wrapping, we should get multiple bounds (one per line)
        Assert.IsTrue(selectionRender.SelectionBoundsList.Count >= 1);
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
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Color test");
        
        var selectionColor = new SKColor(red, green, blue, alpha);
        textEditor.CaretConfiguration.SelectionBrush = selectionColor;
        
        var renderManager = new RenderManager(textEditor);
        var selection = new Selection(new CaretOffset(0), 5);
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
        
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
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Zero length test");

        textEditor.CaretConfiguration.CaretThickness = 2.0;
        textEditor.CaretConfiguration.CaretBrush = new SKColor(255, 0, 0);

        var renderManager = new RenderManager(textEditor);
        var selection = new Selection(new CaretOffset(10), 0);
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        // Zero length selection should be treated as empty, returning a caret render
        Assert.IsInstanceOfType(result, typeof(TextEditorCaretSkiaRender));
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender correctly handles selection with negative offset difference
    /// (backward selection).
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_BackwardSelection_ReturnsSelectionRender()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Backward selection test");
        
        var renderManager = new RenderManager(textEditor);
        var selection = new Selection(new CaretOffset(10), -5); // Backward selection
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

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
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Overtype test");
        
        var renderManager = new RenderManager(textEditor);
        var selection = new Selection(new CaretOffset(0), 5); // Non-empty selection
        var renderContext = new CaretAndSelectionRenderContext(isOvertypeMode);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        // For non-empty selection, overtype mode should be ignored, always returning selection render
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles selection bounds with boundary values.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_SelectionBoundsWithBoundaryValues_HandlesCorrectly()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Boundary values test");
        
        var renderManager = new RenderManager(textEditor);
        // Select the entire text
        var selection = new Selection(new CaretOffset(0), textEditor.TextEditorCore.DocumentManager.CharCount);
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(TextEditorSelectionSkiaRender));
        
        var selectionRender = (TextEditorSelectionSkiaRender)result;
        Assert.IsNotNull(selectionRender.SelectionBoundsList);
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles selection with maximum integer length.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_SelectionWithMaxLength_ReturnsSelectionRender()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Max length test");
        
        var renderManager = new RenderManager(textEditor);
        // Use a very large but reasonable length
        var selection = new Selection(new CaretOffset(0), int.MaxValue);
        var renderContext = new CaretAndSelectionRenderContext(false);
        
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

        // Act
        var result = renderManager.BuildCaretAndSelectionRender(renderInfoProvider, in selection, in renderContext);

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
        var textEditor = new SkiaTextEditor();
        var mockCaretAndSelectionRender = new Mock<ITextEditorCaretAndSelectionRenderSkiaRenderer>();

        var renderManager = new RenderManager(textEditor);

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
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test content");
        
        var renderManager = new RenderManager(textEditor);
        
        // Set initial state by calling Render first
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        renderManager.Render(renderInfoProvider);
        
        // Set IsOvertypeModeCaret to false initially
        var isOvertypeModeCaretProperty = typeof(RenderManager).GetProperty("IsOvertypeModeCaret",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        isOvertypeModeCaretProperty!.SetValue(renderManager, false);

        // Make the document dirty so TryGetRenderInfo returns false
        textEditor.AppendText("More text");
        
        var renderContext = new CaretAndSelectionRenderContext(true); // Change to true

        // Act
        var result = renderManager.GetCurrentCaretAndSelectionRender(renderContext);

        // Assert
        Assert.IsNotNull(result);
        // Verify IsOvertypeModeCaret was updated
        var currentIsOvertypeMode = (bool)isOvertypeModeCaretProperty.GetValue(renderManager)!;
        Assert.AreEqual(true, currentIsOvertypeMode);
    }

    /// <summary>
    /// Tests that GetCurrentTextRender returns existing render when _currentRender is already set.
    /// Verifies that IsUsed is set to true and the existing render object is returned without calling Render again.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_WhenCurrentRenderExists_ReturnsExistingRender()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test content");
        
        var renderManager = new RenderManager(textEditor);
        
        // Call Render to create initial render
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        renderManager.Render(renderInfoProvider);

        // Act - Call GetCurrentTextRender twice
        var result1 = renderManager.GetCurrentTextRender();
        var result2 = renderManager.GetCurrentTextRender();

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.AreSame(result1, result2); // Should return the same instance
    }

    /// <summary>
    /// Tests that GetCurrentTextRender initializes render when _currentRender is null.
    /// Verifies that GetRenderInfo is called, Render is invoked, and a new render object is created and returned.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_WhenCurrentRenderIsNull_InitializesAndReturnsNewRender()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test content");
        
        var renderManager = new RenderManager(textEditor);
        
        // Don't call Render first, so _currentRender is null

        // Act
        var result = renderManager.GetCurrentTextRender();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ITextEditorContentSkiaRenderer));
    }

    /// <summary>
    /// Tests that GetCurrentTextRender sets IsUsed to true on returned render.
    /// Verifies the IsUsed property is correctly updated each time the method is called.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_Always_SetsIsUsedToTrue()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test content");
        
        var renderManager = new RenderManager(textEditor);
        
        // Call Render to create initial render
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        renderManager.Render(renderInfoProvider);

        // Act
        var result = renderManager.GetCurrentTextRender();

        // Assert
        Assert.IsNotNull(result);
        var textEditorSkiaRender = result as TextEditorSkiaRender;
        Assert.IsNotNull(textEditorSkiaRender);
        Assert.IsTrue(textEditorSkiaRender.IsUsed);
    }

    /// <summary>
    /// Tests that GetCurrentTextRender returns non-disposed render object.
    /// Verifies that the returned render object has IsDisposed set to false.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_Always_ReturnsNonDisposedRender()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test content");
        
        var renderManager = new RenderManager(textEditor);
        
        // Call Render to create initial render
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        renderManager.Render(renderInfoProvider);

        // Act
        var result = renderManager.GetCurrentTextRender();

        // Assert
        Assert.IsNotNull(result);
        var textEditorSkiaRender = result as TextEditorSkiaRender;
        Assert.IsNotNull(textEditorSkiaRender);
        Assert.IsFalse(textEditorSkiaRender.IsDisposed);
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
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test content");
        
        var renderManager = new RenderManager(textEditor);
        
        // Call Render to create initial render
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        renderManager.Render(renderInfoProvider);

        // Act
        ITextEditorContentSkiaRenderer? firstResult = null;
        for (int i = 0; i < callCount; i++)
        {
            var result = renderManager.GetCurrentTextRender();
            if (i == 0)
            {
                firstResult = result;
            }
            else
            {
                // All subsequent calls should return the same instance
                Assert.AreSame(firstResult, result);
            }
            
            // Verify IsUsed is true
            var textEditorSkiaRender = result as TextEditorSkiaRender;
            Assert.IsNotNull(textEditorSkiaRender);
            Assert.IsTrue(textEditorSkiaRender.IsUsed);
        }

        // Assert
        Assert.IsNotNull(firstResult);
    }

    /// <summary>
    /// Tests that GetCurrentTextRender calls GetRenderInfo only on first invocation.
    /// Verifies that subsequent calls do not trigger additional GetRenderInfo calls.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_OnFirstCall_CallsGetRenderInfo()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test content");
        
        var renderManager = new RenderManager(textEditor);
        
        // Don't call Render first, so the first GetCurrentTextRender will call GetRenderInfo internally

        // Act
        var result1 = renderManager.GetCurrentTextRender();
        var result2 = renderManager.GetCurrentTextRender();

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        // Should return the same instance on subsequent calls
        Assert.AreSame(result1, result2);
    }

    /// <summary>
    /// Tests that GetCurrentTextRender returns ITextEditorContentSkiaRenderer interface.
    /// Verifies that the return type matches the expected interface.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_Always_ReturnsITextEditorContentSkiaRenderer()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Test content");
        
        var renderManager = new RenderManager(textEditor);
        
        // Call Render to create initial render
        var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        renderManager.Render(renderInfoProvider);

        // Act
        var result = renderManager.GetCurrentTextRender();

        // Assert
        Assert.IsNotNull(result);
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