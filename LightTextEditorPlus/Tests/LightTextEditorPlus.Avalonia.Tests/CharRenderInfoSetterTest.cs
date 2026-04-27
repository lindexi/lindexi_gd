using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Platform;
using LightTextEditorPlus.Platform.Utils;

namespace LightTextEditorPlus.Avalonia.Tests;

[TestClass]
public class CharRenderInfoSetterTest
{
    [TestMethod("测试传入 emoji 表情和空格内容，预期可以正常布局")]
    public void SetCharDataInfoWithEmoji()
    {
        CharData emojiCharData = CreateRuneCharData(0x1F9EA);
        CharData spaceCharData = CreateCharData(' ');

        var setter = Apply
        (
            [emojiCharData, spaceCharData],
            [
                CreateCharRenderInfo(glyphCluster: 0, glyphIndex: 1),
                CreateCharRenderInfo(glyphCluster: 2, glyphIndex: 2)
            ]
        );

        AssertCharDataInfo(setter, emojiCharData, glyphIndex: 1, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, spaceCharData, glyphIndex: 2, CharDataInfoStatus.Normal);
    }

    [TestMethod("测试从右到左字符的 Cluster 倒序内容，预期可以正确设置到对应 CharData")]
    public void SetCharDataInfoWithRightToLeftGlyphClusters()
    {
        CharData charData1 = CreateCharData('A');
        CharData charData2 = CreateCharData('B');
        CharData charData3 = CreateCharData('C');

        var setter = Apply
        (
            [charData1, charData2, charData3],
            [
                CreateCharRenderInfo(glyphCluster: 2, glyphIndex: 3),
                CreateCharRenderInfo(glyphCluster: 1, glyphIndex: 2),
                CreateCharRenderInfo(glyphCluster: 0, glyphIndex: 1)
            ]
        );

        AssertCharDataInfo(setter, charData1, 1, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, charData2, 2, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, charData3, 3, CharDataInfoStatus.Normal);
    }

    [TestMethod("测试 emoji 后接多个普通字符，预期可以按 UTF-16 Cluster 正确回填")]
    public void SetCharDataInfoWithEmojiAndMultipleCharacters()
    {
        CharData emojiCharData = CreateRuneCharData(0x1F9EA);
        CharData charDataA = CreateCharData('A');
        CharData charDataB = CreateCharData('B');

        var setter = Apply
        (
            [emojiCharData, charDataA, charDataB],
            [
                CreateCharRenderInfo(glyphCluster: 0, glyphIndex: 1),
                CreateCharRenderInfo(glyphCluster: 2, glyphIndex: 2),
                CreateCharRenderInfo(glyphCluster: 3, glyphIndex: 3)
            ]
        );

        AssertCharDataInfo(setter, emojiCharData, glyphIndex: 1, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, charDataA, glyphIndex: 2, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, charDataB, glyphIndex: 3, CharDataInfoStatus.Normal);
    }

    [TestMethod("测试普通字符加 emoji 再加普通字符，预期可以正确回填中间 emoji 的 Cluster")]
    public void SetCharDataInfoWithCharacterEmojiCharacter()
    {
        CharData charDataA = CreateCharData('A');
        CharData emojiCharData = CreateRuneCharData(0x1F9EA);
        CharData charDataB = CreateCharData('B');

        var setter = Apply
        (
            [charDataA, emojiCharData, charDataB],
            [
                CreateCharRenderInfo(glyphCluster: 0, glyphIndex: 1),
                CreateCharRenderInfo(glyphCluster: 1, glyphIndex: 2),
                CreateCharRenderInfo(glyphCluster: 3, glyphIndex: 3)
            ]
        );

        AssertCharDataInfo(setter, charDataA, glyphIndex: 1, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, emojiCharData, glyphIndex: 2, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, charDataB, glyphIndex: 3, CharDataInfoStatus.Normal);
    }

    [TestMethod("测试 emoji 加连字加普通字符，预期连字状态可以和 emoji 共存")]
    public void SetCharDataInfoWithEmojiLigatureAndCharacter()
    {
        CharData emojiCharData = CreateRuneCharData(0x1F9EA);
        CharData charDataF = CreateCharData('f');
        CharData charDataI = CreateCharData('i');
        CharData charDataX = CreateCharData('x');

        var setter = Apply
        (
            [emojiCharData, charDataF, charDataI, charDataX],
            [
                CreateCharRenderInfo(glyphCluster: 0, glyphIndex: 1),
                CreateCharRenderInfo(glyphCluster: 2, glyphIndex: 2),
                CreateCharRenderInfo(glyphCluster: 4, glyphIndex: 3)
            ]
        );

        AssertCharDataInfo(setter, emojiCharData, 1, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, charDataF, 2, CharDataInfoStatus.LigatureStart);
        AssertCharDataInfo(setter, charDataI, CharDataInfo.UndefinedGlyphIndex, CharDataInfoStatus.LigatureContinue);
        AssertCharDataInfo(setter, charDataX, 3, CharDataInfoStatus.Normal);
    }

    [TestMethod("测试从右到左两个字符的 Cluster 倒序内容，预期可以正确设置到对应 CharData")]
    public void SetCharDataInfoWithRightToLeftTwoCharacters()
    {
        CharData charDataA = CreateCharData('A');
        CharData charDataB = CreateCharData('B');

        var setter = Apply
        (
            [charDataA, charDataB],
            [
                CreateCharRenderInfo(glyphCluster: 1, glyphIndex: 2),
                CreateCharRenderInfo(glyphCluster: 0, glyphIndex: 1)
            ]
        );

        AssertCharDataInfo(setter, charDataA, 1, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, charDataB, 2, CharDataInfoStatus.Normal);
    }

    [TestMethod("测试从右到左内容里包含 emoji，预期可以正确处理倒序 Cluster 和 UTF-16 长度")]
    public void SetCharDataInfoWithRightToLeftEmoji()
    {
        CharData emojiCharData = CreateRuneCharData(0x1F9EA);
        CharData charDataA = CreateCharData('A');
        CharData charDataB = CreateCharData('B');

        var setter = Apply
        (
            [emojiCharData, charDataA, charDataB],
            [
                CreateCharRenderInfo(glyphCluster: 3, glyphIndex: 3),
                CreateCharRenderInfo(glyphCluster: 2, glyphIndex: 2),
                CreateCharRenderInfo(glyphCluster: 0, glyphIndex: 1)
            ]
        );

        AssertCharDataInfo(setter, emojiCharData, 1, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, charDataA, 2, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, charDataB, 3, CharDataInfoStatus.Normal);
    }

    [TestMethod("测试重复 Cluster 的多个 glyph，预期去重逻辑不会误判为连字")]
    public void SetCharDataInfoWithDuplicatedClusters()
    {
        CharData charDataA = CreateCharData('A');
        CharData charDataB = CreateCharData('B');

        var setter = Apply
        (
            [charDataA, charDataB],
            [
                CreateCharRenderInfo(glyphCluster: 0, glyphIndex: 1),
                CreateCharRenderInfo(glyphCluster: 0, glyphIndex: 1),
                CreateCharRenderInfo(glyphCluster: 1, glyphIndex: 2)
            ]
        );

        AssertCharDataInfo(setter, charDataA, 1, CharDataInfoStatus.Normal);
        AssertCharDataInfo(setter, charDataB, 2, CharDataInfoStatus.Normal);
    }

    class FakeCharDataLayoutInfoSetter : ICharDataLayoutInfoSetter
    {
        public Dictionary<CharData, CharDataInfo> CharDataInfoMap { get; } = new Dictionary<CharData, CharDataInfo>();

        public void SetLayoutStartPoint(CharData charData, TextPointInLineCoordinateSystem point)
        {
            // Ignore
        }

        public void SetCharDataInfo(CharData charData, in CharDataInfo charDataInfo)
        {
            CharDataInfoMap[charData] = charDataInfo;
        }
    }

    private static FakeCharDataLayoutInfoSetter Apply(CharData[] charDataArray, CharRenderInfo[] charRenderInfoArray)
    {
        var setter = new FakeCharDataLayoutInfoSetter();
        var charRenderInfoSetter = new CharRenderInfoSetter(setter);

        charRenderInfoSetter.SetCharDataInfo(charRenderInfoArray, new TextReadOnlyListSpan<CharData>(charDataArray));

        return setter;
    }

    private static CharData CreateCharData(char c)
    {
        return new CharData(new SingleCharObject(c), new LayoutOnlyRunProperty());
    }

    private static CharData CreateRuneCharData(int unicode)
    {
        return new CharData(new RuneCharObject(new Rune(unicode)), new LayoutOnlyRunProperty());
    }

    private static CharRenderInfo CreateCharRenderInfo(uint glyphCluster, ushort glyphIndex)
    {
        var fakeBounds = new TextRect(0, 0, 10, 10);
        return new CharRenderInfo(fakeBounds)
        {
            CharDataInfo = new CharDataInfo(new TextSize(10, 10), new TextSize(10, 10), 10)
            {
                GlyphIndex = glyphIndex
            },
            GlyphCluster = glyphCluster,
        };
    }

    private static void AssertCharDataInfo(FakeCharDataLayoutInfoSetter setter, CharData charData, ushort glyphIndex,
        CharDataInfoStatus status)
    {
        Assert.IsTrue(setter.CharDataInfoMap.TryGetValue(charData, out var charDataInfo));
        Assert.AreEqual(glyphIndex, charDataInfo.GlyphIndex);
        Assert.AreEqual(status, charDataInfo.Status);
    }
}