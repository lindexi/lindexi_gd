using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class SessionCheckpointTests
{
    [TestMethod]
    public void RestoredModelAndSessionProduceSameNextOutput()
    {
        var network = Create();
        var executor = network.CreateTokenExecutor();
        var first = executor.Run(new[] { 0, 1 });
        var state = executor.ExportState(first.State);
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint()).CreateTokenExecutor();
        var expected = executor.Run(new[] { 0 }, state: first.State);
        var actual = restored.Run(new[] { 0 }, state: restored.ImportState(state));
        CollectionAssert.AreEqual(expected.Predictions[0].Logits.ToArray(), actual.Predictions[0].Logits.ToArray());
    }

    [TestMethod]
    public void ParameterUpdateRejectsOldSession()
    {
        var network = Create();
        var executor = network.CreateTokenExecutor();
        var state = executor.ExportState(executor.Run(new[] { 0 }).State);
        network.TrainTokens(new[] { 0, 1 });
        Assert.ThrowsException<InvalidDataException>(() => network.CreateTokenExecutor().ImportState(state));
    }

    [TestMethod]
    public void FractalizationRejectsImplicitSessionMigration()
    {
        var network = Create();
        var executor = network.CreateExecutor();
        var state = executor.ExportState(executor.Run(Inputs(1)).State);
        network.Fractalize("n");
        Assert.ThrowsException<InvalidDataException>(() => network.CreateExecutor().ImportState(state));
    }

    [TestMethod]
    public void ScalarSessionRestorePreservesTime()
    {
        var network = Create();
        var executor = network.CreateExecutor();
        var state = executor.ExportState(executor.Run(Inputs(1)).State);
        Assert.AreEqual(1L, network.CreateExecutor().ImportState(state).Step);
    }

    [TestMethod]
    public void InvalidHistoryLengthIsRejected()
    {
        var executor = Create().CreateExecutor();
        var json = JsonNode.Parse(executor.ExportState(executor.Run(Inputs(1)).State))!;
        json["History"]!["n"] = new JsonArray();
        Assert.ThrowsException<InvalidDataException>(() => executor.ImportState(Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [TestMethod]
    public void UnknownHistoryNodeIsRejected()
    {
        var executor = Create().CreateExecutor();
        var json = JsonNode.Parse(executor.ExportState(executor.Run(Inputs(1)).State))!;
        json["History"]!.AsObject().Remove("n");
        json["History"]!["unknown"] = new JsonArray(0);
        Assert.ThrowsException<InvalidDataException>(() => executor.ImportState(Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [TestMethod]
    public void SessionByteBudgetIsEnforced()
    {
        var executor = Create().CreateExecutor();
        var bytes = executor.ExportState(executor.Run(Inputs(1)).State);
        Assert.ThrowsException<InvalidDataException>(() => executor.ImportState(bytes, 1));
    }

    private static IReadOnlyDictionary<string, double>[] Inputs(double value) =>
        new IReadOnlyDictionary<string, double>[] { new Dictionary<string, double> { ["n"] = value } };

    private static RecursiveNetwork Create()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var node = graph.CreateAndAddElement(new Neuron(), "n");
        graph.AddEdge(node, node, new NeuralConnection(new ScalarParameter(0.5), 1));
        var network = new RecursiveNetwork(graph);
        network.BindText(new TokenBindings("test-v1", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }, 2));
        return network;
    }
}
