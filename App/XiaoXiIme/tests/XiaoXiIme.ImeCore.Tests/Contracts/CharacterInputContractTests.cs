using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class CharacterInputContractTests
{
    [Theory]
    [InlineData('0')]
    [InlineData('2')]
    [InlineData('!')]
    [InlineData(',')]
    [InlineData(';')]
    [InlineData('\'')]
    public void WhenIdleOrdinaryCharacterThenHostOwnsIt(char character)
    {
        var context = CreateContext();
        var key = ImeKey.FromCharacter(character);
        var result = context.ProcessKey(key);
        Assert.Equal
        (
            (false, false, (string?)null, ""),
            (ImeContext.ShouldProcessKey(key, context.Snapshot), result.Handled, result.CommitText,
                result.Snapshot.Composition.Reading)
        );
    }

    [Theory]
    [InlineData("ni", ',', "你,")]
    [InlineData("ni", '!', "你!")]
    [InlineData("ni", '/', "你/")]
    [InlineData("zzzz", '.', "zzzz.")]
    public void WhenCompositionMeetsPunctuationThenCommitsExactlyOnce(string reading, char character, string expected)
    {
        var context = CreateContext();
        context.SetComposition(reading);
        var key = ImeKey.FromCharacter(character);
        var owns = ImeContext.ShouldProcessKey(key, context.Snapshot);
        var result = context.ProcessKey(key);
        Assert.Equal
        (
            (true, true, expected, ""),
            (owns, result.Handled, result.CommitText, result.Snapshot.Composition.Reading)
        );
    }

    [Fact]
    public void WhenDigitSelectsPartialCandidateThenRemainingSyllableIsPreserved()
    {
        var context = CreateContext();
        context.SetComposition("nihao");
        var candidates = context.Snapshot.Candidates;
        var index = candidates.ToList().FindIndex(c => c.Text == "呢" && c.Reading == "ni");
        var result = context.ProcessKey(ImeKey.FromCharacter((char)('1' + index)));
        Assert.Equal(("呢", "hao"), (result.CommitText, result.Snapshot.Composition.Reading));
    }

    [Fact]
    public void WhenPunctuationFollowsPartialSelectionThenRemainderCommitsOnce()
    {
        var context = CreateContext();
        context.SetComposition("nihao");
        var index = context.Snapshot.Candidates.ToList().FindIndex(c => c.Text == "呢" && c.Reading == "ni");
        var first = context.ProcessKey(ImeKey.FromCharacter((char)('1' + index)));
        var last = context.ProcessKey(ImeKey.FromCharacter('!'));
        Assert.Equal(("呢好!", ""), (first.CommitText + last.CommitText, last.Snapshot.Composition.Reading));
    }

    [Fact]
    public void WhenSymbolDigitThenItExtendsSymbolCodeInsteadOfSelecting()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('/'));
        var result = context.ProcessKey(ImeKey.FromCharacter('2'));
        Assert.Equal
            ((true, (string?)null, "/2"), (result.Handled, result.CommitText, result.Snapshot.Composition.Reading));
    }

    [Fact]
    public void WhenFullPinyinSeparatorThenItConstrainsSyllableBoundary()
    {
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    [new("西安", "xi an", 100), new("先", "xian", 1000)], [], ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );
        context.SetComposition("xi'an");
        var result = context.ProcessKey(ImeKey.FromCharacter('!'));
        Assert.Equal("西安!", result.CommitText);
    }

    [Fact]
    public void WhenUnknownKeyDuringCompositionThenItIsNotClaimed()
    {
        var context = CreateContext();
        context.SetComposition("ni");
        Assert.False(ImeContext.ShouldProcessKey(new(ImeKeyKind.Other), context.Snapshot));
    }

    [Theory]
    [InlineData(false, 'a')]
    [InlineData(true, '2')]
    public void WhenCompositionAtLimitThenInsertionIsRejectedWithoutMutation(bool symbol, char character)
    {
        var context = CreateContext();
        var reading = symbol ? "/" + new string('2', 63) : new string('z', 64);
        context.SetComposition(reading);
        var key = ImeKey.FromCharacter(character);
        var owns = ImeContext.ShouldProcessKey(key, context.Snapshot);
        var result = context.ProcessKey(key);
        Assert.Equal((false, false, reading), (owns, result.Handled, result.Snapshot.Composition.Reading));
    }

    [Fact]
    public void WhenPreflightRepeatedThenNoLearningOrCompositionMutationOccurs()
    {
        var learning = new List<ImeDictionaryLearning>();
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    [new("你", "ni", 100)], [], ImeProjectionDefinition.FullPinyin, 1
                )
            ), learning.Add
        );
        context.SetComposition("ni");
        var before = context.Snapshot;
        for (var i = 0; i < 10; i++)
        {
            ImeContext.ShouldProcessKey(ImeKey.FromCharacter('!'), before);
        }

        Assert.Equal
        (
            ("ni", 0, string.Join('|', before.Candidates)),
            (context.Snapshot.Composition.Reading, learning.Count, string.Join('|', context.Snapshot.Candidates))
        );
    }

    private static ImeContext CreateContext() => new
    (
        new InMemoryImeDictionary
        (
            new ImeLexicon
            (
                [new("你", "ni", 100), new("呢", "ni", 90), new("好", "hao", 100)], [], ImeProjectionDefinition.FullPinyin,
                1
            )
        )
    );
}