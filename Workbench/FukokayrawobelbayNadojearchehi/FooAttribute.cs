using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Reflection;

namespace FukokayrawobelbayNadojearchehi;

public class FooAttribute : TestMethodAttribute, ITestDataSource
{
    public override TestResult[] Execute(ITestMethod testMethod)
    {
        testMethod.Invoke([]);

        var testResult = new TestResult()
        {
            DisplayName = "这是单元测试内容",
            Outcome = UnitTestOutcome.Passed,
        };
        return [testResult];
    }

    public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
    {
        return [[156]];
    }

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
    {
        return "Fxxx";
    }
}