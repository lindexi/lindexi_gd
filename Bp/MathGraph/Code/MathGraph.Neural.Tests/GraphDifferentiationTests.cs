using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class GraphDifferentiationTests
{
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void GraphForwardDerivativesMatchFlatReverseDerivatives(int role)
    {
        var five = FiveNodeGraph.Create(7);
        var network = new RecursiveNetwork(five.Graph);
        network.Fractalize(five.Input.Id, 13);
        network.Fractalize(five.Input.Value.Child!.Graph.ElementList[role].Id, 19);
        var json = JsonNode.Parse(network.ExportCheckpoint())!;
        foreach (var parameter in json["Parameters"]!.AsArray()) parameter!["Value"] = 0.31;
        network = RecursiveNetwork.ImportCheckpoint(Encoding.UTF8.GetBytes(json.ToJsonString()));
        var inputs = Rows(five.Input.Id, 0.4, -0.2, 0.6);
        var seeds = Rows(five.Output.Id, 0.2, -0.3, 1);
        var expected = GraphInterpreter.Differentiate(network, inputs, seeds);
        var actual = network.CreateExecutor().Run(inputs).Backward(seeds);
        Assert.IsTrue(expected.Max(p => Math.Abs(p.Value - actual[p.Key])) < 1e-11);
    }

    [TestMethod]
    public void SharedParameterGraphDerivativeMatchesHandCalculation()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var parameter = new ScalarParameter(0.2);
        var a = graph.CreateAndAddElement(new Neuron(parameter, ScalarActivation.Identity), "a");
        var b = graph.CreateAndAddElement(new Neuron(parameter, ScalarActivation.Identity), "b");
        graph.AddEdge(a, b, new NeuralConnection(parameter));
        var gradients = GraphInterpreter.Differentiate(new RecursiveNetwork(graph), Rows("a", 1), Rows("b", 1));
        Assert.AreEqual(2.4, gradients[parameter.Id], 1e-12);
    }

    [TestMethod]
    public void DifferentiationDoesNotChangeModel()
    {
        var five = FiveNodeGraph.Create();
        var network = new RecursiveNetwork(five.Graph);
        var before = network.ExportCheckpoint();
        GraphInterpreter.Differentiate(network, Rows(five.Input.Id, 1), Rows(five.Output.Id, 1));
        CollectionAssert.AreEqual(before, network.ExportCheckpoint());
    }

    [TestMethod]
    public void DerivativeMemoryBudgetIsEnforced()
    {
        var five = FiveNodeGraph.Create();
        var network = new RecursiveNetwork(five.Graph, new NetworkBudget(MaxTapeCells: 20));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => GraphInterpreter.Differentiate(network,
            Rows(five.Input.Id, 1), Rows(five.Output.Id, 1)));
    }

    [TestMethod]
    public void CancellationIsHonoredForEmptyWindow()
    {
        var network = new RecursiveNetwork(FiveNodeGraph.Create().Graph);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => GraphInterpreter.Differentiate(network,
            Rows("unused"), Rows("unused"), cancellation.Token));
    }

    private static IReadOnlyDictionary<string, double>[] Rows(string id, params double[] values) => values
        .Select(value => (IReadOnlyDictionary<string, double>)new Dictionary<string, double> { [id] = value }).ToArray();
}
