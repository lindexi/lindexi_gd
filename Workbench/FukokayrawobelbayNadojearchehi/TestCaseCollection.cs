namespace FukokayrawobelbayNadojearchehi;

internal class TestCaseCollection:List<ContractTestCase>
{
}

public record ContractTestCase(string Contract, Action TestCase);