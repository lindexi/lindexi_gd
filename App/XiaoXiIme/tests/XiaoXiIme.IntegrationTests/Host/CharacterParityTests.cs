using Avalonia.Input;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeModule;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

public class CharacterParityTests
{
    [Theory]
    [InlineData("", Key.D2, 0x32, '2', false)]
    [InlineData("", Key.OemComma, 0xBC, ',', false)]
    [InlineData("", Key.NumPad2, 0x62, '2', false)]
    [InlineData("", Key.D1, 0x31, '!', true)]
    [InlineData("ni", Key.D1, 0x31, '1', false)]
    [InlineData("ni", Key.NumPad1, 0x61, '1', false)]
    [InlineData("ni", Key.D1, 0x31, '!', true)]
    [InlineData("ni", Key.OemComma, 0xBC, ',', false)]
    [InlineData("zzzz", Key.OemPeriod, 0xBE, '.', false)]
    [InlineData("/", Key.D2, 0x32, '2', false)]
    [InlineData("/2", Key.D1, 0x31, '!', true)]
    [InlineData("ni", Key.OemSemicolon, 0xBA, ';', false)]
    [InlineData("ni", Key.OemQuotes, 0xDE, '\'', false)]
    [InlineData("ni'", Key.OemComma, 0xBC, ',', false)]
    public void WhenSameInputThenCoreExperienceAndFormalTranslatorAgree
        (string reading, Key physical, ushort vk, char character, bool shift)
    {
        var dictionary = CreateDictionary();
        var core = new ImeContext(dictionary);
        var formal = new ImeContext(dictionary);
        var experience = new DictionaryDebugSession(dictionary);
        core.SetComposition(reading);
        formal.SetComposition(reading);
        foreach (var value in reading)
        {
            experience.ProcessKey(Key.None, KeyModifiers.None, value.ToString());
        }

        var key = ImeKeyTranslator.Translate(vk, shift ? ImeKeyTranslator.ShiftModifier : 0);
        var owns = ImeContext.ShouldProcessKey(key, formal.Snapshot);
        var actual = formal.ProcessKey(key);
        var expected = core.ProcessKey(ImeKey.FromCharacter(character));
        experience.ProcessKey(physical, shift ? KeyModifiers.Shift : KeyModifiers.None);
        Assert.Equal
        (
            (expected.Handled, expected.Handled, expected.CommitText, expected.Snapshot.Composition.Reading,
                expected.CommitText ?? (expected.Handled ? "" : character.ToString()),
                expected.Snapshot.Composition.Reading),
            (owns, actual.Handled, actual.CommitText, actual.Snapshot.Composition.Reading,
                experience.CommittedText, experience.Candidates.CompositionText)
        );
    }

    [Fact]
    public void WhenSelectingPartialCandidateThenAllRoutesRetainAndCommitRemainder()
    {
        var dictionary = CreateDictionary();
        var core = new ImeContext(dictionary);
        var formal = new ImeContext(dictionary);
        var experience = new DictionaryDebugSession(dictionary);
        core.SetComposition("nihao");
        formal.SetComposition("nihao");
        foreach (var value in "nihao")
        {
            experience.ProcessKey(Key.None, KeyModifiers.None, value.ToString());
        }

        var index = core.Snapshot.Candidates.ToList().FindIndex(c => c.Text == "呢" && c.Reading == "ni");
        var digit = (char)('1' + index);
        var first = core.ProcessKey(ImeKey.FromCharacter(digit));
        var translated = formal.ProcessKey(ImeKeyTranslator.Translate((ushort)digit));
        experience.ProcessKey(Key.None, KeyModifiers.None, digit.ToString());
        var remaining = (first.Snapshot.Composition.Reading, translated.Snapshot.Composition.Reading,
            experience.Candidates.CompositionText);
        var last = core.ProcessKey(ImeKey.FromCharacter('!'));
        var formalLast = formal.ProcessKey(ImeKeyTranslator.Translate(0x31, ImeKeyTranslator.ShiftModifier));
        experience.ProcessKey(Key.D1, KeyModifiers.Shift);
        Assert.Equal
        (
            (("hao", "hao", "hao"), "呢好!", "呢好!", "呢好!"),
            (remaining, first.CommitText + last.CommitText, translated.CommitText + formalLast.CommitText,
                experience.CommittedText)
        );
    }

    [Theory]
    [InlineData(2u)]
    [InlineData(4u)]
    [InlineData(8u)]
    public void WhenShortcutThenFormalPreflightDoesNotClaimComposition(uint modifiers)
    {
        var core = new ImeContext(CreateDictionary());
        core.SetComposition("ni");
        Assert.False(ImeContext.ShouldProcessKey(ImeKeyTranslator.Translate(0x31, modifiers, '!'), core.Snapshot));
    }

    private static InMemoryImeDictionary CreateDictionary() => new
    (
        new ImeLexicon
        (
            [new("你", "ni", 100), new("呢", "ni", 90), new("好", "hao", 100)], [], ImeProjectionDefinition.FullPinyin, 1
        )
    );
}