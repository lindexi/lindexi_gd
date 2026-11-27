using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule.Tests;

public class ImeKeyTranslatorTests
{
    [Theory]
    [InlineData(ImeConstants.VkA, 'a')]
    [InlineData(ImeConstants.VkZ, 'z')]
    public void Translate_AlphabetKey_ReturnsLowercaseCharacter(ushort virtualKey, char expectedCharacter)
    {
        var key = ImeKeyTranslator.Translate(virtualKey);

        Assert.Equal(ImeKeyKind.Character, key.Kind);
        Assert.Equal(expectedCharacter, key.Character);
    }

    [Fact]
    public void Translate_ShiftAlphabetKey_ReturnsUppercaseCharacter()
    {
        var key = ImeKeyTranslator.Translate(ImeConstants.VkA, ImeKeyTranslator.ShiftModifier);

        Assert.Equal('A', key.Character);
    }

    [Fact]
    public void Translate_SlashKey_ReturnsSlashCharacter()
    {
        var key = ImeKeyTranslator.Translate(ImeConstants.VkOem2);

        Assert.Equal('/', key.Character);
    }

    [Theory]
    [InlineData(ImeConstants.Vk1, '1')]
    [InlineData(ImeConstants.Vk9, '9')]
    [InlineData(ImeConstants.Vk0, '0')]
    public void Translate_NumberKey_ReturnsCharacter(ushort virtualKey, char expectedCharacter)
    {
        var key = ImeKeyTranslator.Translate(virtualKey);

        Assert.Equal(ImeKey.FromCharacter(expectedCharacter), key);
    }

    [Theory]
    [InlineData(ImeConstants.VkBack, ImeKeyKind.Backspace)]
    [InlineData(ImeConstants.VkSpace, ImeKeyKind.Space)]
    [InlineData(ImeConstants.VkReturn, ImeKeyKind.Enter)]
    [InlineData(ImeConstants.VkEscape, ImeKeyKind.Escape)]
    [InlineData(ImeConstants.VkUp, ImeKeyKind.PreviousCandidate)]
    [InlineData(ImeConstants.VkDown, ImeKeyKind.NextCandidate)]
    [InlineData(ImeConstants.VkPrior, ImeKeyKind.PreviousCandidatePage)]
    [InlineData(ImeConstants.VkNext, ImeKeyKind.NextCandidatePage)]
    [InlineData(ImeConstants.VkHome, ImeKeyKind.FirstCandidate)]
    [InlineData(ImeConstants.VkEnd, ImeKeyKind.LastCandidate)]
    [InlineData(ImeConstants.VkLeft, ImeKeyKind.MoveCompositionCaretLeft)]
    [InlineData(ImeConstants.VkRight, ImeKeyKind.MoveCompositionCaretRight)]
    public void Translate_ControlKey_ReturnsExpectedKind(ushort virtualKey, ImeKeyKind expectedKind)
    {
        var key = ImeKeyTranslator.Translate(virtualKey);

        Assert.Equal(expectedKind, key.Kind);
    }

    [Theory]
    [InlineData(ImeConstants.VkUp)]
    [InlineData(ImeConstants.VkDown)]
    [InlineData(ImeConstants.VkPrior)]
    [InlineData(ImeConstants.VkNext)]
    [InlineData(ImeConstants.VkHome)]
    [InlineData(ImeConstants.VkEnd)]
    [InlineData(ImeConstants.VkLeft)]
    [InlineData(ImeConstants.VkRight)]
    public void ShouldProcess_NavigationKey_ReturnsTrue(ushort virtualKey)
    {
        Assert.True(ImeKeyTranslator.ShouldProcess(virtualKey));
    }

    [Theory]
    [InlineData(0xBD, '-')]
    [InlineData(0xBB, '=')]
    public void WhenPageCharacterHasNoModifiersThenTranslationPreservesTheCharacter
        (ushort virtualKey, char character)
    {
        Assert.Equal(ImeKey.FromCharacter(character), ImeKeyTranslator.Translate(virtualKey));
    }

    [Theory]
    [InlineData(0xBD, ImeKeyTranslator.ShiftModifier, '_')]
    [InlineData(0xBB, ImeKeyTranslator.ShiftModifier, '+')]
    [InlineData(0xBD, ImeKeyTranslator.ShiftModifier, '-')]
    [InlineData(0xBB, ImeKeyTranslator.ShiftModifier, '=')]
    [InlineData(0xBD, ImeKeyTranslator.ControlModifier, '-')]
    [InlineData(0xBB, ImeKeyTranslator.AltModifier, '=')]
    [InlineData(0xBD, ImeKeyTranslator.MetaModifier, '-')]
    public void WhenPageKeyHasModifiersThenTranslationLeavesItToTheHost
        (ushort virtualKey, uint modifiers, char layoutCharacter)
    {
        Assert.Equal
        (
            (ImeKeyKind.Other, ImeKeyKind.Other),
            (ImeKeyTranslator.Translate(virtualKey, modifiers).Kind,
                ImeKeyTranslator.Translate(virtualKey, modifiers, layoutCharacter).Kind)
        );
    }

    [Fact]
    public void ShouldProcess_UnknownKey_ReturnsFalse()
    {
        Assert.False(ImeKeyTranslator.ShouldProcess(ImeConstants.VkTab));
    }
}