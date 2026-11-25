using DotNetCampus.Localizations;

namespace MediaFoundation.PlayerDemo;

[LocalizedConfiguration(Default = "zh-Hans", EnsureKeysIdentical = true, DependencyMode = DependencyMode.NestedSource, NotificationMode = NotificationMode.CurrentCulturePropertyChanged)]
internal static partial class Lang;
