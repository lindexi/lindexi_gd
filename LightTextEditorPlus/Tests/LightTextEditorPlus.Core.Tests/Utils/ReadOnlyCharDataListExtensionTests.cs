using System.Collections.Generic;
using System.Text;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Utils;

[TestClass()]
public class ReadOnlyCharDataListExtensionTests
{
    [ContractTestCase]
    public void SplitContinuousCharDataTest()
    {
        "获取有六项的 CharData 列表的相邻内容，列表元素是 1-1-1-2-2-2 组织，可以获取到两个列表".Test(() =>
        {
            var runProperty1 = new LayoutOnlyRunProperty();
            var runProperty2 = new LayoutOnlyRunProperty();

            var charData1 = new CharData(new SingleCharObject('1'), runProperty1);
            var charData2 = new CharData(new SingleCharObject('2'), runProperty1);
            var charData3 = new CharData(new SingleCharObject('3'), runProperty1);

            var charData4 = new CharData(new SingleCharObject('4'), runProperty2);
            var charData5 = new CharData(new SingleCharObject('5'), runProperty2);
            var charData6 = new CharData(new SingleCharObject('6'), runProperty2);

            var source = new CharData[]
            {
                charData1,
                charData2,
                charData3,
                charData4,
                charData5,
                charData6,
            };

            var list = new TextReadOnlyListSpan<CharData>(source, 0, source.Length);

            var splitList = list.SplitContinuousCharData((last, current) => ReferenceEquals(last.RunProperty, current.RunProperty)).ToList();

            Assert.AreEqual(2, splitList.Count);

            var list1 = splitList[0];
            var list2 = splitList[1];

            Assert.AreEqual("1",list1[0].CharObject.ToText());
            Assert.AreEqual("2", list1[1].CharObject.ToText());
            Assert.AreEqual("3", list1[2].CharObject.ToText());

            Assert.AreEqual("4", list2[0].CharObject.ToText());
            Assert.AreEqual("5", list2[1].CharObject.ToText());
            Assert.AreEqual("6", list2[2].CharObject.ToText());
        });

        "获取有五项的 CharData 列表的相邻内容，列表元素是 1-2-3-2-1 组织，可以获取到五个列表".Test(() =>
        {
            var charData1 = GetCharData();
            var charData2 = GetCharData();
            var charData3 = GetCharData();
            var source = new CharData[]
            {
                charData1,
                charData2,
                charData3,
                charData2,
                charData1,
            };

            var list = new TextReadOnlyListSpan<CharData>(source, 0, source.Length);
            var splitList = list.SplitContinuousCharData((last, current) => ReferenceEquals(last, current)).ToList();

            Assert.AreEqual(5, splitList.Count);
        });

        "获取有五项的 CharData 列表的相邻内容，列表元素是 1-1-2-2-3 组织，可以获取到三个列表".Test(() =>
        {
            var charData1 = GetCharData();
            var charData2 = GetCharData();
            var charData3 = GetCharData();
            var source = new CharData[]
            {
                charData1,
                charData1,
                charData2,
                charData2,
                charData3,
            };

            var list = new TextReadOnlyListSpan<CharData>(source, 0, source.Length);
            var splitList = list.SplitContinuousCharData((last, current) => ReferenceEquals(last, current)).ToList();

            Assert.AreEqual(3, splitList.Count);
            Assert.AreEqual(2, splitList[0].Count);
            Assert.AreEqual(2, splitList[1].Count);
            Assert.AreEqual(1, splitList[2].Count);
        });

        "获取有五项的 CharData 列表的相邻内容，列表元素是 1-1-2-3-3 组织，可以获取到三个列表".Test(() =>
        {
            var charData1 = GetCharData();
            var charData2 = GetCharData();
            var charData3 = GetCharData();
            var source = new CharData[]
            {
                charData1,
                charData1,
                charData2,
                charData3,
                charData3,
            };

            var list = new TextReadOnlyListSpan<CharData>(source, 0, source.Length);
            var splitList = list.SplitContinuousCharData((last, current) => ReferenceEquals(last, current)).ToList();

            Assert.AreEqual(3, splitList.Count);
            Assert.AreEqual(2, splitList[0].Count);
            Assert.AreEqual(1, splitList[1].Count);
            Assert.AreEqual(2, splitList[2].Count);
        });

        "获取有五项的 CharData 列表的相邻内容，列表元素是 1-1-2-2-2 组织，可以获取到两个列表".Test(() =>
        {
            var charData1 = GetCharData();
            var charData2 = GetCharData();
            var source = new CharData[]
            {
                charData1,
                charData1,
                charData2,
                charData2,
                charData2,
            };

            var list = new TextReadOnlyListSpan<CharData>(source, 0, source.Length);
            var splitList = list.SplitContinuousCharData((last, current) => ReferenceEquals(last, current)).ToList();

            Assert.AreEqual(2, splitList.Count);
            Assert.AreEqual(2, splitList[0].Count);
            Assert.AreEqual(3, splitList[1].Count);
        });

        "获取有三项的 CharData 列表的相邻内容，列表元素是 1-1-2 组织，可以获取到两个列表".Test(() =>
        {
            var charData1 = GetCharData();
            var charData2 = GetCharData();
            var source = new CharData[]
            {
                charData1,
                charData1,
                charData2,
            };

            var list = new TextReadOnlyListSpan<CharData>(source, 0, source.Length);
            var splitList = list.SplitContinuousCharData((last, current) => ReferenceEquals(last, current)).ToList();

            Assert.AreEqual(2, splitList.Count);
            Assert.AreEqual(2, splitList[0].Count);
            Assert.AreEqual(1, splitList[1].Count);
        });

        "获取有三项的 CharData 列表的相邻内容，列表元素是 1-2-2 组织，可以获取到两个列表".Test(() =>
        {
            var charData1 = GetCharData();
            var charData2 = GetCharData();
            var source = new CharData[]
            {
                charData1,
                charData2,
                charData2,
            };

            var list = new TextReadOnlyListSpan<CharData>(source, 0, source.Length);
            var splitList = list.SplitContinuousCharData((last, current) => ReferenceEquals(last, current)).ToList();

            Assert.AreEqual(2, splitList.Count);
            Assert.AreEqual(1, splitList[0].Count);
            Assert.AreEqual(2, splitList[1].Count);
        });

        "获取有两项不同的 CharData 列表的相邻内容，返回是两个列表，每个列表有一个元素".Test(() =>
        {
            var charData1 = GetCharData();
            var charData2 = GetCharData();
            var source = new CharData[]
            {
                charData1,
                charData2
            };

            var list = new TextReadOnlyListSpan<CharData>(source, 0, source.Length);
            var splitList = list.SplitContinuousCharData((last, current) => ReferenceEquals(last,current)).ToList();

            Assert.AreEqual(2, splitList.Count);
            Assert.AreEqual(1, splitList[0].Count);
            Assert.AreEqual(1, splitList[1].Count);
        });

        "获取有两项相同的 CharData 列表的相邻内容，返回是一个列表，列表有两个元素".Test(() =>
        {
            var charData = GetCharData();
            var source = new CharData[]
            {
                charData,
                charData
            };
            var list = new TextReadOnlyListSpan<CharData>(source, 0, 2);

            var splitList = list.SplitContinuousCharData((last, current) => true).ToList();

            Assert.AreEqual(1, splitList.Count);
            Assert.AreEqual(2, splitList[0].Count);
        });

        "获取一个只有一项的 CharData 列表的相邻内容，返回是一个列表，列表只有一个元素".Test(() =>
        {
            var source = new CharData[] { GetCharData() };
            var list = new TextReadOnlyListSpan<CharData>(source, 0, 1);
            var splitList = list.SplitContinuousCharData((last, current) => true).ToList();
            Assert.AreEqual(1, splitList.Count);
            Assert.AreEqual(1, splitList[0].Count);

            splitList = list.SplitContinuousCharData((last, current) => false).ToList();
            Assert.AreEqual(1, splitList.Count);
            Assert.AreEqual(1, splitList[0].Count);
        });

        "获取一个空 CharData 列表的相邻内容，返回是空列表".Test(() =>
        {
            var source = new CharData[0];
            var list = new TextReadOnlyListSpan<CharData>(source, 0, 0);
            var splitList = list.SplitContinuousCharData((last, current) => true).ToList();
            Assert.AreEqual(0, splitList.Count);
        });
    }

    private CharData GetCharData()
    {
        return new CharData(new SingleCharObject((char) ('a' + _count++)), new LayoutOnlyRunProperty());
    }

    private int _count;
}