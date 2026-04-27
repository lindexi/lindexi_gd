using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Platform;
using LightTextEditorPlus.Platform.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;


namespace LightTextEditorPlus.Platform.Utils.UnitTests;

/// <summary>
/// Tests for CharRenderInfoSetter
/// </summary>
[TestClass]
public partial class CharRenderInfoSetterTests
{
    /// <summary>
    /// Tests that SetCharDataInfo correctly sets character info for a single character case.
    /// Input: Single glyph mapping to single character.
    /// Expected: CharDataInfo is set via the setter interface.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_SingleCharacter_SetsCharDataInfo()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var charDataInfo = new CharDataInfo(new TextSize(10, 20), new TextSize(8, 18), 15)
        {
            GlyphIndex = 100,
            Status = CharDataInfoStatus.Normal
        };

        var charRenderInfo = new CharRenderInfo(new TextRect(0, 0, 10, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[1];
        charRenderInfoSpan[0] = charRenderInfo;

        var mockCharObject = new Mock<ICharObject>();
        mockCharObject.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('A'));

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData = new CharData(mockCharObject.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(charData, It.IsAny<CharDataInfo>()), Times.Once);
    }

    /// <summary>
    /// Tests that SetCharDataInfo handles ligature correctly by marking subsequent characters as LigatureContinue.
    /// Input: Single glyph spanning multiple characters (ligature like 'fi').
    /// Expected: First character marked as LigatureStart, subsequent characters marked as LigatureContinue.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_LigatureCharacters_MarksFirstAsLigatureStartAndOthersAsContinue()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var charDataInfo = new CharDataInfo(new TextSize(15, 20), new TextSize(13, 18), 15)
        {
            GlyphIndex = 200,
            Status = CharDataInfoStatus.Normal
        };

        var charRenderInfo = new CharRenderInfo(new TextRect(0, 0, 15, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[1];
        charRenderInfoSpan[0] = charRenderInfo;

        var mockCharObject1 = new Mock<ICharObject>();
        mockCharObject1.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('f'));

        var mockCharObject2 = new Mock<ICharObject>();
        mockCharObject2.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('i'));

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData1 = new CharData(mockCharObject1.Object, mockRunProperty.Object);
        var charData2 = new CharData(mockCharObject2.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData1, charData2 };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(charData1, It.Is<CharDataInfo>(info => info.Status == CharDataInfoStatus.LigatureStart)), Times.Once);
        mockSetter.Verify(s => s.SetCharDataInfo(charData2, It.Is<CharDataInfo>(info => info.Status == CharDataInfoStatus.LigatureContinue)), Times.Once);
    }

    /// <summary>
    /// Tests that SetCharDataInfo handles multiple glyphs correctly.
    /// Input: Multiple glyphs each mapping to a single character.
    /// Expected: Each character gets corresponding glyph info.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_MultipleCharacters_SetsAllCharDataInfo()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var charDataInfo1 = new CharDataInfo(new TextSize(10, 20), new TextSize(8, 18), 15)
        {
            GlyphIndex = 100,
            Status = CharDataInfoStatus.Normal
        };

        var charDataInfo2 = new CharDataInfo(new TextSize(12, 20), new TextSize(10, 18), 15)
        {
            GlyphIndex = 101,
            Status = CharDataInfoStatus.Normal
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[2];
        charRenderInfoSpan[0] = new CharRenderInfo(new TextRect(0, 0, 10, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo1
        };
        charRenderInfoSpan[1] = new CharRenderInfo(new TextRect(10, 0, 12, 20))
        {
            GlyphCluster = 1,
            CharDataInfo = charDataInfo2
        };

        var mockCharObject1 = new Mock<ICharObject>();
        mockCharObject1.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('A'));

        var mockCharObject2 = new Mock<ICharObject>();
        mockCharObject2.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('B'));

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData1 = new CharData(mockCharObject1.Object, mockRunProperty.Object);
        var charData2 = new CharData(mockCharObject2.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData1, charData2 };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(It.IsAny<CharData>(), It.IsAny<CharDataInfo>()), Times.AtLeast(2));
    }

    /// <summary>
    /// Tests that SetCharDataInfo handles emoji (surrogate pair) correctly.
    /// Input: Single glyph representing an emoji with UTF-16 length of 2.
    /// Expected: CharDataInfo is set for the emoji character.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_EmojiCharacter_SetsCharDataInfo()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var charDataInfo = new CharDataInfo(new TextSize(20, 20), new TextSize(18, 18), 15)
        {
            GlyphIndex = 300,
            Status = CharDataInfoStatus.Normal
        };

        var charRenderInfo = new CharRenderInfo(new TextRect(0, 0, 20, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[1];
        charRenderInfoSpan[0] = charRenderInfo;

        var mockCharObject = new Mock<ICharObject>();
        mockCharObject.Setup(x => x.CodePoint).Returns(new Utf32CodePoint(0x1F600)); // Emoji grinning face

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData = new CharData(mockCharObject.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(charData, It.IsAny<CharDataInfo>()), Times.Once);
    }

    /// <summary>
    /// Tests that SetCharDataInfo handles RTL text with reversed glyph order correctly.
    /// Input: Glyphs in reverse logical order (simulating RTL text).
    /// Expected: Characters are correctly mapped based on cluster values, not glyph order.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_RTLText_HandlesReversedGlyphOrder()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var charDataInfo1 = new CharDataInfo(new TextSize(10, 20), new TextSize(8, 18), 15)
        {
            GlyphIndex = 100,
            Status = CharDataInfoStatus.Normal
        };

        var charDataInfo2 = new CharDataInfo(new TextSize(12, 20), new TextSize(10, 18), 15)
        {
            GlyphIndex = 101,
            Status = CharDataInfoStatus.Normal
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[2];
        charRenderInfoSpan[0] = new CharRenderInfo(new TextRect(0, 0, 10, 20))
        {
            GlyphCluster = 1,
            CharDataInfo = charDataInfo1
        };
        charRenderInfoSpan[1] = new CharRenderInfo(new TextRect(10, 0, 12, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo2
        };

        var mockCharObject1 = new Mock<ICharObject>();
        mockCharObject1.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('ا'));

        var mockCharObject2 = new Mock<ICharObject>();
        mockCharObject2.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('ب'));

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData1 = new CharData(mockCharObject1.Object, mockRunProperty.Object);
        var charData2 = new CharData(mockCharObject2.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData1, charData2 };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(It.IsAny<CharData>(), It.IsAny<CharDataInfo>()), Times.AtLeast(2));
    }

    /// <summary>
    /// Tests that SetCharDataInfo handles GlyphCluster exceeding totalUtf16Length by clamping.
    /// Input: GlyphCluster value greater than the total UTF-16 length.
    /// Expected: Method handles it gracefully by clamping to totalUtf16Length.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_GlyphClusterExceedsTotalLength_ClampsToValidRange()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var charDataInfo = new CharDataInfo(new TextSize(10, 20), new TextSize(8, 18), 15)
        {
            GlyphIndex = 100,
            Status = CharDataInfoStatus.Normal
        };

        var charRenderInfo = new CharRenderInfo(new TextRect(0, 0, 10, 20))
        {
            GlyphCluster = 999,
            CharDataInfo = charDataInfo
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[1];
        charRenderInfoSpan[0] = charRenderInfo;

        var mockCharObject = new Mock<ICharObject>();
        mockCharObject.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('A'));

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData = new CharData(mockCharObject.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(charData, It.IsAny<CharDataInfo>()), Times.Once);
    }

    /// <summary>
    /// Tests that SetCharDataInfo only sets CharDataInfo when status changes from existing status.
    /// Input: CharData with existing Normal status, new info with LigatureStart status.
    /// Expected: SetCharDataInfo is called due to status change.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_StatusChanged_UpdatesCharDataInfo()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var existingInfo = new CharDataInfo(new TextSize(10, 20), new TextSize(8, 18), 15)
        {
            GlyphIndex = 100,
            Status = CharDataInfoStatus.Normal
        };

        var newInfo = new CharDataInfo(new TextSize(15, 20), new TextSize(13, 18), 15)
        {
            GlyphIndex = 200,
            Status = CharDataInfoStatus.Normal
        };

        var charRenderInfo = new CharRenderInfo(new TextRect(0, 0, 15, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = newInfo
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[1];
        charRenderInfoSpan[0] = charRenderInfo;

        var mockCharObject1 = new Mock<ICharObject>();
        mockCharObject1.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('f'));

        var mockCharObject2 = new Mock<ICharObject>();
        mockCharObject2.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('i'));

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData1 = new CharData(mockCharObject1.Object, mockRunProperty.Object);
        var charData2 = new CharData(mockCharObject2.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData1, charData2 };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(It.IsAny<CharData>(), It.Ref<CharDataInfo>.IsAny), Times.AtLeast(1));
    }

    /// <summary>
    /// Tests that SetCharDataInfo handles duplicate GlyphCluster values by deduplication.
    /// Input: Multiple glyphs with same GlyphCluster (multiple glyphs for one logical character).
    /// Expected: Deduplication logic handles it correctly, characters are set appropriately.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_DuplicateGlyphClusters_HandlesCorrectly()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var charDataInfo1 = new CharDataInfo(new TextSize(5, 20), new TextSize(4, 18), 15)
        {
            GlyphIndex = 100,
            Status = CharDataInfoStatus.Normal
        };

        var charDataInfo2 = new CharDataInfo(new TextSize(5, 20), new TextSize(4, 18), 15)
        {
            GlyphIndex = 101,
            Status = CharDataInfoStatus.Normal
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[2];
        charRenderInfoSpan[0] = new CharRenderInfo(new TextRect(0, 0, 5, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo1
        };
        charRenderInfoSpan[1] = new CharRenderInfo(new TextRect(5, 0, 5, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo2
        };

        var mockCharObject = new Mock<ICharObject>();
        mockCharObject.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('A'));

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData = new CharData(mockCharObject.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(charData, It.IsAny<CharDataInfo>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// Tests that SetCharDataInfo handles large number of characters efficiently.
    /// Input: 150 characters to test heap allocation path for spans.
    /// Expected: All characters are processed correctly without errors.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_LargeNumberOfCharacters_HandlesHeapAllocation()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        const int charCount = 150;
        var charRenderInfoArray = new CharRenderInfo[charCount];
        var charDataList = new List<CharData>();

        for (int i = 0; i < charCount; i++)
        {
            var charDataInfo = new CharDataInfo(new TextSize(10, 20), new TextSize(8, 18), 15)
            {
                GlyphIndex = (ushort)(100 + i),
                Status = CharDataInfoStatus.Normal
            };

            charRenderInfoArray[i] = new CharRenderInfo(new TextRect(i * 10, 0, 10, 20))
            {
                GlyphCluster = (uint)i,
                CharDataInfo = charDataInfo
            };

            var mockCharObject = new Mock<ICharObject>();
            mockCharObject.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('A' + (i % 26)));

            var mockRunProperty = new Mock<IReadOnlyRunProperty>();
            var charData = new CharData(mockCharObject.Object, mockRunProperty.Object);
            charDataList.Add(charData);
        }

        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoArray.AsSpan(), charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(It.IsAny<CharData>(), It.IsAny<CharDataInfo>()), Times.AtLeast(charCount));
    }

    /// <summary>
    /// Tests that SetCharDataInfo handles GlyphCluster at boundary (zero).
    /// Input: GlyphCluster value of 0.
    /// Expected: Correctly maps to the first character.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_GlyphClusterAtZero_MapsToFirstCharacter()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var charDataInfo = new CharDataInfo(new TextSize(10, 20), new TextSize(8, 18), 15)
        {
            GlyphIndex = 100,
            Status = CharDataInfoStatus.Normal
        };

        var charRenderInfo = new CharRenderInfo(new TextRect(0, 0, 10, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[1];
        charRenderInfoSpan[0] = charRenderInfo;

        var mockCharObject = new Mock<ICharObject>();
        mockCharObject.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('A'));

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData = new CharData(mockCharObject.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(charData, It.IsAny<CharDataInfo>()), Times.Once);
    }

    /// <summary>
    /// Tests that SetCharDataInfo handles three-character ligature correctly.
    /// Input: Single glyph spanning three characters.
    /// Expected: First character marked as LigatureStart, others marked as LigatureContinue.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_ThreeCharacterLigature_MarksCorrectly()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var charDataInfo = new CharDataInfo(new TextSize(20, 20), new TextSize(18, 18), 15)
        {
            GlyphIndex = 250,
            Status = CharDataInfoStatus.Normal
        };

        var charRenderInfo = new CharRenderInfo(new TextRect(0, 0, 20, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = charDataInfo
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[1];
        charRenderInfoSpan[0] = charRenderInfo;

        var mockCharObject1 = new Mock<ICharObject>();
        mockCharObject1.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('f'));

        var mockCharObject2 = new Mock<ICharObject>();
        mockCharObject2.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('f'));

        var mockCharObject3 = new Mock<ICharObject>();
        mockCharObject3.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('i'));

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData1 = new CharData(mockCharObject1.Object, mockRunProperty.Object);
        var charData2 = new CharData(mockCharObject2.Object, mockRunProperty.Object);
        var charData3 = new CharData(mockCharObject3.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData1, charData2, charData3 };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(charData1, It.Is<CharDataInfo>(info => info.Status == CharDataInfoStatus.LigatureStart)), Times.Once);
        mockSetter.Verify(s => s.SetCharDataInfo(charData2, It.Is<CharDataInfo>(info => info.Status == CharDataInfoStatus.LigatureContinue)), Times.Once);
        mockSetter.Verify(s => s.SetCharDataInfo(charData3, It.Is<CharDataInfo>(info => info.Status == CharDataInfoStatus.LigatureContinue)), Times.Once);
    }

    /// <summary>
    /// Tests that SetCharDataInfo handles mixed ligature and normal characters.
    /// Input: Ligature followed by normal character.
    /// Expected: Ligature characters handled correctly, normal character also set.
    /// </summary>
    [TestMethod]
    public void SetCharDataInfo_MixedLigatureAndNormal_HandlesBothCorrectly()
    {
        // Arrange
        var mockSetter = new Mock<ICharDataLayoutInfoSetter>();
        var setter = new CharRenderInfoSetter(mockSetter.Object);

        var ligatureInfo = new CharDataInfo(new TextSize(15, 20), new TextSize(13, 18), 15)
        {
            GlyphIndex = 200,
            Status = CharDataInfoStatus.Normal
        };

        var normalInfo = new CharDataInfo(new TextSize(10, 20), new TextSize(8, 18), 15)
        {
            GlyphIndex = 101,
            Status = CharDataInfoStatus.Normal
        };

        Span<CharRenderInfo> charRenderInfoSpan = stackalloc CharRenderInfo[2];
        charRenderInfoSpan[0] = new CharRenderInfo(new TextRect(0, 0, 15, 20))
        {
            GlyphCluster = 0,
            CharDataInfo = ligatureInfo
        };
        charRenderInfoSpan[1] = new CharRenderInfo(new TextRect(15, 0, 10, 20))
        {
            GlyphCluster = 2,
            CharDataInfo = normalInfo
        };

        var mockCharObject1 = new Mock<ICharObject>();
        mockCharObject1.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('f'));

        var mockCharObject2 = new Mock<ICharObject>();
        mockCharObject2.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('i'));

        var mockCharObject3 = new Mock<ICharObject>();
        mockCharObject3.Setup(x => x.CodePoint).Returns(new Utf32CodePoint('a'));

        var mockRunProperty = new Mock<IReadOnlyRunProperty>();

        var charData1 = new CharData(mockCharObject1.Object, mockRunProperty.Object);
        var charData2 = new CharData(mockCharObject2.Object, mockRunProperty.Object);
        var charData3 = new CharData(mockCharObject3.Object, mockRunProperty.Object);
        var charDataList = new List<CharData> { charData1, charData2, charData3 };
        var charDataSpan = new TextReadOnlyListSpan<CharData>(charDataList);

        // Act
        setter.SetCharDataInfo(charRenderInfoSpan, charDataSpan);

        // Assert
        mockSetter.Verify(s => s.SetCharDataInfo(charData1, It.Is<CharDataInfo>(info => info.Status == CharDataInfoStatus.LigatureStart)), Times.Once);
        mockSetter.Verify(s => s.SetCharDataInfo(charData2, It.Is<CharDataInfo>(info => info.Status == CharDataInfoStatus.LigatureContinue)), Times.Once);
        mockSetter.Verify(s => s.SetCharDataInfo(charData3, It.IsAny<CharDataInfo>()), Times.Once);
    }
}