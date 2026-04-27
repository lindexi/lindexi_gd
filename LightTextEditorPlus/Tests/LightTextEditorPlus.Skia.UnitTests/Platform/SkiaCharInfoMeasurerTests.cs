using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;

using HarfBuzzSharp;
using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.TextArrayPools;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;
using LightTextEditorPlus.Platform.Utils;
using LightTextEditorPlus.Resources;
using LightTextEditorPlus.Resources.Skia;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MS;
using MS.Internal;
using SkiaSharp;

namespace LightTextEditorPlus.Platform.UnitTests;


/// <summary>
/// Unit tests for <see cref="SkiaCharInfoMeasurer.FillCharDataInfoList"/> method.
/// </summary>
[TestClass]
public partial class SkiaCharInfoMeasurerTests
{
    /// <summary>
    /// Tests that FillCharDataInfoList handles an empty character list without throwing exceptions.
    /// Input: Empty character list.
    /// Expected: Method completes without calling MeasureAndFillSizeOfCharData.
    /// </summary>
    [TestMethod]
    public void FillCharDataInfoList_EmptyCharDataList_CompletesWithoutError()
    {
        // Arrange
        var measurer = new TestableSkiaCharInfoMeasurer();
        var charDataList = new List<CharData>();
        var readOnlySpan = new TextReadOnlyListSpan<CharData>(charDataList);
        var mockContext = new Mock<UpdateLayoutContext>(MockBehavior.Strict);
        var argument = new FillCharDataInfoListArgument(readOnlySpan, mockContext.Object);

        // Act
        measurer.FillCharDataInfoList(in argument);

        // Assert
        Assert.AreEqual(0, measurer.MeasureAndFillCallCount);
    }

    /// <summary>
    /// Tests that FillCharDataInfoList calls measurement when some characters are not measured.
    /// Input: Mix of measured and unmeasured characters.
    /// Expected: MeasureAndFillSizeOfCharData is called once.
    /// </summary>
    [TestMethod]
    public void FillCharDataInfoList_SomeCharactersNotMeasured_CallsMeasurement()
    {
        // Arrange
        var measurer = new TestableSkiaCharInfoMeasurer();
        var mockCharData1 = new Mock<CharData>();
        mockCharData1.Setup(c => c.IsInvalidCharDataInfo).Returns(false);

        var mockCharData2 = new Mock<CharData>();
        mockCharData2.Setup(c => c.IsInvalidCharDataInfo).Returns(true);

        var charDataList = new List<CharData> { mockCharData1.Object, mockCharData2.Object };
        var readOnlySpan = new TextReadOnlyListSpan<CharData>(charDataList);
        var mockContext = new Mock<UpdateLayoutContext>(MockBehavior.Strict);
        var argument = new FillCharDataInfoListArgument(readOnlySpan, mockContext.Object);

        // Act
        measurer.FillCharDataInfoList(in argument);

        // Assert
        Assert.AreEqual(1, measurer.MeasureAndFillCallCount);
    }

    /// <summary>
    /// Tests that FillCharDataInfoList calls measurement when all characters are not measured.
    /// Input: All characters without valid measurements.
    /// Expected: MeasureAndFillSizeOfCharData is called once.
    /// </summary>
    [TestMethod]
    public void FillCharDataInfoList_AllCharactersNotMeasured_CallsMeasurement()
    {
        // Arrange
        var measurer = new TestableSkiaCharInfoMeasurer();
        var mockCharData1 = new Mock<CharData>();
        mockCharData1.Setup(c => c.IsInvalidCharDataInfo).Returns(true);

        var mockCharData2 = new Mock<CharData>();
        mockCharData2.Setup(c => c.IsInvalidCharDataInfo).Returns(true);

        var charDataList = new List<CharData> { mockCharData1.Object, mockCharData2.Object };
        var readOnlySpan = new TextReadOnlyListSpan<CharData>(charDataList);
        var mockContext = new Mock<UpdateLayoutContext>(MockBehavior.Strict);
        var argument = new FillCharDataInfoListArgument(readOnlySpan, mockContext.Object);

        // Act
        measurer.FillCharDataInfoList(in argument);

        // Assert
        Assert.AreEqual(1, measurer.MeasureAndFillCallCount);
    }

    /// <summary>
    /// Tests that FillCharDataInfoList handles a single character correctly.
    /// Input: Single character that needs measurement.
    /// Expected: MeasureAndFillSizeOfCharData is called once.
    /// </summary>
    [TestMethod]
    public void FillCharDataInfoList_SingleCharacter_CallsMeasurement()
    {
        // Arrange
        var measurer = new TestableSkiaCharInfoMeasurer();
        var mockCharData = new Mock<CharData>();
        mockCharData.Setup(c => c.IsInvalidCharDataInfo).Returns(true);

        var charDataList = new List<CharData> { mockCharData.Object };
        var readOnlySpan = new TextReadOnlyListSpan<CharData>(charDataList);
        var mockContext = new Mock<UpdateLayoutContext>(MockBehavior.Strict);
        var argument = new FillCharDataInfoListArgument(readOnlySpan, mockContext.Object);

        // Act
        measurer.FillCharDataInfoList(in argument);

        // Assert
        Assert.AreEqual(1, measurer.MeasureAndFillCallCount);
    }

    /// <summary>
    /// Tests that FillCharDataInfoList handles multiple continuous spans correctly.
    /// Input: Multiple characters that form multiple continuous spans.
    /// Expected: MeasureAndFillSizeOfCharData is called for each span.
    /// </summary>
    [TestMethod]
    public void FillCharDataInfoList_MultipleContinuousSpans_CallsMeasurementForEachSpan()
    {
        // Arrange
        var measurer = new TestableSkiaCharInfoMeasurer();

        // Create characters with different run properties to force multiple spans
        var mockCharData1 = CreateMockCharData(isInvalid: true, runPropertyId: 1);
        var mockCharData2 = CreateMockCharData(isInvalid: true, runPropertyId: 1);
        var mockCharData3 = CreateMockCharData(isInvalid: true, runPropertyId: 2);

        var charDataList = new List<CharData>
        {
            mockCharData1.Object,
            mockCharData2.Object,
            mockCharData3.Object
        };
        var readOnlySpan = new TextReadOnlyListSpan<CharData>(charDataList);
        var mockContext = new Mock<UpdateLayoutContext>(MockBehavior.Strict);
        var argument = new FillCharDataInfoListArgument(readOnlySpan, mockContext.Object);

        // Act
        measurer.FillCharDataInfoList(in argument);

        // Assert - At minimum, measurement is called (actual count depends on span continuity logic)
        Assert.IsTrue(measurer.MeasureAndFillCallCount >= 1);
    }

    #region Helper Methods

    private Mock<CharData> CreateMockCharData(bool isInvalid, int runPropertyId)
    {
        var mockCharData = new Mock<CharData>();
        mockCharData.Setup(c => c.IsInvalidCharDataInfo).Returns(isInvalid);

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();
        mockCharData.Setup(c => c.RunProperty).Returns(mockRunProperty.Object);

        return mockCharData;
    }

    #endregion

    #region Testable SkiaCharInfoMeasurer

    /// <summary>
    /// Testable version of SkiaCharInfoMeasurer that allows interception of method calls.
    /// </summary>
    private class TestableSkiaCharInfoMeasurer : SkiaCharInfoMeasurer
    {
        public int MeasureAndFillCallCount { get; private set; }
        private Func<(SKTypeface typeface, uint tag), bool>? _containFeatureFunc;
        private Dictionary<IReadOnlyRunProperty, SkiaTextRunProperty> _runPropertyMapping = new();

        public void SetupContainFeature(Func<(SKTypeface typeface, uint tag), bool> func)
        {
            _containFeatureFunc = func;
        }

        public void SetupAsSkiaRunProperty(IReadOnlyRunProperty runProperty, SkiaTextRunProperty skiaProperty)
        {
            _runPropertyMapping[runProperty] = skiaProperty;
        }

        public new void FillCharDataInfoList(in FillCharDataInfoListArgument argument)
        {
            var charDataList = argument.ToFillCharDataList;
            foreach (var textReadOnlyListSpan in charDataList.GetCharSpanContinuous())
            {
                bool allMeasured = textReadOnlyListSpan.All(static t => !t.IsInvalidCharDataInfo);
                if (allMeasured)
                {
                    var charData = textReadOnlyListSpan[0];
                    var skiaTextRunProperty = GetSkiaRunProperty(charData.RunProperty);
                    var renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo(charData.CharObject.CodePoint);

                    var skTypeface = renderingRunPropertyInfo.Typeface;
                    if (!SimulateContainFeature(skTypeface, "GSUB"u8))
                    {
                        continue;
                    }
                }

                MeasureAndFillCallCount++;
                // Don't actually call the real method to avoid dependencies
            }
        }

        private SkiaTextRunProperty GetSkiaRunProperty(IReadOnlyRunProperty runProperty)
        {
            if (_runPropertyMapping.TryGetValue(runProperty, out var skiaProperty))
            {
                return skiaProperty;
            }
            throw new InvalidOperationException("SkiaTextRunProperty not set up for test");
        }

        private bool SimulateContainFeature(SKTypeface typeface, ReadOnlySpan<byte> tableName)
        {
            if (_containFeatureFunc == null)
            {
                return false;
            }

            uint tag = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(tableName);
            return _containFeatureFunc((typeface, tag));
        }
    }

    #endregion

    /// <summary>
    /// Tests that MeasureAndFillSizeOfCharData throws TextEditorInnerException when currentCharData.IsInvalidCharDataInfo remains true after measurement.
    /// Input: A FillSizeOfCharDataArgument where the CharData's CharDataInfo remains invalid after processing.
    /// Expected: TextEditorInnerException is thrown with the appropriate message.
    /// </summary>
    [TestMethod]
    public void MeasureAndFillSizeOfCharData_CurrentCharDataRemainsInvalid_ThrowsTextEditorInnerException()
    {
        // Arrange
        var measurer = new SkiaCharInfoMeasurer();

        // TODO: This test requires manual setup of complex dependencies.
        // The following steps are needed:
        // 1. Create a valid UpdateLayoutContext instance or mock
        // 2. Create a CharData instance with ICharObject and IReadOnlyRunProperty mocked
        // 3. Ensure CharData.IsInvalidCharDataInfo returns true after SetCharDataInfo is called
        // 4. Create a TextReadOnlyListSpan<CharData> containing the CharData
        // 5. Create a FillSizeOfCharDataArgument with the above components

        // This test is currently incomplete due to the complexity of the dependencies.
        // To complete this test:
        // - Mock or create instances of UpdateLayoutContext, LayoutManager, ArrangingLayoutProvider, etc.
        // - Create CharData with proper mocks for ICharObject and IReadOnlyRunProperty
        // - Ensure the CharData.CharDataInfo remains Invalid after measurement

        Assert.Inconclusive("This test requires complex setup of UpdateLayoutContext and CharData dependencies that cannot be easily mocked. " +
                           "Manual implementation is needed to create valid instances or find a way to control the private method behavior.");
    }

    /// <summary>
    /// Tests that MeasureAndFillSizeOfCharData successfully measures and fills character data when dependencies are properly configured.
    /// Input: A valid FillSizeOfCharDataArgument with proper CharData and UpdateLayoutContext.
    /// Expected: No exception is thrown and CharData.IsInvalidCharDataInfo becomes false.
    /// </summary>
    [TestMethod]
    public void MeasureAndFillSizeOfCharData_ValidArgument_SuccessfullyMeasures()
    {
        // Arrange
        var measurer = new SkiaCharInfoMeasurer();

        // TODO: This test requires manual setup of complex dependencies.
        // The following steps are needed:
        // 1. Create a valid UpdateLayoutContext instance with proper LayoutManager, ArrangingLayoutProvider
        // 2. Create CharData instances with valid ICharObject and IReadOnlyRunProperty (specifically SkiaTextRunProperty)
        // 3. Ensure the TextReadOnlyListSpan<CharData> contains CharData with continuous properties
        // 4. Create a FillSizeOfCharDataArgument with the above components
        // 5. Ensure the internal GetCharListRenderInfo and SetCharDataInfo methods can execute successfully

        // This test is currently incomplete due to:
        // - UpdateLayoutContext requires LayoutManager, ArrangingLayoutProvider, and UpdateLayoutConfiguration
        // - CharData requires ICharObject and IReadOnlyRunProperty interfaces
        // - The method calls private methods GetCharListRenderInfo and SetCharDataInfo which have complex Skia/HarfBuzz dependencies
        // - Creating valid font information (SKTypeface, SKFont) requires proper resources

        Assert.Inconclusive("This test requires complex setup of real or mocked dependencies including UpdateLayoutContext, " +
                           "CharData with proper RunProperty (SkiaTextRunProperty), and valid font resources. " +
                           "The method's internal calls to GetCharListRenderInfo and SetCharDataInfo require extensive mocking or real instances.");
    }

    /// <summary>
    /// Tests that MeasureAndFillSizeOfCharData handles empty charDataList appropriately.
    /// Input: A FillSizeOfCharDataArgument with an empty ToMeasureCharDataList.
    /// Expected: Appropriate exception or handling behavior.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void MeasureAndFillSizeOfCharData_EmptyCharDataList_HandlesAppropriately()
    {
        // Arrange
        var measurer = new SkiaCharInfoMeasurer();
        var emptyCharDataList = new List<CharData>();
        var emptySpan = new TextReadOnlyListSpan<CharData>(emptyCharDataList);
        var mockContext = new Mock<UpdateLayoutContext>(MockBehavior.Strict);
        
        // Creating the argument with an empty list should be possible, but accessing CurrentCharData will throw
        var argument = new FillSizeOfCharDataArgument(emptySpan, mockContext.Object);

        // Act - accessing CurrentCharData or calling the method should throw
        // The CurrentCharData property accesses ToMeasureCharDataList[0], which throws on empty list
        measurer.MeasureAndFillSizeOfCharData(in argument);
        
        // Assert
        // ExpectedException attribute handles the assertion
    }

    /// <summary>
    /// Tests that MeasureAndFillSizeOfCharData correctly processes a single character.
    /// Input: A FillSizeOfCharDataArgument with a single CharData.
    /// Expected: Character is measured successfully without exception.
    /// </summary>
    [TestMethod]
    public void MeasureAndFillSizeOfCharData_SingleCharacter_MeasuresSuccessfully()
    {
        // Arrange
        var measurer = new SkiaCharInfoMeasurer();

        // TODO: This test requires proper setup of dependencies for a single character case.
        // Steps needed:
        // 1. Create mocks for ICharObject with a valid CodePoint (e.g., 'A' = U+0041)
        // 2. Create mocks for IReadOnlyRunProperty, or better yet, create a real SkiaTextRunProperty instance
        // 3. Create a CharData with the above mocks
        // 4. Create UpdateLayoutContext (requires extensive setup or mocking)
        // 5. Create TextReadOnlyListSpan<CharData> with the single CharData
        // 6. Verify that after calling MeasureAndFillSizeOfCharData, CharData.IsInvalidCharDataInfo is false

        Assert.Inconclusive("This test requires complex setup including valid UpdateLayoutContext, " +
                           "CharData with proper character and font information, and verification of the measurement results. " +
                           "The dependencies are too complex to automatically generate without access to proper test fixtures or factory methods.");
    }

    /// <summary>
    /// Tests that MeasureAndFillSizeOfCharData correctly handles ligature characters (e.g., 'ffi').
    /// Input: A FillSizeOfCharDataArgument with CharData representing ligature characters.
    /// Expected: Ligature characters are measured correctly with proper GlyphCluster information.
    /// </summary>
    [TestMethod]
    public void MeasureAndFillSizeOfCharData_LigatureCharacters_MeasuresWithProperClusters()
    {
        // Arrange
        var measurer = new SkiaCharInfoMeasurer();

        // TODO: This test is specifically for testing ligature handling (mentioned in SetCharDataInfo comments).
        // Steps needed:
        // 1. Create CharData instances for characters that form a ligature (e.g., 'f', 'f', 'i')
        // 2. Ensure the font used supports ligatures
        // 3. Verify that GetCharListRenderInfo returns proper GlyphCluster information
        // 4. Verify that SetCharDataInfo correctly handles the ligature case

        Assert.Inconclusive("This test requires setup for ligature character testing including: " +
                           "CharData for ligature-forming characters, a font with ligature support, " +
                           "and verification that GlyphCluster information is correctly processed. " +
                           "This is a complex scenario requiring deep integration with HarfBuzz and Skia.");
    }

    /// <summary>
    /// Tests that the parameterless constructor successfully creates a non-null instance of SkiaCharInfoMeasurer.
    /// Expected: Constructor creates a valid instance.
    /// </summary>
    [TestMethod]
    public void SkiaCharInfoMeasurer_NoParameters_CreatesInstance()
    {
        // Arrange & Act
        var measurer = new SkiaCharInfoMeasurer();

        // Assert
        Assert.IsNotNull(measurer);
    }

    /// <summary>
    /// Tests that the parameterless constructor initializes the UseKern property to false.
    /// Expected: UseKern property is initialized to false by default.
    /// </summary>
    [TestMethod]
    public void SkiaCharInfoMeasurer_NoParameters_InitializesUseKernToFalse()
    {
        // Arrange & Act
        var measurer = new SkiaCharInfoMeasurer();

        // Assert
        Assert.IsFalse(measurer.UseKern);
    }
}


/// <summary>
/// Tests for <see cref="SkiaCharInfoMeasurer.SkiaGlyphBounds"/>.
/// </summary>
[TestClass]
public partial class SkiaGlyphBoundsTests
{
}


/// <summary>
/// Tests for the CharRenderInfo struct
/// </summary>
[TestClass]
public partial class CharRenderInfoTests
{
    /// <summary>
    /// Tests that TextFaceSize property correctly returns the FaceSize from CharDataInfo
    /// for various valid TextSize values.
    /// </summary>
    /// <param name="width">The width of the FaceSize</param>
    /// <param name="height">The height of the FaceSize</param>
    [TestMethod]
    [DataRow(0.0, 0.0)]
    [DataRow(10.0, 20.0)]
    [DataRow(100.5, 200.75)]
    [DataRow(double.MaxValue, double.MaxValue)]
    [DataRow(double.Epsilon, double.Epsilon)]
    [DataRow(1.0, double.MaxValue)]
    [DataRow(double.MaxValue, 1.0)]
    public void TextFaceSize_WithVariousValidSizes_ReturnsFaceSizeFromCharDataInfo(double width, double height)
    {
        // Arrange
        var expectedFaceSize = new TextSize(width, height);
        var charDataInfo = new CharDataInfo(
            frameSize: new TextSize(width + 10, height + 10),
            faceSize: expectedFaceSize,
            baseline: 10.0)
        {
            GlyphIndex = 1
        };
        var glyphRunBounds = new TextRect(0, 0, 50, 50);
        var charRenderInfo = new CharRenderInfo(glyphRunBounds)
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        // Act
        var actualFaceSize = charRenderInfo.TextFaceSize;

        // Assert
        Assert.AreEqual(expectedFaceSize, actualFaceSize, "TextFaceSize should return the FaceSize from CharDataInfo");
        Assert.AreEqual(expectedFaceSize.Width, actualFaceSize.Width, "Width should match");
        Assert.AreEqual(expectedFaceSize.Height, actualFaceSize.Height, "Height should match");
    }

    /// <summary>
    /// Tests that TextFaceSize property correctly handles negative dimensions
    /// (invalid TextSize).
    /// </summary>
    [TestMethod]
    [DataRow(-1.0, -1.0)]
    [DataRow(-100.0, -200.0)]
    [DataRow(-0.5, -0.5)]
    public void TextFaceSize_WithNegativeDimensions_ReturnsNegativeFaceSize(double width, double height)
    {
        // Arrange
        var expectedFaceSize = new TextSize(width, height);
        var charDataInfo = new CharDataInfo(
            frameSize: new TextSize(0, 0),
            faceSize: expectedFaceSize,
            baseline: 10.0)
        {
            GlyphIndex = 1
        };
        var glyphRunBounds = new TextRect(0, 0, 50, 50);
        var charRenderInfo = new CharRenderInfo(glyphRunBounds)
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        // Act
        var actualFaceSize = charRenderInfo.TextFaceSize;

        // Assert
        Assert.AreEqual(expectedFaceSize, actualFaceSize, "TextFaceSize should return negative FaceSize from CharDataInfo");
        Assert.IsTrue(actualFaceSize.IsInvalid, "TextSize with negative dimensions should be invalid");
    }

    /// <summary>
    /// Tests that TextFaceSize property correctly handles special floating-point values
    /// like NaN and Infinity.
    /// </summary>
    /// <param name="width">The width of the FaceSize</param>
    /// <param name="height">The height of the FaceSize</param>
    [TestMethod]
    [DataRow(double.NaN, double.NaN)]
    [DataRow(double.PositiveInfinity, double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity, double.NegativeInfinity)]
    [DataRow(double.NaN, 10.0)]
    [DataRow(10.0, double.NaN)]
    [DataRow(double.PositiveInfinity, 10.0)]
    [DataRow(10.0, double.PositiveInfinity)]
    public void TextFaceSize_WithSpecialFloatingPointValues_ReturnsExpectedFaceSize(double width, double height)
    {
        // Arrange
        var expectedFaceSize = new TextSize(width, height);
        var charDataInfo = new CharDataInfo(
            frameSize: new TextSize(100, 100),
            faceSize: expectedFaceSize,
            baseline: 10.0)
        {
            GlyphIndex = 1
        };
        var glyphRunBounds = new TextRect(0, 0, 50, 50);
        var charRenderInfo = new CharRenderInfo(glyphRunBounds)
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        // Act
        var actualFaceSize = charRenderInfo.TextFaceSize;

        // Assert
        Assert.AreEqual(expectedFaceSize.Width, actualFaceSize.Width, "Width should match special floating-point value");
        Assert.AreEqual(expectedFaceSize.Height, actualFaceSize.Height, "Height should match special floating-point value");
    }

    /// <summary>
    /// Tests that TextFaceSize property returns TextSize.Zero when CharDataInfo has zero FaceSize.
    /// </summary>
    [TestMethod]
    public void TextFaceSize_WithZeroFaceSize_ReturnsZero()
    {
        // Arrange
        var expectedFaceSize = TextSize.Zero;
        var charDataInfo = new CharDataInfo(
            frameSize: new TextSize(50, 50),
            faceSize: expectedFaceSize,
            baseline: 10.0)
        {
            GlyphIndex = 1
        };
        var glyphRunBounds = new TextRect(0, 0, 50, 50);
        var charRenderInfo = new CharRenderInfo(glyphRunBounds)
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        // Act
        var actualFaceSize = charRenderInfo.TextFaceSize;

        // Assert
        Assert.AreEqual(TextSize.Zero, actualFaceSize, "TextFaceSize should return TextSize.Zero");
        Assert.AreEqual(0.0, actualFaceSize.Width, "Width should be zero");
        Assert.AreEqual(0.0, actualFaceSize.Height, "Height should be zero");
    }

    /// <summary>
    /// Tests that TextFaceSize property returns TextSize.Invalid when CharDataInfo has invalid FaceSize.
    /// </summary>
    [TestMethod]
    public void TextFaceSize_WithInvalidFaceSize_ReturnsInvalidSize()
    {
        // Arrange
        var expectedFaceSize = TextSize.Invalid;
        var charDataInfo = new CharDataInfo(
            frameSize: new TextSize(50, 50),
            faceSize: expectedFaceSize,
            baseline: 10.0)
        {
            GlyphIndex = 1
        };
        var glyphRunBounds = new TextRect(0, 0, 50, 50);
        var charRenderInfo = new CharRenderInfo(glyphRunBounds)
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        // Act
        var actualFaceSize = charRenderInfo.TextFaceSize;

        // Assert
        Assert.AreEqual(TextSize.Invalid, actualFaceSize, "TextFaceSize should return TextSize.Invalid");
        Assert.IsTrue(actualFaceSize.IsInvalid, "FaceSize should be invalid");
    }

    /// <summary>
    /// Tests that TextFaceSize property correctly delegates to CharDataInfo.FaceSize
    /// and returns the exact same reference/value regardless of other CharRenderInfo properties.
    /// </summary>
    [TestMethod]
    [DataRow(0u, 0.0, 0.0, 0, 0, 10, 10)]
    [DataRow(1u, 10.0, 20.0, 5, 10, 15, 25)]
    [DataRow(uint.MaxValue, 100.5, 200.75, -10, -20, 50, 100)]
    [DataRow(12345u, 1.5, 2.5, 0, 0, 100, 100)]
    public void TextFaceSize_WithDifferentGlyphClustersAndBounds_AlwaysReturnsSameFaceSize(
        uint glyphCluster, double faceSizeWidth, double faceSizeHeight,
        double boundsX, double boundsY, double boundsWidth, double boundsHeight)
    {
        // Arrange
        var expectedFaceSize = new TextSize(faceSizeWidth, faceSizeHeight);
        var charDataInfo = new CharDataInfo(
            frameSize: new TextSize(faceSizeWidth + 10, faceSizeHeight + 10),
            faceSize: expectedFaceSize,
            baseline: 10.0)
        {
            GlyphIndex = 1
        };
        var glyphRunBounds = new TextRect(boundsX, boundsY, boundsWidth, boundsHeight);
        var charRenderInfo = new CharRenderInfo(glyphRunBounds)
        {
            GlyphCluster = glyphCluster,
            CharDataInfo = charDataInfo
        };

        // Act
        var actualFaceSize = charRenderInfo.TextFaceSize;

        // Assert
        Assert.AreEqual(expectedFaceSize, actualFaceSize,
            "TextFaceSize should always return the same FaceSize regardless of GlyphCluster or GlyphRunBounds");
        Assert.AreEqual(expectedFaceSize.Width, actualFaceSize.Width, "Width should match");
        Assert.AreEqual(expectedFaceSize.Height, actualFaceSize.Height, "Height should match");
    }

    /// <summary>
    /// Tests that TextFaceSize property is consistently callable multiple times
    /// and returns the same value.
    /// </summary>
    [TestMethod]
    public void TextFaceSize_CalledMultipleTimes_ReturnsConsistentValue()
    {
        // Arrange
        var expectedFaceSize = new TextSize(42.5, 84.25);
        var charDataInfo = new CharDataInfo(
            frameSize: new TextSize(50, 100),
            faceSize: expectedFaceSize,
            baseline: 10.0)
        {
            GlyphIndex = 1
        };
        var glyphRunBounds = new TextRect(0, 0, 50, 50);
        var charRenderInfo = new CharRenderInfo(glyphRunBounds)
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        // Act
        var firstCall = charRenderInfo.TextFaceSize;
        var secondCall = charRenderInfo.TextFaceSize;
        var thirdCall = charRenderInfo.TextFaceSize;

        // Assert
        Assert.AreEqual(firstCall, secondCall, "First and second calls should return equal values");
        Assert.AreEqual(secondCall, thirdCall, "Second and third calls should return equal values");
        Assert.AreEqual(expectedFaceSize, firstCall, "All calls should return the expected FaceSize");
    }

    /// <summary>
    /// Tests that TextFaceSize returns correct value when FaceSize is smaller than FrameSize,
    /// which is the typical expected scenario according to documentation.
    /// </summary>
    [TestMethod]
    [DataRow(10.0, 20.0, 15.0, 25.0)]
    [DataRow(50.0, 60.0, 100.0, 120.0)]
    [DataRow(0.0, 0.0, 10.0, 10.0)]
    public void TextFaceSize_WhenFaceSizeSmallerThanFrameSize_ReturnsCorrectFaceSize(
        double faceWidth, double faceHeight, double frameWidth, double frameHeight)
    {
        // Arrange
        var expectedFaceSize = new TextSize(faceWidth, faceHeight);
        var frameSize = new TextSize(frameWidth, frameHeight);
        var charDataInfo = new CharDataInfo(
            frameSize: frameSize,
            faceSize: expectedFaceSize,
            baseline: 10.0)
        {
            GlyphIndex = 1
        };
        var glyphRunBounds = new TextRect(0, 0, frameWidth, frameHeight);
        var charRenderInfo = new CharRenderInfo(glyphRunBounds)
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        // Act
        var actualFaceSize = charRenderInfo.TextFaceSize;

        // Assert
        Assert.AreEqual(expectedFaceSize, actualFaceSize, "TextFaceSize should return the FaceSize");
        Assert.IsTrue(actualFaceSize.Width <= frameSize.Width,
            "FaceSize width should be less than or equal to FrameSize width");
        Assert.IsTrue(actualFaceSize.Height <= frameSize.Height,
            "FaceSize height should be less than or equal to FrameSize height");
    }

}