using System.Text.Json;
using FractalForge.Training;
using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Research.Deepening.Tests;

[TestClass]
public class RunnerTests
{
    [TestMethod]
    public async Task RealTokenizerTrainResumeEvaluateGenerateAndCooldown()
    {
        var directory = Path.Combine(Path.GetTempPath(), "deepening-runner-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var tokenizerPath = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json");
        var tokenizerConfig = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json");
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(tokenizerPath, tokenizerConfig);
        var source = Path.Combine(directory, "source.forge");
        await DeepeningRunner.PrepareAsync(tokenizerPath, tokenizerConfig, source, 7, TextWriter.Null);
        async Task<ForgeCorpus> Corpus(string name, string text)
        {
            var path = Path.Combine(directory, name + ".jsonl");
            await File.WriteAllTextAsync(path, text);
            return new(path, name + "-v1", ForgeArchive.Fingerprint(await File.ReadAllBytesAsync(path)));
        }
        var run = new ForgeRunConfiguration
        {
            Semantics = ForgeRunner.Semantics, Direction = "Deepening", RunId = "integration", OutputRoot = directory,
            SourceArchive = source, SourceSha256 = ForgeArchive.Fingerprint(await File.ReadAllBytesAsync(source)),
            TokenizerPath = tokenizerPath, TokenizerConfigPath = tokenizerConfig, TokenizerIdentity = tokenizer.Identity,
            Training = await Corpus("train", "{\"text\":\"hello\"}\n{\"text\":\"world\"}\n{\"text\":\"hello world\"}\n"),
            Acceptance = await Corpus("accept", "{\"text\":\"test\"}\n"),
            FinalTest = new(Path.Combine(directory, "final-must-not-be-opened.jsonl"), "final-v1", new string('A', 64)),
            DataLimits = new(4096, 16384, 10, 8, 80), Action = new("D2", "A", Weight: 0),
            RuleVersion = "fixed-endpoint-v1", MaxUpdates = 2, MaxTokens = 100, MaxPairTapeCells = 10000000,
            MinimumGain = 100, Optimizer = new(), RandomState = 17
        };
        var config = Path.Combine(directory, "deepening.v1.json");
        await File.WriteAllTextAsync(config, JsonSerializer.Serialize(new DeepeningConfiguration(DeepeningRunner.Semantics, run, DeepeningBranch.D2, 1)));
        var checkpoint = Path.Combine(directory, "Deepening", "integration", "experiment.json");
        var codes = new List<int>();
        foreach (var command in new[] {
            new[] { "train", config, checkpoint, "1" }, new[] { "resume", config, checkpoint },
            new[] { "inspect", config, checkpoint }, new[] { "evaluate", config, checkpoint },
            new[] { "generate", config, checkpoint, "hello", "1", "7" }, new[] { "cooldown", config, checkpoint } })
            codes.Add(await DeepeningRunner.RunAsync(command, TextWriter.Null, Console.Error));
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, codes);
    }
}
