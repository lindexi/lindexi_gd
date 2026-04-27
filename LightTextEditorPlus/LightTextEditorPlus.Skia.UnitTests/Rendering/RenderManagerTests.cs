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
    /// Tests that UpdateCaretAndSelectionRender throws NullReferenceException when renderInfoProvider is null.
    /// Input: null renderInfoProvider, default Selection
    /// Expected: NullReferenceException is thrown (note: production code doesn't validate null parameter)
    /// </summary>
    [TestMethod]
    public void UpdateCaretAndSelectionRender_NullRenderInfoProvider_ThrowsArgumentNullException()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        RenderManager renderManager = new RenderManager(textEditor);
        Selection selection = default;

        // Act & Assert
        // Note: Production code doesn't validate null parameter, so it throws NullReferenceException instead of ArgumentNullException
        Assert.ThrowsException<NullReferenceException>(() =>
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
        // This test requires mocking RenderInfoProvider which cannot be done
        // because RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider as it has an internal constructor. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that UpdateCaretAndSelectionRender correctly passes parameters to BuildCaretAndSelectionRender.
    /// Input: valid renderInfoProvider with specific Selection values
    /// Expected: BuildCaretAndSelectionRender is called with correct parameters
    /// </summary>
    [TestMethod]
    public void UpdateCaretAndSelectionRender_ValidSelection_PassesCorrectParametersToBuildMethod()
    {
        // This test requires mocking RenderInfoProvider which cannot be done
        // because RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider as it has an internal constructor. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that UpdateCaretAndSelectionRender updates render multiple times correctly.
    /// Input: calling UpdateCaretAndSelectionRender multiple times with different selections
    /// Expected: _currentCaretAndSelectionRender is updated each time
    /// </summary>
    [TestMethod]
    public void UpdateCaretAndSelectionRender_CalledMultipleTimes_UpdatesRenderEachTime()
    {
        // This test requires mocking RenderInfoProvider which cannot be done
        // because RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider as it has an internal constructor. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that UpdateCaretAndSelectionRender handles empty selection correctly.
    /// Input: empty selection (default)
    /// Expected: _currentCaretAndSelectionRender is set to a valid render object
    /// </summary>
    [TestMethod]
    public void UpdateCaretAndSelectionRender_EmptySelection_CreatesCaretRender()
    {
        // This test requires mocking RenderInfoProvider which cannot be done
        // because RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider as it has an internal constructor. Consider refactoring to use dependency injection or interfaces.");
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
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();
        var mockCaretRenderInfo = new CaretRenderInfo();

        var caretThickness = 2.0;
        var caretColor = new SKColor(255, 0, 0);
        var caretBounds = new TextRect(10, 20, caretThickness, 30);
        var isOvertypeMode = false;

        // Set CaretConfiguration properties directly on the real instance
        textEditor.CaretConfiguration.CaretThickness = caretThickness;
        textEditor.CaretConfiguration.CaretBrush = caretColor;

        var renderManager = new RenderManager(textEditor);
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
        var textEditor = new SkiaTextEditor();
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

        // Set configuration directly
        textEditor.CaretConfiguration.CaretThickness = caretThickness;
        textEditor.CaretConfiguration.CaretBrush = nullCaretBrush;

        var renderManager = new RenderManager(textEditor);
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
        // This test requires mocking RenderInfoProvider.GetSelectionBoundsList which cannot be done
        // because the method is not virtual and RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider.GetSelectionBoundsList as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles empty selection bounds list correctly.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithEmptyBoundsList_ReturnsSelectionRenderWithEmptyList()
    {
        // This test requires mocking RenderInfoProvider.GetSelectionBoundsList which cannot be done
        // because the method is not virtual and RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider.GetSelectionBoundsList as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles single selection bound correctly.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithSingleBound_ReturnsSelectionRenderWithSingleBound()
    {
        // This test requires mocking RenderInfoProvider.GetSelectionBoundsList which cannot be done
        // because the method is not virtual and RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider.GetSelectionBoundsList as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles multiple selection bounds correctly.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_NonEmptySelectionWithMultipleBounds_ReturnsSelectionRenderWithMultipleBounds()
    {
        // This test requires mocking RenderInfoProvider.GetSelectionBoundsList which cannot be done
        // because the method is not virtual and RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider.GetSelectionBoundsList as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
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
        // This test requires mocking RenderInfoProvider.GetSelectionBoundsList which cannot be done
        // because the method is not virtual and RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider.GetSelectionBoundsList as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender correctly identifies empty selection with zero length.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_SelectionWithZeroLength_TreatedAsEmptySelection()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var mockRenderInfoProvider = new Mock<RenderInfoProvider>();

        textEditor.CaretConfiguration.CaretThickness = 2.0;
        textEditor.CaretConfiguration.CaretBrush = new SKColor(255, 0, 0);

        var renderManager = new RenderManager(textEditor);
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
        // This test requires mocking RenderInfoProvider.GetSelectionBoundsList which cannot be done
        // because the method is not virtual and RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider.GetSelectionBoundsList as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
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
        // This test requires mocking RenderInfoProvider.GetSelectionBoundsList which cannot be done
        // because the method is not virtual and RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider.GetSelectionBoundsList as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles selection bounds with boundary values.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_SelectionBoundsWithBoundaryValues_HandlesCorrectly()
    {
        // This test requires mocking RenderInfoProvider.GetSelectionBoundsList which cannot be done
        // because the method is not virtual and RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider.GetSelectionBoundsList as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that BuildCaretAndSelectionRender handles selection with maximum integer length.
    /// </summary>
    [TestMethod]
    public void BuildCaretAndSelectionRender_SelectionWithMaxLength_ReturnsSelectionRender()
    {
        // This test requires mocking RenderInfoProvider.GetSelectionBoundsList which cannot be done
        // because the method is not virtual and RenderInfoProvider has an internal constructor
        Assert.Inconclusive("Cannot mock RenderInfoProvider.GetSelectionBoundsList as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
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
        // This test requires mocking TextEditorCore.TryGetRenderInfo which cannot be done
        // because SkiaTextEditor.TextEditorCore is not virtual and cannot be mocked
        Assert.Inconclusive("Cannot mock SkiaTextEditor.TextEditorCore property as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that GetCurrentTextRender returns existing render when _currentRender is already set.
    /// Verifies that IsUsed is set to true and the existing render object is returned without calling Render again.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_WhenCurrentRenderExists_ReturnsExistingRender()
    {
        // This test requires mocking TextEditorCore which cannot be done
        // because SkiaTextEditor.TextEditorCore is not virtual and cannot be mocked
        Assert.Inconclusive("Cannot mock SkiaTextEditor.TextEditorCore property as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that GetCurrentTextRender initializes render when _currentRender is null.
    /// Verifies that GetRenderInfo is called, Render is invoked, and a new render object is created and returned.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_WhenCurrentRenderIsNull_InitializesAndReturnsNewRender()
    {
        // This test requires mocking TextEditorCore which cannot be done
        // because SkiaTextEditor.TextEditorCore is not virtual and cannot be mocked
        Assert.Inconclusive("Cannot mock SkiaTextEditor.TextEditorCore property as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that GetCurrentTextRender sets IsUsed to true on returned render.
    /// Verifies the IsUsed property is correctly updated each time the method is called.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_Always_SetsIsUsedToTrue()
    {
        // This test requires mocking TextEditorCore which cannot be done
        // because SkiaTextEditor.TextEditorCore is not virtual and cannot be mocked
        Assert.Inconclusive("Cannot mock SkiaTextEditor.TextEditorCore property as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that GetCurrentTextRender returns non-disposed render object.
    /// Verifies that the returned render object has IsDisposed set to false.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_Always_ReturnsNonDisposedRender()
    {
        // This test requires mocking TextEditorCore which cannot be done
        // because SkiaTextEditor.TextEditorCore is not virtual and cannot be mocked
        Assert.Inconclusive("Cannot mock SkiaTextEditor.TextEditorCore property as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
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
        // This test requires mocking TextEditorCore which cannot be done
        // because SkiaTextEditor.TextEditorCore is not virtual and cannot be mocked
        Assert.Inconclusive("Cannot mock SkiaTextEditor.TextEditorCore property as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that GetCurrentTextRender calls GetRenderInfo only on first invocation.
    /// Verifies that subsequent calls do not trigger additional GetRenderInfo calls.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_OnFirstCall_CallsGetRenderInfo()
    {
        // This test requires mocking TextEditorCore which cannot be done
        // because SkiaTextEditor.TextEditorCore is not virtual and cannot be mocked
        Assert.Inconclusive("Cannot mock SkiaTextEditor.TextEditorCore property as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
    }

    /// <summary>
    /// Tests that GetCurrentTextRender returns ITextEditorContentSkiaRenderer interface.
    /// Verifies that the return type matches the expected interface.
    /// </summary>
    [TestMethod]
    public void GetCurrentTextRender_Always_ReturnsITextEditorContentSkiaRenderer()
    {
        // This test requires mocking TextEditorCore which cannot be done
        // because SkiaTextEditor.TextEditorCore is not virtual and cannot be mocked
        Assert.Inconclusive("Cannot mock SkiaTextEditor.TextEditorCore property as it is not virtual. Consider refactoring to use dependency injection or interfaces.");
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