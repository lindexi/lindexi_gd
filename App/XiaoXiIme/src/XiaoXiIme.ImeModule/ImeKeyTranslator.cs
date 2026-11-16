using System.Runtime.InteropServices;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public static class ImeKeyTranslator
{
    public const uint ShiftModifier = 0x01;
    public const uint ControlModifier = 0x02;
    public const uint AltModifier = 0x04;
    public const uint MetaModifier = 0x08;

    /// <summary>Converts a key to input data; a layout-resolved character takes precedence over key labels.</summary>
    public static ImeKey Translate(ushort virtualKey, uint modifiers = 0, char? character = null)
    {
        if ((modifiers & (ControlModifier | AltModifier | MetaModifier)) != 0)
        {
            return new(ImeKeyKind.Other);
        }

        if (character is { } value && !char.IsControl(value) && !char.IsWhiteSpace(value))
        {
            return ImeKey.FromCharacter(value);
        }

        var shift = (modifiers & ShiftModifier) != 0;
        return virtualKey switch
        {
            ImeConstants.VkBack => new(ImeKeyKind.Backspace),
            ImeConstants.VkSpace => new(ImeKeyKind.Space),
            ImeConstants.VkReturn => new(ImeKeyKind.Enter),
            ImeConstants.VkEscape => new(ImeKeyKind.Escape),
            ImeConstants.VkUp => ImeKey.PreviousCandidate(),
            ImeConstants.VkDown => ImeKey.NextCandidate(),
            ImeConstants.VkPrior => ImeKey.PreviousCandidatePage(),
            ImeConstants.VkNext => ImeKey.NextCandidatePage(),
            ImeConstants.VkHome => ImeKey.FirstCandidate(),
            ImeConstants.VkEnd => ImeKey.LastCandidate(),
            ImeConstants.VkLeft => ImeKey.MoveCompositionCaretLeft(),
            ImeConstants.VkRight => ImeKey.MoveCompositionCaretRight(),
            >= 0x30 and <= 0x39 => ImeKey.FromCharacter(shift ? ")!@#$%^&*("[virtualKey - 0x30] : (char)virtualKey),
            >= 0x60 and <= 0x69 => ImeKey.FromCharacter((char)('0' + virtualKey - 0x60)),
            >= ImeConstants.VkA and <= ImeConstants.VkZ => ImeKey.FromCharacter
                ((char)((shift ? 'A' : 'a') + virtualKey - ImeConstants.VkA)),
            0xBA => ImeKey.FromCharacter(shift ? ':' : ';'),
            0xBB => ImeKey.FromCharacter(shift ? '+' : '='),
            0xBC => ImeKey.FromCharacter(shift ? '<' : ','),
            0xBD => ImeKey.FromCharacter(shift ? '_' : '-'),
            0xBE => ImeKey.FromCharacter(shift ? '>' : '.'),
            0xBF => ImeKey.FromCharacter(shift ? '?' : '/'),
            0xC0 => ImeKey.FromCharacter(shift ? '~' : '`'),
            0xDB => ImeKey.FromCharacter(shift ? '{' : '['),
            0xDC => ImeKey.FromCharacter(shift ? '|' : '\\'),
            0xDD => ImeKey.FromCharacter(shift ? '}' : ']'),
            0xDE => ImeKey.FromCharacter(shift ? '"' : '\''),
            0x6A => ImeKey.FromCharacter('*'),
            0x6B => ImeKey.FromCharacter('+'),
            0x6D => ImeKey.FromCharacter('-'),
            0x6E => ImeKey.FromCharacter('.'),
            0x6F => ImeKey.FromCharacter('/'),
            _ => new(ImeKeyKind.Other)
        };
    }

    /// <summary>Resolves printable input against the calling thread's active Windows keyboard layout without changing dead-key state.</summary>
    public static unsafe ImeKey TranslateNative(ushort virtualKey, uint scanCode, byte* keyState, uint modifiers)
    {
        if (!OperatingSystem.IsWindows() || keyState is null)
        {
            return Translate(virtualKey, modifiers);
        }

        if ((modifiers & (ControlModifier | AltModifier | MetaModifier)) != 0)
        {
            return new(ImeKeyKind.Other);
        }

        var text = stackalloc char[8];
        var count = ToUnicodeEx(virtualKey, scanCode, keyState, text, 8, 4, GetKeyboardLayout(0));
        if (count == 1)
        {
            return Translate(virtualKey, modifiers, text[0]);
        }

        // Dead keys and multi-character layout results belong to the host, not US-key fallback.
        return count != 0 || virtualKey is >= 0x30 and <= 0x5A or >= 0x60 and <= 0x6F or >= 0xBA and <= 0xE2
            ? new(ImeKeyKind.Other)
            : Translate(virtualKey, modifiers);
    }

    public static bool ShouldProcess(ushort virtualKey, uint modifiers = 0)
    {
        var key = Translate(virtualKey, modifiers);
        return key.Kind is not ImeKeyKind.Other && key.Character != ';';
    }

    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint threadId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern unsafe int ToUnicodeEx
        (uint virtualKey, uint scanCode, byte* keyState, char* text, int capacity, uint flags, nint layout);
}