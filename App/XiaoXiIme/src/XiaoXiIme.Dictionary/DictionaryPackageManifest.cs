namespace XiaoXiIme.Dictionary;

public sealed record DictionaryPackageManifest
{
    public const int CurrentFormatVersion = 1;

    public const string ExpectedPackageKind = "XiaoXiImeDictionary";

    public const string CurrentCompilerVersion = "1";

    public int FormatVersion { get; init; } = CurrentFormatVersion;

    public string PackageKind { get; init; } = ExpectedPackageKind;

    public string CreatedBy { get; init; } = string.Empty;

    public string CompilerVersion { get; init; } = CurrentCompilerVersion;

    public IReadOnlyList<DictionaryPackageSource> Sources { get; init; } = [];

    public DictionaryPackageParameters Parameters { get; init; } = new();

    public IReadOnlyList<DictionaryPackageShard> Shards { get; init; } = [];

    public DictionaryPackageCounts Counts { get; init; } = new();
}