using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BearcenikoriDajebeqehe;

[TestClass]
public class TestClass
{
    [TestMethod]
    public void Foo()
    {
        var a = 1;
        a++;
        Assert.AreEqual(2, a);
    }
}
