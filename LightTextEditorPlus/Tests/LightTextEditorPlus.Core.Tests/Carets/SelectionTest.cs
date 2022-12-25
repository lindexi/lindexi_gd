using MSTest.Extensions.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core.Tests.Carets;

[TestClass]
public class SelectionTest
{
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
