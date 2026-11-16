using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class LegacySentenceTests
{
    private static ImeContext CreateTailPrefix(bool word = false, string scheme = "naturalCodeDoublePinyin")
    {
        var entries = new List<ImePhoneticEntry>
        {
            new("大", "da", 100000), new("字", "zi", 100), new("在", "zai", 90),
            new("好", "hao", 100), new("本", "ben", 100), new("额", "e", 90)
        };
        if (word)
        {
            entries.Add(new("大字", "da zi", 1));
        }

        return new
            (new InMemoryImeDictionary(new ImeLexicon(entries, [], ImeProjectionDefinition.ForScheme(scheme), 1)));
    }

    [Fact]
    public void TailPrefixWordBeatsHighFrequencyLocalCharacters()
    {
        var context = CreateTailPrefix(true);
        Assert.Equal("大字", context.QueryConversionList("daz")[0].Text);
    }

    [Theory]
    [InlineData("naturalCodeDoublePinyin")]
    [InlineData("xiaoheDoublePinyin")]
    [InlineData("abcDoublePinyin")]
    [InlineData("microsoftDoublePinyin")]
    [InlineData("sogouDoublePinyin")]
    [InlineData("jiajiaDoublePinyin")]
    [InlineData("fullPinyin")]
    public void TailPrefixComposesWithoutWholeWord(string scheme)
    {
        var context = CreateTailPrefix(scheme: scheme);
        Assert.Equal("大字", context.QueryConversionList("daz")[0].Text);
    }

    [Fact]
    public void TailPrefixLocalSelectionLeavesLastKey()
    {
        var context = CreateTailPrefix();
        context.SetComposition("daz");
        var index = context.Snapshot.Candidates.ToList().FindIndex(c => c.Text == "大");
        var result = context.ProcessKey(ImeKey.SelectCandidate(index));
        Assert.Equal(("大", "z"), (result.CommitText, result.Snapshot.Composition.Reading));
    }

    [Fact]
    public void ThreeSyllableTailPrefixSurvivesEditAndBackspace()
    {
        var context = CreateTailPrefix();
        var projection = ImeProjectionDefinition.ForScheme("naturalCodeDoublePinyin");
        var hao = projection.Entries.First(p => p.Initial + p.Final == "hao").Input;
        context.SetComposition(hao + "daz");
        context.ProcessKey(new(ImeKeyKind.MoveCompositionCaretLeft));
        context.ProcessKey(ImeKey.FromCharacter('q'));
        context.ProcessKey(new(ImeKeyKind.Backspace));
        Assert.Equal("好大字", context.Snapshot.Candidates[0].Text);
    }

    [Fact]
    public void CompleteDoubleKeySyllableIsNotSplitIntoPrefixes()
    {
        var context = CreateTailPrefix();
        Assert.DoesNotContain(context.QueryConversionList("be"), c => c.Text == "本额");
    }

    private static ImeContext Create(Action<ImeDictionaryLearning>? learn = null) => new
    (
        CanonicalDictionaryFixture.Create
        (
            [
                new("比", "bi", 100), new("笔", "bi", 90), new("闭", "bi", 80), new("臂", "bi", 70),
                new("醋", "cu", 100), new("粗", "cu", 90), new("贝", "bei", 110), new("尔", "er", 100),
            ]
        ), learn
    );

    [Fact]
    public void MissingWholeWordProducesSentenceAndLocalCharacters()
    {
        var context = Create();
        context.SetComposition("bicu");
        Assert.Equal(new[] { "比醋", "比", "笔", "闭", "臂" }, context.Snapshot.Candidates.Take(5).Select(c => c.Text));
    }

    [Fact]
    public void DigitThreeConsumesOnlyConfirmedPrefix()
    {
        var context = Create();
        context.SetComposition("bicu");
        var result = context.ProcessKey(ImeKey.SelectCandidate(2));
        Assert.Equal
        (
            ("笔", "cu", "醋"),
            (result.CommitText, result.Snapshot.Composition.Reading, result.Snapshot.Candidates.FirstOrDefault()?.Text)
        );
    }

    [Fact]
    public void PrefixSegmentsHaveDictionaryEvidence()
    {
        var context = Create();
        Assert.DoesNotContain(context.QueryConversionList("be"), c => c.Text == "贝尔");
    }

    [Fact]
    public void InvalidSuffixIsNotConsumedBySentence()
    {
        var context = Create();
        Assert.DoesNotContain(context.QueryConversionList("bicuqz"), c => c.Text == "比醋" && c.Reading == "bi cu qz");
    }

    [Fact]
    public void ThreeSyllablesWithoutWholeWordAreComposed()
    {
        var context = Create();
        Assert.Contains(context.QueryConversionList("bicubi"), c => c.Text == "比醋比");
    }

    [Fact]
    public void SelectingLocalCandidateRetainsInvalidSuffixVerbatim()
    {
        var context = Create();
        context.SetComposition("bicuqz");
        var result = context.ProcessKey(ImeKey.SelectCandidate(0));
        Assert.Equal(("比", "cuqz"), (result.CommitText, result.Snapshot.Composition.Reading));
    }

    [Fact]
    public void MiddleEditAndBackspaceRestoreSentence()
    {
        var context = Create();
        context.SetComposition("bicu");
        context.ProcessKey(new(ImeKeyKind.MoveCompositionCaretLeft));
        context.ProcessKey(ImeKey.FromCharacter('q'));
        context.ProcessKey(new(ImeKeyKind.Backspace));
        Assert.Equal
        (
            ("bicu", 3, "比醋"), (context.Snapshot.Composition.Reading,
                context.Snapshot.Composition.CaretIndex, context.Snapshot.Candidates[0].Text)
        );
    }

    [Fact]
    public void DoublePinyinDoesNotUseFullPinyinInteriorPrefixes()
    {
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    [new("贝", "bei", 100), new("尔", "er", 90)], [], ImeProjectionDefinition.XiaoheDoublePinyin, 1
                )
            )
        );
        Assert.DoesNotContain(context.QueryConversionList("be"), c => c.Text == "贝尔");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FrequencyChoosesBestThreeSegmentPathRegardlessOfLoadOrder(bool reverse)
    {
        ImeCandidate[] entries =
        [
            new("低频词", "bi cu", 1), new("比", "bi", 100),
            new("醋", "cu", 100), new("好", "hao", 100)
        ];
        var context = new ImeContext(CanonicalDictionaryFixture.Create(reverse ? entries.Reverse() : entries));
        Assert.Equal("比醋好", context.QueryConversionList("bicuhao")[0].Text);
    }

    [Fact]
    public void PrefixPathComparesAllSplitsRatherThanLongestHead()
    {
        var context = new ImeContext
        (
            CanonicalDictionaryFixture.Create
            (
                new ImeCandidate[]
                {
                    new("冷", "bei", 1), new("儿", "er", 1),
                    new("不", "bu", 100), new("额", "en", 100), new("好", "hao", 100)
                }
            )
        );
        Assert.Empty(context.QueryConversionList("behao"));
    }

    [Fact]
    public void CompleteSegmentsBeatHigherFrequencyAbbreviations()
    {
        var context = new ImeContext
        (
            CanonicalDictionaryFixture.Create
            (
                new ImeCandidate[]
                {
                    new("比", "bi", 1), new("醋", "cu", 1),
                    new("边", "bian", 10000), new("粗", "cuan", 10000)
                }
            )
        );
        Assert.Equal("比醋", context.QueryConversionList("bicu")[0].Text);
    }

    [Fact]
    public void PrefixSelectionConsumesEvidenceAndLearnsCanonicalSegmentsOnce()
    {
        var learned = new List<ImeDictionaryLearning>();
        var context = Create(learned.Add);
        context.SetComposition("be");
        context.ProcessKey(ImeKey.SelectCandidate(0));
        Assert.Equal
        (
            (1, "be", "bei", "", "be:bei"),
            (learned.Count, learned[0].OriginalInput, learned[0].Candidate.Reading,
                context.Snapshot.Composition.Reading,
                string.Join
                (
                    "|", learned[0].Segments.Select
                    (s =>
                        "be".Substring(s.InputStart, s.InputLength) + ":" + s.CanonicalReading
                    )
                ))
        );
    }

    [Fact]
    public void InteriorPrefixIsAvailableForLocalSelectionWithStrictConsumption()
    {
        var learned = new List<ImeDictionaryLearning>();
        var context = Create(learned.Add);
        context.SetComposition("be");
        var index = context.Snapshot.Candidates.ToList().FindIndex(c => c.Text == "贝");
        Assert.True(index >= 0);
        var result = context.ProcessKey(ImeKey.SelectCandidate(index));
        Assert.Equal
        (
            ("贝", "", 1, "be", "bei"), (result.CommitText,
                result.Snapshot.Composition.Reading, learned.Count,
                learned[0].OriginalInput, learned[0].Candidate.Reading)
        );
    }

    [Fact]
    public void PartialLearningKeepsCanonicalReadingAndOriginalRange()
    {
        ImeDictionaryLearning? learning = null;
        var context = Create(value => learning = value);
        context.SetComposition("bicu");
        context.ProcessKey(ImeKey.SelectCandidate(2));
        Assert.Equal(("bi", "bi"), (learning?.Candidate.Reading, learning?.OriginalInput));
    }
}