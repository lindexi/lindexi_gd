using FractalForge.Core;
using FractalForge.Training;
using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Research.Deepening.Tests;

[TestClass]
public class DeepeningTests
{
    private static ForgeModel Model()
    {
        var model = ForgeModel.Create(ForgeTopology.CreateDiamond(7, feedback: true));
        model.Network.BindText(new TokenBindings("deepening-test-v1", ["I"], ["O"],
            new IReadOnlyList<double>[] { new double[] { -.8 }, new double[] { .2 }, new double[] { .9 } }, seed: 4));
        return model;
    }

    private static DeepeningRules Rules(int cooldown = 0) => new("test-v1", "train-v1", "accept-v1", 2, 100, 10000000, 100, cooldown, new());

    [TestMethod]
    public void D0PreservesCompleteArchive()
    {
        var source = Model();
        var proposal = DeepeningStrategy.Propose(source, DeepeningBranch.D0, "A");
        CollectionAssert.AreEqual(ForgeArchive.Export(source), ForgeArchive.Export(proposal.Model));
    }

    [TestMethod]
    public void D1PreservesLossWithRealIdentity()
    {
        var source = Model();
        var candidate = DeepeningStrategy.Propose(source, DeepeningBranch.D1, "A").Model;
        Assert.AreEqual(source.Network.EvaluateTokens(new[] { 0, 1, 2, 1 }), candidate.Network.EvaluateTokens(new[] { 0, 1, 2, 1 }), 1e-12);
    }

    [TestMethod]
    public void D1AddsActualComputationalDepth()
    {
        var source = Model();
        Assert.IsTrue(DeepeningStrategy.LongestPath(DeepeningStrategy.Propose(source, DeepeningBranch.D1, "A").Model) > DeepeningStrategy.LongestPath(source));
    }

    [TestMethod]
    public void D2InitiallyPreservesLoss()
    {
        var source = Model();
        var proposal = DeepeningStrategy.Propose(source, DeepeningBranch.D2, "A");
        Assert.AreEqual(source.Network.EvaluateTokens(new[] { 0, 1, 2 }), proposal.Model.Network.EvaluateTokens(new[] { 0, 1, 2 }), 1e-12);
    }

    [TestMethod]
    public void D2GateLearnsThroughRealBackwardPass()
    {
        var proposal = DeepeningStrategy.Propose(Model(), DeepeningBranch.D2, "A");
        proposal.Model.Network.TrainTokens(new[] { 0, 1, 2, 0, 2 });
        var weights = proposal.Model.Network.Graph.ElementList.SelectMany(n => n.OutEdgeList).Select(e => e.EdgeInfo!.Weight);
        Assert.IsTrue(weights.Where(w => proposal.Gates.Contains(w.Id)).Any(w => w.Value != 0));
    }

    [TestMethod]
    public void D3TotalAbsoluteGateWeightBoundsInitialCorrection()
    {
        var proposal = DeepeningStrategy.Propose(Model(), DeepeningBranch.D3, "A", .03);
        var weights = proposal.Model.Network.Graph.ElementList.SelectMany(n => n.OutEdgeList).Select(e => e.EdgeInfo!.Weight);
        Assert.AreEqual(.03, weights.Where(w => proposal.Gates.Contains(w.Id)).Sum(w => Math.Abs(w.Value)), 1e-14);
    }

    [TestMethod]
    public void D2RetainsEveryParentRoute()
    {
        var source = Model();
        var candidate = DeepeningStrategy.Propose(source, DeepeningBranch.D2, "A").Model;
        CollectionAssert.IsSubsetOf(source.Topology.Routes.Select(r => r.Id).ToArray(), candidate.Topology.Routes.Select(r => r.Id).ToArray());
    }

    [TestMethod]
    public void D1RejectsFeedbackEnteringTarget()
    {
        Assert.ThrowsException<InvalidOperationException>(() => DeepeningStrategy.Propose(Model(), DeepeningBranch.D1, "I"));
    }

    [TestMethod]
    public void D2RejectsDelayedIncomingRoute()
    {
        var model = Model().Apply(new("add-feedback", "O", Destination: "A", Delay: 1, Weight: .01));
        Assert.ThrowsException<InvalidOperationException>(() => DeepeningStrategy.Propose(model, DeepeningBranch.D2, "A"));
    }

    [TestMethod]
    public void LocalOutputResolvesToRecordedMerge()
    {
        var first = DeepeningStrategy.Propose(Model(), DeepeningBranch.D1, "A").Model;
        var output = first.Topology.Units.Last().Output;
        Assert.AreEqual("A", DeepeningStrategy.Propose(first, DeepeningBranch.D1, output).Target);
    }

    [TestMethod]
    public void CancelledAdvanceDoesNotCommit()
    {
        var experiment = DeepeningExperiment.Begin(Model(), DeepeningBranch.D2, "A", 0, Rules());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => experiment.Advance(new[] { 0, 1, 2 }, 0, cancellation.Token));
    }

    [TestMethod]
    public async Task ResumeMatchesUninterruptedNumericalState()
    {
        var rules = Rules();
        var experiment = DeepeningExperiment.Begin(Model(), DeepeningBranch.D3, "A", .02, rules);
        experiment.Advance(new[] { 0, 1, 2 }, 0);
        var path = Path.Combine(Path.GetTempPath(), "deepening-tests", Guid.NewGuid().ToString("N"), "experiment.json");
        await experiment.SaveAsync(path);
        var restored = await DeepeningExperiment.LoadAsync(path, rules);
        experiment.Advance(new[] { 2, 1, 0 }, 1);
        restored.Advance(new[] { 2, 1, 0 }, 1);
        CollectionAssert.AreEqual(experiment.State.Candidate, restored.State.Candidate);
    }

    [TestMethod]
    public void EvaluationSelectsTrainedKeepNotSource()
    {
        var experiment = DeepeningExperiment.Begin(Model(), DeepeningBranch.D2, "A", 0, Rules());
        experiment.Advance(new[] { 0, 1, 2 }, 0);
        experiment.Advance(new[] { 2, 1, 0 }, 1);
        experiment.Evaluate("accept-v1", new[] { new[] { 0, 2, 1 } });
        CollectionAssert.AreEqual(experiment.State.Keep, ForgeArchive.Export(experiment.Selected));
    }

    [TestMethod]
    public void CooldownPreventsPrematureGrowth()
    {
        var experiment = DeepeningExperiment.Begin(Model(), DeepeningBranch.D2, "A", 0, Rules(1));
        experiment.Advance(new[] { 0, 1 }, 0);
        experiment.Advance(new[] { 1, 2 }, 1);
        experiment.Evaluate("accept-v1", new[] { new[] { 0, 2 } });
        Assert.ThrowsException<InvalidOperationException>(() => experiment.Next(DeepeningBranch.D1, "A", 0, Rules()));
    }

    [TestMethod]
    public void CooldownAllowsContinuousGrowthAtSameSamplePosition()
    {
        var experiment = DeepeningExperiment.Begin(Model(), DeepeningBranch.D2, "A", 0, Rules(1));
        experiment.Advance(new[] { 0, 1 }, 0);
        experiment.Advance(new[] { 1, 2 }, 1);
        experiment.Evaluate("accept-v1", new[] { new[] { 0, 2 } });
        experiment.Cooldown(new[] { 2, 0 });
        Assert.AreEqual(3L, experiment.Next(DeepeningBranch.D1, "A", 0, Rules()).State.Position);
    }

    [DataTestMethod]
    [DataRow(DeepeningBranch.D0)]
    [DataRow(DeepeningBranch.D1)]
    public void IdentityBranchesRejectUnusedPerturbationBudget(DeepeningBranch branch)
    {
        Assert.ThrowsException<InvalidOperationException>(() => DeepeningStrategy.Propose(Model(), branch, "A", .01));
    }

    [TestMethod]
    public void CooldownRejectsExhaustedTokenBudgetWithoutPublishingState()
    {
        var experiment = DeepeningExperiment.Begin(Model(), DeepeningBranch.D2, "A", 0, Rules(1) with { MaxTokens = 2 });
        experiment.Advance(new[] { 0, 1 }, 0);
        experiment.Advance(new[] { 1, 2 }, 1);
        experiment.Evaluate("accept-v1", new[] { new[] { 0, 2 } });
        var before = System.Text.Json.JsonSerializer.Serialize(experiment.State);
        Assert.ThrowsException<InvalidOperationException>(() => experiment.Cooldown(new[] { 2, 0 }));
        Assert.AreEqual(before, System.Text.Json.JsonSerializer.Serialize(experiment.State));
    }

    [TestMethod]
    public void CooldownCountsTokensAtExactBudgetBoundary()
    {
        var experiment = DeepeningExperiment.Begin(Model(), DeepeningBranch.D2, "A", 0, Rules(1) with { MaxTokens = 3 });
        experiment.Advance(new[] { 0, 1 }, 0);
        experiment.Advance(new[] { 1, 2 }, 1);
        experiment.Evaluate("accept-v1", new[] { new[] { 0, 2 } });
        experiment.Cooldown(new[] { 2, 0 });
        Assert.AreEqual(3L, experiment.State.Tokens);
    }

    [TestMethod]
    public void D1KeepsMultipleSourcesSeparate()
    {
        var source = Model();
        var candidate = DeepeningStrategy.Propose(source, DeepeningBranch.D1, "B").Model;
        Assert.AreEqual(source.Topology.Routes.Count(r => r.Path[^1] == "B"), candidate.Topology.Units.Count(u => u.Target == "B"));
    }

    [TestMethod]
    public void D3CorrectionIsNumericallyBoundedAndNonzero()
    {
        var source = Model();
        var proposal = DeepeningStrategy.Propose(source, DeepeningBranch.D3, "A", .01);
        var before = source.Network.CreateTokenExecutor().Run(new[] { 0, 1, 2 }).Predictions;
        var after = proposal.Model.Network.CreateTokenExecutor().Run(new[] { 0, 1, 2 }).Predictions;
        var difference = before.Zip(after).Max(pair => pair.First.Logits.Zip(pair.Second.Logits).Max(values => Math.Abs(values.First - values.Second)));
        Assert.IsTrue(difference > 0 && difference < .01);
    }

    [TestMethod]
    public async Task ResumeRejectsChangedRules()
    {
        var experiment = DeepeningExperiment.Begin(Model(), DeepeningBranch.D0, "A", 0, Rules());
        var path = Path.Combine(Path.GetTempPath(), "deepening-tests", Guid.NewGuid().ToString("N"), "experiment.json");
        await experiment.SaveAsync(path);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => DeepeningExperiment.LoadAsync(path, Rules() with { MinimumGain = 0 }));
    }

    [TestMethod]
    public async Task EntryCancellationReturns130()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.AreEqual(130, await DeepeningRunner.RunAsync(Array.Empty<string>(), TextWriter.Null, TextWriter.Null, cancellation.Token));
    }
}
