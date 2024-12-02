using LightTextEditorPlus.Core.Primitive.Collections;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextReadOnlyListSpanTest
{
    [ContractTestCase]
    public void ToReadOnlyListSpan()
    {
        "给定列表包含 1234567 元素，取第2到第5个作为 ReadOnlyListSpan 列表，可以取出 3456 元素".Test(() =>
        {
            // Arrange
            List<int> list = new List<int>()
            {
                1, 2, 3, 4, 5, 6, 7
            };

            // Action
            // 取第2到第5个作为 ReadOnlyListSpan 列表
            var readOnlyListSpan = new TextReadOnlyListSpan<int>(list, 2, 4);

            // Assert
            // 可以取出 3456 元素
            Assert.AreEqual(4, readOnlyListSpan.Count);

            Assert.AreEqual(3, readOnlyListSpan[0]);
            Assert.AreEqual(4, readOnlyListSpan[1]);
            Assert.AreEqual(5, readOnlyListSpan[2]);
            Assert.AreEqual(6, readOnlyListSpan[3]);

            int index = 0;
            foreach (var value in readOnlyListSpan)
            {
                switch (index)
                {
                    case 0:
                        Assert.AreEqual(3, value);
                        break;
                    case 1:
                        Assert.AreEqual(4, value);
                        break;
                    case 2:
                        Assert.AreEqual(5, value);
                        break;
                    case 3:
                        Assert.AreEqual(6, value);
                        break;
                }

                index++;
            }

            Assert.AreEqual(4, index);
        });
    }
}