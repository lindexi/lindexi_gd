using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class ConnectionBudgetTests
{
    [TestMethod]
    public void ParallelEdgesCountSeparatelyEvenWhenSharingWeight()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var a = graph.CreateAndAddElement(new Neuron(), "a");
        var b = graph.CreateAndAddElement(new Neuron(), "b");
        var weight = new ScalarParameter(0.2);
        graph.AddEdge(a, b, new NeuralConnection(weight));
        graph.AddEdge(a, b, new NeuralConnection(weight));
        Assert.ThrowsException<InvalidOperationException>(() => new RecursiveNetwork(graph, new NetworkBudget(MaxConnections: 1)));
    }

    [TestMethod]
    public void FailedFractalizationPreservesCheckpoint()
    {
        var model = Create(5);
        var before = model.ExportCheckpoint();
        Assert.ThrowsException<InvalidOperationException>(() => model.Fractalize("n"));
        CollectionAssert.AreEqual(before, model.ExportCheckpoint());
    }

    [TestMethod]
    public void ExactlySixConnectionsPermitOneFractalization()
    {
        var model = Create(6);
        model.Fractalize("n");
        Assert.AreEqual(6, model.GetNodeObservations().Sum(n => n.OutgoingConnections));
    }

    [TestMethod]
    public void BudgetIsGlobalAcrossRecursiveGraphs()
    {
        var model = Create(11);
        model.Fractalize("n");
        var child = model.Graph.ElementList[0].Value.Child!.Input.Id;
        Assert.ThrowsException<InvalidOperationException>(() => model.Fractalize(child));
    }

    [TestMethod]
    public void ControllerOnlyKeepsWhenNoConnectionBudgetRemains()
    {
        var model = Create(5);
        model.ConfigureStructureController();
        Assert.IsNull(model.BeginStructureObservation(1).NodeId);
    }

    [TestMethod]
    public void ZeroConnectionBudgetAllowsIsolatedNeuron()
    {
        Assert.AreEqual(1, Create(0).GetNodeObservations().Count);
    }

    [TestMethod]
    public void NegativeConnectionBudgetIsRejected()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Create(-1));
    }

    [TestMethod]
    public void CheckpointCannotRaiseCallerConnectionLimit()
    {
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(Create(6).ExportCheckpoint(),
            new NetworkBudget(MaxConnections: 5)));
    }

    [TestMethod]
    public void LegacyCheckpointUsesDefaultConnectionBudget()
    {
        var json = JsonNode.Parse(Create(6).ExportCheckpoint())!;
        json["Budget"]!.AsObject().Remove("MaxConnections");
        var restored = RecursiveNetwork.ImportCheckpoint(Encoding.UTF8.GetBytes(json.ToJsonString()));
        Assert.AreEqual(new NetworkBudget().MaxConnections, restored.Budget.MaxConnections);
    }

    private static RecursiveNetwork Create(int maxConnections)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        return new RecursiveNetwork(graph, new NetworkBudget(MaxConnections: maxConnections));
    }
}
