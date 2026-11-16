namespace XiaoXiIme.Dictionary;

public sealed record DictionaryPackageCounts
{
    public int Candidates { get; init; }

    public int ExactKeys { get; init; }

    public int PrefixKeys { get; init; }

    public int ShapeEntries { get; init; }

    public int SymbolInputs { get; init; }
}