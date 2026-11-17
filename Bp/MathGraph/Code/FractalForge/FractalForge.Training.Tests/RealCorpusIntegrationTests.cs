using System.Text.Json;
using FractalForge.Core;
using FractalForge.Research.Widening;
using FractalForge.Research.Deepening;
using FractalForge.Research.Adaptive;
using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Training.Tests;

[TestClass]
public class RealCorpusIntegrationTests
{
    [TestMethod]
    public async Task RealPrefixPreparesFourDirectionsAndResumesBoundedTraining()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "minimind_dataset"))) root = root.Parent;
        Assert.IsNotNull(root);
        var workspace = root.FullName;
        var output = Path.Combine(workspace, ".coding-agent", "fractalforge-real", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        Console.WriteLine("Real corpus artifacts: " + output);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var ct = timeout.Token;
        var data = await ForgeCorpusPreparation.PreparePrefixAsync(Path.Combine(workspace, "minimind_dataset", "pretrain_t2t_mini.jsonl"),
            Path.Combine(output, "data"), 32, cancellationToken: ct);
        var tokenizerPath = Path.Combine(workspace, "minimind_model", "tokenizer.json");
        var tokenizerConfig = Path.Combine(workspace, "minimind_model", "tokenizer_config.json");
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(tokenizerPath, tokenizerConfig, cancellationToken: ct);
        var model = ForgeModel.Create(ForgeTopology.CreateDiamond(7, feedback: true));
        model.Network.BindText(new TokenBindings(tokenizer.Identity, ["I"], ["O"],
            Enumerable.Range(0, tokenizer.VocabularySize).Select(i => (IReadOnlyList<double>)new[] { (double)i / tokenizer.VocabularySize }).ToArray(), seed: 7));
        var archive = ForgeArchive.Export(model);
        var sourcePath = Path.Combine(output, "baseline.forge");
        await File.WriteAllBytesAsync(sourcePath, archive, ct);
        var run = new ForgeRunConfiguration
        {
            Semantics = ForgeRunner.Semantics, Direction = "Widening", RunId = "real-prefix-W0", OutputRoot = output,
            SourceArchive = sourcePath, SourceSha256 = ForgeArchive.Fingerprint(archive), TokenizerPath = tokenizerPath,
            TokenizerConfigPath = tokenizerConfig, TokenizerIdentity = tokenizer.Identity, Training = data.Training,
            Acceptance = data.Acceptance, FinalTest = data.FinalTest, DataLimits = new(1048576, 16777216, 32, 8, 256),
            Action = new("clone-widen", "A"), RuleVersion = "real-prefix-smoke-only-v1", MaxUpdates = 2,
            MaxTokens = 256, MaxPairTapeCells = 10000000, MinimumGain = 100, Optimizer = new(), RandomState = 17
        };
        var json = new JsonSerializerOptions { WriteIndented = true };
        async Task<string> Config(string name, object value)
        {
            var path = Path.Combine(output, name + ".json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, json), ct);
            return path;
        }
        var errors = new StringWriter();
        var w0 = await Config("W0", new WideningExperiment(WideningExperiment.Version, "W0", run));
        Assert.AreEqual(0, await WideningRunner.RunAsync(["train", w0, "1"], TextWriter.Null, errors, ct), errors.ToString());
        Assert.AreEqual(0, await WideningRunner.RunAsync(["resume", w0, Path.Combine(output, "Widening", run.RunId, "keep.json"), "1"], TextWriter.Null, errors, ct), errors.ToString());
        await Config("W1", new WideningExperiment(WideningExperiment.Version, "W1", run with { RunId = "real-prefix-W1" }));
        var deepRun = run with { Direction = "Deepening", RunId = "real-prefix-D2", Action = new("D2", "A") };
        var deep = await Config("D2", new DeepeningConfiguration(DeepeningRunner.Semantics, deepRun, DeepeningBranch.D2, 0));
        var checkpoint = Path.Combine(output, "Deepening", deepRun.RunId, "checkpoint.json");
        Assert.AreEqual(0, await DeepeningRunner.RunAsync(["train", deep, checkpoint, "1"], TextWriter.Null, errors, ct), errors.ToString());
        Assert.AreEqual(0, await DeepeningRunner.RunAsync(["resume", deep, checkpoint, "1"], TextWriter.Null, errors, ct), errors.ToString());
        var feedback = await Config("F1", run with { Direction = "Feedback", RunId = "real-prefix-F1", Action = new("add-feedback", "A", Destination: "A", Delay: 1, Weight: .01) });
        Assert.AreEqual(0, await ForgeRunner.RunAsync("Feedback", ["train", feedback, "1"], TextWriter.Null, errors, ct), errors.ToString());
        Assert.AreEqual(0, await ForgeRunner.RunAsync("Feedback", ["resume", feedback, Path.Combine(output, "Feedback", "real-prefix-F1", "run.json"), "1"], TextWriter.Null, errors, ct), errors.ToString());
        var adaptiveRun = run with { Direction = "Adaptive", RunId = "real-prefix-A1" };
        var policy = new AdaptiveOptions("A1", 1, 0, 0, 1, 1, .1, .01, .01, 17, 64, 1000000, 10000000);
        var adaptive = await Config("A1", new AdaptiveConfiguration(AdaptiveRunner.Semantics, adaptiveRun, policy, [new("clone-widen", "A")]));
        Assert.AreEqual(0, await AdaptiveRunner.RunAsync(["train", adaptive, "1"], TextWriter.Null, errors, ct), errors.ToString());
        Assert.AreEqual(0, await AdaptiveRunner.RunAsync(["resume", adaptive, Path.Combine(output, "Adaptive", adaptiveRun.RunId, "adaptive.json"), "1"], TextWriter.Null, errors, ct), errors.ToString());
        await File.WriteAllTextAsync(Path.Combine(output, "smoke-result.json"), JsonSerializer.Serialize(new { Status = "passed", data.SourceRecords,
            data.SourceBytes, data.PrefixSha256, data.ReachedEnd, UpdatesPerDirection = 2, MaxSampleTokens = 8, FinalTestUsed = false, AcceptanceUsed = false, ProposalUsed = false }), ct);
    }
}
