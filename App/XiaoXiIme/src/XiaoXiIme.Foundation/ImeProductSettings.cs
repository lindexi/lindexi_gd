namespace XiaoXiIme.Foundation;

/// <summary>Supported product input schemes; changes take effect on the next Host startup.</summary>
public enum ImeProductInputScheme
{
    FullPinyin = 0,
    XiaoheDoublePinyin = 1,
    SogouDoublePinyin = 2,
    MicrosoftDoublePinyin = 3,
    AbcDoublePinyin = 4,
    JiajiaDoublePinyin = 5,
    NaturalCodeDoublePinyin = 6
}

/// <summary>Saved status bar origin in desktop physical pixels, not caret coordinates.</summary>
public sealed record ImeStatusBarPosition(int X, int Y);

/// <summary>User preferences owned and persisted exclusively by the Host.</summary>
public sealed record ImeProductSettings(
    int SchemaVersion = 1,
    long Revision = 0,
    ImeProductInputScheme InputScheme = ImeProductInputScheme.FullPinyin,
    bool StatusBarVisible = true,
    ImeStatusBarPosition? StatusBarPosition = null,
    int FuzzyRuleMask = 0);
