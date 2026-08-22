namespace XiaoXiIme.Dictionary;

public sealed record DictionaryPackageManifest
{
    public const int CurrentFormatVersion = 1;

    public const string ExpectedPackageKind = "XiaoXiImeDictionary";

    public int FormatVersion { get; init; } = CurrentFormatVersion;

    public string PackageKind { get; init; } = ExpectedPackageKind;

    public string CreatedBy { get; init; } = string.Empty;

    public IReadOnlyList<DictionaryPackageSource> Sources { get; init; } = [];

    public DictionaryPackageParameters Parameters { get; init; } = new();

    public IReadOnlyList<DictionaryPackageShard> Shards { get; init; } = [];

    public DictionaryPackageCounts Counts { get; init; } = new();
}

public sealed record DictionaryPackageSource(string Path, long Length);

public sealed record DictionaryPackageParameters
{
    public string InputScheme { get; init; } = "fullPinyin";

    public bool EnablePrefixIndex { get; init; } = true;

    public int MaxPrefixCandidatesPerKey { get; init; } = 128;
}

public sealed record DictionaryPackageShard(string Role, string Path, long Length);

public sealed record DictionaryPackageCounts
{
    public int Candidates { get; init; }

    public int ExactKeys { get; init; }

    public int PrefixKeys { get; init; }
}
