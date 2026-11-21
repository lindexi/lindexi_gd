using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class TrainingJobTests
{
    [TestMethod]
    public void RestoredJobContinuesWithoutResupplyingOptimizer()
    {
        var dataset = Dataset();
        var network = Create();
        network.ConfigureTrainingJob(dataset, 2, new AdamWOptions(LearningRate: 0.02));
        network.TrainNextSample(dataset);
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint());
        network.TrainNextSample(dataset);
        restored.TrainNextSample(dataset);
        CollectionAssert.AreEqual(network.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public void CursorWrapsAtEpochBoundary()
    {
        var dataset = Dataset();
        var network = Create();
        network.ConfigureTrainingJob(dataset, 2);
        network.TrainNextSample(dataset);
        network.TrainNextSample(dataset);
        Assert.AreEqual(0, network.TrainingJob!.NextSampleIndex);
    }

    [TestMethod]
    public void CompletedJobRejectsAdditionalUpdate()
    {
        var dataset = Dataset();
        var network = Create();
        network.ConfigureTrainingJob(dataset);
        network.TrainNextSample(dataset);
        network.TrainNextSample(dataset);
        Assert.ThrowsException<InvalidOperationException>(() => network.TrainNextSample(dataset));
    }

    [TestMethod]
    public void DifferentDatasetDoesNotAdvanceModelOrCursor()
    {
        var network = Create();
        network.ConfigureTrainingJob(Dataset());
        var before = network.ExportCheckpoint();
        var changed = new TokenDataset("test", new IReadOnlyList<int>[] { new[] { 1, 1 }, new[] { 0, 0 } });
        Assert.ThrowsException<ArgumentException>(() => network.TrainNextSample(changed));
        CollectionAssert.AreEqual(before, network.ExportCheckpoint());
    }

    [TestMethod]
    public void CancelledTrainingDoesNotAdvanceCursor()
    {
        var dataset = Dataset();
        var network = Create();
        network.ConfigureTrainingJob(dataset);
        var before = network.ExportCheckpoint();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => network.TrainNextSample(dataset, cancellation.Token));
        CollectionAssert.AreEqual(before, network.ExportCheckpoint());
    }

    [TestMethod]
    public void DirectTrainingCannotBypassJobCursor()
    {
        var network = Create();
        network.ConfigureTrainingJob(Dataset());
        Assert.ThrowsException<InvalidOperationException>(() => network.TrainTokens(new[] { 0, 1 }));
    }

    [TestMethod]
    public void DatasetCopiesCallerOwnedSamples()
    {
        var sample = new[] { 0, 1 };
        var dataset = new TokenDataset("test", new[] { sample });
        sample[0] = 1;
        Assert.AreEqual(0, dataset[0][0]);
    }

    [TestMethod]
    public void DatasetIdentityIncludesTokenizer()
    {
        var samples = new[] { new[] { 0, 1 } };
        Assert.AreNotEqual(new TokenDataset("a", samples).Identity, new TokenDataset("b", samples).Identity);
    }

    [TestMethod]
    public void InvalidOptimizerIsRejectedBeforeConfiguringJob()
    {
        var network = Create();
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => network.ConfigureTrainingJob(Dataset(), optimizer: new AdamWOptions(Beta1: 1)));
        Assert.IsNull(network.TrainingJob);
    }

    [TestMethod]
    public void InvalidRestoredCursorIsRejected()
    {
        var network = Create();
        network.ConfigureTrainingJob(Dataset());
        var json = JsonNode.Parse(network.ExportCheckpoint())!;
        json["TrainingJob"]!["CompletedSamples"] = 3;
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [TestMethod]
    public void SampleWindowBudgetIsValidatedBeforeTraining()
    {
        var network = Create(new NetworkBudget(MaxSteps: 2));
        var dataset = new TokenDataset("test", new[] { new[] { 0, 1, 0 } });
        Assert.ThrowsException<ArgumentException>(() => network.ConfigureTrainingJob(dataset));
    }

    private static TokenDataset Dataset() => new("test", new[] { new[] { 0, 1, 0 }, new[] { 1, 0, 1 } });

    private static RecursiveNetwork Create(NetworkBudget? budget = null)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        var network = new RecursiveNetwork(graph, budget);
        network.BindText(new TokenBindings("test", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }));
        return network;
    }
}
