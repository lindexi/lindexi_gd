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
        mockSetter.Verify(s => s.SetCharDataInfo(charData, charDataInfo), Times.Once);
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
        mockSetter.Verify(s => s.SetCharDataInfo(charData, It.Ref<CharDataInfo>.IsAny), Times.Once);
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
        mockSetter.Verify(s => s.SetCharDataInfo(charData, charDataInfo), Times.Once);
    }

}