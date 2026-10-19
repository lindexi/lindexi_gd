using System.Diagnostics;
using System.Security.Cryptography;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(ProductionDictionaryCollection))]
public class LegacySentenceProductionTests(ProductionDictionaryFixture fixture)
{
    [Fact]
    public void NaturalCodeTailPrefixCoversInputBeforeLocalSelection()
    {
        var path = DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: "naturalCodeDoublePinyin", baseDirectory: fixture.HostRoot);
        var watch = Stopwatch.StartNew();
        var dictionary = DictionaryPackageLoader.Load(path);
        var load = watch.Elapsed;
        var context = new ImeContext(dictionary);
        watch.Restart();
        context.SetComposition("daz");
        var cold = watch.Elapsed;
        var candidates = context.Snapshot.Candidates.ToArray();
        watch.Restart();
        context.SetComposition("daz");
        var warm = watch.Elapsed;
        var local = Array.FindIndex(candidates, c => c.Text == "大" && c.Reading == "da");
        var evidence = $"load={load.TotalMilliseconds:F3}ms; cold={cold.TotalMilliseconds:F3}ms; warm={warm.TotalMilliseconds:F3}ms\n" +
            string.Join("\n", candidates.Select(c => $"{c.Text}|{c.Reading}|{c.Score}"));
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "natural-daz-performance.txt"), evidence);
        Assert.True(local > 0 && candidates.Take(local).Any(c => c.Reading.Split(' ').Length == 2), evidence);
        while (local >= context.Snapshot.CandidateWindow.PageStart + context.Snapshot.CandidateWindow.PageSize)
        {
            context.ProcessKey(ImeKey.NextCandidatePage());
        }
        var result = context.ProcessKey(ImeKey.SelectCandidate(local - context.Snapshot.CandidateWindow.PageStart));
        Assert.Equal(("大", "z"), (result.CommitText, result.Snapshot.Composition.Reading));
        Assert.True(cold < TimeSpan.FromSeconds(15) && warm < TimeSpan.FromSeconds(15), evidence);
    }

    [Theory]
    [InlineData("mz", "mei", "没有")]
    [InlineData("uohw", "shuo hua", "说话了")]
    [InlineData("m", "mei", "美国")]
    public void NaturalCodePackageExcludesUnenteredSyllables(string input, string reading, string excluded)
    {
        var path = DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: "naturalCodeDoublePinyin", baseDirectory: fixture.HostRoot);
        var context = new ImeContext(DictionaryPackageLoader.Load(path));
        var candidates = context.QueryConversionList(input);
        Assert.True(candidates.Any(c => c.Reading == reading) && candidates.All(c => c.Text != excluded &&
            (input != "mz" && input != "m" || !c.Reading.Contains(' '))),
            string.Join("|", candidates.Select(c => c.Text + ":" + c.Reading)));
    }

    [Theory]
    [InlineData("mz", false)]
    [InlineData("mz", true)]
    [InlineData("uohw", false)]
    [InlineData("uohw", true)]
    public void NaturalCodeColdAndWarmQueriesRetainOnlyEvidencedCandidates(string input, bool fuzzy)
    {
        var path = DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: "naturalCodeDoublePinyin", baseDirectory: fixture.HostRoot);
        var watch = Stopwatch.StartNew();
        var dictionary = DictionaryPackageLoader.Load(path);
        var load = watch.Elapsed;
        var context = new ImeContext(dictionary);
        watch.Restart();
        if (fuzzy) context = context.PrepareFuzzyRulesChange([ImeFuzzyRule.ZhZ]);
        var prepare = watch.Elapsed;
        watch.Restart();
        var cold = context.QueryConversionList(input).ToArray();
        var coldTime = watch.Elapsed;
        watch.Restart();
        var warm = context.QueryConversionList(input).ToArray();
        var warmTime = watch.Elapsed;
        var evidence = $"input={input}; fuzzy={fuzzy}; entries={dictionary.GetLexicon().Entries.Count}; manifestSHA256={Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(path, "manifest.json"))))}\n" +
            $"load={load.TotalMilliseconds:F3}ms; prepare={prepare.TotalMilliseconds:F3}ms; cold={coldTime.TotalMilliseconds:F3}ms; warm={warmTime.TotalMilliseconds:F3}ms\n" +
            string.Join("\n", cold.Select(c => $"{c.Text}|{c.Reading}|{c.Score}"));
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, $"prediction-performance-{input}-{fuzzy}.txt"), evidence);
        Assert.Equal(cold, warm);
        Assert.DoesNotContain(cold, c => input == "mz" ? c.Reading.Contains(' ') : c.Reading.Split(' ').Length > 2);
        Assert.Contains(cold, c => c.Reading == (input == "mz" ? "mei" : "shuo hua"));
        Assert.True(coldTime < TimeSpan.FromSeconds(15) && warmTime < TimeSpan.FromSeconds(15), evidence);
    }

    [Theory]
    [InlineData("bicu", "bi", "cu")]
    [InlineData("be", "be", "")]
    public void FullPackageLocalSelectionConsumesOnlyEvidencedPrefix(string input, string consumed, string remaining)
    {
        var path = DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: "fullPinyin", baseDirectory: fixture.HostRoot);
        var watch = Stopwatch.StartNew();
        var dictionary = DictionaryPackageLoader.Load(path);
        var load = watch.Elapsed;
        var learned = new List<ImeDictionaryLearning>();
        var context = new ImeContext(dictionary, learned.Add);
        watch.Restart();
        context.SetComposition(input);
        var cold = watch.Elapsed;
        var top = context.Snapshot.Candidates.ToArray();
        watch.Restart();
        context.SetComposition(input);
        var warm = watch.Elapsed;
        var index = top.ToList().FindIndex(c => c.Reading == (input == "bicu" ? "bi" : "bei"));
        Assert.True(index >= 0, string.Join("|", top.Select(c => c.Text + ":" + c.Reading)));
        var selected = context.ProcessKey(ImeKey.SelectCandidate(index));
        var evidence = $"input={input}; manifestSHA256={Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(path, "manifest.json"))))}\n" +
            $"load={load.TotalMilliseconds:F3}ms; cold={cold.TotalMilliseconds:F3}ms; warm={warm.TotalMilliseconds:F3}ms\n" +
            string.Join("\n", top.Take(12).Select((c, i) => $"{i + 1}:{c.Text}|{c.Reading}|{c.Score}")) +
            $"\nselected={index + 1}; commit={selected.CommitText}; remaining={selected.Snapshot.Composition.Reading}; learned={learned.Count}; consumed={learned[0].OriginalInput}; canonical={learned[0].Candidate.Reading}\n" +
            "remainingTop=" + string.Join("|", selected.Snapshot.Candidates.Take(9).Select(c => c.Text + ":" + c.Reading));
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, $"legacy-sentence-production-{input}.txt"), evidence);
        Assert.Equal((remaining, 1, consumed), (selected.Snapshot.Composition.Reading, learned.Count, learned[0].OriginalInput));
        Assert.True(cold < TimeSpan.FromSeconds(15), evidence);
    }
}
