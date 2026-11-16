namespace XiaoXiIme.Dictionary;

public sealed record DictionaryPackageParameters
{
    public string InputScheme { get; init; } = DictionaryPackageFormat.FullPinyinInputScheme;

    public string? ProjectionVersion { get; init; }

    public string? RulesSha256 { get; init; }

    public bool EnablePrefixIndex { get; init; } = true;

    public int MaxPrefixCandidatesPerKey { get; init; } = 128;
}