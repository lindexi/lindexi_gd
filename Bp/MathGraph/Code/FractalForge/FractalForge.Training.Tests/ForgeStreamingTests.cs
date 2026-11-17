using System.Text.Json;
using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Training.Tests;

[TestClass]
public class ForgeStreamingTests
{
    private static async Task<(ForgeCorpus Corpus, MiniMindTextTokenizer Tokenizer)> CreateAsync(string text)
    {
        var directory = Path.Combine(Path.GetTempPath(), "forge-stream", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Console.WriteLine(directory);
        var path = Path.Combine(directory, "data.jsonl");
        await File.WriteAllTextAsync(path, text);
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json"),
            Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json"));
        return (new(path, "stream-v1", ForgeArchive.Fingerprint(await File.ReadAllBytesAsync(path))), tokenizer);
    }

    [TestMethod]
    public async Task ResumedStreamMatchesCompleteSampleSuffix()
    {
        var f = await CreateAsync("{\"text\":\"hello\"}\n{\"text\":\"world\"}\n{\"text\":\"hello world\"}");
        var limits = new JsonlDatasetOptions(4096, 16384, 10, 8, 80);
        var all = new List<string>();
        await foreach (var sample in ForgeJsonlStream.ReadAsync(f.Corpus, f.Tokenizer, limits, 0, default)) all.Add(string.Join(',', sample));
        var resumed = new List<string>();
        await foreach (var sample in ForgeJsonlStream.ReadAsync(f.Corpus, f.Tokenizer, limits, 1, default)) resumed.Add(string.Join(',', sample));
        CollectionAssert.AreEqual(all.Skip(1).ToArray(), resumed.ToArray());
    }

    [TestMethod]
    public async Task StreamDoesNotParseUnusedMalformedTail()
    {
        var f = await CreateAsync("{\"text\":\"hello\"}\nnot-json\n");
        await using var reader = ForgeJsonlStream.ReadAsync(f.Corpus, f.Tokenizer, new(4096, 16384, 10, 8, 80), 0, default).GetAsyncEnumerator();
        Assert.IsTrue(await reader.MoveNextAsync());
    }

    [TestMethod]
    public async Task ChangedContentIsRejectedBeforeFirstSample()
    {
        var f = await CreateAsync("{\"text\":\"hello\"}\n");
        await File.AppendAllTextAsync(f.Corpus.Path, " ");
        await using var reader = ForgeJsonlStream.ReadAsync(f.Corpus, f.Tokenizer, new(4096, 16384, 10, 8, 80), 0, default).GetAsyncEnumerator();
        await Assert.ThrowsExceptionAsync<InvalidDataException>(async () => { await reader.MoveNextAsync(); });
    }

    [TestMethod]
    public async Task SkippedSamplesStillConsumeTokenBudget()
    {
        var f = await CreateAsync("{\"text\":\"hello world\"}\n{\"text\":\"hello world\"}\n");
        await using var reader = ForgeJsonlStream.ReadAsync(f.Corpus, f.Tokenizer, new(4096, 16384, 10, 8, 2), 1, default).GetAsyncEnumerator();
        await Assert.ThrowsExceptionAsync<InvalidDataException>(async () => { await reader.MoveNextAsync(); });
    }

    [TestMethod]
    public async Task WrongContentRoleIsRejectedBeforeTraining()
    {
        var f = await CreateAsync("{\"text\":\"hello\"}\n");
        var corpus = f.Corpus with { ContentRole = (ForgeCorpusPreparation.Role("hello") + 1) % 4 };
        await using var reader = ForgeJsonlStream.ReadAsync(corpus, f.Tokenizer, new(4096, 16384, 10, 8, 80), 0, default).GetAsyncEnumerator();
        await Assert.ThrowsExceptionAsync<InvalidDataException>(async () => { await reader.MoveNextAsync(); });
    }

    [TestMethod]
    public void EscapedJsonHasSameDecodedContentRole()
    {
        using var document = JsonDocument.Parse("{\"text\":\"h\\u0065llo\"}");
        Assert.AreEqual(ForgeCorpusPreparation.Role("hello"), ForgeCorpusPreparation.Role(document.RootElement.GetProperty("text").GetString() ?? ""));
    }

    [TestMethod]
    public async Task PreparationSeparatesRepeatedDecodedContentAcrossFourRoles()
    {
        var rows = Enumerable.Range(0, 128).SelectMany(i => new[] { JsonSerializer.Serialize(new { text = "sample " + i }), JsonSerializer.Serialize(new { text = "sample " + i }) });
        var f = await CreateAsync(string.Join('\n', rows) + "\n");
        var prepared = await ForgeCorpusPreparation.PreparePrefixAsync(f.Corpus.Path, f.Corpus.Path + ".split", 256);
        var contents = new List<string[]>();
        foreach (var corpus in new[] { prepared.Training, prepared.Proposal, prepared.Acceptance, prepared.FinalTest })
            contents.Add((await File.ReadAllLinesAsync(corpus.Path)).Select(s => JsonDocument.Parse(s).RootElement.GetProperty("text").GetString() ?? "").Distinct().ToArray());
        Assert.AreEqual(contents.Sum(c => c.Length), contents.SelectMany(c => c).Distinct().Count());
    }
}
