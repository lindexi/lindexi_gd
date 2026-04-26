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
    [Ignore]
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
        var fakeCharDataInfo = new CharDataInfo(new TextSize(10, 10), new TextSize(10, 10), 10)
        {
            GlyphIndex = 1
        };

        var charRenderInfo1 = new CharRenderInfo(fakeBounds)
        {
            CharDataInfo = fakeCharDataInfo,
            GlyphCluster = 0,
        };
        var charRenderInfo2 = new CharRenderInfo(fakeBounds)
        {
            CharDataInfo = fakeCharDataInfo,
            GlyphCluster = 2, // 为什么是 2 呢？因为 emoji 占用两个字符，随后第三个字符是空格，所以空格的 GlyphCluster 是 2
        };
        var charRenderInfoSpan = new CharRenderInfo[] { charRenderInfo1, charRenderInfo2 };

        var charRenderInfoSetter = new CharRenderInfoSetter(setter);
        charRenderInfoSetter.SetCharDataInfo(charRenderInfoSpan, charDataList);

        // 验证 CharDataInfo 是否正确设置
        Assert.IsTrue(setter.CharDataInfoMap.TryGetValue(emojiCharData, out var emojiInfo));
        _ = emojiInfo;
        Assert.IsTrue(setter.CharDataInfoMap.TryGetValue(spaceCharData, out var spaceInfo));
        _ = spaceInfo;
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