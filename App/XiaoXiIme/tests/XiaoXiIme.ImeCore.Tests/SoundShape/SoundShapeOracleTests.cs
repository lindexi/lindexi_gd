using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class SoundShapeOracleTests
{
    [Fact]
    public void ShortInputsMatchIndependentCompletePathEnumeration()
    {
        var lexicon = new ImeLexicon
        (
            [new("你", "ni", 2), new("好", "hao", 1), new("你好", "ni hao", 3)], [],
            new("oracle", [new("n", "i", "ni"), new("h", "ao", "ha")]), 1
        );
        var context = new ImeContext(new InMemoryImeDictionary(lexicon), Options(), null);
        var differences = new List<string>();
        foreach (var input in Inputs(4))
        {
            var expected = Enumerate(input).Distinct().Order(StringComparer.Ordinal).ToArray();
            var actual = context.QueryInterpretations(input).Candidates
                .Where(c => c.Interpretations.Any(p => p.Kind == ImeInterpretationKind.Complete))
                .Select(c => c.Candidate.Text).Distinct().Order(StringComparer.Ordinal).ToArray();
            if (!expected.SequenceEqual(actual))
            {
                differences.Add(input);
            }
        }

        Assert.Empty(differences);
    }

    [Fact]
    public void ExternalLearningInvalidatesPreviouslyIssuedSelection()
    {
        var dictionary = new UserDictionary
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    [new("你", "ni", 1)], [], ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );
        var context = new ImeContext(dictionary, Options(), null);
        context.ProcessKey(new(ImeKeyKind.Character, 'n'));
        context.ProcessKey(new(ImeKeyKind.Character, 'i'));
        var id = context.QueryInterpretations("ni").Candidates.Single
            (c => c.SelectionId.HasValue).SelectionId.GetValueOrDefault();
        dictionary.Learn
        (
            new(new ImeCandidate("你", "ni", 1), "ni")
                { Segments = [new("你", "ni", 0, 2, 0, 1)] }
        );
        Assert.False(context.SelectCandidate(id).Handled);
    }

    [Fact]
    public void FiniteAlphabetMatchesIndependentELQHSegmentsAndEvidence()
    {
        var lexicon = new ImeLexicon
        (
            [new("你", "ni", 1)], [new("你", "rx", "oracle")],
            new("oracle", [new("n", "i", "ni")]), 1
        );
        var context = new ImeContext
        (
            new InMemoryImeDictionary(lexicon), Options() with
            {
                LengthPolicy = ImeShapeLengthPolicy.OptionalTwo
            }, null
        );
        var differences = new List<string>();
        foreach (var input in OracleInputs(4))
        {
            var expected = new List<string>();
            EnumeratePaths(0, "", [], []);
            var actual = context.QueryInterpretations(input).Candidates.SelectMany
            (c => c.Interpretations.Select
                (p =>
                    Signature(c.Candidate.Text, p)
                )
            ).Order(StringComparer.Ordinal).ToArray();
            if (!expected.Order(StringComparer.Ordinal).SequenceEqual(actual))
            {
                differences.Add(input);
            }

            void EnumeratePaths(int start, string text, ImeConfirmedSegment[] segments, ImePathEvidence[] evidence)
            {
                var suffix = input[start..];
                if (suffix == "n")
                {
                    Finish(start + 1, [.. evidence, new(ImeEvidenceKind.Phonetic, new(start, 1), 0, "n", "ni", null)]);
                }

                if (!suffix.StartsWith("ni", StringComparison.Ordinal))
                {
                    return;
                }

                ImePathEvidence[] phonetic =
                    [.. evidence, new(ImeEvidenceKind.Phonetic, new(start, 2), 0, "ni", "ni", null)];
                Finish(start + 2, phonetic);
                if (suffix == "niR")
                {
                    Pending
                    (
                        ImeInterpretationKind.IncompleteShape,
                        [.. phonetic, new(ImeEvidenceKind.PerCharacterShape, new(start + 2, 1), 0, "R", "ni", "rx")]
                    );
                }

                if (suffix.StartsWith("niRX", StringComparison.Ordinal))
                {
                    Finish
                    (
                        start + 4,
                        [.. phonetic, new(ImeEvidenceKind.PerCharacterShape, new(start + 2, 2), 0, "RX", "ni", "rx")]
                    );
                }

                void Pending(ImeInterpretationKind kind, ImePathEvidence[] ev)
                {
                    if (start == 0)
                    {
                        expected.Add(Signature(text + "你", new(kind, input.Length, null, "", segments, ev)));
                    }
                }

                void Finish(int end, ImePathEvidence[] ev)
                {
                    ImeConfirmedSegment[] confirmed = [.. segments, new("你", "ni", start, end - start, text.Length, 1)];
                    if (start == 0 || end == input.Length)
                    {
                        expected.Add
                        (
                            Signature
                            (
                                text + "你", new
                                (
                                    end == input.Length
                                        ? ImeInterpretationKind.Complete
                                        : ImeInterpretationKind.Partial,
                                    end, new(0, end), input[end..], confirmed, ev
                                )
                            )
                        );
                    }

                    if (end < input.Length)
                    {
                        EnumeratePaths(end, text + "你", confirmed, ev);
                    }
                }
            }
        }

        Assert.Empty(differences);
    }

    private static string Signature(string text, ImeInterpretation path) =>
        $"{text}|{path.Kind}|{path.CoverageEnd}|{path.ConsumedRange}|{path.RemainingInput}|{string.Join(';', path.Segments)}|{string.Join(';', path.Evidence)}";

    private static IEnumerable<string> OracleInputs(int remaining)
    {
        yield return "";
        if (remaining == 0)
        {
            yield break;
        }

        foreach (var suffix in OracleInputs(remaining - 1))
        foreach (var character in "niRX")
        {
            yield return character + suffix;
        }
    }

    private static ImeInputOptions Options() => new()
    {
        CasePolicy = ImeShapeCasePolicy.UppercaseOnly,
        Placement = ImeShapePlacement.PerCharacter,
        LengthPolicy = ImeShapeLengthPolicy.OptionalOne,
    };

    private static IEnumerable<string> Inputs(int remaining)
    {
        yield return "";
        if (remaining == 0)
        {
            yield break;
        }

        foreach (var suffix in Inputs(remaining - 1))
        foreach (var character in "niha")
        {
            yield return character + suffix;
        }
    }

    private static IEnumerable<string> Enumerate(string input)
    {
        if (input.Length == 0)
        {
            yield break;
        }

        foreach (var (alias, text) in new[]
                     { ("n", "你"), ("ni", "你"), ("h", "好"), ("ha", "好"), ("nih", "你好"), ("niha", "你好") })
        {
            if (input == alias)
            {
                yield return text;
            }
            else if (alias.Length >= 2 && input.StartsWith(alias, StringComparison.Ordinal))
            {
                foreach (var suffix in Enumerate(input[alias.Length..]))
                {
                    yield return text + suffix;
                }
            }
        }
    }
}