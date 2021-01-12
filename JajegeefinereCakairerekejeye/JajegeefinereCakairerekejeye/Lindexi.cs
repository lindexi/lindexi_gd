using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MSTest.Extensions.Contracts;

namespace JajegeefinereCakairerekejeye
{
    [TestClass]
    public class Lindexi
    {
        [ContractTestCase]
        public void Doubi()
        {
            "元素可以继承多个接口".Test(() =>
            {
                var mock = new Mock<IF1>();
                mock.As<IF2>();

                var f = mock.Object;

                Assert.IsInstanceOfType(f, typeof(IF1));
                Assert.IsInstanceOfType(f, typeof(IF2));
            });
        }
    }
}