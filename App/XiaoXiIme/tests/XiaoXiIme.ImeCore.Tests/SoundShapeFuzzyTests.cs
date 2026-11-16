using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class SoundShapeFuzzyTests
{
    public static IEnumerable<object[]> ZhangCases()
    {
        string[] full = ["zhang", "zang", "zhan", "zan"];
        string[] keys = ["vh", "zh", "vj", "zj"];
        string[][] exact = [["张"], ["脏"], ["毡"], ["簪"]];
        string[][] initials = [["张", "脏"], ["张", "脏"], ["毡", "簪"], ["毡", "簪"]];
        string[][] finals = [["张", "毡"], ["脏", "簪"], ["张", "毡"], ["脏", "簪"]];
        IReadOnlyList<ImeFuzzyRule>[] rules = [[], [ImeFuzzyRule.ZhZ], [ImeFuzzyRule.AngAn], [ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn]];
        string[][][] outputs = [exact, initials, finals, Enumerable.Repeat(new[] { "张", "脏", "毡", "簪" }, 4).ToArray()];
        for (var rule = 0; rule < 4; rule++)
        {
            for (var input = 0; input < 4; input++)
            {
                yield return ["F0" + (rule + 1), "Full", full[input], rules[rule], outputs[rule][input]];
                yield return ["H08", "Xiaohe", keys[input], rules[rule], outputs[rule][input]];
            }
        }
    }

    [Theory]
    [MemberData(nameof(ZhangCases))]
    public void F01_F04_H08_ZhangHasCompleteBidirectionalClosure(string id, string projection, string input,
        IReadOnlyList<ImeFuzzyRule> rules, string[] expected)
    {
        var context = Create(InventedSoundShapeFixture.Z(projection), rules);
        Type(context, input);

        var backed = Complete(context).Where(c => c.Interpretations.Any(p =>
            string.Concat(p.Evidence.Where(e => e.Kind == ImeEvidenceKind.Phonetic).Select(e => e.Input)) == input &&
            p.Evidence.Where(e => e.Kind == ImeEvidenceKind.Phonetic).All(e =>
                projection != "Full" || e.Range.Length == (p.Evidence.FirstOrDefault(f =>
                    f.Kind == ImeEvidenceKind.Fuzzy && f.Range == e.Range)?.Input ?? e.CanonicalReading).Length)));
        Assert.True(expected.SequenceEqual(backed.Select(candidate => candidate.Candidate.Text)),
            $"{id}: fuzzy closure differs for {projection}/{input}");
    }

    public static IEnumerable<object[]> TsCases()
    {
        string[] fullFirst = ["zhuan", "zuan", "zhuang", "zuang"];
        string[] fullLast = ["shen", "sen", "sheng", "seng"];
        string[] keyFirst = ["vr", "zr", "vl", "zl"];
        string[] keyLast = ["uf", "sf", "ug", "sg"];
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                yield return ["Full", fullFirst[i] + fullLast[j]];
                yield return ["Xiaohe", keyFirst[i] + keyLast[j]];
            }
        }
    }

    [Theory]
    [MemberData(nameof(TsCases))]
    public void F05_F06_H08_AllSixteenInputsReturnAllSixteenCanonicalWords(string projection, string input)
    {
        var lexicon = InventedSoundShapeFixture.TS(projection);
        var context = Create(lexicon, [ImeFuzzyRule.ZhZ, ImeFuzzyRule.UanUang, ImeFuzzyRule.ShS, ImeFuzzyRule.EngEn]);
        Type(context, input);

        Assert.Equal(lexicon.Entries.Select(entry => (entry.Text, entry.CanonicalReading, entry.BaseFrequency, input.Length, "")),
            Complete(context).Select(candidate => (candidate.Candidate.Text, candidate.Candidate.Reading, candidate.Rank.Frequency,
                candidate.Interpretations.First().CoverageEnd, candidate.Interpretations.First().RemainingInput)));
    }

    [Fact]
    public void F07_DisablingRulesImmediatelyRestoresExactResultWithoutChangingInput()
    {
        var context = Create(InventedSoundShapeFixture.Z(), []);
        Type(context, "zhang");
        context.UpdateFuzzyRules([ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn]);
        context.UpdateFuzzyRules([]);

        Assert.Equal(("zhang", 5, "张"), (context.Snapshot.Composition.Reading,
            context.Snapshot.Composition.CaretIndex, string.Join('|', Complete(context).Select(candidate => candidate.Candidate.Text))));
    }

    [Fact]
    public void F07_UpdatingRulesNeverCommitsOrLearns()
    {
        var learning = new List<ImeDictionaryLearning>();
        var context = new ImeContext(new InMemoryImeDictionary(InventedSoundShapeFixture.Z()),
            InventedSoundShapeFixture.Options(auto: ImeAutoCommitPolicy.Unique), learning.Add);
        Type(context, "za");

        var result = context.UpdateFuzzyRules([ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn]);

        Assert.Equal((true, null, 0), (result.Handled, result.CommitText, learning.Count));
    }

    [Fact]
    public void F08_DuplicateRulesDoNotMultiplyFrequency()
    {
        var context = Create(InventedSoundShapeFixture.Z(), [ImeFuzzyRule.ZhZ, ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn, ImeFuzzyRule.AngAn]);
        Type(context, "zhang");

        Assert.Equal(new[] { 80, 70, 60, 50 }, Complete(context).Select(candidate => candidate.Rank.Frequency));
    }

    [Fact]
    public void F08_FuzzyRulesDoNotApplyToIndependentShapeQuery()
    {
        var context = Create(InventedSoundShapeFixture.Z(), [ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn]);

        Assert.Empty(context.QueryShape("zg"));
    }

    [Fact]
    public void F09_ResetKeepsRulesButDoesNotAffectAnotherContext()
    {
        var first = Create(InventedSoundShapeFixture.Z(), [ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn]);
        var second = Create(InventedSoundShapeFixture.Z(), []);
        Type(first, "zhang");
        first.Reset();
        Type(first, "zhang");
        Type(second, "zhang");

        Assert.Equal(("张|脏|毡|簪", "张"), (string.Join('|', Complete(first).Select(candidate => candidate.Candidate.Text)),
            string.Join('|', Complete(second).Select(candidate => candidate.Candidate.Text))));
    }

    [Fact]
    public void C09_UnencodableFuzzyUpdateIsRejectedAtomically()
    {
        var fixture = new InventedSoundShapeFixture("PATH");
        fixture.Type("ni");

        Assert.Throws<NotSupportedException>(() => fixture.Context.UpdateFuzzyRules([ImeFuzzyRule.AngAn]));
    }

    private static ImeContext Create(ImeLexicon lexicon, IReadOnlyList<ImeFuzzyRule> rules) =>
        new(new InMemoryImeDictionary(lexicon), InventedSoundShapeFixture.Options(sort: ImeCandidateSortPolicy.Automatic, rules: rules), null);

    internal static void Type(ImeContext context, string input)
    {
        foreach (var character in input)
        {
            context.ProcessKey(ImeKey.FromCharacter(character));
        }
    }

    private static IEnumerable<ImeConversionCandidate> Complete(ImeContext context) =>
        context.QueryInterpretations(context.Snapshot.Composition.Reading).Candidates
            .Where(candidate => candidate.Interpretations.Any(path => path.Kind == ImeInterpretationKind.Complete));
}
