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
        var setter = new FakeCharDataLayoutInfoSetter();

        // 构造两个 CharData 对象，一个包含 emoji 表情，一个包含空格
        Rune emojiRune = new Rune(0x1F9EA); // 🧪
        ICharObject emojiCharObject = new RuneCharObject(emojiRune);
        IReadOnlyRunProperty styleRunProperty = new LayoutOnlyRunProperty();

        CharData emojiCharData = new CharData(emojiCharObject, styleRunProperty);
        CharData spaceCharData = new CharData(new SingleCharObject(' '), styleRunProperty);

        var charDataList = new TextReadOnlyListSpan<CharData>([emojiCharData, spaceCharData]);

        // 模拟 CharRenderInfo 的内容
        var fakeBounds = new TextRect(0, 0, 10, 10);
        var fakeCharDataInfo1 = new CharDataInfo(new TextSize(10, 10), new TextSize(10, 10), 10)
        {
            GlyphIndex = 1
        };
        var fakeCharDataInfo2 = fakeCharDataInfo1 with
        {
            GlyphIndex = 2
        };

        var charRenderInfo1 = new CharRenderInfo(fakeBounds)
        {
            CharDataInfo = fakeCharDataInfo1,
            GlyphCluster = 0,
        };
        var charRenderInfo2 = new CharRenderInfo(fakeBounds)
        {
            CharDataInfo = fakeCharDataInfo2,
            GlyphCluster = 2, // 为什么是 2 呢？因为 emoji 占用两个字符，随后第三个字符是空格，所以空格的 GlyphCluster 是 2
        };
        var charRenderInfoSpan = new CharRenderInfo[] { charRenderInfo1, charRenderInfo2 };

        var charRenderInfoSetter = new CharRenderInfoSetter(setter);
        charRenderInfoSetter.SetCharDataInfo(charRenderInfoSpan, charDataList);

        // 验证 CharDataInfo 是否正确设置
        Assert.IsTrue(setter.CharDataInfoMap.TryGetValue(emojiCharData, out var emojiInfo));
        Assert.IsTrue(setter.CharDataInfoMap.TryGetValue(spaceCharData, out var spaceInfo));
        Assert.AreEqual((ushort) 1, emojiInfo.GlyphIndex);
        Assert.AreEqual(CharDataInfoStatus.Normal, emojiInfo.Status);
        Assert.AreEqual((ushort) 2, spaceInfo.GlyphIndex);
        Assert.AreEqual(CharDataInfoStatus.Normal, spaceInfo.Status);
    }

    [TestMethod("测试从右到左字符的 Cluster 倒序内容，预期可以正确设置到对应 CharData")]
    public void SetCharDataInfoWithRightToLeftGlyphClusters()
    {
        var setter = new FakeCharDataLayoutInfoSetter();

        IReadOnlyRunProperty styleRunProperty = new LayoutOnlyRunProperty();
        CharData charData1 = new CharData(new SingleCharObject('A'), styleRunProperty);
        CharData charData2 = new CharData(new SingleCharObject('B'), styleRunProperty);
        CharData charData3 = new CharData(new SingleCharObject('C'), styleRunProperty);

        var charDataList = new TextReadOnlyListSpan<CharData>([charData1, charData2, charData3]);

        var fakeBounds = new TextRect(0, 0, 10, 10);
        var fakeCharDataInfo1 = new CharDataInfo(new TextSize(10, 10), new TextSize(10, 10), 10)
        {
            GlyphIndex = 1
        };
        var fakeCharDataInfo2 = fakeCharDataInfo1 with
        {
            GlyphIndex = 2
        };
        var fakeCharDataInfo3 = fakeCharDataInfo1 with
        {
            GlyphIndex = 3
        };

        var charRenderInfoSpan = new CharRenderInfo[]
        {
            new(fakeBounds)
            {
                CharDataInfo = fakeCharDataInfo3,
                GlyphCluster = 2,
            },
            new(fakeBounds)
            {
                CharDataInfo = fakeCharDataInfo2,
                GlyphCluster = 1,
            },
            new(fakeBounds)
            {
                CharDataInfo = fakeCharDataInfo1,
                GlyphCluster = 0,
            }
        };

        var charRenderInfoSetter = new CharRenderInfoSetter(setter);
        charRenderInfoSetter.SetCharDataInfo(charRenderInfoSpan, charDataList);

        Assert.IsTrue(setter.CharDataInfoMap.TryGetValue(charData1, out var charDataInfo1));
        Assert.IsTrue(setter.CharDataInfoMap.TryGetValue(charData2, out var charDataInfo2));
        Assert.IsTrue(setter.CharDataInfoMap.TryGetValue(charData3, out var charDataInfo3));
        Assert.AreEqual((ushort) 1, charDataInfo1.GlyphIndex);
        Assert.AreEqual((ushort) 2, charDataInfo2.GlyphIndex);
        Assert.AreEqual((ushort) 3, charDataInfo3.GlyphIndex);
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
}