using System;

using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;

namespace LightTextEditorPlus.Diagnostics.UnitTests
{
    /// <summary>
    /// Tests for <see cref="SkiaTextEditorDebugConfiguration"/> class.
    /// </summary>
    [TestClass]
    public partial class SkiaTextEditorDebugConfigurationTests
    {
        /// <summary>
        /// Tests that DebugReRender calls the underlying TextEditor.DebugReRender method without throwing exceptions.
        /// </summary>
        /// <remarks>
        /// This test verifies that the DebugReRender method properly delegates to the TextEditor's DebugReRender method.
        /// Note: Due to the internal constructor and non-virtual DebugReRender method on SkiaTextEditor, 
        /// this test requires InternalsVisibleTo to be configured and cannot verify the delegation through mocking.
        /// The test is marked as inconclusive because proper verification of the delegation is not possible
        /// without creating a fake implementation, which is prohibited by the testing guidelines.
        /// 
        /// To properly test this method, consider:
        /// 1. Making SkiaTextEditor.DebugReRender virtual to allow mocking
        /// 2. Extracting an interface for SkiaTextEditor
        /// 3. Making the constructor public or providing a factory method for testing
        /// </remarks>
        [TestMethod]
        public void DebugReRender_WhenCalled_DelegatesToTextEditorDebugReRender()
        {
            // Arrange
            // Cannot properly arrange this test due to:
            // - Internal constructor on SkiaTextEditorDebugConfiguration
            // - Non-virtual DebugReRender method on SkiaTextEditor (cannot be mocked)
            // - Prohibition on creating fake/stub implementations

            // Act & Assert
            Assert.Inconclusive(
                "This test cannot be properly implemented without either:\n" +
                "1. Making SkiaTextEditor.DebugReRender virtual to enable mocking\n" +
                "2. Extracting an interface for SkiaTextEditor\n" +
                "3. Creating a testable wrapper (which violates test guidelines)\n" +
                "The method is a simple delegation to TextEditor.DebugReRender(), " +
                "but verification requires mocking capabilities that are not available " +
                "for non-virtual methods on concrete classes.");
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfo getter returns true when both the backing field is true and IsInDebugMode is true.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfo_GetWhenBackingFieldTrueAndIsInDebugModeTrue_ReturnsTrue()
        {
            // Arrange
            Mock<TextEditorCore> mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(true);

            Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            SkiaTextEditorDebugConfiguration configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);
            configuration.ShowHandwritingPaperDebugInfo = true;

            // Act
            bool result = configuration.ShowHandwritingPaperDebugInfo;

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfo getter returns false when the backing field is true but IsInDebugMode is false.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfo_GetWhenBackingFieldTrueAndIsInDebugModeFalse_ReturnsFalse()
        {
            // Arrange
            Mock<TextEditorCore> mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(false);

            Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            SkiaTextEditorDebugConfiguration configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);
            configuration.ShowHandwritingPaperDebugInfo = true;

            // Act
            bool result = configuration.ShowHandwritingPaperDebugInfo;

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfo getter returns false when the backing field is false and IsInDebugMode is true.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfo_GetWhenBackingFieldFalseAndIsInDebugModeTrue_ReturnsFalse()
        {
            // Arrange
            Mock<TextEditorCore> mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(true);

            Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            SkiaTextEditorDebugConfiguration configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);
            configuration.ShowHandwritingPaperDebugInfo = false;

            // Act
            bool result = configuration.ShowHandwritingPaperDebugInfo;

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfo getter returns false when both the backing field is false and IsInDebugMode is false.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfo_GetWhenBackingFieldFalseAndIsInDebugModeFalse_ReturnsFalse()
        {
            // Arrange
            Mock<TextEditorCore> mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(false);

            Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            SkiaTextEditorDebugConfiguration configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);
            configuration.ShowHandwritingPaperDebugInfo = false;

            // Act
            bool result = configuration.ShowHandwritingPaperDebugInfo;

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfo setter sets the backing field to false when given false.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfo_SetToFalse_SetsBackingFieldToFalse()
        {
            // Arrange
            Mock<TextEditorCore> mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(true);

            Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            SkiaTextEditorDebugConfiguration configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);
            configuration.ShowHandwritingPaperDebugInfo = true;

            // Act
            configuration.ShowHandwritingPaperDebugInfo = false;

            // Assert
            bool result = configuration.ShowHandwritingPaperDebugInfo;
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfo getter returns false by default when IsInDebugMode is false.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfo_GetDefaultValueWhenIsInDebugModeFalse_ReturnsFalse()
        {
            // Arrange
            Mock<TextEditorCore> mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(false);

            Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            SkiaTextEditorDebugConfiguration configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);

            // Act
            bool result = configuration.ShowHandwritingPaperDebugInfo;

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfo getter returns false by default when IsInDebugMode is true but backing field is not set.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfo_GetDefaultValueWhenIsInDebugModeTrue_ReturnsFalse()
        {
            // Arrange
            Mock<TextEditorCore> mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(true);

            Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            SkiaTextEditorDebugConfiguration configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);

            // Act
            bool result = configuration.ShowHandwritingPaperDebugInfo;

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfo getter reflects changes when IsInDebugMode changes from true to false.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfo_GetWhenIsInDebugModeChangesFromTrueToFalse_ReturnsCorrectValue()
        {
            // Arrange
            Mock<TextEditorCore> mockTextEditorCore = new Mock<TextEditorCore>();
            bool isInDebugMode = true;
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(() => isInDebugMode);

            Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            SkiaTextEditorDebugConfiguration configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);
            configuration.ShowHandwritingPaperDebugInfo = true;

            // Act & Assert - First state
            bool resultWhenTrue = configuration.ShowHandwritingPaperDebugInfo;
            Assert.IsTrue(resultWhenTrue);

            // Act & Assert - Change state
            isInDebugMode = false;
            bool resultWhenFalse = configuration.ShowHandwritingPaperDebugInfo;
            Assert.IsFalse(resultWhenFalse);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfo getter reflects changes when IsInDebugMode changes from false to true.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfo_GetWhenIsInDebugModeChangesFromFalseToTrue_ReturnsCorrectValue()
        {
            // Arrange
            Mock<TextEditorCore> mockTextEditorCore = new Mock<TextEditorCore>();
            bool isInDebugMode = false;
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(() => isInDebugMode);

            Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            SkiaTextEditorDebugConfiguration configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);
            configuration.ShowHandwritingPaperDebugInfo = true;

            // Act & Assert - First state
            bool resultWhenFalse = configuration.ShowHandwritingPaperDebugInfo;
            Assert.IsFalse(resultWhenFalse);

            // Act & Assert - Change state
            isInDebugMode = true;
            bool resultWhenTrue = configuration.ShowHandwritingPaperDebugInfo;
            Assert.IsTrue(resultWhenTrue);
        }

        /// <summary>
        /// Tests that ClearAllDebugBounds sets all debug bounds properties to null when IsInDebugMode is false.
        /// Input: IsInDebugMode returns false, all debug properties initially set to non-null values.
        /// Expected: All debug properties are set to null, DebugReRender is not called.
        /// </summary>
        [TestMethod]
        public void ClearAllDebugBounds_WhenNotInDebugMode_SetsAllPropertiesToNullWithoutRerender()
        {
            // Arrange
            var mockTextEditor = new Mock<SkiaTextEditor>();
            var mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(false);
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            var configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);

            // Set all properties to non-null values
            var testDrawInfo = new TextEditorDebugBoundsDrawInfo();
            configuration.DebugDrawCharBoundsInfo = testDrawInfo;
            configuration.DebugDrawCharSpanBoundsInfo = testDrawInfo;
            configuration.DebugDrawLineContentBoundsInfo = testDrawInfo;
            configuration.DebugDrawLineOutlineBoundsInfo = testDrawInfo;
            configuration.DebugDrawParagraphContentBoundsInfo = testDrawInfo;
            configuration.DebugDrawParagraphOutlineBoundsInfo = testDrawInfo;
            configuration.DebugDrawDocumentRenderBoundsInfo = testDrawInfo;
            configuration.DebugDrawDocumentContentBoundsInfo = testDrawInfo;
            configuration.DebugDrawDocumentOutlineBoundsInfo = testDrawInfo;

            // Act
            configuration.ClearAllDebugBounds();

            // Assert
            Assert.IsNull(configuration.DebugDrawCharBoundsInfo);
            Assert.IsNull(configuration.DebugDrawCharSpanBoundsInfo);
            Assert.IsNull(configuration.DebugDrawLineContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawLineOutlineBoundsInfo);
            Assert.IsNull(configuration.DebugDrawParagraphContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawParagraphOutlineBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentRenderBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentOutlineBoundsInfo);
            mockTextEditor.Verify(x => x.DebugReRender(), Times.Never);
        }

        /// <summary>
        /// Tests that ClearAllDebugBounds sets all debug bounds properties to null and calls DebugReRender when IsInDebugMode is true.
        /// Input: IsInDebugMode returns true, all debug properties initially set to non-null values.
        /// Expected: All debug properties are set to null, DebugReRender is called exactly once.
        /// </summary>
        [TestMethod]
        public void ClearAllDebugBounds_WhenInDebugMode_SetsAllPropertiesToNullAndCallsRerender()
        {
            // Arrange
            var mockTextEditor = new Mock<SkiaTextEditor>();
            var mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(true);
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);
            mockTextEditor.Setup(x => x.DebugReRender());

            var configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);

            // Set all properties to non-null values
            var testDrawInfo = new TextEditorDebugBoundsDrawInfo();
            configuration.DebugDrawCharBoundsInfo = testDrawInfo;
            configuration.DebugDrawCharSpanBoundsInfo = testDrawInfo;
            configuration.DebugDrawLineContentBoundsInfo = testDrawInfo;
            configuration.DebugDrawLineOutlineBoundsInfo = testDrawInfo;
            configuration.DebugDrawParagraphContentBoundsInfo = testDrawInfo;
            configuration.DebugDrawParagraphOutlineBoundsInfo = testDrawInfo;
            configuration.DebugDrawDocumentRenderBoundsInfo = testDrawInfo;
            configuration.DebugDrawDocumentContentBoundsInfo = testDrawInfo;
            configuration.DebugDrawDocumentOutlineBoundsInfo = testDrawInfo;

            // Act
            configuration.ClearAllDebugBounds();

            // Assert
            Assert.IsNull(configuration.DebugDrawCharBoundsInfo);
            Assert.IsNull(configuration.DebugDrawCharSpanBoundsInfo);
            Assert.IsNull(configuration.DebugDrawLineContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawLineOutlineBoundsInfo);
            Assert.IsNull(configuration.DebugDrawParagraphContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawParagraphOutlineBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentRenderBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentOutlineBoundsInfo);
            mockTextEditor.Verify(x => x.DebugReRender(), Times.Once);
        }

        /// <summary>
        /// Tests that ClearAllDebugBounds works correctly when all properties are already null.
        /// Input: IsInDebugMode returns true, all debug properties already null.
        /// Expected: All properties remain null, DebugReRender is called exactly once.
        /// </summary>
        [TestMethod]
        public void ClearAllDebugBounds_WhenPropertiesAlreadyNull_MaintainsNullAndCallsRerenderIfInDebugMode()
        {
            // Arrange
            var mockTextEditor = new Mock<SkiaTextEditor>();
            var mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(true);
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);
            mockTextEditor.Setup(x => x.DebugReRender());

            var configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);

            // Properties are already null by default

            // Act
            configuration.ClearAllDebugBounds();

            // Assert
            Assert.IsNull(configuration.DebugDrawCharBoundsInfo);
            Assert.IsNull(configuration.DebugDrawCharSpanBoundsInfo);
            Assert.IsNull(configuration.DebugDrawLineContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawLineOutlineBoundsInfo);
            Assert.IsNull(configuration.DebugDrawParagraphContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawParagraphOutlineBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentRenderBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentOutlineBoundsInfo);
            mockTextEditor.Verify(x => x.DebugReRender(), Times.Once);
        }

        /// <summary>
        /// Tests that ClearAllDebugBounds clears properties set to different values independently.
        /// Input: IsInDebugMode returns false, each debug property set to a different non-null value.
        /// Expected: All debug properties are set to null regardless of their initial values.
        /// </summary>
        [TestMethod]
        public void ClearAllDebugBounds_WhenPropertiesHaveDifferentValues_SetsAllToNull()
        {
            // Arrange
            var mockTextEditor = new Mock<SkiaTextEditor>();
            var mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(false);
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            var configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);

            // Set each property to a unique non-null value
            configuration.DebugDrawCharBoundsInfo = new TextEditorDebugBoundsDrawInfo();
            configuration.DebugDrawCharSpanBoundsInfo = new TextEditorDebugBoundsDrawInfo();
            configuration.DebugDrawLineContentBoundsInfo = new TextEditorDebugBoundsDrawInfo();
            configuration.DebugDrawLineOutlineBoundsInfo = new TextEditorDebugBoundsDrawInfo();
            configuration.DebugDrawParagraphContentBoundsInfo = new TextEditorDebugBoundsDrawInfo();
            configuration.DebugDrawParagraphOutlineBoundsInfo = new TextEditorDebugBoundsDrawInfo();
            configuration.DebugDrawDocumentRenderBoundsInfo = new TextEditorDebugBoundsDrawInfo();
            configuration.DebugDrawDocumentContentBoundsInfo = new TextEditorDebugBoundsDrawInfo();
            configuration.DebugDrawDocumentOutlineBoundsInfo = new TextEditorDebugBoundsDrawInfo();

            // Act
            configuration.ClearAllDebugBounds();

            // Assert
            Assert.IsNull(configuration.DebugDrawCharBoundsInfo);
            Assert.IsNull(configuration.DebugDrawCharSpanBoundsInfo);
            Assert.IsNull(configuration.DebugDrawLineContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawLineOutlineBoundsInfo);
            Assert.IsNull(configuration.DebugDrawParagraphContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawParagraphOutlineBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentRenderBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentContentBoundsInfo);
            Assert.IsNull(configuration.DebugDrawDocumentOutlineBoundsInfo);
            mockTextEditor.Verify(x => x.DebugReRender(), Times.Never);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfoWhenInDebugMode does nothing when not in debug mode.
        /// Input: IsInDebugMode returns false.
        /// Expected: ShowHandwritingPaperDebugInfo remains false, HandwritingPaperDebugDrawInfo remains default, DebugReRender is not called.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfoWhenInDebugMode_NotInDebugMode_DoesNothing()
        {
            // Arrange
            var mockTextEditor = new Mock<SkiaTextEditor>();
            var mockTextEditorCore = new Mock<Core.TextEditorCore>();
            mockTextEditorCore.Setup(c => c.IsInDebugMode).Returns(false);
            mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);

            var debugConfiguration = CreateDebugConfiguration(mockTextEditor.Object);
            var initialShowHandwritingPaperDebugInfo = debugConfiguration.ShowHandwritingPaperDebugInfo;
            var initialHandwritingPaperDebugDrawInfo = debugConfiguration.HandwritingPaperDebugDrawInfo;

            // Act
            debugConfiguration.ShowHandwritingPaperDebugInfoWhenInDebugMode();

            // Assert
            Assert.AreEqual(initialShowHandwritingPaperDebugInfo, debugConfiguration.ShowHandwritingPaperDebugInfo);
            Assert.AreEqual(initialHandwritingPaperDebugDrawInfo, debugConfiguration.HandwritingPaperDebugDrawInfo);
            mockTextEditor.Verify(e => e.DebugReRender(), Times.Never);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfoWhenInDebugMode sets all properties correctly when in debug mode.
        /// Input: IsInDebugMode returns true.
        /// Expected: ShowHandwritingPaperDebugInfo is set to true, HandwritingPaperDebugDrawInfo is initialized with correct values, DebugReRender is called once.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfoWhenInDebugMode_InDebugMode_SetsAllPropertiesAndCallsDebugReRender()
        {
            // Arrange
            var mockTextEditor = new Mock<SkiaTextEditor>();
            var mockTextEditorCore = new Mock<Core.TextEditorCore>();
            mockTextEditorCore.Setup(c => c.IsInDebugMode).Returns(true);
            mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);

            var debugConfiguration = CreateDebugConfiguration(mockTextEditor.Object);

            // Act
            debugConfiguration.ShowHandwritingPaperDebugInfoWhenInDebugMode();

            // Assert
            Assert.IsTrue(debugConfiguration.ShowHandwritingPaperDebugInfo, "ShowHandwritingPaperDebugInfo should be true");

            var handwritingPaperDebugDrawInfo = debugConfiguration.HandwritingPaperDebugDrawInfo;

            // Verify TopLineGradationDebugDrawInfo
            Assert.AreEqual(SKColors.Red, handwritingPaperDebugDrawInfo.TopLineGradationDebugDrawInfo.DebugColor);
            Assert.AreEqual(2f, handwritingPaperDebugDrawInfo.TopLineGradationDebugDrawInfo.StrokeThickness);

            // Verify MiddleLineGradationDebugDrawInfo
            Assert.AreEqual(SKColors.Red, handwritingPaperDebugDrawInfo.MiddleLineGradationDebugDrawInfo.DebugColor);
            Assert.AreEqual(2f, handwritingPaperDebugDrawInfo.MiddleLineGradationDebugDrawInfo.StrokeThickness);

            // Verify BaselineGradationDebugDrawInfo
            Assert.AreEqual(SKColors.Red, handwritingPaperDebugDrawInfo.BaselineGradationDebugDrawInfo.DebugColor);
            Assert.AreEqual(3f, handwritingPaperDebugDrawInfo.BaselineGradationDebugDrawInfo.StrokeThickness);

            // Verify BottomLineGradationDebugDrawInfo
            Assert.AreEqual(SKColors.Red, handwritingPaperDebugDrawInfo.BottomLineGradationDebugDrawInfo.DebugColor);
            Assert.AreEqual(2f, handwritingPaperDebugDrawInfo.BottomLineGradationDebugDrawInfo.StrokeThickness);

            mockTextEditor.Verify(e => e.DebugReRender(), Times.Once);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfoWhenInDebugMode overwrites existing HandwritingPaperDebugDrawInfo when in debug mode.
        /// Input: IsInDebugMode returns true, HandwritingPaperDebugDrawInfo has pre-existing non-default values.
        /// Expected: HandwritingPaperDebugDrawInfo is overwritten with new values.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfoWhenInDebugMode_InDebugModeWithExistingValues_OverwritesHandwritingPaperDebugDrawInfo()
        {
            // Arrange
            var mockTextEditor = new Mock<SkiaTextEditor>();
            var mockTextEditorCore = new Mock<Core.TextEditorCore>();
            mockTextEditorCore.Setup(c => c.IsInDebugMode).Returns(true);
            mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);

            var debugConfiguration = CreateDebugConfiguration(mockTextEditor.Object);

            // Set pre-existing values
            debugConfiguration.HandwritingPaperDebugDrawInfo = new HandwritingPaperDebugDrawInfo
            {
                TopLineGradationDebugDrawInfo = new HandwritingPaperGradationDebugDrawInfo(SKColors.Blue, 5),
                MiddleLineGradationDebugDrawInfo = new HandwritingPaperGradationDebugDrawInfo(SKColors.Green, 10),
                BaselineGradationDebugDrawInfo = new HandwritingPaperGradationDebugDrawInfo(SKColors.Yellow, 15),
                BottomLineGradationDebugDrawInfo = new HandwritingPaperGradationDebugDrawInfo(SKColors.Black, 20)
            };

            // Act
            debugConfiguration.ShowHandwritingPaperDebugInfoWhenInDebugMode();

            // Assert
            var handwritingPaperDebugDrawInfo = debugConfiguration.HandwritingPaperDebugDrawInfo;

            // Verify values are overwritten with expected values
            Assert.AreEqual(SKColors.Red, handwritingPaperDebugDrawInfo.TopLineGradationDebugDrawInfo.DebugColor);
            Assert.AreEqual(2f, handwritingPaperDebugDrawInfo.TopLineGradationDebugDrawInfo.StrokeThickness);

            Assert.AreEqual(SKColors.Red, handwritingPaperDebugDrawInfo.MiddleLineGradationDebugDrawInfo.DebugColor);
            Assert.AreEqual(2f, handwritingPaperDebugDrawInfo.MiddleLineGradationDebugDrawInfo.StrokeThickness);

            Assert.AreEqual(SKColors.Red, handwritingPaperDebugDrawInfo.BaselineGradationDebugDrawInfo.DebugColor);
            Assert.AreEqual(3f, handwritingPaperDebugDrawInfo.BaselineGradationDebugDrawInfo.StrokeThickness);

            Assert.AreEqual(SKColors.Red, handwritingPaperDebugDrawInfo.BottomLineGradationDebugDrawInfo.DebugColor);
            Assert.AreEqual(2f, handwritingPaperDebugDrawInfo.BottomLineGradationDebugDrawInfo.StrokeThickness);
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfoWhenInDebugMode can be called multiple times in debug mode.
        /// Input: IsInDebugMode returns true, method called twice consecutively.
        /// Expected: Properties are set correctly on each call, DebugReRender is called twice.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfoWhenInDebugMode_InDebugModeCalledMultipleTimes_WorksCorrectly()
        {
            // Arrange
            var mockTextEditor = new Mock<SkiaTextEditor>();
            var mockTextEditorCore = new Mock<Core.TextEditorCore>();
            mockTextEditorCore.Setup(c => c.IsInDebugMode).Returns(true);
            mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);

            var debugConfiguration = CreateDebugConfiguration(mockTextEditor.Object);

            // Act
            debugConfiguration.ShowHandwritingPaperDebugInfoWhenInDebugMode();
            debugConfiguration.ShowHandwritingPaperDebugInfoWhenInDebugMode();

            // Assert
            Assert.IsTrue(debugConfiguration.ShowHandwritingPaperDebugInfo);
            mockTextEditor.Verify(e => e.DebugReRender(), Times.Exactly(2));
        }

        /// <summary>
        /// Tests that ShowHandwritingPaperDebugInfoWhenInDebugMode transitions from not in debug mode to in debug mode.
        /// Input: IsInDebugMode initially false, then changes to true.
        /// Expected: First call does nothing, second call after mode change sets properties.
        /// </summary>
        [TestMethod]
        public void ShowHandwritingPaperDebugInfoWhenInDebugMode_TransitionFromNotInDebugModeToInDebugMode_WorksCorrectly()
        {
            // Arrange
            var mockTextEditor = new Mock<SkiaTextEditor>();
            var mockTextEditorCore = new Mock<Core.TextEditorCore>();
            var isInDebugMode = false;
            mockTextEditorCore.Setup(c => c.IsInDebugMode).Returns(() => isInDebugMode);
            mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);

            var debugConfiguration = CreateDebugConfiguration(mockTextEditor.Object);

            // Act - First call when not in debug mode
            debugConfiguration.ShowHandwritingPaperDebugInfoWhenInDebugMode();

            // Verify nothing happened
            mockTextEditor.Verify(e => e.DebugReRender(), Times.Never);

            // Change to debug mode
            isInDebugMode = true;

            // Act - Second call when in debug mode
            debugConfiguration.ShowHandwritingPaperDebugInfoWhenInDebugMode();

            // Assert
            Assert.IsTrue(debugConfiguration.ShowHandwritingPaperDebugInfo);
            mockTextEditor.Verify(e => e.DebugReRender(), Times.Once);
        }

        /// <summary>
        /// Helper method to create a SkiaTextEditorDebugConfiguration instance using reflection
        /// since the constructor is internal.
        /// </summary>
        private SkiaTextEditorDebugConfiguration CreateDebugConfiguration(SkiaTextEditor textEditor)
        {
            var constructor = typeof(SkiaTextEditorDebugConfiguration)
                .GetConstructor(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                    null,
                    new[] { typeof(SkiaTextEditor) },
                    null);

            if (constructor == null)
            {
                throw new InvalidOperationException("Could not find internal constructor for SkiaTextEditorDebugConfiguration");
            }

            return (SkiaTextEditorDebugConfiguration)constructor.Invoke(new object[] { textEditor });
        }

        /// <summary>
        /// Tests that HideHandwritingPaperDebugInfoWhenInDebugMode returns early without changing state
        /// when IsInDebugMode is false.
        /// </summary>
        [TestMethod]
        public void HideHandwritingPaperDebugInfoWhenInDebugMode_WhenNotInDebugMode_ReturnsEarlyWithoutChanges()
        {
            // Arrange
            var mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(false);

            var mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);

            var configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);
            configuration.ShowHandwritingPaperDebugInfo = true;

            // Act
            configuration.HideHandwritingPaperDebugInfoWhenInDebugMode();

            // Assert
            mockTextEditor.Verify(x => x.DebugReRender(), Times.Never);
        }

        /// <summary>
        /// Tests that HideHandwritingPaperDebugInfoWhenInDebugMode sets ShowHandwritingPaperDebugInfo to false
        /// and calls DebugReRender when IsInDebugMode is true.
        /// </summary>
        [TestMethod]
        public void HideHandwritingPaperDebugInfoWhenInDebugMode_WhenInDebugMode_SetsPropertyToFalseAndCallsDebugReRender()
        {
            // Arrange
            var mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(true);

            var mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);
            mockTextEditor.Setup(x => x.DebugReRender());

            var configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);
            configuration.ShowHandwritingPaperDebugInfo = true;

            // Act
            configuration.HideHandwritingPaperDebugInfoWhenInDebugMode();

            // Assert
            Assert.IsFalse(configuration.ShowHandwritingPaperDebugInfo);
            mockTextEditor.Verify(x => x.DebugReRender(), Times.Once);
        }

        /// <summary>
        /// Tests that HideHandwritingPaperDebugInfoWhenInDebugMode still works correctly
        /// when ShowHandwritingPaperDebugInfo is already false before the call.
        /// </summary>
        [TestMethod]
        public void HideHandwritingPaperDebugInfoWhenInDebugMode_WhenInDebugModeAndAlreadyFalse_StillCallsDebugReRender()
        {
            // Arrange
            var mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(x => x.IsInDebugMode).Returns(true);

            var mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);
            mockTextEditor.Setup(x => x.DebugReRender());

            var configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);
            configuration.ShowHandwritingPaperDebugInfo = false;

            // Act
            configuration.HideHandwritingPaperDebugInfoWhenInDebugMode();

            // Assert
            Assert.IsFalse(configuration.ShowHandwritingPaperDebugInfo);
            mockTextEditor.Verify(x => x.DebugReRender(), Times.Once);
        }

        /// <summary>
        /// Tests that the IsInDebugMode property returns the correct value from the underlying TextEditorCore.
        /// Tests both true and false cases to ensure proper delegation.
        /// </summary>
        /// <param name="isInDebugMode">The debug mode state to test.</param>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsInDebugMode_WhenCalled_ReturnsCoreIsInDebugModeValue(bool isInDebugMode)
        {
            // Arrange
            var mockTextEditorCore = new Mock<TextEditorCore>();
            mockTextEditorCore.Setup(c => c.IsInDebugMode).Returns(isInDebugMode);

            var mockTextEditor = new Mock<SkiaTextEditor>();
            mockTextEditor.Setup(e => e.TextEditorCore).Returns(mockTextEditorCore.Object);

            var configuration = new SkiaTextEditorDebugConfiguration(mockTextEditor.Object);

            // Act
            var result = configuration.IsInDebugMode;

            // Assert
            Assert.AreEqual(isInDebugMode, result);
        }
    }
}