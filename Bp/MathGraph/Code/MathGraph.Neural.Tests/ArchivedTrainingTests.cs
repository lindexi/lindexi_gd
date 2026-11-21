using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class ArchivedTrainingTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task DefaultArchivesInitialAndCompletionForShortRunAsync()
    {
        var model = Create();
        var result = await model.RunTrainingAsync(Data(), Root());
        var files = Directory.GetFiles(result.Directory, "checkpoint-*.json").Order().ToArray();
        var counts = new List<long>();
        foreach (var file in files) counts.Add((await RecursiveNetwork.LoadCheckpointAsync(file)).TrainingJob!.CompletedSamples);
        CollectionAssert.AreEqual(new long[] { 0, 3 }, counts.ToArray());
    }

    [TestMethod]
    public async Task HistoricalCheckpointResumesIdenticallyAsync()
    {
        var model = Create();
        var first = await model.RunTrainingAsync(Data(), Root(), new TrainingArchiveOptions(SaveEveryUpdates: 1));
        var history = Directory.GetFiles(first.Directory, "*periodic.json").Order().First();
        var restored = await RecursiveNetwork.LoadCheckpointAsync(history);
        await restored.RunTrainingAsync(Data(), Root());
        CollectionAssert.AreEqual(model.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public async Task LargerIntervalStillArchivesFinalUpdateAsync()
    {
        var result = await Create().RunTrainingAsync(Data(), Root(), new TrainingArchiveOptions(SaveEveryUpdates: 10));
        Assert.AreEqual(2, Directory.GetFiles(result.Directory, "checkpoint-*.json").Length);
    }

    [TestMethod]
    public async Task CancellationArchivesConsistentStateAsync()
    {
        var model = Create();
        var root = Root();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => model.RunTrainingAsync(Data(), root, cancellationToken: cancellation.Token));
        var archive = Directory.GetFiles(root, "*cancelled.json", SearchOption.AllDirectories).Single();
        CollectionAssert.AreEqual(model.ExportCheckpoint(), (await RecursiveNetwork.LoadCheckpointAsync(archive)).ExportCheckpoint());
    }

    [TestMethod]
    public async Task EveryStructureDecisionAndFeedbackIsArchivedAsync()
    {
        var model = Create();
        model.ConfigureStructureController(new StructureOptions(1), 1);
        var result = await model.RunTrainingAsync(Data(), Root(), structureSelection: Selection());
        CollectionAssert.AreEqual(new[] { 3, 3 }, new[] {
            Directory.GetFiles(result.Directory, "*structure.json").Length,
            Directory.GetFiles(result.Directory, "*feedback.json").Length });
    }

    [TestMethod]
    public async Task PendingStructureArchiveResumesIdenticalTrainingAsync()
    {
        var model = Create();
        model.ConfigureStructureController(new StructureOptions(3), 1);
        var result = await model.RunTrainingAsync(Data(), Root(), structureSelection: Selection());
        var pending = Directory.GetFiles(result.Directory, "*structure.json").Single();
        var restored = await RecursiveNetwork.LoadCheckpointAsync(pending);
        await restored.RunTrainingAsync(Data(), Root(), structureSelection: Selection());
        CollectionAssert.AreEqual(model.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public async Task UnwritableArchiveLocationDoesNotStartTrainingAsync()
    {
        var model = Create();
        var path = Root();
        await File.WriteAllTextAsync(path, "occupied");
        await Assert.ThrowsExceptionAsync<IOException>(() => model.RunTrainingAsync(Data(), path));
        Assert.AreEqual(0L, model.TrainingJob!.CompletedSamples);
    }

    [TestMethod]
    public async Task RepeatedRunsPreserveExistingArchivesAsync()
    {
        var root = Root();
        var model = Create();
        var first = await model.RunTrainingAsync(Data(), root);
        var before = await File.ReadAllBytesAsync(first.LastCheckpoint);
        await model.RunTrainingAsync(Data(), root);
        CollectionAssert.AreEqual(before, await File.ReadAllBytesAsync(first.LastCheckpoint));
    }

    [TestMethod]
    public async Task NumericalFailureArchivesLastConsistentModelAsync()
    {
        var model = Create(new AdamWOptions(LearningRate: double.MaxValue, WeightDecay: double.MaxValue));
        var before = model.ExportCheckpoint();
        var root = Root();
        await Assert.ThrowsExceptionAsync<ArithmeticException>(() => model.RunTrainingAsync(Data(), root));
        var file = Directory.GetFiles(root, "*numerical-error.json", SearchOption.AllDirectories).Single();
        CollectionAssert.AreEqual(before, (await RecursiveNetwork.LoadCheckpointAsync(file)).ExportCheckpoint());
    }

    [TestMethod]
    public async Task RetentionAppliesAcrossRunDirectoriesAsync()
    {
        var root = Root();
        var first = await Create().RunTrainingAsync(Data(), root, new TrainingArchiveOptions(MaxCheckpoints: 2));
        var second = await Create().RunTrainingAsync(Data(), root, new TrainingArchiveOptions(MaxCheckpoints: 2));
        Assert.AreEqual(2, Directory.GetFiles(root, "checkpoint-*.json", SearchOption.AllDirectories).Length);
        Assert.IsFalse(File.Exists(first.LastCheckpoint));
        Assert.AreEqual(second.LastCheckpoint, CheckpointArchive.FindLatest(root));
        Assert.AreEqual(3L, (await RecursiveNetwork.LoadCheckpointAsync(second.LastCheckpoint)).TrainingJob!.CompletedSamples);
    }

    [TestMethod]
    public async Task TemporaryFilesAreNotSelectedAsLatestAsync()
    {
        var root = Root();
        var result = await Create().RunTrainingAsync(Data(), root);
        await File.WriteAllTextAsync(Path.Combine(result.Directory, "checkpoint-incomplete.json.tmp"), "partial");
        Assert.AreEqual(result.LastCheckpoint, CheckpointArchive.FindLatest(root));
    }

    [TestMethod]
    public void DefaultPolicyUsesTenUpdatesAndOneHundredArchives()
    {
        Assert.AreEqual(new TrainingArchiveOptions(10, 600, 120, 100), new TrainingArchiveOptions());
    }

    private string Root()
    {
        var path = Path.Combine(Path.GetTempPath(), "mathgraph-archive-" + Guid.NewGuid().ToString("N"));
        TestContext.WriteLine(path);
        return path;
    }

    private static TokenDataset Data() => new("test", new[] { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 0, 1, 0 } });
    private static TokenDataset Selection() => new("test", new[] { new[] { 1, 0, 0 } });

    private static RecursiveNetwork Create(AdamWOptions? optimizer = null)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(new ScalarParameter(0.1)), "n");
        var model = new RecursiveNetwork(graph, new NetworkBudget(MaxNodes: 6));
        model.BindText(new TokenBindings("test", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }));
        model.ConfigureTrainingJob(Data(), optimizer: optimizer);
        return model;
    }
}
