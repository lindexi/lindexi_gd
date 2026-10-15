using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class LabelMaskTests
{
    [TestMethod]
    public void MaskedLossMatchesSelectedPrediction()
    {
        var network = Create();
        var logits = network.CreateTokenExecutor().Run(new[] { 0, 1 }).Predictions[1].Logits;
        var max = logits.Max();
        var expected = Math.Log(logits.Sum(v => Math.Exp(v - max))) + max - logits[0];
        Assert.AreEqual(expected, network.EvaluateTokens(new[] { 0, 1, 0 }, labelMask: new[] { false, false, true }), 1e-12);
    }

    [TestMethod]
    public void LabelMaskDoesNotDeleteContext()
    {
        var network = Create();
        network.TrainTokens(new[] { 0, 1, 0 }, labelMask: new[] { false, false, true });
        Assert.AreEqual(2L, network.Graph.ElementList[0].Value.Statistics.Samples);
    }

    [TestMethod]
    public void MaskedPromptReceivesGradientThroughFeedback()
    {
        var network = Create();
        var feedback = network.Graph.ElementList[0].InEdgeList[0].EdgeInfo!.Weight;
        var before = feedback.Value;
        network.TrainTokens(new[] { 0, 1, 0 }, options: new AdamWOptions(WeightDecay: 0), labelMask: new[] { false, false, true });
        Assert.AreNotEqual(before, feedback.Value);
    }

    [TestMethod]
    public void PaddingAndLabelsRemainAligned()
    {
        var network = Create();
        var expected = network.EvaluateTokens(new[] { 0, 1, 0 }, labelMask: new[] { false, false, true });
        var actual = network.EvaluateTokens(new[] { 0, -1, 1, 0 }, new[] { true, false, true, true },
            labelMask: new[] { false, true, false, true });
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void AllMaskedLabelsDoNotCommitUpdate()
    {
        var network = Create();
        var before = network.ExportCheckpoint();
        Assert.ThrowsException<ArgumentException>(() => network.TrainTokens(new[] { 0, 1 }, labelMask: new[] { true, false }));
        CollectionAssert.AreEqual(before, network.ExportCheckpoint());
    }

    [TestMethod]
    public void WrongLabelCountIsRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => Create().EvaluateTokens(new[] { 0, 1 }, labelMask: new[] { true }));
    }

    [TestMethod]
    public void AllTrueLabelsPreserveExistingTraining()
    {
        var network = Create();
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint());
        network.TrainTokens(new[] { 0, 1, 0 });
        restored.TrainTokens(new[] { 0, 1, 0 }, labelMask: new[] { true, true, true });
        CollectionAssert.AreEqual(network.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public void MaskedCrossEntropyParameterGradientsMatchFiniteDifference()
    {
        var network = Create();
        var checkpoint = JsonNode.Parse(network.ExportCheckpoint())!;
        var labels = new[] { false, false, true };
        var tokens = new[] { 0, 1, 0 };
        network.TrainTokens(tokens, options: new AdamWOptions(WeightDecay: 0), labelMask: labels);
        var trained = JsonNode.Parse(network.ExportCheckpoint())!["Parameters"]!.AsArray()
            .ToDictionary(p => p!["Id"]!.GetValue<Guid>(), p => p!["First"]!.GetValue<double>() / 0.1);
        var errors = new List<double>();
        foreach (var parameter in checkpoint["Parameters"]!.AsArray())
        {
            var value = parameter!["Value"]!.GetValue<double>();
            var id = parameter["Id"]!.GetValue<Guid>();
            parameter["Value"] = value + 1e-6;
            var positive = Restore(checkpoint).EvaluateTokens(tokens, labelMask: labels);
            parameter["Value"] = value - 1e-6;
            var negative = Restore(checkpoint).EvaluateTokens(tokens, labelMask: labels);
            parameter["Value"] = value;
            errors.Add(Math.Abs((positive - negative) / 2e-6 - trained[id]));
        }
        Assert.IsTrue(errors.Max() < 1e-7);
    }

    [TestMethod]
    public void DatasetIdentityIncludesSupervisedPositions()
    {
        var samples = new[] { new[] { 0, 1, 0 } };
        var first = new TokenDataset("test", samples, labelMasks: new IReadOnlyList<bool>[] { new[] { false, false, true } });
        var second = new TokenDataset("test", samples, labelMasks: new IReadOnlyList<bool>[] { new[] { false, true, true } });
        Assert.AreNotEqual(first.Identity, second.Identity);
    }

    [TestMethod]
    public void DatasetCopiesCallerLabelMask()
    {
        var labels = new[] { false, false, true };
        var dataset = new TokenDataset("test", new[] { new[] { 0, 1, 0 } }, labelMasks: new IReadOnlyList<bool>[] { labels });
        labels[1] = true;
        Assert.IsFalse(dataset.GetLabelMask(0)![1]);
    }

    [TestMethod]
    public void RestoredJobPreservesMaskedTraining()
    {
        var dataset = new TokenDataset("test", new[] { new[] { 0, 1, 0 } },
            labelMasks: new IReadOnlyList<bool>[] { new[] { false, false, true } });
        var network = Create();
        network.ConfigureTrainingJob(dataset, 2);
        network.TrainNextSample(dataset);
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint());
        network.TrainNextSample(dataset);
        restored.TrainNextSample(dataset);
        CollectionAssert.AreEqual(network.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    private static RecursiveNetwork Restore(JsonNode json) => RecursiveNetwork.ImportCheckpoint(Encoding.UTF8.GetBytes(json.ToJsonString()));

    private static RecursiveNetwork Create()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var n = graph.CreateAndAddElement(new Neuron(new ScalarParameter(0.1)), "n");
        graph.AddEdge(n, n, new NeuralConnection(new ScalarParameter(0.4), 1));
        var network = new RecursiveNetwork(graph);
        network.BindText(new TokenBindings("test", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }, seed: 17));
        return network;
    }
}
