using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;
using LightTextEditorPlus.Core.Primitive.Collections;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Layout;

[TestClass]
public class WordCharHelperTest
{
    [ContractTestCase]
    public void ReadWordCharCount()
    {
        "读取 文本？？库,123.12 字符串，可以正确分割".Test(() =>
        {
            var list = ToCharData(@"文本？？库,123.12");

            var splitList = WordCharHelper.TraversalSplit(list).ToList();

            Assert.AreEqual(7, splitList.Count);
            Assert.AreEqual("文", splitList[0]);
            Assert.AreEqual("？", splitList[2]);
            Assert.AreEqual(",", splitList[5]);
            Assert.AreEqual("123.12", splitList[6]);
        });

        "读取 文本ab库,123.12 字符串，可以正确分割".Test(() =>
        {
            var list = ToCharData(@"文本ab库,123.12");

            var splitList = WordCharHelper.TraversalSplit(list).ToList();

            Assert.AreEqual(6, splitList.Count);
            Assert.AreEqual("文", splitList[0]);
            Assert.AreEqual("ab", splitList[2]);
            Assert.AreEqual(",", splitList[4]);
            Assert.AreEqual("123.12", splitList[5]);
        });

        "读取 文本 库,123.12 字符串，可以正确分割".Test(() =>
        {
            var list = ToCharData(@"文本 库,123.12");

            var splitList = WordCharHelper.TraversalSplit(list).ToList();

            Assert.AreEqual(6, splitList.Count);
            Assert.AreEqual("文", splitList[0]);
            Assert.AreEqual(",", splitList[4]);
            Assert.AreEqual("123.12", splitList[5]);
        });

        "读取 文本库,123.12 字符串，可以正确分割".Test(() =>
        {
            var list = ToCharData(@"文本库,123.12");

            var splitList = WordCharHelper.TraversalSplit(list).ToList();

            Assert.AreEqual(5, splitList.Count);
            Assert.AreEqual("文", splitList[0]);
            Assert.AreEqual(",", splitList[3]);
            Assert.AreEqual("123.12", splitList[4]);
        });

        "读取 aa,123.12 字符串，可以正确分割".Test(() =>
        {
            var list = ToCharData(@"aa,123.12");

            var splitList = WordCharHelper.TraversalSplit(list).ToList();

            Assert.AreEqual(3, splitList.Count);
            Assert.AreEqual("aa", splitList[0]);
            Assert.AreEqual(",", splitList[1]);
            Assert.AreEqual("123.12", splitList[2]);
        });

        "读取 aa123.12 字符串，可以正确分割".Test(() =>
        {
            var list = ToCharData(@"aa123.12");

            var splitList = WordCharHelper.TraversalSplit(list).ToList();

            Assert.AreEqual(2, splitList.Count);
            Assert.AreEqual("aa", splitList[0]);
            Assert.AreEqual("123.12", splitList[1]);
        });

        "读取 123.12aa 字符串，可以正确分割".Test(() =>
        {
            var list = ToCharData(@"123.12aa");

            var splitList = WordCharHelper.TraversalSplit(list).ToList();

            Assert.AreEqual(2, splitList.Count);
            Assert.AreEqual("123.12", splitList[0]);
            Assert.AreEqual("aa", splitList[1]);
        });

        "读取 123.12 aa 字符串，可以分割为三个词".Test(() =>
        {
            var list = ToCharData(@"123.12 aa");

            var splitList = WordCharHelper.TraversalSplit(list).ToList();

            Assert.AreEqual(3, splitList.Count);
            Assert.AreEqual("123.12", splitList[0]);
            Assert.AreEqual(" ", splitList[1]);
            Assert.AreEqual("aa", splitList[2]);
        });
    }

    private TextReadOnlyListSpan<CharData> ToCharData(string text)
    {
        var list = new List<CharData>();
        for (var i = 0; i < text.Length; i++)
        {
            CharData charData = new CharData(new TextSpanCharObject(text, i), new LayoutOnlyRunProperty());
            list.Add(charData);
        }

        return new TextReadOnlyListSpan<CharData>(list, 0, list.Count);
    }
}
