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

    [Theory]
    [InlineData(ImeConstants.Vk1, 0)]
    [InlineData(ImeConstants.Vk9, 8)]
    [InlineData(ImeConstants.Vk0, 9)]
    public void Translate_NumberKey_ReturnsCandidateSelection(ushort virtualKey, int expectedCandidateIndex)
    {
        var key = ImeKeyTranslator.Translate(virtualKey);

        Assert.Equal(ImeKeyKind.CandidateSelection, key.Kind);
        Assert.Equal(expectedCandidateIndex, key.CandidateIndex);
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

    [Fact]
    public void ShouldProcess_UnknownKey_ReturnsFalse()
    {
        Assert.False(ImeKeyTranslator.ShouldProcess(ImeConstants.VkTab));
    }
}
