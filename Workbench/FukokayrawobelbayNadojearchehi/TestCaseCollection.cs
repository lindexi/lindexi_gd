namespace FukokayrawobelbayNadojearchehi;

internal class TestCaseCollection:List<ContractTestCase>
{
    public TestCaseCollection()
    {
    }

    public TestCaseCollection(IEnumerable<ContractTestCase> collection) : base(collection)
    {
    }
}

public record ContractTestCase(string Contract, Func<Task> TestCase);