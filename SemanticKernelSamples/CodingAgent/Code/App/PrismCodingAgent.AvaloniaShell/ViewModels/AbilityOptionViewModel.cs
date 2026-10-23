using PrismCodingAgent.AvaloniaShell.Abilities;

namespace PrismCodingAgent.AvaloniaShell.ViewModels;

/// <summary>
/// 表示输入区可选择的发送前能力。
/// </summary>
public sealed class AbilityOptionViewModel : ViewModelBase
{
    private readonly bool _usesDefaultCompressionName;

    internal AbilityOptionViewModel(AbilityDefinition definition, bool usesDefaultCompressionName = false)
    {
        Definition = definition;
        _usesDefaultCompressionName = usesDefaultCompressionName;
    }

    internal AbilityDefinition Definition { get; }

    /// <summary>获取稳定能力标识。</summary>
    public string Id => Definition.Id;

    /// <summary>获取当前语言下的显示名称。</summary>
    public string DisplayName => Id == AbilityCatalog.ProgrammingId
        ? Localizations.Lang.Current.ProgrammingAbilityText
        : _usesDefaultCompressionName
            ? Localizations.Lang.Current.CompressWithInstructionsAbilityText
            : Definition.DisplayName;

    internal void RefreshDisplayName() => OnPropertyChanged(nameof(DisplayName));

    /// <summary>获取是否为内置压缩动作。</summary>
    public bool IsCompression => Id == AbilityCatalog.CompressionId;
}
