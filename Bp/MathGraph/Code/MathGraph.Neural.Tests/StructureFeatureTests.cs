using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class StructureFeatureTests
{
    [TestMethod]
    public void FeedbackUsesDecisionTimeFeatures()
    {
        var model = Create(true);
        Train(model, 1);
        model.BeginStructureObservation(2);
        var pending = JsonNode.Parse(model.ExportCheckpoint())!["Controller"]!["Pending"]!;
        var rows = pending["Features"]!.AsArray().Select(row => row!.AsArray().Select(v => v!.GetValue<double>()).ToArray()).ToArray();
        var probabilities = pending["Probabilities"]!.AsArray().Select(v => v!.GetValue<double>()).ToArray();
        var selected = pending["Selected"]!.GetValue<int>();
        Train(model, 20);
        var feedback = model.CompleteStructureObservation(1);
        var actual = Weights(model);
        var expected = Enumerable.Range(0, 8).Select(f => 0.01 * feedback.Advantage *
            (rows[selected][f] - rows.Select((row, i) => probabilities[i] * row[f]).Sum())).ToArray();
        Assert.IsTrue(expected.Zip(actual, (a, b) => Math.Abs(a - b)).Max() < 1e-12);
    }

    [TestMethod]
    public void FeatureFeedbackRestoresIdentically()
    {
        var model = Create(true);
        Train(model, 1);
        model.BeginStructureObservation(2);
        var restored = RecursiveNetwork.ImportCheckpoint(model.ExportCheckpoint());
        Train(model, 3);
        Train(restored, 3);
        model.CompleteStructureObservation(1);
        restored.CompleteStructureObservation(1);
        CollectionAssert.AreEqual(model.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public void DisabledFeaturesRetainZeroWeights()
    {
        var model = Create(false);
        Train(model, 1);
        model.BeginStructureObservation(2);
        Train(model, 3);
        model.CompleteStructureObservation(1);
        CollectionAssert.AreEqual(new double[8], Weights(model));
    }

    [TestMethod]
    public void ZeroWeightsPreserveFirstAction()
    {
        var model = Create(false);
        Train(model, 1);
        var json = JsonNode.Parse(model.ExportCheckpoint())!;
        json["Controller"]!["Options"]!["UseNodeFeatures"] = true;
        var featureModel = Restore(json);
        Assert.AreEqual(model.BeginStructureObservation(2), featureModel.BeginStructureObservation(2));
    }

    [TestMethod]
    public void LatestWindowDoesNotRetainOldActivationMean()
    {
        var model = Create(true);
        Train(model, 1);
        var output = model.CreateExecutor().Run(Inputs(9)).Outputs[0]["n"];
        Train(model, 9);
        Assert.AreEqual(output, model.Graph.ElementList[0].Value.Statistics.LastWindow!.Mean, 1e-12);
    }

    [TestMethod]
    public void BadFeatureDimensionIsRejected()
    {
        var model = Create(true);
        model.BeginStructureObservation(2);
        var json = JsonNode.Parse(model.ExportCheckpoint())!;
        json["Controller"]!["Pending"]!["Features"]![0] = new JsonArray();
        Assert.ThrowsException<InvalidDataException>(() => Restore(json));
    }

    [TestMethod]
    public void LearnedSharedWeightsChangeNextProbabilities()
    {
        var model = Create(true);
        Train(model, 1);
        model.BeginStructureObservation(2);
        Train(model, 3);
        model.CompleteStructureObservation(1);
        var json = JsonNode.Parse(model.ExportCheckpoint())!;
        json["Controller"]!["Options"]!["UseNodeFeatures"] = false;
        var baseline = Restore(json);
        model.BeginStructureObservation(1);
        baseline.BeginStructureObservation(1);
        var a = JsonNode.Parse(model.ExportCheckpoint())!["Controller"]!["Pending"]!["Probabilities"]!.ToJsonString();
        var b = JsonNode.Parse(baseline.ExportCheckpoint())!["Controller"]!["Pending"]!["Probabilities"]!.ToJsonString();
        Assert.AreNotEqual(a, b);
    }

    private static double[] Weights(RecursiveNetwork model) => JsonNode.Parse(model.ExportCheckpoint())!["Controller"]!["FeatureWeights"]!
        .AsArray().Select(v => v!.GetValue<double>()).ToArray();
    private static RecursiveNetwork Restore(JsonNode json) => RecursiveNetwork.ImportCheckpoint(Encoding.UTF8.GetBytes(json.ToJsonString()));
    private static IReadOnlyDictionary<string, double>[] Inputs(double value) =>
        new IReadOnlyDictionary<string, double>[] { new Dictionary<string, double> { ["n"] = value } };
    private static void Train(RecursiveNetwork model, double value) => model.TrainSquaredError(Inputs(value), "n", new[] { 0d });
    private static RecursiveNetwork Create(bool features)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(activation: ScalarActivation.Identity), "n");
        var model = new RecursiveNetwork(graph);
        model.ConfigureStructureController(new StructureOptions(1, UseNodeFeatures: features), 1);
        return model;
    }
}
