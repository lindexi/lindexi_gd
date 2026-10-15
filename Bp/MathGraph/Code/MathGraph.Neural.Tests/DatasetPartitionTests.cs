using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class DatasetPartitionTests
{
    [TestMethod]
    public void SameSeedReproducesAllPartitions()
    {
        var source = Create();
        var first = DatasetPartition.Create(source);
        var second = DatasetPartition.Create(source);
        CollectionAssert.AreEqual(new[] { first.Training.Identity, first.StructureSelection.Identity, first.Test.Identity },
            new[] { second.Training.Identity, second.StructureSelection.Identity, second.Test.Identity });
    }

    [TestMethod]
    public void EverySourceSampleAppearsExactlyOnce()
    {
        var source = Create();
        var partition = DatasetPartition.Create(source);
        var indices = partition.TrainingIndices.Concat(partition.StructureSelectionIndices).Concat(partition.TestIndices).Order().ToArray();
        CollectionAssert.AreEqual(Enumerable.Range(0, source.Count).ToArray(), indices);
    }

    [TestMethod]
    public void DuplicateTokensWithDifferentLabelsRemainTogether()
    {
        var samples = new[] { new[] { 0, 1, 2 }, new[] { 0, 1, 2 }, new[] { 2, 1, 0 }, new[] { 1, 0, 2 } };
        var masks = new IReadOnlyList<bool>[] { new[] { false, false, true }, new[] { false, true, true },
            new[] { false, true, true }, new[] { false, true, true } };
        var partition = DatasetPartition.Create(new TokenDataset("test", samples, labelMasks: masks));
        var sets = new[] { partition.TrainingIndices, partition.StructureSelectionIndices, partition.TestIndices };
        Assert.AreEqual(1, sets.Count(set => set.Contains(0) && set.Contains(1)));
    }

    [TestMethod]
    public void SupervisionMasksArePreserved()
    {
        var samples = Enumerable.Range(0, 10).Select(i => new[] { i, 1, 2 }).ToArray();
        var masks = samples.Select(_ => (IReadOnlyList<bool>)new[] { false, false, true }).ToArray();
        var source = new TokenDataset("test", samples, labelMasks: masks);
        var split = DatasetPartition.Create(source);
        var actual = Enumerable.Range(0, split.Training.Count).SelectMany(i => split.Training.GetLabelMask(i)!).ToArray();
        var expected = split.TrainingIndices.SelectMany(i => masks[i]).ToArray();
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void MembershipDoesNotDependOnInputOrdering()
    {
        var samples = Enumerable.Range(0, 20).Select(i => new[] { i, 1 }).ToArray();
        var first = DatasetPartition.Create(new TokenDataset("test", samples));
        var second = DatasetPartition.Create(new TokenDataset("test", samples.AsEnumerable().Reverse()));
        CollectionAssert.AreEqual(Enumerable.Range(0, first.Test.Count).Select(i => first.Test[i][0]).Order().ToArray(),
            Enumerable.Range(0, second.Test.Count).Select(i => second.Test[i][0]).Order().ToArray());
    }

    [TestMethod]
    public void GroupFractionsHaveExpectedCounts()
    {
        var split = DatasetPartition.Create(Create(), new DatasetPartitionOptions(0.2, 0.3));
        CollectionAssert.AreEqual(new[] { 5, 2, 3 }, new[] { split.Training.Count, split.StructureSelection.Count, split.Test.Count });
    }

    [TestMethod]
    public void FewerThanThreeContentGroupsAreRejected()
    {
        var source = new TokenDataset("test", new[] { new[] { 0, 1 }, new[] { 0, 1 }, new[] { 1, 0 } });
        Assert.ThrowsException<ArgumentException>(() => DatasetPartition.Create(source));
    }

    [DataTestMethod]
    [DataRow(0d, 0.1)]
    [DataRow(0.5, 0.5)]
    [DataRow(-0.1, 0.2)]
    [DataRow(double.NaN, 0.1)]
    public void InvalidFractionsAreRejected(double selection, double test)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => DatasetPartition.Create(Create(), new DatasetPartitionOptions(selection, test)));
    }

    [TestMethod]
    public void TokenBudgetIsEnforced()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => DatasetPartition.Create(Create(), maxTotalTokens: 2));
    }

    [TestMethod]
    public void CancellationIsHonored()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => DatasetPartition.Create(Create(), cancellationToken: cancellation.Token));
    }

    private static TokenDataset Create() => new("test", Enumerable.Range(0, 10).Select(i => new[] { i, 1 }));
}
