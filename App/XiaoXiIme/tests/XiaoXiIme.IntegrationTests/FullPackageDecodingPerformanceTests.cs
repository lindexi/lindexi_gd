using System.Diagnostics;
using System.Security.Cryptography;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using Xunit.Abstractions;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(ProductionDictionaryCollection))]
public class FullPackageDecodingPerformanceTests(ProductionDictionaryFixture fixture, ITestOutputHelper output)
{
    [Theory]
    [InlineData("fullPinyin", "niRXhaoNZ")]
    [InlineData("xiaoheDoublePinyin", "niRXhcNZ")]
    public void CompleteProductionPackageDecodesRealWordWithinRegressionBudget(string scheme, string input)
    {
        var path = DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: scheme, baseDirectory: fixture.HostRoot);
        var watch = Stopwatch.StartNew();
        var dictionary = DictionaryPackageLoader.Load(path);
        var load = watch.Elapsed;
        watch.Restart();
        var lexicon = dictionary.GetLexicon();
        var canonical = watch.Elapsed;
        watch.Restart();
        var context = new ImeContext(dictionary, new ImeInputOptions
        {
            CasePolicy = ImeShapeCasePolicy.UppercaseOnly,
            Placement = ImeShapePlacement.Both,
            LengthPolicy = ImeShapeLengthPolicy.OptionalOneOrTwo,
        }, null);
        var construct = watch.Elapsed;
        var identity = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(path, "manifest.json"))));
        var build = scheme == "fullPinyin" ? fixture.FullPinyinBuildTime : fixture.XiaoheBuildTime;
        var metadata = $"build={build.TotalMilliseconds:F1}ms; scheme={scheme}; input={input}; manifestSHA256={identity}; entries={lexicon.Entries.Count}; shapes={lexicon.ShapeEntries.Count}; load={load.TotalMilliseconds:F1}ms; canonical={canonical.TotalMilliseconds:F1}ms; context={construct.TotalMilliseconds:F1}ms";
        output.WriteLine(metadata);
        watch.Restart();
        var cold = context.QueryInterpretations(input);
        var coldTime = watch.Elapsed;
        var coldMetrics = ReadMetrics(context);
        watch.Restart();
        var warm = context.QueryInterpretations(input);
        var warmTime = watch.Elapsed;
        var warmMetrics = ReadMetrics(context);
        var measurement = $"{metadata}\ncold={coldTime.TotalMilliseconds:F1}ms; warm={warmTime.TotalMilliseconds:F1}ms; total={cold.TotalCount}; returned={cold.Candidates.Count}\ncoldMetrics={coldMetrics}\nwarmMetrics={warmMetrics}";
        output.WriteLine(measurement);
        var evidencePath = Path.Combine(AppContext.BaseDirectory, $"full-package-performance-{scheme}.txt");
        File.WriteAllText(evidencePath, measurement + "\n" + string.Join('\n', cold.Candidates.Where(c => c.Candidate.Text == "你好").Select(c =>
            $"{c.Candidate.Text}|{c.Candidate.Reading}|{string.Join(';', c.Interpretations.Select(p => $"{p.Kind}:{p.CoverageEnd}:segments={p.Segments.Count}:shape={p.Evidence.Count(e => e.Kind == ImeEvidenceKind.PerCharacterShape)}"))}")));
        Assert.Contains(cold.Candidates, c => c.Candidate.Text == "你好" && c.Candidate.Reading == "ni hao" &&
            c.Interpretations.Any(p => p.Kind == ImeInterpretationKind.Complete && p.CoverageEnd == input.Length &&
                p.Evidence.Count(e => e.Kind == ImeEvidenceKind.PerCharacterShape) == 2));
        Assert.Equal(cold.TotalCount, warm.TotalCount);
        // This elapsed assertion runs after synchronous decoding; it cannot cancel or terminate it.
        Assert.True(coldTime < TimeSpan.FromSeconds(15), measurement);
    }

    [Theory]
    [InlineData("fullPinyin", "niRXhaoNZ")]
    [InlineData("xiaoheDoublePinyin", "niRXhcNZ")]
    public void CompleteProductionPackageColdAndWarmPagesHaveIdenticalOrderedEvidence(string scheme, string input)
    {
        var dictionary = DictionaryPackageLoader.Load(DictionaryPackageLocations.ResolvePackageDirectory(
            inputScheme: scheme, baseDirectory: fixture.HostRoot));
        var options = new ImeInputOptions
        {
            CasePolicy = ImeShapeCasePolicy.UppercaseOnly,
            Placement = ImeShapePlacement.Both,
            LengthPolicy = ImeShapeLengthPolicy.OptionalOneOrTwo,
        };
        var warm = new ImeContext(dictionary, options, null);
        var total = warm.QueryInterpretations(input).TotalCount;
        var signatures = new List<string>();
        for (var offset = 0; offset < total; offset += 100)
        {
            var cold = new ImeContext(dictionary, options, null).QueryInterpretations(input, offset);
            var page = warm.QueryInterpretations(input, offset);
            Assert.Equal(total, cold.TotalCount);
            Assert.Equal(total, page.TotalCount);
            Assert.Equal(Math.Min(100, total - offset), page.Candidates.Count);
            Assert.Equal(cold.Candidates.Select(Signature), page.Candidates.Select(Signature));
            signatures.AddRange(page.Candidates.Select(Signature));
        }
        Assert.Empty(warm.QueryInterpretations(input, total).Candidates);
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join('\n', signatures))));
        var evidence = $"scheme={scheme}; input={input}; total={total}; orderedEvidenceSHA256={hash}";
        output.WriteLine(evidence);
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, $"full-package-pages-{scheme}.txt"), evidence);
    }

    private static string Signature(ImeConversionCandidate candidate) =>
        System.Text.Json.JsonSerializer.Serialize(new { candidate.Candidate, candidate.Rank, candidate.Interpretations });

    private static string ReadMetrics(ImeContext context)
    {
        var field = typeof(ImeContext).GetField("_lastDecodeMetrics",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        var metrics = Assert.IsType<string>(field.GetValue(context));
        Assert.False(string.IsNullOrWhiteSpace(metrics));
        Assert.Contains("candidates=", metrics);
        return metrics;
    }
}
