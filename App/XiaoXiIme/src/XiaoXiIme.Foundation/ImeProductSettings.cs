namespace XiaoXiIme.Foundation;

/// <summary>User preferences owned and persisted exclusively by the Host.</summary>
public sealed record ImeProductSettings(
    int SchemaVersion = 1,
    long Revision = 0,
    ImeProductInputScheme InputScheme = ImeProductInputScheme.FullPinyin,
    bool StatusBarVisible = true,
    ImeStatusBarPosition? StatusBarPosition = null,
    int FuzzyRuleMask = 0);
