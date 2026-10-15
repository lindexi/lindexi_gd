using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class RecursiveNetworkTests
{
    [TestMethod]
    public void DelayedConnectionReadsPreviousStep()
    {
        var (network, _) = Recurrent(0.5);
        var result = network.CreateExecutor().Run(Inputs("n", 1, 0, 0));
        CollectionAssert.AreEqual(new[] { 1d, 0.5, 0.25 }, result.Outputs.Select(r => r["n"]).ToArray());
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void EveryRoleCanFractalizeWithoutChangingOldTrajectory(int role)
    {
        var five = FiveNodeGraph.Create(7);
        var network = new RecursiveNetwork(five.Graph);
        var inputs = Inputs(five.Input.Id, 1, 0.5, -0.2, 0);
        var before = network.CreateExecutor().Run(inputs);
        network.Fractalize(five.Graph.ElementList[role].Id, 8);
        var child = five.Graph.ElementList[role].Value.Child!;
        network.Fractalize(child.Input.Id, 9);
        var after = network.CreateExecutor().Run(inputs);
        CollectionAssert.AreEqual(before.Outputs.Select(r => r[five.Output.Id]).ToArray(),
            after.Outputs.Select(r => r[five.Output.Id]).ToArray());
    }

    [TestMethod]
    public void InterpreterMatchesFlatRecursiveExecution()
    {
        var five = FiveNodeGraph.Create(9);
        var network = new RecursiveNetwork(five.Graph);
        network.Fractalize(five.Input.Id, 5);
        var inputs = Inputs(five.Input.Id, 1, 2, -1);
        network.TrainSquaredError(inputs, five.Output.Id, new[] { 1d, 1, 1 });
        var expected = GraphInterpreter.Run(network, inputs);
        var actual = network.CreateExecutor().Run(inputs).Outputs;
        var difference = expected.SelectMany((row, q) => row.Select(p => Math.Abs(p.Value - actual[q][p.Key]))).Max();
        Assert.AreEqual(0, difference, 1e-12);
    }

    [TestMethod]
    public void FeedbackGradientMatchesFiniteDifference()
    {
        var (network, weight) = Recurrent(0.4);
        var inputs = Inputs("n", 1, 0.2, -0.3);
        var gradient = network.CreateExecutor().Run(inputs).Backward(Inputs("n", 0, 0, 1))[weight.Id];
        var positive = Recurrent(0.400001).Network.CreateExecutor().Run(inputs).Outputs[2]["n"];
        var negative = Recurrent(0.399999).Network.CreateExecutor().Run(inputs).Outputs[2]["n"];
        Assert.AreEqual((positive - negative) / 0.000002, gradient, 1e-8);
    }

    [TestMethod]
    public void SharedParameterAccumulatesEveryReference()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var weight = new ScalarParameter(0.3);
        var a = graph.CreateAndAddElement(new Neuron(activation: ScalarActivation.Identity), "a");
        var b = graph.CreateAndAddElement(new Neuron(activation: ScalarActivation.Identity), "b");
        graph.AddEdge(a, b, new NeuralConnection(weight));
        graph.AddEdge(a, b, new NeuralConnection(weight));
        var network = new RecursiveNetwork(graph);
        var gradients = network.CreateExecutor().Run(Inputs("a", 2)).Backward(Inputs("b", 1));
        Assert.AreEqual(4, gradients[weight.Id], 1e-12);
    }

    [TestMethod]
    public void SharedParameterUpdatesOnlyOnce()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var parameter = new ScalarParameter(0.3);
        graph.CreateAndAddElement(new Neuron(parameter), "a");
        graph.CreateAndAddElement(new Neuron(parameter), "b");
        var network = new RecursiveNetwork(graph);
        network.TrainSquaredError(Inputs("a", 1), "a", new[] { 0d });
        Assert.AreEqual(1L, parameter.Updates);
    }

    [TestMethod]
    public void ZeroDelayCycleIsRejected()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var n = graph.CreateAndAddElement(new Neuron());
        graph.AddEdge(n, n, new NeuralConnection(new ScalarParameter(1)));
        Assert.ThrowsException<InvalidOperationException>(() => new RecursiveNetwork(graph));
    }

    [TestMethod]
    public void BudgetFailureDoesNotPublishChild()
    {
        var five = FiveNodeGraph.Create();
        var network = new RecursiveNetwork(five.Graph, new NetworkBudget(MaxNodes: 5));
        Assert.ThrowsException<InvalidOperationException>(() => network.Fractalize(five.Input.Id));
        Assert.IsNull(five.Input.Value.Child);
    }

    [TestMethod]
    public void WindowContinuationMatchesFullSequence()
    {
        var (network, _) = Recurrent(0.5);
        var executor = network.CreateExecutor();
        var first = executor.Run(Inputs("n", 1, 2));
        var continued = executor.Run(Inputs("n", 3), first.State);
        var full = executor.Run(Inputs("n", 1, 2, 3));
        Assert.AreEqual(full.Outputs[2]["n"], continued.Outputs[0]["n"]);
    }

    [TestMethod]
    public void OldExecutorRetainsParameterSnapshot()
    {
        var (network, _) = Recurrent(0.5);
        var executor = network.CreateExecutor();
        var inputs = Inputs("n", 1, 1);
        var before = executor.Run(inputs).Outputs[1]["n"];
        network.TrainSquaredError(inputs, "n", new[] { 0d, 0 });
        Assert.AreEqual(before, executor.Run(inputs).Outputs[1]["n"]);
    }

    [TestMethod]
    public void NonFiniteLossDoesNotCommitParameters()
    {
        var (network, weight) = Recurrent(0.5);
        Assert.ThrowsException<ArithmeticException>(() => network.TrainSquaredError(Inputs("n", 1), "n", new[] { double.MaxValue }));
        Assert.AreEqual(0L, weight.Updates);
    }

    [TestMethod]
    public void CancelledTrainingDoesNotCommitParameters()
    {
        var (network, weight) = Recurrent(0.5);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => network.TrainSquaredError(Inputs("n", 1), "n",
            new[] { 0d }, cancellationToken: cancellation.Token));
        Assert.AreEqual(0L, weight.Updates);
    }

    [TestMethod]
    public void ZeroGateBlocksChildTaskGradient()
    {
        var five = FiveNodeGraph.Create(3);
        var network = new RecursiveNetwork(five.Graph);
        network.Fractalize(five.Input.Id, 5);
        var gradients = network.CreateExecutor().Run(Inputs(five.Input.Id, 1))
            .Backward(Inputs(five.Output.Id, 1));
        Assert.AreEqual(0d, gradients[five.Input.Value.Child!.Input.Value.Bias.Id]);
    }

    [TestMethod]
    public void StateFromAnotherSnapshotIsRejected()
    {
        var (network, _) = Recurrent(0.5);
        var state = network.CreateExecutor().Run(Inputs("n", 1)).State;
        Assert.ThrowsException<InvalidOperationException>(() => network.CreateExecutor().Run(Inputs("n", 1), state));
    }

    [TestMethod]
    public void EmptyWindowDoesNotAdvanceState()
    {
        var (network, _) = Recurrent(0.5);
        var executor = network.CreateExecutor();
        var state = executor.Run(Inputs("n", 1)).State;
        var empty = executor.Run(Inputs("n"), state);
        Assert.AreEqual(state.Step, empty.State.Step);
    }

    [TestMethod]
    public void FutureInputsDoNotChangePrefix()
    {
        var (network, _) = Recurrent(0.5);
        var executor = network.CreateExecutor();
        var first = executor.Run(Inputs("n", 1, 2, 3));
        var second = executor.Run(Inputs("n", 1, 2, -100));
        Assert.AreEqual(first.Outputs[1]["n"], second.Outputs[1]["n"]);
    }

    [TestMethod]
    public void ZeroGateDerivativeEqualsChildOutput()
    {
        var (network, _) = Recurrent(0.5);
        network.Fractalize("n", 12);
        var neuron = network.Graph.ElementList[0].Value;
        var result = network.CreateExecutor().Run(Inputs("n", 1));
        var gradient = result.Backward(Inputs("n", 1));
        Assert.AreEqual(result.Outputs[0][neuron.Child!.Output.Id], gradient[neuron.Gate!.Id], 1e-12);
    }

    [TestMethod]
    public void NonFiniteExecutionReportsNodeAndTime()
    {
        var (network, _) = Recurrent(2);
        var error = Assert.ThrowsException<ArithmeticException>(() => network.CreateExecutor().Run(Inputs("n", double.MaxValue, 0)));
        StringAssert.Contains(error.Message, "node=n, q=1");
    }

    private static IReadOnlyDictionary<string, double>[] Inputs(string id, params double[] values) =>
        values.Select(v => (IReadOnlyDictionary<string, double>)new Dictionary<string, double> { [id] = v }).ToArray();

    private static (RecursiveNetwork Network, ScalarParameter Weight) Recurrent(double value)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var node = graph.CreateAndAddElement(new Neuron(activation: ScalarActivation.Identity), "n");
        var weight = new ScalarParameter(value);
        graph.AddEdge(node, node, new NeuralConnection(weight, 1));
        return (new RecursiveNetwork(graph), weight);
    }
}
