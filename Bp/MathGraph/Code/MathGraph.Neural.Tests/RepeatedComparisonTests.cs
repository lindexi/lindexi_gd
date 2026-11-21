using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class RepeatedComparisonTests
{
    [TestMethod]
    public void SummaryMeanMatchesRawReports()
    {
        var report = Run(Create());
        var expected = report.Runs.Average(r => r.Arms.Single(a => a.Mode == StructureComparisonMode.LearnedGrowth).FinalTest.MeanCrossEntropy);
        Assert.AreEqual(expected, report.Summary.Single(s => s.Mode == StructureComparisonMode.LearnedGrowth).MeanTestLoss, 1e-12);
    }

    [TestMethod]
    public void SampleDeviationMatchesTwoRunFormula()
    {
        var report = Run(Create());
        var values = report.Runs.Select(r => r.Arms.Single(a => a.Mode == StructureComparisonMode.LearnedGrowth).FinalTest.MeanCrossEntropy).ToArray();
        Assert.AreEqual(Math.Abs(values[0] - values[1]) / Math.Sqrt(2),
            report.Summary.Single(s => s.Mode == StructureComparisonMode.LearnedGrowth).SampleStandardDeviation, 1e-12);
    }

    [TestMethod]
    public void RunsShareInitialCheckpoint()
    {
        var report = Run(Create());
        Assert.AreEqual(1, report.Runs.Select(r => r.InitialCheckpointSha256).Distinct().Count());
    }

    [TestMethod]
    public void SourceRemainsUnchanged()
    {
        var model = Create();
        var before = model.ExportCheckpoint();
        Run(model);
        CollectionAssert.AreEqual(before, model.ExportCheckpoint());
    }

    [TestMethod]
    public void DuplicateSeedsAreRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => Create().CompareStructureSeeds(Partition(), new[] { 0 },
            new ulong[] { 1, 1 }, new StructureComparisonOptions(1, 1)));
    }

    [TestMethod]
    public void AggregateUpdateBudgetIsEnforced()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Create().CompareStructureSeeds(Partition(), new[] { 0 },
            new ulong[] { 1, 2 }, new StructureComparisonOptions(1, 200)));
    }

    [TestMethod]
    public void CancellationDoesNotModifySource()
    {
        var model = Create();
        var before = model.ExportCheckpoint();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => model.CompareStructureSeeds(Partition(), new[] { 0 },
            new ulong[] { 1, 2 }, new StructureComparisonOptions(1, 1), cancellationToken: cancellation.Token));
        CollectionAssert.AreEqual(before, model.ExportCheckpoint());
    }

    private static RepeatedComparisonReport Run(RecursiveNetwork model) => model.CompareStructureSeeds(Partition(),
        new[] { 0 }, new ulong[] { 1, 2 }, new StructureComparisonOptions(1, 1));

    private static DatasetPartition Partition() => DatasetPartition.Create(new TokenDataset("test",
        Enumerable.Range(0, 8).Select(i => new[] { i % 2, (i >> 1) % 2, (i >> 2) % 2 })), new DatasetPartitionOptions(0.25, 0.25));

    private static RecursiveNetwork Create()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        var model = new RecursiveNetwork(graph, new NetworkBudget(MaxNodes: 6));
        model.BindText(new TokenBindings("test", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }));
        return model;
    }
}
