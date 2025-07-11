using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Reflection;

namespace FukokayrawobelbayNadojearchehi;

public class FooAttribute : TestMethodAttribute //, ITestDataSource
{
    public override TestResult[] Execute(ITestMethod testMethod)
    {
        var testCaseCollection = new TestCaseCollection();
        ContractTest.TestCaseCollection.Value = testCaseCollection;

        testMethod.Invoke([]);

        ContractTest.TestCaseCollection.Value = null;

        TestResult[] resultList = new TestResult[testCaseCollection.Count];

        for (var i = 0; i < testCaseCollection.Count; i++)
        {
            var testCase = testCaseCollection[i];

            testCase.TestCase();

            resultList[i] = new TestResult()
            {
                DisplayName = testCase.Contract,
                Outcome = UnitTestOutcome.Passed,
                DatarowIndex = i,
            };
        }

        return resultList;
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