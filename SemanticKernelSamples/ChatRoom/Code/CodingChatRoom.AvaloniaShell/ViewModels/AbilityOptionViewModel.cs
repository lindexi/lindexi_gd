using CodingChatRoom.AvaloniaShell.Abilities;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 表示输入区可选择的发送前能力。
/// </summary>
public sealed class AbilityOptionViewModel
{
    internal AbilityOptionViewModel(AbilityDefinition definition)
    {
        Definition = definition;
    }

    internal AbilityDefinition Definition { get; }

    /// <summary>获取稳定能力标识。</summary>
    public string Id => Definition.Id;

    /// <summary>获取当前语言下的显示名称。</summary>
    public string DisplayName => Definition.DisplayName;

    /// <summary>获取是否为内置压缩动作。</summary>
    public bool IsCompression => Id == AbilityCatalog.CompressionId;
}
