using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            var customTestManager = new CustomTestManager();
            customTestManager.Run();
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



    public class TestManagerRunResult
    {
        public TestManagerRunResult(int allTestCount, List<TestExceptionResult> testExceptionResultList)
        {
            AllTestCount = allTestCount;
            TestExceptionResultList = testExceptionResultList;
        }

        public bool Success => FailTestCount == 0;

        public int AllTestCount { get; }

        public int FailTestCount => TestExceptionResultList.Count;

        public int SuccessTestCount => AllTestCount - FailTestCount;

        public List<TestExceptionResult> TestExceptionResultList { get; }
    }

    public class TestExceptionResult
    {
        public TestExceptionResult(string displayName, Exception exception)
        {
            DisplayName = displayName;
            Exception = exception;
        }

        public string DisplayName { get; }

        public Exception Exception { get; }
    }

    public class CustomTestManager
    {
        public TestManagerRunResult Run(Assembly assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            var exceptionList = new List<TestExceptionResult>();
            int count = 0;

            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<TestClassAttribute>() != null)
                {
                    foreach (var methodInfo in type.GetMethods())
                    {
                        var contractTestCaseAttribute = methodInfo.GetCustomAttribute<ContractTestCaseAttribute>();
                        if (contractTestCaseAttribute != null)
                        {
                            // 获取执行次数
                            foreach (var data in contractTestCaseAttribute.GetData(methodInfo))
                            {
                                count++;
                                var displayName = contractTestCaseAttribute.GetDisplayName(methodInfo, data);

                                try
                                {
                                    contractTestCaseAttribute.Execute(new FakeTestMethod(methodInfo));
                                }
#pragma warning disable CA1031 // 不捕获常规异常类型
                                catch (Exception e)
#pragma warning restore CA1031 // 不捕获常规异常类型
                                {
                                    exceptionList.Add(new TestExceptionResult(displayName, e));
                                }
                            }
                        }
                    }
                }
            }

            return new TestManagerRunResult(count, exceptionList);
        }

        class FakeTestMethod : ITestMethod
        {
            public FakeTestMethod(MethodInfo methodInfo, object obj = null)
            {
                MethodInfo = methodInfo;
                Obj = obj ?? Activator.CreateInstance(methodInfo.DeclaringType);
            }

            public TestResult Invoke(object[] arguments)
            {
                MethodInfo.Invoke(Obj, arguments);
                return new TestResult();
            }

            public Attribute[] GetAllAttributes(bool inherit)
            {
                return MethodInfo.GetCustomAttributes().ToArray();
            }

            public TAttributeType[] GetAttributes<TAttributeType>(bool inherit) where TAttributeType : Attribute
            {
                return MethodInfo.GetCustomAttributes().OfType<TAttributeType>().ToArray();
            }

            private object Obj { get; }
            public string TestMethodName => MethodInfo.Name;
            public string TestClassName => MethodInfo.DeclaringType?.Name ?? string.Empty;
            public Type ReturnType => MethodInfo.ReturnType;
            public object[] Arguments { set; get; }
            public ParameterInfo[] ParameterTypes => MethodInfo.GetParameters();
            public MethodInfo MethodInfo { set; get; }

            public FakeTestInfo Parent { get; } = new FakeTestInfo();
        }

        class FakeTestInfo
        {
            // 命名不能更改，框架内使用反射获取

            private MethodInfo testCleanupMethod;
            private MethodInfo testInitializeMethod;

            public Queue<MethodInfo> BaseTestInitializeMethodsQueue { set; get; }

            public Queue<MethodInfo> BaseTestCleanupMethodsQueue { set; get; }
        }
    }

   

    
}
