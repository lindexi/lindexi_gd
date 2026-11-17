using FractalForge.Training;
using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Research.Feedback.Tests;

[TestClass]
public class FeedbackExperimentTests
{
    private static ForgeModel Model(double one = 1) => FeedbackExperiment.CreateBaseline(
        new TokenBindings("feedback-test-v1", ["I"], ["O"], [new double[] { 0 }, new double[] { one }], seed: 4), 7);

    [DataTestMethod]
    [DataRow(FeedbackTopology.Local, "A", "A")]
    [DataRow(FeedbackTopology.CrossLayer, "O", "I")]
    public void ActionCreatesPhysicalDelayedEdge(FeedbackTopology topology, string source, string target)
    {
        var model = Model();
        var candidate = model.Apply(FeedbackExperiment.CreateAction(topology, 3, .2));
        var edge = candidate.Topology.Expand().Single(e => !model.Topology.Expand().Any(old => old.Id == e.Id));
        Assert.AreEqual((source, target, 3, .2), (edge.From, edge.To, edge.Delay, edge.InitialValue));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void NonpositiveDelayRejected(int delay) => Assert.ThrowsException<ArgumentException>(() =>
        FeedbackExperiment.CreateAction(FeedbackTopology.Local, delay, .1));

    [TestMethod]
    public void DelayedFeedbackOnlyChangesLaterPredictions()
    {
        var model = Model();
        var candidate = model.Apply(FeedbackExperiment.CreateAction(FeedbackTopology.Local, 2, .8));
        var before = model.Network.CreateTokenExecutor().Run([1, 0, 0]);
        var after = candidate.Network.CreateTokenExecutor().Run([1, 0, 0]);
        CollectionAssert.AreEqual(before.Predictions[1].Logits.ToArray(), after.Predictions[1].Logits.ToArray());
        Assert.AreNotEqual(before.Predictions[2].Logits[0], after.Predictions[2].Logits[0]);
    }

    [TestMethod]
    public void FeedbackAdditionPreservesEveryExistingParameterState()
    {
        var model = Model();
        model.Network.TrainTokens([1, 0, 1]);
        var candidate = model.Apply(FeedbackExperiment.CreateAction(FeedbackTopology.Local, 2, .1));
        var old = model.Network.Graph.ElementList.Select(n => n.Value.Bias)
            .Concat(model.Network.Graph.ElementList.SelectMany(n => n.OutEdgeList).Select(e => e.EdgeInfo!.Weight))
            .Concat(model.Network.TextBindings!.Parameters).DistinctBy(p => p.Id).ToArray();
        var current = candidate.Network.Graph.ElementList.Select(n => n.Value.Bias)
            .Concat(candidate.Network.Graph.ElementList.SelectMany(n => n.OutEdgeList).Select(e => e.EdgeInfo!.Weight))
            .Concat(candidate.Network.TextBindings!.Parameters).DistinctBy(p => p.Id).ToDictionary(p => p.Id);
        CollectionAssert.AreEqual(old.Select(p => (p.Id, p.Value, p.FirstMoment, p.SecondMoment, p.Updates)).ToArray(),
            old.Select(p => current[p.Id]).Select(p => (p.Id, p.Value, p.FirstMoment, p.SecondMoment, p.Updates)).ToArray());
    }

    [TestMethod]
    public void MultipleDelaysRemainIndependentPhysicalEdges()
    {
        var model = Model();
        var candidate = model.Apply(FeedbackExperiment.CreateAction(FeedbackTopology.Local, 1, .1))
            .Apply(FeedbackExperiment.CreateAction(FeedbackTopology.Local, 3, .2));
        CollectionAssert.AreEqual(new[] { 1, 3 }, candidate.Topology.Expand()
            .Where(e => e.From == "A" && e.To == "A").Select(e => e.Delay).Order().ToArray());
    }

    [TestMethod]
    public void SampledGenerationRestorePreservesRandomStream()
    {
        var model = Model();
        var session = model.Network.CreateTokenExecutor().CreateGenerationSession([1, 0], 5,
            new SamplingOptions(DoSample: true), seed: 71);
        session.NextToken();
        var restored = FeedbackExperiment.RestoreEquivalent(model, model, session.ExportCheckpoint());
        session.NextToken();
        restored.NextToken();
        CollectionAssert.AreEqual(session.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public void OldStateCannotBeUsedByChangedLayout()
    {
        var model = Model();
        var old = model.Network.CreateTokenExecutor().Run([1]).State;
        var candidate = model.Apply(FeedbackExperiment.CreateAction(FeedbackTopology.Local, 1, .1));
        Assert.ThrowsException<InvalidOperationException>(() => candidate.Network.CreateTokenExecutor().Run([0], state: old));
    }

    [TestMethod]
    public void EquivalentGenerationRestoreContinuesExactly()
    {
        var model = Model();
        var session = FeedbackExperiment.StartZero(model, [1, 0], 4);
        session.NextToken();
        var restored = FeedbackExperiment.RestoreEquivalent(model, ForgeArchive.Import(ForgeArchive.Export(model)), session.ExportCheckpoint());
        session.NextToken();
        restored.NextToken();
        CollectionAssert.AreEqual(session.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public void CrossStructureMigrationIsExplicitlyRejected()
    {
        var model = Model();
        var session = FeedbackExperiment.StartZero(model, [1], 2);
        var candidate = model.Apply(FeedbackExperiment.CreateAction(FeedbackTopology.Local, 1, 0));
        Assert.ThrowsException<NotSupportedException>(() => FeedbackExperiment.RestoreEquivalent(model, candidate, session.ExportCheckpoint()));
    }

    [TestMethod]
    public void SharedRestoreAlsoRejectsChangedModel()
    {
        var model = Model();
        var session = FeedbackExperiment.StartZero(model, [1], 2);
        var candidate = model.Apply(FeedbackExperiment.CreateAction(FeedbackTopology.CrossLayer, 2, .1));
        Assert.ThrowsException<InvalidDataException>(() => candidate.Network.CreateTokenExecutor().RestoreGenerationSession(session.ExportCheckpoint()));
    }

    [TestMethod]
    public void ZeroResetDoesNotReuseGeneratedHistory()
    {
        var model = Model();
        var first = FeedbackExperiment.StartZero(model, [1], 3);
        var original = first.ExportCheckpoint();
        first.NextToken();
        CollectionAssert.AreEqual(original, FeedbackExperiment.StartZero(model, [1], 3).ExportCheckpoint());
    }

    [TestMethod]
    public void PrefixWarmupNeverReadsFutureToken()
    {
        var model = Model();
        var candidate = model.Apply(FeedbackExperiment.CreateAction(FeedbackTopology.CrossLayer, 2, .2));
        var result = FeedbackExperiment.WarmSeenPrefix(model, candidate, [1, 0, int.MaxValue], 2, 2);
        CollectionAssert.AreEqual(FeedbackExperiment.StartZero(candidate, [1, 0], 2).ExportCheckpoint(), result.Candidate.ExportCheckpoint());
    }

    [TestMethod]
    public void PrefixWarmupReportsBothBranchesCost()
    {
        var model = Model();
        Assert.AreEqual(4L, FeedbackExperiment.WarmSeenPrefix(model, model, [1, 0], 2, 2).ReplayInternalSteps);
    }

    [TestMethod]
    public void PrefixWarmupRejectsDifferentEncoding() => Assert.ThrowsException<InvalidOperationException>(() =>
        FeedbackExperiment.WarmSeenPrefix(Model(), Model(.5), [1], 1, 2));

    [TestMethod]
    public void PrefixBeyondSeenPositionRejected() => Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
        FeedbackExperiment.WarmSeenPrefix(Model(), Model(), [1], 2, 2));
}
