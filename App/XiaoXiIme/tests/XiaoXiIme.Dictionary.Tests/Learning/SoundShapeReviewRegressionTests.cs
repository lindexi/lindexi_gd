using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class SoundShapeReviewRegressionTests
{
    [Theory]
    [InlineData("reading")]
    [InlineData("alias")]
    [InlineData("frequency")]
    [InlineData("dangling")]
    public void R08_InvalidLexiconIsRejectedBeforeUse(string defect)
    {
        var lexicon = CreateLexicon();
        lexicon = defect switch
        {
            "reading" => lexicon with { Entries = [new("你", "", 40)] },
            "alias" => lexicon with { LookupAliases = [new("你", "ni", "")] },
            "frequency" => lexicon with { Entries = [new("你", "ni", -1)] },
            _ => lexicon with { LookupAliases = [new("泥", "ni", "n")] },
        };
        Assert.Throws<ArgumentException>(() => new InMemoryImeDictionary(lexicon));
    }

    [Theory]
    [InlineData("gap")]
    [InlineData("overlap")]
    [InlineData("reading")]
    [InlineData("incomplete")]
    public void R06_InvalidPayloadIsAtomicallyRejected(string defect)
    {
        var dictionary = new UserDictionary(new InMemoryImeDictionary(CreateLexicon()));
        var second = defect switch
        {
            "gap" => new ImeConfirmedSegment("你", "ni", 2, 2, 2, 1),
            "overlap" => new ImeConfirmedSegment("你", "ni", 1, 2, 1, 1),
            "reading" => new ImeConfirmedSegment("你", "nin", 2, 2, 1, 1),
            _ => new ImeConfirmedSegment("你", "ni", 2, 2, 1, 1),
        };
        var text = defect is "gap" or "incomplete" ? "你你你" : "你你";
        var error = Record.Exception
        (() => dictionary.Learn
            (
                new(new ImeCandidate(text, "ni ni", 40), "nini")
                    { Segments = [new("你", "ni", 0, 2, 0, 1), second] }
            )
        );
        Assert.Equal
        (
            (typeof(ArgumentException), 0, 1L),
            (error?.GetType(), dictionary.GetEntries().Count, dictionary.GetLexicon().Version)
        );
    }

    [Fact]
    public void R06_InputGapRejectsEntireLearningEvent()
    {
        var dictionary = new UserDictionary(new InMemoryImeDictionary(CreateLexicon()));
        var error = Record.Exception
        (() => dictionary.Learn
            (
                new(new ImeCandidate("你你", "ni ni", 40), "niqni")
                    { Segments = [new("你", "ni", 0, 2, 0, 1), new("你", "ni", 3, 2, 1, 1)] }
            )
        );
        Assert.Equal
        (
            (typeof(ArgumentException), 0, 1L),
            (error?.GetType(), dictionary.GetEntries().Count, dictionary.GetLexicon().Version)
        );
    }

    [Theory]
    [InlineData("fullPinyin")]
    [InlineData("xiaoheDoublePinyin")]
    public void R07_V1RecoversAllCandidatesBeyondPrefixCache(string scheme)
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Review", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            Enumerable.Range(0, 110).Select(i => new PhoneticDictionaryEntry($"词{i}", "ni", i)), path,
            new DictionaryPackageParameters { InputScheme = scheme, MaxPrefixCandidatesPerKey = 1 }
        );
        Assert.Equal(110, DictionaryPackageLoader.Load(path).GetLexicon().Entries.Count);
    }

    private static ImeLexicon CreateLexicon() => new([new("你", "ni", 40)], [], ImeProjectionDefinition.FullPinyin, 1);
}