using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class TokenTrainingTests
{
    [TestMethod]
    public void InternalStepsAdvanceSharedClock()
    {
        var network = Create(3);
        Assert.AreEqual(6L, network.CreateTokenExecutor().Run(new[] { 0, 1 }).State.Step);
    }

    [TestMethod]
    public void PaddingDoesNotAdvanceState()
    {
        var network = Create(2);
        Assert.AreEqual(4L, network.CreateTokenExecutor().Run(new[] { 0, -1, 1 }, new[] { true, false, true }).State.Step);
    }

    [TestMethod]
    public void PaddingMatchesUnpaddedSequence()
    {
        var executor = Create().CreateTokenExecutor();
        var padded = executor.Run(new[] { 0, -1, 1 }, new[] { true, false, true });
        var plain = executor.Run(new[] { 0, 1 });
        CollectionAssert.AreEqual(plain.Predictions[1].Logits.ToArray(), padded.Predictions[1].Logits.ToArray());
    }

    [TestMethod]
    public void TokenContinuationMatchesFullSequence()
    {
        var executor = Create(2).CreateTokenExecutor();
        var first = executor.Run(new[] { 0 });
        var second = executor.Run(new[] { 1 }, state: first.State);
        var full = executor.Run(new[] { 0, 1 });
        CollectionAssert.AreEqual(full.Predictions[1].Logits.ToArray(), second.Predictions[0].Logits.ToArray());
    }

    [TestMethod]
    public void FutureTokensDoNotChangePrefix()
    {
        var executor = Create().CreateTokenExecutor();
        var first = executor.Run(new[] { 0, 1, 0 });
        var second = executor.Run(new[] { 0, 1, 1 });
        CollectionAssert.AreEqual(first.Predictions[1].Logits.ToArray(), second.Predictions[1].Logits.ToArray());
    }

    [TestMethod]
    public void TrainingReducesNextTokenCrossEntropy()
    {
        var network = Create();
        var tokens = new[] { 0, 1, 0, 1, 0, 1 };
        var options = new AdamWOptions(LearningRate: 0.03, WeightDecay: 0);
        var initial = network.TrainTokens(tokens, options: options);
        for (var i = 0; i < 80; i++) network.TrainTokens(tokens, options: options);
        Assert.IsTrue(network.TrainTokens(tokens, options: options) < initial * 0.5);
    }

    [TestMethod]
    public void MaskedTrainingMatchesCompactTraining()
    {
        var padded = Create();
        var compact = Create();
        var first = padded.TrainTokens(new[] { 0, -1, 1, 0 }, new[] { true, false, true, true });
        var second = compact.TrainTokens(new[] { 0, 1, 0 });
        Assert.AreEqual(second, first, 1e-12);
    }

    [TestMethod]
    public void ReadoutParametersUpdateOncePerWindow()
    {
        var network = Create();
        network.TrainTokens(new[] { 0, 1, 0 });
        CollectionAssert.AreEqual(Enumerable.Repeat(1L, network.TextBindings!.Parameters.Count).ToArray(),
            network.TextBindings.Parameters.Select(p => p.Updates).ToArray());
    }

    [TestMethod]
    public void ExistingTextSnapshotSurvivesTraining()
    {
        var network = Create();
        var executor = network.CreateTokenExecutor();
        var before = executor.Run(new[] { 0 }).Predictions[0].Logits.ToArray();
        network.TrainTokens(new[] { 0, 1 });
        CollectionAssert.AreEqual(before, executor.Run(new[] { 0 }).Predictions[0].Logits.ToArray());
    }

    [TestMethod]
    public void FractalizationPreservesTextBindingsAndLogits()
    {
        var network = Create();
        var before = network.CreateTokenExecutor().Run(new[] { 0, 1 });
        network.Fractalize("input", 17);
        var after = network.CreateTokenExecutor().Run(new[] { 0, 1 });
        CollectionAssert.AreEqual(before.Predictions[1].Logits.ToArray(), after.Predictions[1].Logits.ToArray());
    }

    [TestMethod]
    public void InvalidLabelDoesNotUpdateParameters()
    {
        var network = Create();
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => network.TrainTokens(new[] { 0, 99 }));
        Assert.AreEqual(0L, network.TextBindings!.Parameters[0].Updates);
    }

    [TestMethod]
    public void CancellationDoesNotUpdateReadout()
    {
        var network = Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => network.TrainTokens(new[] { 0, 1 }, cancellationToken: cancellation.Token));
        Assert.AreEqual(0L, network.TextBindings!.Parameters[0].Updates);
    }

    [TestMethod]
    public void GreedyGenerationUsesNextTokenReadout()
    {
        var network = Create();
        var executor = network.CreateTokenExecutor();
        var logits = executor.Run(new[] { 0 }).Predictions[0].Logits;
        var expected = logits[1] > logits[0] ? 1 : 0;
        Assert.AreEqual(expected, executor.Generate(new[] { 0 }, 1)[0]);
    }

    [TestMethod]
    public void EosStopsGeneration()
    {
        var executor = Create().CreateTokenExecutor();
        var eos = executor.Generate(new[] { 0 }, 1)[0];
        Assert.AreEqual(1, executor.Generate(new[] { 0 }, 10, eos).Count);
    }

    private static RecursiveNetwork Create(int steps = 1)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var input = graph.CreateAndAddElement(new Neuron(), "input");
        var output = graph.CreateAndAddElement(new Neuron(), "output");
        graph.AddEdge(input, output, new NeuralConnection(new ScalarParameter(0.7)));
        graph.AddEdge(output, input, new NeuralConnection(new ScalarParameter(0.2), 1));
        var network = new RecursiveNetwork(graph);
        network.BindText(new TokenBindings("test-vocabulary-v1", new[] { "input" }, new[] { "input", "output" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }, steps, 17));
        return network;
    }
}
