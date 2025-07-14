using Microsoft.VisualStudio.TestTools.UnitTesting;

using MSTest.Extensions.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FawkeqawjageJuqugeaha;

[TestClass]
public class Class1
{
    [ContractTestCase]
    public void TestMethod1()
    {
        "单元测试1".Test(() =>
        {
            var expect = 1;
            var actual = 1;
            Assert.AreEqual(expect, actual);
        });
    }
}