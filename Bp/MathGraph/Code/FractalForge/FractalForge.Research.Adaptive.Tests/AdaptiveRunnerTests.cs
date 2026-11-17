using System.Text.Json;
using FractalForge.Training;
using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Research.Adaptive.Tests;

[TestClass]
public class AdaptiveRunnerTests
{
    private sealed record Fixture(string Configuration, string Manifest, string Directory);

    private static async Task<Fixture> CreateAsync(string branch)
    {
        var directory = Path.Combine(Path.GetTempPath(), "adaptive-entry-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var tokenizerPath = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json");
        var tokenizerConfig = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json");
        async Task<ForgeCorpus> Corpus(string name, string text)
        {
            var path = Path.Combine(directory, name + ".jsonl");
            await File.WriteAllTextAsync(path, text);
            return new(path, name + "-v1", ForgeArchive.Fingerprint(await File.ReadAllBytesAsync(path)));
        }
        var template = new ForgeRunConfiguration
        {
            Semantics = ForgeRunner.Semantics, Direction = "Adaptive", RunId = branch, OutputRoot = directory,
            SourceArchive = "prepared-by-entry", SourceSha256 = new string('A', 64),
            TokenizerPath = tokenizerPath, TokenizerConfigPath = tokenizerConfig, TokenizerIdentity = "prepared-by-entry",
            Training = await Corpus("train", "{\"text\":\"hello\"}\n{\"text\":\"world\"}\n{\"text\":\"again\"}\n"),
            Acceptance = await Corpus("accept", "{\"text\":\"hello world\"}\n"),
            FinalTest = new(Path.Combine(directory, "final-must-not-be-opened.jsonl"), "final-v1", new string('B', 64)),
            DataLimits = new(4096, 16384, 10, 8, 80), Action = new("clone-widen", "A"),
            RuleVersion = "fixed-v1", MaxUpdates = 2, MaxTokens = 40, MaxPairTapeCells = 100000000,
            MinimumGain = -100, Optimizer = new(), RandomState = 17
        };
        var templatePath = Path.Combine(directory, "template.json");
        var optionsPath = Path.Combine(directory, "policy.json");
        var configPath = Path.Combine(directory, "adaptive.json");
        await File.WriteAllTextAsync(templatePath, JsonSerializer.Serialize(template));
        await File.WriteAllTextAsync(optionsPath, JsonSerializer.Serialize(new AdaptiveOptions(branch, 1, 0, 0, 1, 1, .2, .1, .01, 17, 100, 100000, 100000000)));
        var errors = new StringWriter();
        var code = await AdaptiveRunner.RunAsync(["prepare", templatePath, optionsPath, configPath, "7", "false"], TextWriter.Null, errors);
        Assert.AreEqual(0, code, errors.ToString());
        return new(configPath, Path.Combine(directory, "Adaptive", branch, "adaptive.json"), directory);
    }

    private static async Task RunAsync(Fixture fixture, params string[] arguments)
    {
        var errors = new StringWriter();
        Assert.AreEqual(0, await AdaptiveRunner.RunAsync(arguments, TextWriter.Null, errors), fixture.Directory + " " + errors);
    }

    [TestMethod]
    public async Task EntryPreparesTrainsResumesEvaluatesInspectsAndGeneratesWithoutOpeningFinalTest()
    {
        var fixture = await CreateAsync("A1");
        await RunAsync(fixture, "train", fixture.Configuration, "1");
        await RunAsync(fixture, "resume", fixture.Configuration, fixture.Manifest, "1");
        await RunAsync(fixture, "evaluate", fixture.Configuration, fixture.Manifest);
        await RunAsync(fixture, "inspect", fixture.Configuration, fixture.Manifest);
        await RunAsync(fixture, "generate", fixture.Configuration, fixture.Manifest, "hello", "2", "9");
        Assert.IsTrue(File.Exists(fixture.Manifest));
    }

    [TestMethod]
    public async Task A0EntryRunsOrdinaryTrainingAndGeneration()
    {
        var fixture = await CreateAsync("A0");
        await RunAsync(fixture, "train", fixture.Configuration, "2");
        await RunAsync(fixture, "generate", fixture.Configuration, fixture.Manifest, "hello", "2", "9");
        using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(fixture.Manifest));
        Assert.AreEqual(0L, json.RootElement.GetProperty("PairTokens").GetInt64());
    }

    [TestMethod]
    public async Task ResumeAutomaticallyCompletesEndpointBeforeFurtherTraining()
    {
        var fixture = await CreateAsync("A1");
        await RunAsync(fixture, "train", fixture.Configuration, "2");
        await RunAsync(fixture, "resume", fixture.Configuration, fixture.Manifest, "1");
        using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(fixture.Manifest));
        Assert.AreEqual(1, json.RootElement.GetProperty("EvaluationCount").GetInt32());
    }
}
