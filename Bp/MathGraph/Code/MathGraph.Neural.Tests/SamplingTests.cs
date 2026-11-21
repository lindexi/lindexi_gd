using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class SamplingTests
{
    [TestMethod]
    public void TopKRetainsThresholdTies()
    {
        var probabilities = TokenSampling.Probabilities(new[] { 2d, 2, 0 }, Array.Empty<int>(), new SamplingOptions(TopK: 1));
        CollectionAssert.AreEqual(new[] { 0.5, 0.5, 0 }, probabilities.ToArray());
    }

    [TestMethod]
    public void TopPKeepsFirstCrossingToken()
    {
        var probabilities = TokenSampling.Probabilities(new[] { Math.Log(0.5), Math.Log(0.3), Math.Log(0.2) },
            Array.Empty<int>(), new SamplingOptions(TopP: 0.6));
        Assert.AreEqual(0.375, probabilities[1], 1e-12);
    }

    [TestMethod]
    public void TopPMasksTokensAfterCrossing()
    {
        var probabilities = TokenSampling.Probabilities(new[] { Math.Log(0.5), Math.Log(0.3), Math.Log(0.2) },
            Array.Empty<int>(), new SamplingOptions(TopP: 0.6));
        Assert.AreEqual(0d, probabilities[2]);
    }

    [TestMethod]
    public void RepetitionPenaltyMatchesMiniMindNegativeLogitDivision()
    {
        var probabilities = TokenSampling.Probabilities(new[] { -2d, -1 }, new[] { 0, 0 }, new SamplingOptions(RepetitionPenalty: 2));
        CollectionAssert.AreEqual(new[] { 0.5, 0.5 }, probabilities.ToArray());
    }

    [TestMethod]
    public void TemperaturePrecedesRepetitionPenalty()
    {
        var probabilities = TokenSampling.Probabilities(new[] { 4d, 1 }, new[] { 0 }, new SamplingOptions(Temperature: 2, RepetitionPenalty: 4));
        CollectionAssert.AreEqual(new[] { 0.5, 0.5 }, probabilities.ToArray());
    }

    [TestMethod]
    public void NonFiniteLogitsAreRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => TokenSampling.Probabilities(new[] { double.NaN, 0 }, Array.Empty<int>()));
    }

    [DataTestMethod]
    [DataRow(0d)]
    [DataRow(-1d)]
    [DataRow(1.1)]
    public void InvalidTopPIsRejected(double topP)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => TokenSampling.Probabilities(new[] { 0d, 1 }, Array.Empty<int>(), new SamplingOptions(TopP: topP)));
    }

    [TestMethod]
    public void RestoredGenerationMatchesContinuousTokensAndState()
    {
        var model = Create();
        var original = model.CreateTokenExecutor().CreateGenerationSession(new[] { 0, 1 }, 8,
            new SamplingOptions(Temperature: 0.8, TopP: 0.9, RepetitionPenalty: 1.2), 19);
        original.NextToken();
        original.NextToken();
        var restoredModel = RecursiveNetwork.ImportCheckpoint(model.ExportCheckpoint());
        var restored = restoredModel.CreateTokenExecutor().RestoreGenerationSession(original.ExportCheckpoint());
        for (var i = 0; i < 6; i++)
        {
            original.NextToken();
            restored.NextToken();
        }
        CollectionAssert.AreEqual(original.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public void CancelledGenerationDoesNotConsumeRandomState()
    {
        var session = Create().CreateTokenExecutor().CreateGenerationSession(new[] { 0 }, 3);
        var before = session.ExportCheckpoint();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => session.NextToken(cancellation.Token));
        CollectionAssert.AreEqual(before, session.ExportCheckpoint());
    }

    [TestMethod]
    public void GreedySessionMatchesExistingGreedyGeneration()
    {
        var executor = Create().CreateTokenExecutor();
        var expected = executor.Generate(new[] { 0 }, 5);
        var session = executor.CreateGenerationSession(new[] { 0 }, 5, new SamplingOptions(DoSample: false));
        var actual = Enumerable.Range(0, 5).Select(_ => session.NextToken()).ToArray();
        CollectionAssert.AreEqual(expected.ToArray(), actual);
    }

    [TestMethod]
    public void EosTerminatesSession()
    {
        var executor = Create().CreateTokenExecutor();
        var eos = executor.Generate(new[] { 0 }, 1)[0];
        var session = executor.CreateGenerationSession(new[] { 0 }, 4, new SamplingOptions(DoSample: false), eosToken: eos);
        session.NextToken();
        Assert.IsTrue(session.IsCompleted);
    }

    [TestMethod]
    public void FinishedSessionRejectsNextToken()
    {
        var session = Create().CreateTokenExecutor().CreateGenerationSession(new[] { 0 }, 0);
        Assert.ThrowsException<InvalidOperationException>(() => session.NextToken());
    }

    [TestMethod]
    public void ChangedModelRejectsGenerationCheckpoint()
    {
        var model = Create();
        var session = model.CreateTokenExecutor().CreateGenerationSession(new[] { 0 }, 3);
        model.TrainTokens(new[] { 0, 1 });
        Assert.ThrowsException<InvalidDataException>(() => model.CreateTokenExecutor().RestoreGenerationSession(session.ExportCheckpoint()));
    }

    [TestMethod]
    public void CorruptContextIsRejected()
    {
        var executor = Create().CreateTokenExecutor();
        var session = executor.CreateGenerationSession(new[] { 0 }, 3);
        var json = JsonNode.Parse(session.ExportCheckpoint())!;
        json["Context"]![0] = 999;
        Assert.ThrowsException<InvalidDataException>(() => executor.RestoreGenerationSession(Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    private static RecursiveNetwork Create()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var node = graph.CreateAndAddElement(new Neuron(), "n");
        graph.AddEdge(node, node, new NeuralConnection(new ScalarParameter(0.4), 1));
        var network = new RecursiveNetwork(graph);
        network.BindText(new TokenBindings("test", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 0d }, new[] { 1d } }, 2));
        return network;
    }
}
