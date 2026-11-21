using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class GradientVerificationTests
{
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void RecursiveNonzeroGateGradientsMatchInterpreterFiniteDifference(int role)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var root = graph.CreateAndAddElement(new Neuron(new ScalarParameter(0.13)), "root");
        graph.AddEdge(root, root, new NeuralConnection(new ScalarParameter(0.21), 2));
        var network = new RecursiveNetwork(graph);
        network.Fractalize("root", 17);
        network.Fractalize(root.Value.Child!.Graph.ElementList[role].Id, 23);
        var checkpoint = JsonNode.Parse(network.ExportCheckpoint())!;
        foreach (var parameter in checkpoint["Parameters"]!.AsArray())
            parameter!["Value"] = 0.27;
        network = Restore(checkpoint);
        VerifyAllParameters(network, Inputs("root", 0.4, -0.2, 0.7, 0.1), Inputs("root", 0.3, -0.4, 0.2, 1));
    }

    [DataTestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void SharedBiasAndParallelDelayedWeightsMatchFiniteDifference(int delay)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var shared = new ScalarParameter(0.23);
        var a = graph.CreateAndAddElement(new Neuron(shared), "a");
        var b = graph.CreateAndAddElement(new Neuron(shared), "b");
        graph.AddEdge(a, b, new NeuralConnection(shared));
        graph.AddEdge(a, b, new NeuralConnection(shared));
        graph.AddEdge(b, a, new NeuralConnection(shared, delay));
        VerifyAllParameters(new RecursiveNetwork(graph), Inputs("a", 0.1, 0.4, -0.2, 0.3, 0.5),
            Inputs("b", 0.2, -0.4, 0.7, 0.1, 1));
    }

    [DataTestMethod]
    [DataRow(ScalarActivation.Identity, -0.4)]
    [DataRow(ScalarActivation.Tanh, -0.4)]
    [DataRow(ScalarActivation.Relu, -0.4)]
    [DataRow(ScalarActivation.Relu, 0.4)]
    public void ActivationDerivativeMatchesFiniteDifference(ScalarActivation activation, double input)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(new ScalarParameter(0.1), activation), "n");
        VerifyAllParameters(new RecursiveNetwork(graph), Inputs("n", input), Inputs("n", 1));
    }

    [TestMethod]
    public void ContinuationTruncatesGradientButUsesSavedHistoryValue()
    {
        var (network, weight) = Recurrent();
        var executor = network.CreateExecutor();
        var first = executor.Run(Inputs("n", 1, 0));
        var second = executor.Run(Inputs("n", 0), first.State);
        Assert.AreEqual(0.5, second.Backward(Inputs("n", 1))[weight.Id], 1e-12);
    }

    [TestMethod]
    public void FullWindowIncludesGradientThroughHistoricalComputation()
    {
        var (network, weight) = Recurrent();
        var full = network.CreateExecutor().Run(Inputs("n", 1, 0, 0));
        Assert.AreEqual(1, full.Backward(Inputs("n", 0, 0, 1))[weight.Id], 1e-12);
    }

    [TestMethod]
    public void CancelledBackwardLeavesModelUnchanged()
    {
        var (network, _) = Recurrent();
        var before = network.ExportCheckpoint();
        var result = network.CreateExecutor().Run(Inputs("n", 1, 0));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => result.Backward(Inputs("n", 0, 1), cancellation.Token));
        CollectionAssert.AreEqual(before, network.ExportCheckpoint());
    }

    private static void VerifyAllParameters(RecursiveNetwork network,
        IReadOnlyDictionary<string, double>[] inputs, IReadOnlyDictionary<string, double>[] seeds)
    {
        var analytic = network.CreateExecutor().Run(inputs).Backward(seeds);
        var source = JsonNode.Parse(network.ExportCheckpoint())!;
        var errors = new List<double>();
        foreach (var parameter in source["Parameters"]!.AsArray())
        {
            var id = parameter!["Id"]!.GetValue<Guid>();
            var value = parameter["Value"]!.GetValue<double>();
            const double epsilon = 1e-6;
            parameter["Value"] = value + epsilon;
            var positive = Objective(Restore(source));
            parameter["Value"] = value - epsilon;
            var negative = Objective(Restore(source));
            parameter["Value"] = value;
            var numerical = (positive - negative) / (2 * epsilon);
            errors.Add(Math.Abs(analytic[id] - numerical) / Math.Max(1, Math.Abs(numerical)));
        }
        Assert.IsTrue(errors.Max() < 2e-7, $"Maximum normalized gradient error: {errors.Max():R}");

        double Objective(RecursiveNetwork model)
        {
            var outputs = GraphInterpreter.Run(model, inputs);
            return seeds.SelectMany((row, q) => row.Select(p => p.Value * outputs[q][p.Key])).Sum();
        }
    }

    private static RecursiveNetwork Restore(JsonNode json) => RecursiveNetwork.ImportCheckpoint(Encoding.UTF8.GetBytes(json.ToJsonString()));

    private static IReadOnlyDictionary<string, double>[] Inputs(string id, params double[] values) => values
        .Select(value => (IReadOnlyDictionary<string, double>)new Dictionary<string, double> { [id] = value }).ToArray();

    private static (RecursiveNetwork Network, ScalarParameter Weight) Recurrent()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var node = graph.CreateAndAddElement(new Neuron(activation: ScalarActivation.Identity), "n");
        var weight = new ScalarParameter(0.5);
        graph.AddEdge(node, node, new NeuralConnection(weight, 1));
        return (new RecursiveNetwork(graph), weight);
    }
}
