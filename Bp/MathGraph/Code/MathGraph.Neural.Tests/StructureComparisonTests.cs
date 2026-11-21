using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class StructureComparisonTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void FourArmsUseSameUpdateBudget()
    {
        var report = Create().CompareStructures(Partition(), new[] { 0, -1 });
        CollectionAssert.AreEqual(new[] { 4, 4, 4, 4 }, report.Arms.Select(a => a.Rounds.Sum(r => r.Training.Count)).ToArray());
    }

    [TestMethod]
    public void ComparisonDoesNotModifySourceModel()
    {
        var model = Create();
        var before = model.ExportCheckpoint();
        model.CompareStructures(Partition(), new[] { 0, -1 });
        CollectionAssert.AreEqual(before, model.ExportCheckpoint());
    }

    [TestMethod]
    public void FixedArmRetainsInitialNodeCount()
    {
        var report = Create().CompareStructures(Partition(), new[] { 0, -1 });
        Assert.AreEqual(1, report.Arms.Single(a => a.Mode == StructureComparisonMode.FixedInitial).FinalNodeCount);
    }

    [TestMethod]
    public void PresetArmFollowsDeclaredGrowthSchedule()
    {
        var report = Create().CompareStructures(Partition(), new[] { 0, -1 });
        Assert.AreEqual(6, report.Arms.Single(a => a.Mode == StructureComparisonMode.PresetGrowth).FinalNodeCount);
    }

    [TestMethod]
    public void RepeatedComparisonReproducesNumericalResults()
    {
        var model = Create();
        var partition = Partition();
        var first = model.CompareStructures(partition, new[] { 0, -1 });
        var second = model.CompareStructures(partition, new[] { 0, -1 });
        CollectionAssert.AreEqual(first.Arms.SelectMany(a => a.Rounds.Select(r => r.SelectionLossAfter)).ToArray(),
            second.Arms.SelectMany(a => a.Rounds.Select(r => r.SelectionLossAfter)).ToArray());
    }

    [TestMethod]
    public void FinalTestIdentityIsReportedForEveryArm()
    {
        var partition = Partition();
        var report = Create().CompareStructures(partition, new[] { 0, -1 });
        CollectionAssert.AreEqual(Enumerable.Repeat(partition.Test.Identity, 4).ToArray(), report.Arms.Select(a => a.FinalTest.DatasetIdentity).ToArray());
    }

    [TestMethod]
    public void InvalidPresetDoesNotChangeSource()
    {
        var model = Create();
        var before = model.ExportCheckpoint();
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => model.CompareStructures(Partition(), new[] { 99, -1 }));
        CollectionAssert.AreEqual(before, model.ExportCheckpoint());
    }

    [TestMethod]
    public void CancelledComparisonDoesNotChangeSource()
    {
        var model = Create();
        var before = model.ExportCheckpoint();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => model.CompareStructures(Partition(), new[] { 0, -1 }, cancellationToken: cancellation.Token));
        CollectionAssert.AreEqual(before, model.ExportCheckpoint());
    }

    [TestMethod]
    public void ComparisonProducesInspectableCpuReport()
    {
        var report = Create().CompareStructures(Partition(), new[] { 0, -1 });
        TestContext.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Assert.IsTrue(report.Arms.All(a => double.IsFinite(a.FinalTest.MeanCrossEntropy) && a.FinalNodeCount <= report.Budget.MaxNodes));
    }

    [TestMethod]
    public void FinalTestLabelsCannotInfluenceTrainingOrStructureSelection()
    {
        var samples = Enumerable.Range(0, 12).Select(i => new[] { i % 2, (i >> 1) % 2, (i >> 2) % 2, (i >> 3) % 2 }).ToArray();
        var config = new DatasetPartitionOptions(0.25, 0.25);
        var firstPartition = DatasetPartition.Create(new TokenDataset("test", samples), config);
        var masks = samples.Select(_ => (IReadOnlyList<bool>)new[] { true, true, true, true }).ToArray();
        foreach (var index in firstPartition.TestIndices) masks[index] = new[] { false, false, false, true };
        var secondPartition = DatasetPartition.Create(new TokenDataset("test", samples, labelMasks: masks), config);
        var model = Create();
        var first = model.CompareStructures(firstPartition, new[] { 0, -1 });
        var second = model.CompareStructures(secondPartition, new[] { 0, -1 });
        CollectionAssert.AreEqual(first.Arms.SelectMany(a => a.Rounds.Select(r => r.SelectionLossAfter)).ToArray(),
            second.Arms.SelectMany(a => a.Rounds.Select(r => r.SelectionLossAfter)).ToArray());
    }

    [TestMethod]
    public void RandomArmReportsUniformActionProbability()
    {
        var report = Create().CompareStructures(Partition(), new[] { 0, -1 });
        var rounds = report.Arms.Single(a => a.Mode == StructureComparisonMode.RandomGrowth).Rounds;
        Assert.IsTrue(rounds.All(r => Math.Abs(r.ActionProbability - 1.0 / (r.CandidateCount + 1)) < 1e-12));
    }

    [TestMethod]
    public void LearnedFeedbackChangesLaterActionDistribution()
    {
        var report = Create().CompareStructures(Partition(), new[] { 0, -1 }, controller: new StructureOptions(2, CostPerNode: 1));
        var later = report.Arms.Single(a => a.Mode == StructureComparisonMode.LearnedGrowth).Rounds[1];
        Assert.IsTrue(Math.Abs(later.ActionProbability - 1.0 / (later.CandidateCount + 1)) > 1e-8);
    }

    private static DatasetPartition Partition()
    {
        var samples = Enumerable.Range(0, 12).Select(i => new[] { i % 2, (i >> 1) % 2, (i >> 2) % 2, (i >> 3) % 2 });
        return DatasetPartition.Create(new TokenDataset("test", samples), new DatasetPartitionOptions(0.25, 0.25));
    }

    private static RecursiveNetwork Create()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var n = graph.CreateAndAddElement(new Neuron(new ScalarParameter(0.1)), "n");
        graph.AddEdge(n, n, new NeuralConnection(new ScalarParameter(0.2), 1));
        var model = new RecursiveNetwork(graph, new NetworkBudget(MaxNodes: 16));
        model.BindText(new TokenBindings("test", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }, seed: 17));
        return model;
    }
}
