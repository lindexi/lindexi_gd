using System.Text.Json;
using FractalForge.Training;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Research.Feedback.Tests;

[TestClass]
public class FeedbackRunnerTests
{
    public TestContext TestContext { get; set; } = null!;
    private sealed record Fixture(string Config, string Manifest, ForgeRunConfiguration Configuration);

    private async Task<Fixture> PrepareAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), "feedback-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        TestContext.WriteLine(directory);
        async Task<ForgeCorpus> CorpusAsync(string name, string text)
        {
            var path = Path.Combine(directory, name + ".jsonl");
            await File.WriteAllTextAsync(path, text);
            return new(path, name, ForgeArchive.Fingerprint(await File.ReadAllBytesAsync(path)));
        }
        var template = new ForgeRunConfiguration
        {
            Semantics = ForgeRunner.Semantics, Direction = "Feedback", RunId = "short", OutputRoot = directory,
            SourceArchive = "prepared-by-entry", SourceSha256 = new string('A', 64),
            TokenizerPath = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json"),
            TokenizerConfigPath = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json"), TokenizerIdentity = "prepared-by-entry",
            Training = await CorpusAsync("train", "{\"text\":\"hello\"}\n{\"text\":\"world\"}\n"),
            Acceptance = await CorpusAsync("accept", "{\"text\":\"hello world\"}\n"),
            FinalTest = new(Path.Combine(directory, "never-open.jsonl"), "final", new string('B', 64)),
            DataLimits = new(4096, 16384, 10, 8, 80),
            Action = FeedbackExperiment.CreateAction(FeedbackTopology.Local, 2, .01), RuleVersion = "feedback-short-v1",
            MaxUpdates = 2, MaxTokens = 30, MaxPairTapeCells = 10000000, MinimumGain = 100, Optimizer = new(), RandomState = 17
        };
        var templatePath = Path.Combine(directory, "template.json");
        var config = Path.Combine(directory, "config.json");
        await File.WriteAllTextAsync(templatePath, JsonSerializer.Serialize(template));
        Assert.AreEqual(0, await RunAsync("prepare", templatePath, config, "7", "Local", "2", "0.01", "F1"));
        return new(config, Path.Combine(directory, "Feedback", "short", "run.json"), await ForgeRunConfiguration.LoadAsync(config));
    }

    private static Task<int> RunAsync(params string[] arguments) => FeedbackRunner.RunAsync(arguments, TextWriter.Null, TextWriter.Null);

    private static PairedTrial Load(Fixture fixture)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(fixture.Manifest));
        var file = manifest.RootElement.GetProperty("TrialFile").GetString() ?? throw new InvalidDataException();
        var c = fixture.Configuration;
        return PairedTrial.Load(Path.Combine(Path.GetDirectoryName(fixture.Manifest) ?? throw new InvalidDataException(), file),
            new(c.Direction + "/" + c.RunId, c.Training.Identity, c.Acceptance.Identity, c.RuleVersion,
                c.MaxUpdates, c.MaxTokens, c.MaxPairTapeCells, c.MinimumGain, c.Optimizer));
    }

    [TestMethod]
    public async Task PreparedBaselineRetainsOriginalFeedback()
    {
        var fixture = await PrepareAsync();
        var model = ForgeArchive.Import(await File.ReadAllBytesAsync(fixture.Configuration.SourceArchive));
        Assert.IsTrue(model.Topology.Expand().Any(e => e.From == "O" && e.To == "I" && e.Delay == 1));
    }

    [TestMethod]
    public async Task EntryTrainsResumesInspectsEvaluatesAndGeneratesWithoutFinalTest()
    {
        var f = await PrepareAsync();
        Assert.AreEqual(0, await RunAsync("train", f.Config, "1"));
        Assert.AreEqual(0, await RunAsync("resume", f.Config, f.Manifest));
        Assert.AreEqual(0, await RunAsync("inspect", f.Config, f.Manifest));
        Assert.AreEqual(0, await RunAsync("evaluate", f.Config, f.Manifest));
        Assert.AreEqual(0, await RunAsync("generate", f.Config, f.Manifest, "hello", "1", "7"));
    }

    [TestMethod]
    public async Task ResumeConsumesExactlySameWindow()
    {
        var f = await PrepareAsync();
        Assert.AreEqual(0, await RunAsync("train", f.Config, "1"));
        var expected = Load(f);
        var tokenizer = await MathGraphs.Neural.MiniMindTextTokenizer.LoadAsync(f.Configuration.TokenizerPath, f.Configuration.TokenizerConfigPath);
        await using var stream = File.OpenRead(f.Configuration.Training.Path);
        var data = await MathGraphs.Neural.PretrainingJsonl.LoadAsync(stream, tokenizer, f.Configuration.DataLimits);
        expected.Advance(data[1], 1, f.Configuration.RandomState);
        Assert.AreEqual(0, await RunAsync("resume", f.Config, f.Manifest));
        CollectionAssert.AreEqual(expected.Candidate.Network.ExportCheckpoint(), Load(f).Candidate.Network.ExportCheckpoint());
    }

    [TestMethod]
    public async Task ConfigurationChangeCannotResume()
    {
        var f = await PrepareAsync();
        Assert.AreEqual(0, await RunAsync("train", f.Config, "1"));
        await File.WriteAllTextAsync(f.Config, JsonSerializer.Serialize(f.Configuration with { RandomState = 18 }));
        Assert.AreNotEqual(0, await RunAsync("resume", f.Config, f.Manifest));
    }

    [TestMethod]
    public async Task UnsupportedTrainingMigrationRejected()
    {
        var f = await PrepareAsync();
        Assert.ThrowsException<NotSupportedException>(() => FeedbackExperiment.ValidateConfiguration(f.Configuration, FeedbackStateRule.F3SeenPrefix));
    }

    [TestMethod]
    public async Task CancelledEntryReturns130()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.AreEqual(130, await FeedbackRunner.RunAsync([], TextWriter.Null, TextWriter.Null, cancellation.Token));
    }

    [TestMethod]
    public async Task InvalidArgumentsReturn2() => Assert.AreEqual(2, await RunAsync());
}
