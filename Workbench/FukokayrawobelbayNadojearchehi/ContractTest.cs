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
}