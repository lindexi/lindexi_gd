using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public static class ImeKeyTranslator
{
    public static ImeKey Translate(ushort virtualKey, uint modifiers = 0)
    {
        return virtualKey switch
        {
            ImeConstants.VkBack => new ImeKey(ImeKeyKind.Backspace),
            ImeConstants.VkSpace => new ImeKey(ImeKeyKind.Space),
            ImeConstants.VkReturn => new ImeKey(ImeKeyKind.Enter),
            ImeConstants.VkEscape => new ImeKey(ImeKeyKind.Escape),
            ImeConstants.VkUp => ImeKey.PreviousCandidate(),
            ImeConstants.VkDown => ImeKey.NextCandidate(),
            ImeConstants.VkPrior => ImeKey.PreviousCandidatePage(),
            ImeConstants.VkNext => ImeKey.NextCandidatePage(),
            ImeConstants.VkHome => ImeKey.FirstCandidate(),
            ImeConstants.VkEnd => ImeKey.LastCandidate(),
            ImeConstants.VkLeft => ImeKey.MoveCompositionCaretLeft(),
            ImeConstants.VkRight => ImeKey.MoveCompositionCaretRight(),
            >= ImeConstants.Vk0 and <= ImeConstants.Vk9 => ImeKey.SelectCandidate(virtualKey == ImeConstants.Vk0 ? 9 : virtualKey - ImeConstants.Vk0 - 1),
            >= ImeConstants.VkA and <= ImeConstants.VkZ => ImeKey.FromCharacter((char)('a' + virtualKey - ImeConstants.VkA)),
            _ => new ImeKey(ImeKeyKind.Other),
        };
    }

    public static bool ShouldProcess(ushort virtualKey, uint modifiers = 0)
    {
        var key = Translate(virtualKey, modifiers);
        return key.Kind is not ImeKeyKind.Other;
    }
}
