using DotNetCampus.Localizations;

namespace PrismCodingAgent.AvaloniaShell.Localizations;

/// <summary>提供应用界面文本及运行时语言通知。</summary>
[LocalizedConfiguration(
    Default = "zh-Hans",
    Current = "zh-Hans",
    EnsureKeysIdentical = true,
    GenerationMode = GenerationMode.Dictionary,
    DependencyMode = DependencyMode.Library,
    NotificationMode = NotificationMode.CurrentCulturePropertyChanged)]
public static partial class Lang
{
    internal static string Format(LocalizedString<object> text, object? argument) => text.ToString(argument!);

    internal static string Format(LocalizedString<object, object> text, object? first, object? second) => text.ToString(first!, second!);
}
