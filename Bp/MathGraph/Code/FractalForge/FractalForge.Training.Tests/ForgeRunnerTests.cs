using System.Text.Json;
using FractalForge.Core;
using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Training.Tests;

[TestClass]
public class ForgeRunnerTests
{
    private sealed record Fixture(string ConfigPath, string ManifestPath, ForgeRunConfiguration Configuration, MiniMindTextTokenizer Tokenizer);

    private static async Task<Fixture> CreateAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), "forge-runner-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var tokenizerPath = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json");
        var tokenizerConfig = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json");
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(tokenizerPath, tokenizerConfig);
        var model = ForgeModel.Create(ForgeTopology.CreateDiamond(7, feedback: false));
        model.Network.BindText(new TokenBindings(tokenizer.Identity, ["I"], ["O"],
            Enumerable.Range(0, tokenizer.VocabularySize).Select(i => (IReadOnlyList<double>)new double[] { (double)i / tokenizer.VocabularySize }).ToArray(), seed: 4));
        var source = ForgeArchive.Export(model);
        var sourcePath = Path.Combine(directory, "source.forge");
        await File.WriteAllBytesAsync(sourcePath, source);
        async Task<ForgeCorpus> Corpus(string name, string content)
        {
            var path = Path.Combine(directory, name + ".jsonl");
            await File.WriteAllTextAsync(path, content);
            return new(path, name + "-v1", ForgeArchive.Fingerprint(await File.ReadAllBytesAsync(path)));
        }
        var config = new ForgeRunConfiguration
        {
            Semantics = ForgeRunner.Semantics, Direction = "Feedback", RunId = "test", OutputRoot = directory,
            SourceArchive = sourcePath, SourceSha256 = ForgeArchive.Fingerprint(source),
            TokenizerPath = tokenizerPath, TokenizerConfigPath = tokenizerConfig, TokenizerIdentity = tokenizer.Identity,
            Training = await Corpus("train", "{\"text\":\"hello\"}\n{\"text\":\"world\"}\n"),
            Acceptance = await Corpus("accept", "{\"text\":\"hello world\"}\n"),
            FinalTest = new(Path.Combine(directory, "never-open-final.jsonl"), "final-v1", new string('A', 64)),
            DataLimits = new(4096, 16384, 10, 8, 80), Action = new("add-feedback", "O", Destination: "A", Delay: 1, Weight: .01),
            RuleVersion = "fixed-endpoint-v1", MaxUpdates = 2, MaxTokens = 30, MaxPairTapeCells = 10000000,
            MinimumGain = 100, Optimizer = new(), RandomState = 17
        };
        var configPath = Path.Combine(directory, "config.json");
        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config));
        return new(configPath, Path.Combine(directory, "Feedback", "test", "run.json"), config, tokenizer);
    }

    private static Task<int> RunAsync(Fixture fixture, params string[] arguments) =>
        ForgeRunner.RunAsync("Feedback", arguments, TextWriter.Null, TextWriter.Null);

    private static PairedTrial Load(Fixture fixture)
    {
        using var json = JsonDocument.Parse(File.ReadAllBytes(fixture.ManifestPath));
        var path = Path.Combine(Path.GetDirectoryName(fixture.ManifestPath)!, json.RootElement.GetProperty("TrialFile").GetString()!);
        var c = fixture.Configuration;
        return PairedTrial.Load(path, new(c.Direction + "/" + c.RunId, c.Training.Identity, c.Acceptance.Identity,
            c.RuleVersion, c.MaxUpdates, c.MaxTokens, c.MaxPairTapeCells, c.MinimumGain, c.Optimizer));
    }

    [TestMethod]
    public async Task ResumeMatchesUninterruptedContinuation()
    {
        var f = await CreateAsync();
        Assert.AreEqual(0, await RunAsync(f, "train", f.ConfigPath, "1"));
        var expected = Load(f);
        await using var stream = File.OpenRead(f.Configuration.Training.Path);
        var dataset = await PretrainingJsonl.LoadAsync(stream, f.Tokenizer, f.Configuration.DataLimits);
        expected.Advance(dataset[1], 1, f.Configuration.RandomState);
        Assert.AreEqual(0, await RunAsync(f, "resume", f.ConfigPath, f.ManifestPath));
        var actual = Load(f);
        CollectionAssert.AreEqual(expected.Keep.Network.ExportCheckpoint(), actual.Keep.Network.ExportCheckpoint());
        CollectionAssert.AreEqual(expected.Candidate.Network.ExportCheckpoint(), actual.Candidate.Network.ExportCheckpoint());
        Assert.AreEqual(expected.Progress, actual.Progress);
    }

    [TestMethod]
    public async Task TrainingCreatesRealDelayedConnection()
    {
        var f = await CreateAsync();
        Assert.AreEqual(0, await RunAsync(f, "train", f.ConfigPath));
        var trial = Load(f);
        Assert.AreEqual(trial.Keep.MeasureCost().Connections + 1, trial.Candidate.MeasureCost().Connections);
    }

    [TestMethod]
    public async Task EvaluateAndGenerateDoNotOpenFinalTest()
    {
        var f = await CreateAsync();
        Assert.AreEqual(0, await RunAsync(f, "train", f.ConfigPath));
        Assert.AreEqual(0, await RunAsync(f, "evaluate", f.ConfigPath, f.ManifestPath));
        Assert.AreEqual(0, await RunAsync(f, "inspect", f.ConfigPath, f.ManifestPath));
        Assert.AreEqual(0, await RunAsync(f, "generate", f.ConfigPath, f.ManifestPath, "hello", "1", "7"));
    }

    [TestMethod]
    public async Task WrongDirectionIsRejectedBeforeCreatingRun()
    {
        var f = await CreateAsync();
        Assert.AreNotEqual(0, await ForgeRunner.RunAsync("Widening", ["train", f.ConfigPath], TextWriter.Null, TextWriter.Null));
        Assert.IsFalse(File.Exists(f.ManifestPath));
    }

    [TestMethod]
    public async Task ChangedConfigurationCannotResume()
    {
        var f = await CreateAsync();
        Assert.AreEqual(0, await RunAsync(f, "train", f.ConfigPath, "1"));
        await File.WriteAllTextAsync(f.ConfigPath, JsonSerializer.Serialize(f.Configuration with { RandomState = 18 }));
        Assert.AreNotEqual(0, await RunAsync(f, "resume", f.ConfigPath, f.ManifestPath));
    }

    [TestMethod]
    public async Task AcceptanceCannotAliasTraining()
    {
        var f = await CreateAsync();
        await File.WriteAllTextAsync(f.ConfigPath, JsonSerializer.Serialize(f.Configuration with { Acceptance = f.Configuration.Training }));
        Assert.AreNotEqual(0, await RunAsync(f, "train", f.ConfigPath));
    }

    [TestMethod]
    public async Task CorruptedCorpusIsRejected()
    {
        var f = await CreateAsync();
        await File.AppendAllTextAsync(f.Configuration.Training.Path, "invalid\n");
        Assert.AreNotEqual(0, await RunAsync(f, "train", f.ConfigPath));
    }

    [TestMethod]
    public async Task MalformedJsonlDoesNotCommitAnUpdate()
    {
        var f = await CreateAsync();
        await File.WriteAllTextAsync(f.Configuration.Training.Path, "{\"text\":42}\n");
        var corpus = f.Configuration.Training with { Sha256 = ForgeArchive.Fingerprint(await File.ReadAllBytesAsync(f.Configuration.Training.Path)) };
        await File.WriteAllTextAsync(f.ConfigPath, JsonSerializer.Serialize(f.Configuration with { Training = corpus }));
        Assert.AreNotEqual(0, await RunAsync(f, "train", f.ConfigPath));
        Assert.AreEqual(0, Load(f).Progress.Updates);
    }

    [TestMethod]
    public async Task AnotherRunCannotReadThisManifest()
    {
        var f = await CreateAsync();
        Assert.AreEqual(0, await RunAsync(f, "train", f.ConfigPath, "1"));
        await File.WriteAllTextAsync(f.ConfigPath, JsonSerializer.Serialize(f.Configuration with { RunId = "other" }));
        Assert.AreNotEqual(0, await RunAsync(f, "resume", f.ConfigPath, f.ManifestPath));
    }

    [TestMethod]
    public async Task UnknownConfigurationFieldIsRejected()
    {
        var f = await CreateAsync();
        var json = JsonSerializer.Serialize(f.Configuration);
        await File.WriteAllTextAsync(f.ConfigPath, json[..^1] + ",\"Unknown\":true}");
        Assert.AreNotEqual(0, await RunAsync(f, "train", f.ConfigPath));
    }

    [TestMethod]
    public async Task DuplicateConfigurationFieldIsRejected()
    {
        var f = await CreateAsync();
        var json = JsonSerializer.Serialize(f.Configuration);
        await File.WriteAllTextAsync(f.ConfigPath, json[..^1] + ",\"RunId\":\"other\"}");
        Assert.AreNotEqual(0, await RunAsync(f, "train", f.ConfigPath));
    }

    [TestMethod]
    public async Task ChangedSourceCannotResume()
    {
        var f = await CreateAsync();
        Assert.AreEqual(0, await RunAsync(f, "train", f.ConfigPath, "1"));
        await File.AppendAllTextAsync(f.Configuration.SourceArchive, " ");
        Assert.AreNotEqual(0, await RunAsync(f, "resume", f.ConfigPath, f.ManifestPath));
    }

    [TestMethod]
    public async Task StreamedEvaluationMatchesExistingEvaluation()
    {
        var f = await CreateAsync();
        Assert.AreEqual(0, await RunAsync(f, "train", f.ConfigPath));
        var trial = Load(f);
        var expected = Load(f);
        await using var stream = File.OpenRead(f.Configuration.Acceptance.Path);
        var dataset = await PretrainingJsonl.LoadAsync(stream, f.Tokenizer, f.Configuration.DataLimits);
        var samples = new IReadOnlyList<int>[] { dataset[0] };
        var loss = expected.Evaluate(f.Configuration.Acceptance.Identity, samples);
        Assert.AreEqual(loss, await trial.EvaluateAsync(f.Configuration.Acceptance.Identity, Samples()));
        async IAsyncEnumerable<IReadOnlyList<int>> Samples()
        {
            await Task.Yield();
            yield return dataset[0];
        }
    }

    [TestMethod]
    public async Task CancelledStreamedEvaluationDoesNotPublishResult()
    {
        var f = await CreateAsync();
        Assert.AreEqual(0, await RunAsync(f, "train", f.ConfigPath));
        var trial = Load(f);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => trial.EvaluateAsync(f.Configuration.Acceptance.Identity, Samples(), cancellation.Token));
        Assert.IsNull(trial.Evaluation);
        async IAsyncEnumerable<IReadOnlyList<int>> Samples()
        {
            await Task.Yield();
            yield return new[] { 0, 1 };
        }
    }

    [TestMethod]
    public async Task CancellationReturns130()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.AreEqual(130, await ForgeRunner.RunAsync("Feedback", ["train", "missing.json"], TextWriter.Null, TextWriter.Null, cancellation.Token));
    }

    [TestMethod]
    public async Task UnknownCommandReturnsNonzero()
    {
        Assert.AreEqual(2, await ForgeRunner.RunAsync("Feedback", ["unknown", "missing.json"], TextWriter.Null, TextWriter.Null));
    }
}
