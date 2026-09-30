using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

public sealed record ImeHostStatus(
    bool IsRunning,
    string? LastError = null,
    string? DictionaryPackagePath = null,
    bool IsUsingFallbackDictionary = false,
    string? UserDictionaryPath = null,
    string? UserDictionaryError = null,
    string? IsolatedUserDictionaryPath = null,
    ImeAboutState? About = null,
    string? DictionaryLoadError = null,
    string? DictionaryInputScheme = null)
{
    public ImeAboutState EffectiveAbout => About ?? ImeAboutState.SeWzc;

    public static ImeHostStatus Stopped { get; } = new(false, About: ImeAboutState.SeWzc);

    public static ImeHostStatus Running { get; } = new(true, About: ImeAboutState.SeWzc);
}
