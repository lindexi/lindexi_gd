using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class NodeStatisticsTests
{
    [DataTestMethod]
    [DataRow(0, 2d)]
    [DataRow(1, 2d)]
    [DataRow(2, 1d)]
    [DataRow(3, 1d)]
    public void TrainingStatisticsMatchHandCalculation(int field, double expected)
    {
        var network = Create();
        Train(network);
        var statistics = network.Graph.ElementList[0].Value.Statistics;
        var values = new[] { (double)statistics.Samples, statistics.ActivationMean,
            statistics.ActivationVariance, statistics.MeanAbsoluteGradient };
        Assert.AreEqual(expected, values[field], 1e-12);
    }

    [TestMethod]
    public void FailedOptimizerDoesNotCommitStatistics()
    {
        var network = Create();
        var before = network.ExportCheckpoint();
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => network.TrainSquaredError(Inputs(1, 3), "n",
            new[] { 0d, 0 }, new AdamWOptions(LearningRate: -1)));
        CollectionAssert.AreEqual(before, network.ExportCheckpoint());
    }

    [TestMethod]
    public void InferenceDoesNotChangeStatistics()
    {
        var network = Create();
        network.CreateExecutor().Run(Inputs(1, 3)).Backward(Inputs(1, 1));
        Assert.AreEqual(0L, network.Graph.ElementList[0].Value.Statistics.Samples);
    }

    [TestMethod]
    public void RestoredStatisticsContinueIdentically()
    {
        var network = Create();
        Train(network);
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint());
        Train(network);
        Train(restored);
        CollectionAssert.AreEqual(network.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public void InvalidVarianceAccumulatorIsRejected()
    {
        var network = Create();
        Train(network);
        var json = JsonNode.Parse(network.ExportCheckpoint())!;
        json["Graph"]!["Nodes"]![0]!["Statistics"]!["ActivationM2"] = -1;
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [TestMethod]
    public void ChildStatisticsStartAtZero()
    {
        var network = Create();
        Train(network);
        network.Fractalize("n");
        CollectionAssert.AreEqual(new long[5], network.Graph.ElementList[0].Value.Child!.Graph.ElementList
            .Select(n => n.Value.Statistics.Samples).ToArray());
    }

    [TestMethod]
    public void ObservationsExposeRecursiveDepth()
    {
        var network = Create();
        network.Fractalize("n");
        CollectionAssert.AreEqual(new[] { 0, 1, 1, 1, 1, 1 }, network.GetNodeObservations().Select(o => o.Depth).ToArray());
    }

    [TestMethod]
    public void CompletedActionStoresRecentReward()
    {
        var network = Create();
        network.ConfigureStructureController(new StructureOptions(1), 1);
        network.BeginStructureObservation(2);
        Train(network);
        var feedback = network.CompleteStructureObservation(1);
        Assert.AreEqual(feedback.Reward, network.Graph.ElementList[0].Value.Statistics.RecentStructureReward);
    }

    [TestMethod]
    public void TokenPaddingDoesNotAddStatisticsSamples()
    {
        var network = Create();
        network.BindText(new TokenBindings("test", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }, 3));
        network.TrainTokens(new[] { 0, -1, 1, 0 }, new[] { true, false, true, true });
        Assert.AreEqual(6L, network.Graph.ElementList[0].Value.Statistics.Samples);
    }

    private static IReadOnlyDictionary<string, double>[] Inputs(params double[] values) => values
        .Select(v => (IReadOnlyDictionary<string, double>)new Dictionary<string, double> { ["n"] = v }).ToArray();

    private static void Train(RecursiveNetwork network) => network.TrainSquaredError(Inputs(1, 3), "n", new[] { 0d, 0 });

    private static RecursiveNetwork Create()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(activation: ScalarActivation.Identity), "n");
        return new RecursiveNetwork(graph);
    }
}
