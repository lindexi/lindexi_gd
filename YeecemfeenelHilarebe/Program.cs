using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;
using MSTest.Extensions.Core;
using MSTest.Extensions.Utils;

namespace YeecemfeenelHilarebe
{
    class Program
    {
        static void Main(string[] args)
        {
            var methodInfo = typeof(FooTest).GetMethod("Foo");
            var fooTestMethod = new FooTestMethod(methodInfo)
            {
            };

            var testMethodProxy = new TestMethodProxy(fooTestMethod);
            var testResult = testMethodProxy.Invoke(new object[0]);
        }
    }

    [TestClass]
    public class FooTest
    {
        [ContractTestCase]
        public void Foo()
        {
            "123".Test(() =>
            {

            });
        }
    }

    class FooTestMethod : ITestMethod
    {
        public FooTestMethod(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
        }

        public TestResult Invoke(object[] arguments)
        {
            throw new NotImplementedException();
        }

        public Attribute[] GetAllAttributes(bool inherit)
        {
            return MethodInfo.GetCustomAttributes().ToArray();
        }

        public TAttributeType[] GetAttributes<TAttributeType>(bool inherit) where TAttributeType : Attribute
        {
            return MethodInfo.GetCustomAttributes().OfType<TAttributeType>().ToArray();
        }

        public string TestMethodName => MethodInfo.Name;
        public string TestClassName => MethodInfo.DeclaringType?.Name ?? string.Empty;
        public Type ReturnType => MethodInfo.ReturnType;
        public object[] Arguments { set; get; }
        public ParameterInfo[] ParameterTypes => MethodInfo.GetParameters();
        public MethodInfo MethodInfo { set; get; }

        public object Parent { get; } = new object();
    }
}
