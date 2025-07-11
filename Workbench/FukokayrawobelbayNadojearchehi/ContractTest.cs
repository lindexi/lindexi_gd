using System.Reflection;

namespace FukokayrawobelbayNadojearchehi;

public static partial class ContractTest
{
    public static void Test(this string contract, Action testCase)
    {
        if (TestCaseCollection.Value is {} collection)
        {
            collection.Add(new ContractTestCase(contract, testCase));
        }
    }

    internal static AsyncLocal<TestCaseCollection?> TestCaseCollection { get; } = new AsyncLocal<TestCaseCollection?>();

    internal static TestCaseIndexer Method { get; } = new TestCaseIndexer();
}

internal class TestCaseIndexer
{
    public Dictionary<MethodInfo /*TestMethod*/, TestCaseCollection> TestMethodDictionary { get; } = [];
}