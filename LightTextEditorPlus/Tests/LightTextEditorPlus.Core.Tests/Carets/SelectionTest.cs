using MSTest.Extensions.Contracts;
using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core.Tests.Carets;

[TestClass]
public class SelectionTest
{
    [ContractTestCase]
    public void Contains()
    {
        "传入在反向选择范围内的光标坐标，可以返回包含在选择范围内".Test(() =>
        {
            // Arrange
            var selection = new Selection(new CaretOffset(20), new CaretOffset(10));

            // Action
            // Assert
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(true, selection.Contains(new CaretOffset(10 + i)));
            }
        });

        "传入不包含在选择范围内的光标坐标，可以返回没有包含在选择范围内".Test(() =>
        {
            // Arrange
            var start = 10;
            var length = 10;
            var selection = new Selection(new CaretOffset(start), length);

            // Action
            // Assert
            Assert.AreEqual(false, selection.Contains(new CaretOffset(9)));
            Assert.AreEqual(false, selection.Contains(new CaretOffset(21)));
        });

        "传入包含在选择范围内的光标坐标，可以返回包含在选择范围内".Test(() =>
        {
            // Arrange
            var start = 10;
            var length = 10;
            var selection = new Selection(new CaretOffset(start), length);

            // Action
            // Assert
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(true, selection.Contains(new CaretOffset(10 + i)));
            }
        });
    }

    [ContractTestCase]
    public void CreateSelection()
    {
        "创建长度为0的选择范围，选择的起点和结束点相同".Test(() =>
        {
            // Arrange
            var start = 10;
            var length = 0;

            // Action
            var selection = new Selection(new CaretOffset(start), length);

            // Assert
            Assert.AreEqual(selection.StartOffset, selection.EndOffset);
            Assert.AreEqual(selection.FrontOffset, selection.BehindOffset);
        });
    }

    [ContractTestCase]
    public void GetLength()
    {
        "给定长度和开始点，可以拿到和传入长度相同的选择长度".Test(() =>
        {
            // Arrange
            var startOffset = new CaretOffset();
            var length = 100;

            // Action
            var selection = new Selection(startOffset, length);

            // Assert
            Assert.AreEqual(length, selection.Length);
        });

        //"修改结束光标，可以更新长度".Test(() =>
        //{
        //    // Arrange
        //    var startOffset = new CaretOffset();
        //    var endOffset = new CaretOffset()
        //    {
        //        Offset = 100
        //    };

        //    var selection = new Selection(startOffset, endOffset);

        //    selection.EndOffset = new CaretOffset()
        //    {
        //        Offset = 9
        //    };

        //    // Action

        //    var length = selection.Length;

        //    // Assert
        //    Assert.AreEqual(9, length);
        //});

        "传入两个光标，可以拿到正确长度".Test(() =>
        {
            // Arrange
            var startOffset = new CaretOffset();
            var endOffset = new CaretOffset(100)
            {
                //Offset = 100
            };

            var selection = new Selection(startOffset, endOffset);

            // Action
            var length = selection.Length;

            // Assert
            Assert.AreEqual(100, length);
        });
    }
}
