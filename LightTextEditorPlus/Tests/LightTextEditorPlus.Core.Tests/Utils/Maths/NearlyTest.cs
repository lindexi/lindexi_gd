using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;
using LightTextEditorPlus.Core.Utils.Maths;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Utils.Maths;

[TestClass]
public class NearlyTest
{
    [ContractTestCase]
    public void TestEquals()
    {
        "传入两个相差1的数，返回不相同".Test(() =>
        {
            // Arrange
            var n1 = 1;
            var n2 = 2;

            // Action
            // Assert
            Assert.AreEqual(false, Nearly.Equals(n1, n2));
        });
    }
}
