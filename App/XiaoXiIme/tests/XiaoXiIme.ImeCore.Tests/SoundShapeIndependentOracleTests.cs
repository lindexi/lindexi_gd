using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class SoundShapeIndependentOracleTests
{
    public static IEnumerable<object[]> Policies()
    {
        foreach (var casing in Enum.GetValues<ImeShapeCasePolicy>())
            foreach (var placement in Enum.GetValues<ImeShapePlacement>())
                foreach (var length in Enum.GetValues<ImeShapeLengthPolicy>())
                    if (length != ImeShapeLengthPolicy.RequiredTwo || placement == ImeShapePlacement.PerCharacter)
                        yield return [casing, placement, length];
    }

    [Theory]
    [MemberData(nameof(Policies))]
    public void R09_SmallGrammarMatchesCanonicalPathsEvidenceAndConsumption(
        ImeShapeCasePolicy casing, ImeShapePlacement placement, ImeShapeLengthPolicy length)
    {
        var lexicon = new ImeLexicon(
            [new("你", "ni", 2), new("你", "nin", 3), new("你你", "ni ni", 4), new("张", "zhang", 1)],
            [new("你", "rx", "oracle"), new("张", "rx", "oracle")],
            new("oracle", [new("n", "i", "ni"), new("n", "in", "ni"),
                new("zh", "ang", "va"), new("z", "ang", "za"), new("zh", "an", "vn"), new("z", "an", "zn")]), 1);
        var options = new ImeInputOptions
        {
            CasePolicy = casing, Placement = placement, LengthPolicy = length,
            FuzzyRules = [ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn],
        };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon), options, null);
        // Explicit grammar terminals, not production projection or fuzzy helpers.
        (string Text, string Reading, string[] Syllables, (string Key, string Spoken)[][] Sounds)[] words =
        [
            ("你", "ni", ["ni"], [[("ni", "ni")]]),
            ("你", "nin", ["nin"], [[("ni", "nin")]]),
            ("你你", "ni ni", ["ni", "ni"], [[("ni", "ni")], [("ni", "ni")]]),
            ("张", "zhang", ["zhang"], [[("va", "zhang"), ("za", "zang"), ("vn", "zhan"), ("zn", "zan")]]),
        ];
        string[] inputs = ["n", "ni", "niR", "niRX", "nir", "nirx", "NIr", "NIrx",
            "nin", "nini", "niRXniRX", "nirxnirx", "niniR", "niniRR", "ninirr", "niRXn", "niRXniR",
            "va", "za", "vn", "zn", "znR", "znRX", "znrx", "znRXn", "niq"];
        foreach (var input in inputs)
        {
            var expected = new HashSet<string>(StringComparer.Ordinal);
            var sentences = new List<(string Signature, int Missing, double Weight, int Count)>();
            Walk(0, "", [], []);
            var best = sentences.OrderBy(p => p.Missing).ThenByDescending(p => p.Weight).ThenBy(p => p.Count).FirstOrDefault();
            if (best.Signature is not null) expected.Add(best.Signature);
            var actual = context.QueryInterpretations(input).Candidates.SelectMany(c => c.Interpretations.Select(p =>
                Signature(c.Candidate.Text, p))).Order(StringComparer.Ordinal).ToArray();
            Assert.True(expected.Order(StringComparer.Ordinal).SequenceEqual(actual),
                $"{casing}/{placement}/{length}/{input}\nExpected: {string.Join('\n', expected.Order())}\nActual: {string.Join('\n', actual)}");

            void Walk(int start, string text, ImeConfirmedSegment[] confirmed, ImePathEvidence[] previous)
            {
                foreach (var word in words)
                {
                    ReadSound(0, start, previous);
                    void Pending(ImeInterpretationKind kind, ImePathEvidence[] evidence)
                    {
                        if (start == 0) expected.Add(Signature(word.Text,
                            new(kind, input.Length, null, "", confirmed, evidence)));
                    }
                    void Finish(int end, ImePathEvidence[] evidence)
                    {
                        ImeConfirmedSegment[] segments = [.. confirmed, new(word.Text, word.Reading, start, end - start, text.Length, word.Text.Length)];
                        var signature = Signature(text + word.Text, new(end == input.Length ? ImeInterpretationKind.Complete : ImeInterpretationKind.Partial,
                            end, new(0, end), input[end..], segments, evidence));
                        if (start == 0) expected.Add(signature);
                        else if (end == input.Length)
                        {
                            var missing = evidence.Where(e => e.Kind == ImeEvidenceKind.Phonetic).Sum(e => 2 - e.Range.Length);
                            var weight = segments.Sum(s => s.InputLength * Math.Log(1d +
                                (s.Text == "张" ? 1 : s.Text == "你你" ? 4 : s.CanonicalReading == "nin" ? 3 : 2)));
                            sentences.Add((signature, missing, weight, segments.Length));
                        }
                        if (end < input.Length) Walk(end, text + word.Text, segments, evidence);
                    }
                    void ReadSound(int index, int position, ImePathEvidence[] evidence)
                    {
                        if (index == word.Syllables.Length)
                        {
                            Finish(position, evidence);
                            if (placement != ImeShapePlacement.PerCharacter)
                                ReadShape(word.Text.Length == 1 ? "rx" : "rr", position, index - 1,
                                    ImeEvidenceKind.AfterWordShape, evidence, Finish);
                            return;
                        }
                        if (position == input.Length) return;
                        foreach (var sound in word.Sounds[index])
                        {
                            var available = Math.Min(2, input.Length - position);
                            var fragment = input.Substring(position, available);
                            if (casing != ImeShapeCasePolicy.LowercaseOnly && fragment.Any(char.IsUpper)) continue;
                            if (!sound.Key.StartsWith(fragment.ToLowerInvariant(), StringComparison.Ordinal)) continue;
                            ImePathEvidence[] phonetic = [.. evidence, new(ImeEvidenceKind.Phonetic, new(position, available), index,
                                fragment, word.Syllables[index], null)];
                            if (available < 2)
                            {
                                if (index == word.Syllables.Length - 1)
                                {
                                    if (length == ImeShapeLengthPolicy.RequiredTwo) Pending(ImeInterpretationKind.IncompleteShape, phonetic);
                                    else Finish(position + available, phonetic);
                                }
                                continue;
                            }
                            if (sound.Spoken != word.Syllables[index])
                                phonetic = [.. phonetic, new(ImeEvidenceKind.Fuzzy, new(position, 2), index, sound.Spoken, word.Syllables[index], null)];
                            if (length != ImeShapeLengthPolicy.RequiredTwo) ReadSound(index + 1, position + 2, phonetic);
                            if (placement != ImeShapePlacement.AfterWord)
                                ReadShape("rx", position + 2, index, ImeEvidenceKind.PerCharacterShape, phonetic,
                                    (end, ev) => ReadSound(index + 1, end, ev));
                        }
                    }
                    void ReadShape(string alias, int position, int index, ImeEvidenceKind kind, ImePathEvidence[] evidence,
                        Action<int, ImePathEvidence[]> next)
                    {
                        int[] sizes = length == ImeShapeLengthPolicy.OptionalOne ? [1] :
                            length == ImeShapeLengthPolicy.OptionalOneOrTwo ? [1, 2] : [2];
                        foreach (var size in sizes)
                        {
                            var available = Math.Min(size, input.Length - position);
                            var fragment = input.Substring(position, available);
                            if (casing == ImeShapeCasePolicy.UppercaseOnly && fragment.Any(char.IsLower) ||
                                casing == ImeShapeCasePolicy.LowercaseOnly && fragment.Any(char.IsUpper)) continue;
                            if (!alias.StartsWith(fragment.ToLowerInvariant(), StringComparison.Ordinal)) continue;
                            var ev = available == 0 ? evidence : [.. evidence, new ImePathEvidence(kind, new(position, available), index,
                                fragment, word.Reading, alias)];
                            if (available == size) next(position + size, ev);
                            else if (index == word.Syllables.Length - 1 && (available > 0 || length == ImeShapeLengthPolicy.RequiredTwo))
                                Pending(ImeInterpretationKind.IncompleteShape, ev);
                        }
                    }
                }
            }
        }
    }

    private static string Signature(string text, ImeInterpretation path) =>
        $"{text}|{path.Kind}|{path.CoverageEnd}|{path.ConsumedRange}|{path.RemainingInput}|{string.Join(';', path.Segments)}|{string.Join(';', path.Evidence)}";
}
