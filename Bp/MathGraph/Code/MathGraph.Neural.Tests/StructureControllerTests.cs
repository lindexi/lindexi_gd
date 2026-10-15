using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class StructureControllerTests
{
    [TestMethod]
    public void ControllerSelectsAndPublishesLegalFractal()
    {
        var network = Create();
        network.ConfigureStructureController(new StructureOptions(1), 1);
        var decision = network.BeginStructureObservation(1);
        Assert.AreEqual("n", decision.NodeId);
    }

    [TestMethod]
    public void NoRemainingBudgetSelectsKeep()
    {
        var network = Create(new NetworkBudget(MaxNodes: 1));
        network.ConfigureStructureController();
        Assert.IsNull(network.BeginStructureObservation(1).NodeId);
    }

    [TestMethod]
    public void RewardSubtractsDeclaredNodeCost()
    {
        var network = Create();
        network.ConfigureStructureController(new StructureOptions(1, CostPerNode: 0.1), 1);
        network.BeginStructureObservation(2);
        Train(network);
        Assert.AreEqual(0.5, network.CompleteStructureObservation(1).Reward, 1e-12);
    }

    [TestMethod]
    public void ObservationRequiresSuccessfulUpdates()
    {
        var network = Create();
        network.ConfigureStructureController(new StructureOptions(2));
        network.BeginStructureObservation(2);
        Train(network);
        Assert.ThrowsException<InvalidOperationException>(() => network.CompleteStructureObservation(1));
    }

    [TestMethod]
    public void PendingObservationPreventsAnotherDecision()
    {
        var network = Create();
        network.ConfigureStructureController();
        network.BeginStructureObservation(2);
        Assert.ThrowsException<InvalidOperationException>(() => network.BeginStructureObservation(1));
    }

    [TestMethod]
    public void PendingObservationRestoresIdenticalLearningUpdate()
    {
        var network = Create();
        network.ConfigureStructureController(new StructureOptions(1));
        network.BeginStructureObservation(2);
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint());
        Train(network);
        Train(restored);
        network.CompleteStructureObservation(1);
        restored.CompleteStructureObservation(1);
        CollectionAssert.AreEqual(network.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public void RandomStateRestoresNextDecision()
    {
        var network = Create();
        network.ConfigureStructureController(new StructureOptions(1));
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint());
        Assert.AreEqual(network.BeginStructureObservation(2), restored.BeginStructureObservation(2));
    }

    [TestMethod]
    public void InvalidFeedbackDoesNotModifyPendingState()
    {
        var network = Create();
        network.ConfigureStructureController(new StructureOptions(1));
        network.BeginStructureObservation(2);
        Train(network);
        var before = network.ExportCheckpoint();
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => network.CompleteStructureObservation(double.NaN));
        CollectionAssert.AreEqual(before, network.ExportCheckpoint());
    }

    [TestMethod]
    public void CancellationDoesNotConsumeRandomState()
    {
        var network = Create();
        network.ConfigureStructureController();
        var before = network.ExportCheckpoint();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => network.BeginStructureObservation(1, cancellation.Token));
        CollectionAssert.AreEqual(before, network.ExportCheckpoint());
    }

    [TestMethod]
    public void InvalidPendingProbabilitiesAreRejected()
    {
        var network = Create();
        network.ConfigureStructureController();
        network.BeginStructureObservation(1);
        var json = JsonNode.Parse(network.ExportCheckpoint())!;
        json["Controller"]!["Pending"]!["Probabilities"]![0] = -1;
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [TestMethod]
    public void PositiveRewardRaisesSelectedActionScore()
    {
        var network = Create();
        network.ConfigureStructureController(new StructureOptions(1, CostPerNode: 0), 1);
        network.BeginStructureObservation(2);
        Train(network);
        network.CompleteStructureObservation(1);
        var json = JsonNode.Parse(network.ExportCheckpoint())!;
        Assert.IsTrue(json["Controller"]!["Scores"]!["n"]!.GetValue<double>() > 0);
    }

    [TestMethod]
    public void LegacyModelWithoutControllerRemainsReadable()
    {
        var json = JsonNode.Parse(Create().ExportCheckpoint())!.AsObject();
        json["Format"] = 1;
        json.Remove("Controller");
        json.Remove("TrainingUpdates");
        Assert.AreEqual(1, RecursiveNetwork.ImportCheckpoint(Encoding.UTF8.GetBytes(json.ToJsonString())).Graph.ElementList.Count);
    }

    [TestMethod]
    public void ManualFractalizationCannotContaminatePendingReward()
    {
        var network = Create();
        network.ConfigureStructureController();
        network.BeginStructureObservation(1);
        Assert.ThrowsException<InvalidOperationException>(() => network.Fractalize("n"));
    }

    [TestMethod]
    public void EvaluationDoesNotMutateTrainingState()
    {
        var network = CreateTextNetwork();
        var before = network.ExportCheckpoint();
        network.EvaluateTokens(new[] { 0, 1, 0 });
        CollectionAssert.AreEqual(before, network.ExportCheckpoint());
    }

    [TestMethod]
    public void EvaluationMatchesTrainingPreUpdateLoss()
    {
        var network = CreateTextNetwork();
        var expected = network.EvaluateTokens(new[] { 0, 1, 0 });
        Assert.AreEqual(expected, network.TrainTokens(new[] { 0, 1, 0 }), 1e-12);
    }

    [TestMethod]
    public void TokenTrainingCompletesStructureRewardLoop()
    {
        var network = CreateTextNetwork();
        network.ConfigureStructureController(new StructureOptions(1));
        var initial = network.EvaluateTokens(new[] { 1, 0, 1 });
        var decision = network.BeginStructureObservation(initial);
        network.TrainTokens(new[] { 0, 1, 0 });
        var final = network.EvaluateTokens(new[] { 1, 0, 1 });
        var feedback = network.CompleteStructureObservation(final);
        Assert.AreEqual(initial - final - 0.001 * decision.AddedNodes, feedback.Reward, 1e-12);
    }

    private static RecursiveNetwork CreateTextNetwork()
    {
        var network = Create();
        network.BindText(new TokenBindings("test-v1", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }));
        return network;
    }

    private static void Train(RecursiveNetwork network) => network.TrainSquaredError(
        new IReadOnlyDictionary<string, double>[] { new Dictionary<string, double> { ["n"] = 1 } }, "n", new[] { 0d });

    private static RecursiveNetwork Create(NetworkBudget? budget = null)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        return new RecursiveNetwork(graph, budget);
    }
}
