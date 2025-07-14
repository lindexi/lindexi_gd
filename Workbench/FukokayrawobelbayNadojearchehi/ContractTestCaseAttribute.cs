using System.Reflection;

namespace FukokayrawobelbayNadojearchehi;

public class ContractTestCaseAttribute : TestMethodAttribute, ITestDataSource
{
    public override TestResult[] Execute(ITestMethod testMethod)
    {
        if (testMethod.Arguments is [ContractTestCase contractTestCase])
        {
            try
            {
                var task = contractTestCase.TestCase();
                task.Wait();

                return
                [
                    new TestResult()
                    {
                        DisplayName = contractTestCase.Contract,
                        Outcome = UnitTestOutcome.Passed,
                    }
                ];
            }
            catch (Exception e)
            {
                return
                [
                    new TestResult()
                    {
                        DisplayName = contractTestCase.Contract,
                        Outcome = UnitTestOutcome.Failed,
                        TestFailureException = e,
                    }
                ];
            }
        }

        var result = testMethod.Invoke(testMethod.Arguments!);

        return [result];
    }

    public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
    {
        if (!ContractTest.Method.TestMethodDictionary.TryGetValue(methodInfo,out var testCaseCollection))
        {
            var type = methodInfo.DeclaringType!;
            var testInstance = Activator.CreateInstance(type);

            testCaseCollection = new TestCaseCollection();
            ContractTest.TestCaseCollection.Value = testCaseCollection;
            methodInfo.Invoke(testInstance, null);
            ContractTest.TestCaseCollection.Value = null;

            testCaseCollection = new TestCaseCollection(testCaseCollection);

            ContractTest.Method.TestMethodDictionary[methodInfo] = new TestCaseCollection(testCaseCollection);
        }

        foreach (ContractTestCase contractTestCase in testCaseCollection)
        {
            yield return [contractTestCase];
        }
    }

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
    {
        if (data is [ContractTestCase contractTestCase])
        {
            return contractTestCase.Contract;
        }

        return methodInfo.Name;
    }
}