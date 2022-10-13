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
}
