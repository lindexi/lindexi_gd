using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class ExecutionOutputsTests
{
    [TestMethod]
    public void OutputViewEnumeratesComputedValues()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(activation: ScalarActivation.Identity), "n");
        var output = new RecursiveNetwork(graph).CreateExecutor().Run(new[]
        {
            (IReadOnlyDictionary<string, double>)new Dictionary<string, double> { ["n"] = 3 }
        }).Outputs[0];
        CollectionAssert.AreEqual(new[] { new KeyValuePair<string, double>("n", 3) }, output.ToArray());
    }

    [TestMethod]
    public void MissingOutputReturnsFalse()
    {
        var output = CreateOutput();
        Assert.IsFalse(output.TryGetValue("missing", out _));
    }

    [TestMethod]
    public void MissingOutputIndexerThrows()
    {
        var output = CreateOutput();
        Assert.ThrowsException<KeyNotFoundException>(() => _ = output["missing"]);
    }

    [TestMethod]
    public void OutputViewSurvivesSubsequentExecution()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(activation: ScalarActivation.Identity), "n");
        var executor = new RecursiveNetwork(graph).CreateExecutor();
        var inputs = new[] { (IReadOnlyDictionary<string, double>)new Dictionary<string, double> { ["n"] = 3 } };
        var output = executor.Run(inputs).Outputs[0];
        executor.Run(new[] { (IReadOnlyDictionary<string, double>)new Dictionary<string, double> { ["n"] = 7 } });
        Assert.AreEqual(3d, output["n"]);
    }

    private static IReadOnlyDictionary<string, double> CreateOutput()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        return new RecursiveNetwork(graph).CreateExecutor().Run(new[]
        {
            (IReadOnlyDictionary<string, double>)new Dictionary<string, double>()
        }).Outputs[0];
    }
}
