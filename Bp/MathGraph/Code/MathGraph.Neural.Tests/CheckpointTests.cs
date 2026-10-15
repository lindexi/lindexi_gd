using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class CheckpointTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void RecursiveCheckpointRoundTripPreservesAllModelData()
    {
        var network = Create();
        network.Fractalize("input", 7);
        network.TrainTokens(new[] { 0, 1, 0 });
        var bytes = network.ExportCheckpoint();
        var restored = RecursiveNetwork.ImportCheckpoint(bytes);
        CollectionAssert.AreEqual(bytes, restored.ExportCheckpoint());
    }

    [TestMethod]
    public void RestoredTrainingProducesIdenticalNextUpdate()
    {
        var network = Create();
        network.Fractalize("input", 11);
        var options = new AdamWOptions(LearningRate: 0.02);
        network.TrainTokens(new[] { 0, 1, 0 }, options: options);
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint());
        network.TrainTokens(new[] { 1, 0, 1 }, options: options);
        restored.TrainTokens(new[] { 1, 0, 1 }, options: options);
        CollectionAssert.AreEqual(network.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public void RestoredTextPredictionsAreIdentical()
    {
        var network = Create();
        network.TrainTokens(new[] { 0, 1 });
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint());
        CollectionAssert.AreEqual(network.CreateTokenExecutor().Run(new[] { 0, 1 }).Predictions[1].Logits.ToArray(),
            restored.CreateTokenExecutor().Run(new[] { 0, 1 }).Predictions[1].Logits.ToArray());
    }

    [TestMethod]
    public void SharedParametersRemainSameObjectAfterRestore()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var parameter = new ScalarParameter(0.2);
        graph.CreateAndAddElement(new Neuron(parameter), "a");
        graph.CreateAndAddElement(new Neuron(parameter), "b");
        var restored = RecursiveNetwork.ImportCheckpoint(new RecursiveNetwork(graph).ExportCheckpoint());
        Assert.AreSame(restored.Graph.ElementList[0].Value.Bias, restored.Graph.ElementList[1].Value.Bias);
    }

    [DataTestMethod]
    [DataRow("Format", "999")]
    [DataRow("Version", "-1")]
    [DataRow("Parameters", "null")]
    [DataRow("Graph", "null")]
    public void InvalidRootDataIsRejected(string name, string value)
    {
        var json = JsonNode.Parse(Create().ExportCheckpoint())!.AsObject();
        json[name] = JsonNode.Parse(value);
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [TestMethod]
    public void MissingParameterReferenceIsRejected()
    {
        var json = JsonNode.Parse(Create().ExportCheckpoint())!;
        json["Graph"]!["Nodes"]![0]!["Bias"] = Guid.NewGuid();
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [TestMethod]
    public void DuplicateParameterIdentityIsRejected()
    {
        var json = JsonNode.Parse(Create().ExportCheckpoint())!;
        var parameters = json["Parameters"]!.AsArray();
        parameters.Add(parameters[0]!.DeepClone());
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [TestMethod]
    public void NegativeOptimizerMomentIsRejected()
    {
        var json = JsonNode.Parse(Create().ExportCheckpoint())!;
        json["Parameters"]![0]!["Second"] = -1;
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [TestMethod]
    public void UnreferencedParameterIsRejected()
    {
        var json = JsonNode.Parse(Create().ExportCheckpoint())!;
        var parameter = json["Parameters"]![0]!.DeepClone();
        parameter["Id"] = Guid.NewGuid();
        json["Parameters"]!.AsArray().Add(parameter);
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [TestMethod]
    public void OversizedCheckpointIsRejectedBeforeDeserialization()
    {
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(Create().ExportCheckpoint(), maxBytes: 1));
    }

    [TestMethod]
    public void ResourceLimitCannotBeRaisedByCheckpoint()
    {
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(Create().ExportCheckpoint(), new NetworkBudget(MaxNodes: 1)));
    }

    [TestMethod]
    public void TruncatedCheckpointIsRejected()
    {
        var bytes = Create().ExportCheckpoint();
        Assert.ThrowsException<InvalidDataException>(() => RecursiveNetwork.ImportCheckpoint(bytes[..^1]));
    }

    [TestMethod]
    public async Task PublishedCheckpointLoadsIdenticallyAsync()
    {
        var path = TemporaryPath();
        var network = Create();
        await network.SaveCheckpointAsync(path);
        var restored = await RecursiveNetwork.LoadCheckpointAsync(path);
        CollectionAssert.AreEqual(network.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public async Task CancelledSavePreservesPublishedCheckpointAsync()
    {
        var path = TemporaryPath();
        var network = Create();
        await network.SaveCheckpointAsync(path);
        var before = network.ExportCheckpoint();
        network.TrainTokens(new[] { 0, 1 });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => network.SaveCheckpointAsync(path, cancellation.Token));
        CollectionAssert.AreEqual(before, (await RecursiveNetwork.LoadCheckpointAsync(path)).ExportCheckpoint());
    }

    private string TemporaryPath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        TestContext.WriteLine(path);
        return path;
    }

    private static RecursiveNetwork Create()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var input = graph.CreateAndAddElement(new Neuron(), "input");
        var output = graph.CreateAndAddElement(new Neuron(), "output");
        graph.AddEdge(input, output, new NeuralConnection(new ScalarParameter(0.7)));
        graph.AddEdge(output, input, new NeuralConnection(new ScalarParameter(0.2), 1));
        var network = new RecursiveNetwork(graph);
        network.BindText(new TokenBindings("test-v1", new[] { "input" }, new[] { "input", "output" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }, 2, 5));
        return network;
    }
}
