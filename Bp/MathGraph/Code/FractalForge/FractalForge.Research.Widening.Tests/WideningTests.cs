using System.Text.Json;
using FractalForge.Core;
using FractalForge.Training;
using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Research.Widening.Tests;

[TestClass]
public class WideningTests
{
    private static ForgeModel Model()
    {
        var model = ForgeModel.Create(ForgeTopology.CreateDiamond(7));
        model.Network.BindText(new TokenBindings("test", ["I"], ["O"], new IReadOnlyList<double>[] { new double[] { 0 }, new double[] { .5 }, new double[] { 1 } }));
        return model;
    }

    [DataTestMethod]
    [DataRow("W0")]
    [DataRow("W1")]
    [DataRow("W2")]
    public void CandidatePreservesImmediateTokenLoss(string branch)
    {
        var source = Model();
        var candidate = WideningStrategy.CreateCandidate(source, branch, "A");
        Assert.AreEqual(0, WideningMetrics.MeasureLoss(source, candidate, new[] { 0, 1, 2, 1 }).Delta, 1e-12);
    }

    [TestMethod]
    public void KeepIsIndependentSnapshot()
    {
        var source = Model();
        var expected = ForgeArchive.Fingerprint(ForgeArchive.Export(source));
        WideningStrategy.CreateCandidate(source, "W0", "A").Network.TrainTokens(new[] { 0, 1, 2 });
        Assert.AreEqual(expected, ForgeArchive.Fingerprint(ForgeArchive.Export(source)));
    }

    [TestMethod]
    public void ClonesHaveIndependentBiasIdentities()
    {
        var candidate = WideningStrategy.CreateCandidate(Model(), "W1", "A");
        var unit = candidate.Topology.Units.Last();
        Assert.AreNotEqual(candidate.Topology.Biases[unit.A].ParameterId, candidate.Topology.Biases[unit.B].ParameterId);
    }

    [TestMethod]
    public void UnequalSplitHasMeasuredWeightedSignalRatio()
    {
        var candidate = WideningStrategy.CreateCandidate(Model(), "W2", "A", .25);
        var signal = WideningMetrics.MeasureBranches(candidate, candidate.Topology.Units.Last(),
            new IReadOnlyDictionary<string, double>[] { new Dictionary<string, double> { ["I"] = .2 }, new Dictionary<string, double> { ["I"] = .8 } });
        Assert.AreEqual(1.0 / 3, signal.AWeightedRms / signal.BWeightedRms, 1e-12);
    }

    [TestMethod]
    public void SymmetricBranchesAreCorrelated()
    {
        var candidate = WideningStrategy.CreateCandidate(Model(), "W1", "A");
        var signal = WideningMetrics.MeasureBranches(candidate, candidate.Topology.Units.Last(),
            new IReadOnlyDictionary<string, double>[] { new Dictionary<string, double> { ["I"] = .2 }, new Dictionary<string, double> { ["I"] = .8 } });
        Assert.AreEqual(1.0, signal.Correlation!.Value, 1e-12);
    }

    [TestMethod]
    public void ConstantProbeCorrelationIsUndefined()
    {
        var candidate = WideningStrategy.CreateCandidate(Model(), "W1", "A");
        Assert.IsNull(WideningMetrics.MeasureBranches(candidate, candidate.Topology.Units.Last(),
            new IReadOnlyDictionary<string, double>[] { new Dictionary<string, double> { ["I"] = .2 } }).Correlation);
    }

    [TestMethod]
    public void PerturbationIsExplicitlyUnsupported() =>
        Assert.ThrowsException<NotSupportedException>(() => WideningStrategy.CreateCandidate(Model(), "W3", "A"));

    [TestMethod]
    public void EqualSplitIsRejectedForW2() =>
        Assert.ThrowsException<InvalidDataException>(() => WideningStrategy.CreateCandidate(Model(), "W2", "A", .5));

    [TestMethod]
    public void BoundInputCannotBeWidened() =>
        Assert.ThrowsException<InvalidOperationException>(() => WideningStrategy.CreateCandidate(Model(), "W1", "I"));

    [TestMethod]
    public async Task CancelledEntryReturns130()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        Assert.AreEqual(130, await WideningRunner.RunAsync(Array.Empty<string>(), TextWriter.Null, TextWriter.Null, cts.Token));
    }

    [TestMethod]
    public async Task DuplicatePreparationFieldsAreRejected()
    {
        var path = Path.Combine(Path.GetTempPath(), "widening-duplicate-" + Guid.NewGuid().ToString("N") + ".json");
        await File.WriteAllTextAsync(path, "{\"Seed\":1,\"Seed\":2}");
        Assert.AreEqual(1, await WideningRunner.RunAsync(new[] { "prepare", path, Path.GetTempPath() }, TextWriter.Null, TextWriter.Null));
    }

    [DataTestMethod]
    [DataRow("W0", "keep.json")]
    [DataRow("W1", "run.json")]
    [DataRow("W2", "run.json")]
    public async Task PreparedExperimentCanTrainResumeEvaluateAndGenerate(string branch, string manifestName)
    {
        var directory = Path.Combine(Path.GetTempPath(), "widening-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        async Task<ForgeCorpus> Corpus(string name, string text)
        {
            var path = Path.Combine(directory, name + ".jsonl");
            await File.WriteAllTextAsync(path, text);
            return new(path, name, ForgeArchive.Fingerprint(await File.ReadAllBytesAsync(path)));
        }
        var config = new ForgeRunConfiguration
        {
            Semantics = ForgeRunner.Semantics, Direction = "Widening", RunId = "short", OutputRoot = directory,
            SourceArchive = "prepared", SourceSha256 = new string('A', 64),
            TokenizerPath = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json"),
            TokenizerConfigPath = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json"), TokenizerIdentity = "prepared",
            Training = await Corpus("train", "{\"text\":\"hello\"}\n{\"text\":\"world\"}\n"),
            Acceptance = await Corpus("accept", "{\"text\":\"hello world\"}\n"),
            FinalTest = new(Path.Combine(directory, "not-present.jsonl"), "final", new string('B', 64)),
            DataLimits = new(4096, 16384, 10, 8, 80), Action = new("clone-widen", "A"),
            RuleVersion = "fixed-v1", MaxUpdates = 2, MaxTokens = 40, MaxPairTapeCells = 10000000,
            MinimumGain = 100, Optimizer = new(), RandomState = 17
        };
        var requestPath = Path.Combine(directory, "request.json");
        await File.WriteAllTextAsync(requestPath, JsonSerializer.Serialize(new WideningPreparation(config, null, 7, true, "A", .3)));
        Assert.AreEqual(0, await WideningRunner.RunAsync(new[] { "prepare", requestPath, directory }, TextWriter.Null, TextWriter.Null));
        var prepared = JsonSerializer.Deserialize<WideningExperiment>(await File.ReadAllTextAsync(Path.Combine(directory, branch + ".json")))!;
        await WideningStrategy.PrepareAsync(new(config, prepared.Run.SourceArchive, 7, true, "A", .3), Path.Combine(directory, "imported"));
        var experiment = Path.Combine(directory, branch + ".json");
        var manifest = Path.Combine(directory, "Widening", "short-" + branch, manifestName);
        using var error = new StringWriter();
        var tokensPath = Path.Combine(directory, "tokens.json");
        var probesPath = Path.Combine(directory, "probes.json");
        await File.WriteAllTextAsync(tokensPath, "[0,1,2]");
        await File.WriteAllTextAsync(probesPath, "[{\"I\":0.2},{\"I\":0.8}]");
        Assert.AreEqual(0, await WideningRunner.RunAsync(new[] { "diagnose", experiment, tokensPath, probesPath }, TextWriter.Null, error), error.ToString());
        Assert.AreEqual(0, await WideningRunner.RunAsync(new[] { "train", experiment, "1" }, TextWriter.Null, error), error.ToString());
        Assert.AreEqual(0, await WideningRunner.RunAsync(new[] { "resume", experiment, manifest, "1" }, TextWriter.Null, error), error.ToString());
        Assert.AreEqual(0, await WideningRunner.RunAsync(new[] { "inspect", experiment, manifest }, TextWriter.Null, error), error.ToString());
        Assert.AreEqual(0, await WideningRunner.RunAsync(new[] { "evaluate", experiment, manifest }, TextWriter.Null, error), error.ToString());
        Assert.AreEqual(0, await WideningRunner.RunAsync(new[] { "generate", experiment, manifest, "hello", "1", "4" }, TextWriter.Null, error), error.ToString());
    }
}
