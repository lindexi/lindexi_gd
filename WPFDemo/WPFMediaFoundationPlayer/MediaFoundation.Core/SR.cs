using DotNetCampus.Localizations;

namespace MediaFoundation.Core;

/// <summary>播放器核心的强类型本地化资源。</summary>
[LocalizedConfiguration(Default = "zh-Hans", Current = "zh-Hans", EnsureKeysIdentical = true, DependencyMode = DependencyMode.NestedSource, NotificationMode = NotificationMode.CurrentCulturePropertyChanged)]
public static partial class SR;
