using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Reflection;

namespace FukokayrawobelbayNadojearchehi;

public class FooAttribute : TestMethodAttribute, ITestDataSource
{
    public override TestResult[] Execute(ITestMethod testMethod)
    {
        if (testMethod.Arguments is {Length:1} t && t[0] is ContractTestCase contractTestCase)
        {
            contractTestCase.TestCase();
            return new[]
            {
                new TestResult()
                {
                    DisplayName = contractTestCase.Contract,
                    Outcome = UnitTestOutcome.Passed,
                    //DatarowIndex = 0,
                }
            };
        }

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
                //DatarowIndex = i,
            };
        }

        return resultList;
    }

    public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
    {
        var type = methodInfo.DeclaringType;
        var testInstance = Activator.CreateInstance(type);

        var testCaseCollection = new TestCaseCollection();
        ContractTest.TestCaseCollection.Value = testCaseCollection;
        methodInfo.Invoke(testInstance, []);
        ContractTest.TestCaseCollection.Value = null;

        ContractTest.Method.TestMethodDictionary[methodInfo] = testCaseCollection;

        foreach (ContractTestCase contractTestCase in testCaseCollection)
        {
            yield return [contractTestCase];
        }
    }

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
    {
        if (ContractTest.Method.TestMethodDictionary.TryGetValue(methodInfo,out var collection) && data is
            {
                Length: 1
            } t && t[0] is ContractTestCase contractTestCase)
        {
            foreach (var testCase in collection)
            {
                if (ReferenceEquals(testCase, contractTestCase))
                {
                    return testCase.Contract;
                }
            }
        }

        return "Fxxx";
    }
}