using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class PipelineTrainingTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task FirstUpdateOccursBeforeEndAndResumeMatchesContinuousAsync()
    {
        var tokenizer = await TokenizerAsync();
        var root = Root();
        var path = Path.Combine(root, "data.jsonl");
        await File.WriteAllLinesAsync(path, Enumerable.Range(0, 20).Select(i => $"{{\"text\":\"Hello {i}\"}}"));
        var model = Create(tokenizer, path, epochs: 2);
        var original = model.ExportCheckpoint();
        using var cancellation = new CancellationTokenSource();
        var observer = new Capture(value =>
        {
            if (value.Stage == "periodic" && value.TrainedSamples == 1) cancellation.Cancel();
        });
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => model.RunPipelineAsync(tokenizer, root,
            archive: new TrainingArchiveOptions(SaveEveryUpdates: 1), progress: observer, cancellationToken: cancellation.Token));
        var first = observer.Values.First(v => v.Stage == "periodic");
        Assert.IsTrue(first.ByteOffset < new FileInfo(path).Length);
        var resumed = await RecursiveNetwork.LoadCheckpointAsync(observer.Values.Last(v => v.Stage == "cancelled").Checkpoint!);
        Assert.AreEqual(1, resumed.PipelineTraining!.WarmupCompleted);
        await resumed.RunPipelineAsync(tokenizer, root);
        var continuous = RecursiveNetwork.ImportCheckpoint(original);
        await continuous.RunPipelineAsync(tokenizer, root);
        CollectionAssert.AreEqual(continuous.ExportCheckpoint(), resumed.ExportCheckpoint());
    }

    [TestMethod]
    public async Task ProgressReusesSuccessfulTrainingLossAsync()
    {
        var tokenizer = await TokenizerAsync();
        var root = Root();
        var path = Path.Combine(root, "loss.jsonl");
        await File.WriteAllLinesAsync(path, Enumerable.Range(0, 12).Select(i => $"{{\"text\":\"Hello {i}\"}}"));
        var model = Create(tokenizer, path);
        var observer = new Capture(_ => { });
        await model.RunPipelineAsync(tokenizer, root, progress: observer);
        var first = observer.Values.First(v => v.Stage == "consumed" && v.Learning!.TrainingSamples == 1).Learning!;
        Assert.AreEqual(first.LastTrainingLoss!.Value, first.WeightedLossSum / first.SupervisedTokens, 1e-12);
    }

    [TestMethod]
    public async Task ProducerErrorReachesCallerWithoutHangingAsync()
    {
        var tokenizer = await TokenizerAsync();
        var root = Root();
        var path = Path.Combine(root, "bad.jsonl");
        await File.WriteAllTextAsync(path, "{invalid}\n");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => Create(tokenizer, path).RunPipelineAsync(tokenizer, root, cancellationToken: timeout.Token));
    }

    [TestMethod]
    public async Task PrefetchCapacityDoesNotChangeTrainingResultAsync()
    {
        var tokenizer = await TokenizerAsync();
        var root = Root();
        var path = Path.Combine(root, "data.jsonl");
        await File.WriteAllLinesAsync(path, Enumerable.Range(0, 12).Select(i => $"{{\"text\":\"Hello {i}\"}}"));
        var model = Create(tokenizer, path);
        var json = System.Text.Json.Nodes.JsonNode.Parse(model.ExportCheckpoint())!;
        json["Pipeline"]!["Options"]!["PrefetchCapacity"] = 8;
        var other = RecursiveNetwork.ImportCheckpoint(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        await model.RunPipelineAsync(tokenizer, root);
        await other.RunPipelineAsync(tokenizer, root);
        var a = System.Text.Json.Nodes.JsonNode.Parse(model.ExportCheckpoint())!;
        var b = System.Text.Json.Nodes.JsonNode.Parse(other.ExportCheckpoint())!;
        a["Pipeline"]!["Options"]!["PrefetchCapacity"] = 8;
        Assert.AreEqual(a.ToJsonString(), b.ToJsonString());
    }

    [TestMethod]
    public async Task SelectionFreezesAndDoesNotBlockTrainingAsync()
    {
        var tokenizer = await TokenizerAsync();
        var root = Root();
        var path = Path.Combine(root, "data.jsonl");
        await File.WriteAllLinesAsync(path, Enumerable.Range(0, 40).Select(i => $"{{\"text\":\"Sample {i}\"}}"));
        var model = Create(tokenizer, path, selectionFraction: 0.3, controller: new StructureOptions(2));
        var result = await model.RunPipelineAsync(tokenizer, root);
        Assert.IsTrue(model.PipelineTraining!.SelectionFrozen);
        Assert.AreEqual(2, model.PipelineTraining.Selection!.Length);
        Assert.IsTrue(result.CompletedSamples > 0);
        Assert.IsTrue(Directory.GetFiles(result.Directory, "*structure.json").Length > 0);
    }

    [TestMethod]
    public async Task PendingObservationAndWarmupRestoreFromConsumedPositionAsync()
    {
        var tokenizer = await TokenizerAsync();
        var root = Root();
        var path = Path.Combine(root, "data.jsonl");
        await File.WriteAllLinesAsync(path, Enumerable.Range(0, 24).Select(i => $"{{\"text\":\"Test {i}\"}}"));
        var model = Create(tokenizer, path, selectionFraction: 0.3, controller: new StructureOptions(2, UseNodeFeatures: true));
        var initial = model.ExportCheckpoint();
        using var cancel = new CancellationTokenSource();
        var capture = new Capture(p => { if (p.Stage == "structure") cancel.Cancel(); });
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => model.RunPipelineAsync(tokenizer, root, progress: capture, cancellationToken: cancel.Token));
        var restored = await RecursiveNetwork.LoadCheckpointAsync(capture.Values.Last(p => p.Stage == "cancelled").Checkpoint!);
        await restored.RunPipelineAsync(tokenizer, root);
        var continuous = RecursiveNetwork.ImportCheckpoint(initial);
        await continuous.RunPipelineAsync(tokenizer, root);
        Assert.AreEqual(continuous.PipelineTraining!.TrainedSamples, restored.PipelineTraining!.TrainedSamples);
        CollectionAssert.AreEqual(continuous.CreateTokenExecutor().Run(new[] { 1, 2 }).Predictions[1].Logits.ToArray(),
            restored.CreateTokenExecutor().Run(new[] { 1, 2 }).Predictions[1].Logits.ToArray());
    }

    [TestMethod]
    public async Task TimingsSeparateTrainingAndArchiveCostsAsync()
    {
        var tokenizer = await TokenizerAsync();
        var root = Root();
        var path = Path.Combine(root, "timing.jsonl");
        await File.WriteAllLinesAsync(path, Enumerable.Range(0, 5).Select(i => $"{{\"text\":\"Hello {i}\"}}"));
        var model = Create(tokenizer, path);
        var capture = new Capture(_ => { });
        var result = await model.RunPipelineAsync(tokenizer, root, progress: capture);
        var timing = capture.Values[^1].Timings!;
        Assert.AreEqual((long)Directory.GetFiles(result.Directory, "checkpoint-*.json").Length, timing.Saves);
        Assert.AreEqual(new FileInfo(result.LastCheckpoint).Length, timing.LastCheckpointBytes);
        Assert.IsTrue(timing.TrainingSeconds > 0 && timing.SerializationSeconds > 0 && timing.WriteSeconds > 0);
        Assert.IsTrue(timing.TrainingSeconds + timing.EvaluationSeconds + timing.SerializationSeconds + timing.WriteSeconds + timing.RetentionSeconds <= timing.ElapsedSeconds);
        Assert.AreEqual("saving", capture.Values[0].Stage);
        TestContext.WriteLine(System.Text.Json.JsonSerializer.Serialize(timing));
    }

    private RecursiveNetwork Create(MiniMindTextTokenizer tokenizer, string path, int epochs = 1,
        double selectionFraction = 0.01, StructureOptions? controller = null)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        var model = new RecursiveNetwork(graph, new NetworkBudget(MaxNodes: 6));
        model.BindText(new TokenBindings(tokenizer.Identity, new[] { "n" }, new[] { "n" },
            Enumerable.Range(0, tokenizer.VocabularySize).Select(i => (IReadOnlyList<double>)new[] { (double)i / tokenizer.VocabularySize }).ToArray()));
        model.ConfigurePipeline(path, "pretrain", tokenizer, new PipelineOptions(epochs, 2, 1, 2),
            new JsonlDatasetOptions(MaxSampleTokens: 8), new DatasetPartitionOptions(selectionFraction, 0.01), new AdamWOptions(), controller);
        return model;
    }

    private string Root()
    {
        var path = Path.Combine(Path.GetTempPath(), "pipeline-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        TestContext.WriteLine(path);
        return path;
    }

    private sealed class Capture(Action<PipelineProgress> action) : IProgress<PipelineProgress>
    {
        internal List<PipelineProgress> Values { get; } = [];
        public void Report(PipelineProgress value) { Values.Add(value); action(value); }
    }

    private static Task<MiniMindTextTokenizer> TokenizerAsync() => MiniMindTextTokenizer.LoadAsync(
        Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json"),
        Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json"));
}
