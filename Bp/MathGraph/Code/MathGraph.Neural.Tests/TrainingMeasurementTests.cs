using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class TrainingMeasurementTests
{
    [TestMethod]
    public void TrainingMeasuresOnlySupervisedTargets()
    {
        var model = Create();
        model.TrainTokens(new[] { 0, 1, 0 }, labelMask: new[] { false, false, true });
        Assert.AreEqual(1, model.LastTrainingMeasurement!.SupervisedTokens);
    }

    [TestMethod]
    public void StageDurationsFitWithinTotal()
    {
        var model = Create();
        model.TrainTokens(new[] { 0, 1, 0 });
        var m = model.LastTrainingMeasurement!;
        Assert.IsTrue(m.LayoutAndSnapshot + m.Forward + m.LossAndReadoutBackward + m.GraphBackwardAndStatistics + m.OptimizerCommit <= m.Total);
    }

    [TestMethod]
    public void FailedTrainingPreservesPreviousMeasurement()
    {
        var model = Create();
        model.TrainTokens(new[] { 0, 1 });
        var before = model.LastTrainingMeasurement;
        Assert.ThrowsException<ArgumentException>(() => model.TrainTokens(new[] { 0, 1 }, labelMask: new[] { false, false }));
        Assert.AreSame(before, model.LastTrainingMeasurement);
    }

    [TestMethod]
    public void StructureMeasurementUsesPublishedVersion()
    {
        var model = Create();
        model.Fractalize("n");
        Assert.AreEqual(model.Version, model.LastStructureMeasurement!.ModelVersion);
    }

    [TestMethod]
    public void DatasetEvaluationWeightsByTargetsNotSamples()
    {
        var model = Create();
        var shortSample = new[] { 0, 1 };
        var longSample = new[] { 1, 0, 1, 0 };
        var expected = (model.EvaluateTokens(shortSample) + 3 * model.EvaluateTokens(longSample)) / 4;
        var dataset = new TokenDataset("test", new[] { shortSample, longSample });
        Assert.AreEqual(expected, model.EvaluateDataset(dataset).MeanCrossEntropy, 1e-12);
    }

    [TestMethod]
    public void DatasetEvaluationPreservesCheckpoint()
    {
        var model = Create();
        var before = model.ExportCheckpoint();
        model.EvaluateDataset(new TokenDataset("test", new[] { new[] { 0, 1 } }));
        CollectionAssert.AreEqual(before, model.ExportCheckpoint());
    }

    [TestMethod]
    public void DatasetEvaluationCountsMaskedTargets()
    {
        var dataset = new TokenDataset("test", new[] { new[] { 0, 1, 0 } },
            labelMasks: new IReadOnlyList<bool>[] { new[] { false, false, true } });
        Assert.AreEqual(1L, Create().EvaluateDataset(dataset).SupervisedTokens);
    }

    private static RecursiveNetwork Create()
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        var model = new RecursiveNetwork(graph);
        model.BindText(new TokenBindings("test", new[] { "n" }, new[] { "n" },
            new IReadOnlyList<double>[] { new[] { -1d }, new[] { 1d } }));
        return model;
    }
}
