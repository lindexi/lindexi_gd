using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class LocalDatasetTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task LocalPretrainingPrefixLoadsAsync()
    {
        var tokenizer = await LoadTokenizerAsync();
        using var stream = await ReadPrefixAsync("pretrain_t2t_mini.jsonl");
        var dataset = await PretrainingJsonl.LoadAsync(stream, tokenizer,
            new JsonlDatasetOptions(MaxSamples: 2, MaxSampleTokens: 64, MaxTotalTokens: 128));
        TestContext.WriteLine($"Dataset={dataset.Identity}; Samples={dataset.Count}");
        Assert.AreEqual(2, dataset.Count);
    }

    [TestMethod]
    public async Task LocalSftPrefixContainsSupervisedTargetsAsync()
    {
        var tokenizer = await LoadTokenizerAsync();
        using var stream = await ReadPrefixAsync("sft_t2t_mini.jsonl");
        var dataset = await ChatJsonl.LoadAsync(stream, tokenizer,
            new JsonlDatasetOptions(MaxSamples: 2, MaxSampleTokens: 128, MaxTotalTokens: 256));
        TestContext.WriteLine($"Dataset={dataset.Identity}; Samples={dataset.Count}");
        Assert.IsTrue(Enumerable.Range(0, dataset.Count).All(i => dataset.GetLabelMask(i)!.Skip(1).Any(value => value)));
    }

    [DataTestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [TestCategory("LocalExperiment")]
    public async Task LocalPretrainingMultiSeedComparisonProducesReportAsync(int rounds)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var tokenizer = await LoadTokenizerAsync();
        using var stream = await ReadPrefixAsync("pretrain_t2t_mini.jsonl", 12);
        var dataset = await PretrainingJsonl.LoadAsync(stream, tokenizer,
            new JsonlDatasetOptions(MaxSamples: 12, MaxSampleTokens: 24, MaxTotalTokens: 288), timeout.Token);
        var partition = DatasetPartition.Create(dataset, new DatasetPartitionOptions(0.25, 0.25, 7), cancellationToken: timeout.Token);
        var five = FiveNodeGraph.Create(17);
        var model = new RecursiveNetwork(five.Graph, new NetworkBudget(MaxNodes: 5 + 5 * rounds, MaxDepth: 2, MaxSteps: 64));
        var nodes = five.Graph.ElementList.Select(n => n.Id).ToArray();
        var encoding = Enumerable.Range(0, tokenizer.VocabularySize).Select(id =>
            (IReadOnlyList<double>)Enumerable.Range(0, 5).Select(bit => ((id >> bit) & 1) == 0 ? -1d : 1d).ToArray()).ToArray();
        model.BindText(new TokenBindings(tokenizer.Identity, nodes, nodes, encoding, seed: 19));
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "minimind_dataset"))) directory = directory.Parent;
        var root = directory?.FullName ?? throw new InvalidOperationException();
        var output = Path.Combine(root, "MathGraph", "artifacts", "experiments", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        TestContext.WriteLine(output);
        await model.SaveCheckpointAsync(Path.Combine(output, "initial-model.json"), timeout.Token);
        var report = model.CompareStructureSeeds(partition, Enumerable.Repeat(0, rounds).ToArray(), new ulong[] { 1, 2 },
            new StructureComparisonOptions(rounds, 2), cancellationToken: timeout.Token);
        var payload = new
        {
            Purpose = "Bounded CPU pipeline validation; not a language-quality or significance claim",
            DatasetFile = "minimind_dataset/pretrain_t2t_mini.jsonl",
            PrefixLines = 12,
            Rounds = rounds,
            MaxSampleTokens = 24,
            InputEncoding = "Five fixed low token-ID bits mapped to -1/+1; many IDs share encoding",
            InitialCheckpoint = "initial-model.json",
            TrainIndices = partition.TrainingIndices,
            SelectionIndices = partition.StructureSelectionIndices,
            TestIndices = partition.TestIndices,
            Report = report
        };
        await File.WriteAllTextAsync(Path.Combine(output, "report.json"),
            System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }), timeout.Token);
        TestContext.AddResultFile(Path.Combine(output, "report.json"));
        Assert.IsTrue(report.Runs.Count == 2 && report.Summary.All(s => double.IsFinite(s.MeanTestLoss)));
    }

    [DataTestMethod]
    [DataRow(0, 2)]
    [DataRow(2, 2)]
    [DataRow(2, 10)]
    [TestCategory("LocalExperiment")]
    public async Task LocalFeatureAblationArchivesBothPoliciesAsync(int warmupUpdates, int observationUpdates)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var tokenizer = await LoadTokenizerAsync();
        using var stream = await ReadPrefixAsync("pretrain_t2t_mini.jsonl", 12);
        var data = await PretrainingJsonl.LoadAsync(stream, tokenizer,
            new JsonlDatasetOptions(MaxSamples: 12, MaxSampleTokens: 24, MaxTotalTokens: 288), timeout.Token);
        var partition = DatasetPartition.Create(data, new DatasetPartitionOptions(0.25, 0.25, 7), cancellationToken: timeout.Token);
        var five = FiveNodeGraph.Create(17);
        var initial = new RecursiveNetwork(five.Graph, new NetworkBudget(MaxNodes: 20, MaxDepth: 2, MaxSteps: 64));
        var nodes = five.Graph.ElementList.Select(n => n.Id).ToArray();
        var encoding = Enumerable.Range(0, tokenizer.VocabularySize).Select(id =>
            (IReadOnlyList<double>)Enumerable.Range(0, 5).Select(bit => ((id >> bit) & 1) == 0 ? -1d : 1d).ToArray()).ToArray();
        initial.BindText(new TokenBindings(tokenizer.Identity, nodes, nodes, encoding, seed: 19));
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "minimind_dataset"))) directory = directory.Parent;
        var root = directory?.FullName ?? throw new InvalidOperationException();
        var output = Path.Combine(root, "MathGraph", "artifacts", "experiments", "feature-ablation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        TestContext.WriteLine(output);
        await initial.SaveCheckpointAsync(Path.Combine(output, "initial-model.json"), timeout.Token);
        var warmupIndices = new List<int>();
        for (var update = 0; update < warmupUpdates; update++)
        {
            timeout.Token.ThrowIfCancellationRequested();
            var index = update % partition.Training.Count;
            initial.TrainTokens(partition.Training[index], labelMask: partition.Training.GetLabelMask(index), cancellationToken: timeout.Token);
            await initial.SaveCheckpointAsync(Path.Combine(output, $"warmup-{update + 1:D4}.json"), timeout.Token);
            warmupIndices.Add(index);
        }
        var checkpoint = initial.ExportCheckpoint();
        await initial.SaveCheckpointAsync(Path.Combine(output, "common-start-model.json"), timeout.Token);
        var commonState = System.Text.Json.Nodes.JsonNode.Parse(checkpoint)!;
        Assert.AreEqual((long)warmupUpdates, commonState["TrainingUpdates"]!.GetValue<long>());
        Assert.IsNull(commonState["Controller"]);
        Assert.AreEqual(5, initial.GetNodeObservations().Count);
        var reports = new List<object>();
        var losses = new List<double>();
        foreach (var seed in new ulong[] { 1, 2 })
        {
            foreach (var features in new[] { false, true })
            {
                var model = RecursiveNetwork.ImportCheckpoint(checkpoint);
                CollectionAssert.AreEqual(checkpoint, model.ExportCheckpoint());
                var trainingSteps = observationUpdates * 3;
                Assert.AreEqual(0, trainingSteps % partition.Training.Count);
                model.ConfigureTrainingJob(partition.Training, epochs: trainingSteps / partition.Training.Count);
                model.ConfigureStructureController(new StructureOptions(observationUpdates, UseNodeFeatures: features), seed);
                var run = await model.RunTrainingAsync(partition.Training, Path.Combine(output, $"seed-{seed}-features-{features}"),
                    structureSelection: partition.StructureSelection, cancellationToken: timeout.Token);
                var test = model.EvaluateDataset(partition.Test, timeout.Token);
                losses.Add(test.MeanCrossEntropy);
                var decisions = new List<object>();
                foreach (var path in Directory.GetFiles(run.Directory, "*structure.json").Order())
                {
                    var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllBytesAsync(path, timeout.Token))!;
                    var controller = json["Controller"]!;
                    var pending = controller["Pending"]!;
                    var selected = pending["Selected"]!.GetValue<int>();
                    decisions.Add(new
                    {
                        Checkpoint = Path.GetRelativePath(output, path),
                        TrainingUpdatesAtDecision = json["TrainingUpdates"]!.GetValue<long>(),
                        SelectedActionOrdinal = selected,
                        CandidateCountIncludingKeep = pending["Actions"]!.AsArray().Count,
                        Probability = pending["Probabilities"]![selected]!.GetValue<double>(),
                        FeatureWeights = controller["FeatureWeights"]!.DeepClone(),
                        FeatureSnapshot = pending["Features"]!.DeepClone()
                    });
                }
                var activity = new List<object>();
                foreach (var path in Directory.GetFiles(run.Directory, "checkpoint-*.json").Order())
                {
                    var snapshot = await RecursiveNetwork.LoadCheckpointAsync(path, cancellationToken: timeout.Token);
                    Visit(snapshot.Graph);
                    void Visit(MathGraph<Neuron, NeuralConnection> graph)
                    {
                        foreach (var node in graph.ElementList)
                        {
                            if (node.Value.Child is not { } child || node.Value.Gate is not { } gate) continue;
                            activity.Add(new
                            {
                                Checkpoint = Path.GetRelativePath(output, path), Parent = node.Id,
                                Gate = Math.Tanh(gate.Value), GateUpdates = gate.Updates,
                                ChildOutputGradient = child.Output.Value.Statistics.LastWindow?.MeanAbsoluteGradient ?? 0,
                                ChildBiasFirstMomentMax = child.Graph.ElementList.Max(n => Math.Abs(n.Value.Bias.FirstMoment)),
                                ChildWeightFirstMomentMax = child.Graph.ElementList.SelectMany(n => n.OutEdgeList)
                                    .Max(e => Math.Abs(e.EdgeInfo!.Weight.FirstMoment))
                            });
                            Visit(child.Graph);
                        }
                    }
                }
                reports.Add(new { Seed = seed, UseNodeFeatures = features, TestLoss = test.MeanCrossEntropy,
                    CompletedSamples = run.CompletedSamples, Nodes = model.GetNodeObservations().Count,
                    LastCheckpoint = Path.GetRelativePath(output, run.LastCheckpoint), Decisions = decisions, BranchActivity = activity });
            }
        }
        var reportPath = Path.Combine(output, "ablation-report.json");
        await File.WriteAllTextAsync(reportPath, System.Text.Json.JsonSerializer.Serialize(new
        {
            Purpose = "Paired feature-policy ablation; no significance claim",
            Dataset = "minimind_dataset/pretrain_t2t_mini.jsonl", PrefixLines = 12, MaxSampleTokens = 24,
            WarmupUpdates = warmupUpdates,
            WarmupTrainingIndices = warmupIndices,
            ObservationUpdates = observationUpdates,
            PostWarmupUpdates = observationUpdates * 3,
            PostWarmupTrainingOrder = "Restart at training index 0 and cycle deterministically; same for both policies",
            CommonStartCheckpoint = "common-start-model.json",
            InputEncoding = "Five low token-ID bits; collisions retained identically in both policies",
            InitialCheckpointSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(checkpoint)),
            TrainingIdentity = partition.Training.Identity, SelectionIdentity = partition.StructureSelection.Identity,
            TestIdentity = partition.Test.Identity, partition.TrainingIndices, partition.StructureSelectionIndices,
            partition.TestIndices, Runs = reports
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }), timeout.Token);
        TestContext.AddResultFile(reportPath);
        Assert.IsTrue(losses.Count == 4 && losses.All(double.IsFinite));
    }

    private async Task<MemoryStream> ReadPrefixAsync(string name, int count = 2)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "minimind_dataset"))) directory = directory.Parent;
        if (directory is null)
        {
            Assert.Inconclusive("Local minimind_dataset is not present; dataset assets are not distributed with tests.");
            throw new InvalidOperationException();
        }
        var path = Path.Combine(directory.FullName, "minimind_dataset", name);
        TestContext.WriteLine(path);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var reader = new StreamReader(path, new UTF8Encoding(false, true));
        var result = new StringBuilder();
        var buffer = new char[1];
        var lines = 0;
        var lineLength = 0;
        while (lines < count && await reader.ReadAsync(buffer.AsMemory(), timeout.Token) != 0)
        {
            if (++lineLength > 131072) throw new InvalidDataException();
            result.Append(buffer[0]);
            if (buffer[0] == '\n') { lines++; lineLength = 0; }
        }
        return new MemoryStream(Encoding.UTF8.GetBytes(result.ToString()));
    }

    private static Task<MiniMindTextTokenizer> LoadTokenizerAsync() => MiniMindTextTokenizer.LoadAsync(
        Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json"),
        Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json"));
}
