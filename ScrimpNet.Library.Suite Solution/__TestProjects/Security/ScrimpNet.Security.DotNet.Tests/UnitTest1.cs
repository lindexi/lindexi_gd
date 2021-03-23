using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScrimpNet.Security.Core;

namespace ScrimpNet.Security.DotNet.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var list = SecurityUtils.GetSecurityQuestions(new string[] { "Good" }, 5, 10);

        }
    }
}
