using System.Text.Json;
using FractalForge.Core;
using FractalForge.Training;
using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Research.Adaptive.Tests;

[TestClass]
public class AdaptiveTests
{
    private static AdaptiveOptions Options(string branch = "A1") => new(branch, 1, 0, 0, 10, 1, .2, .1, .01, 17, 100, 100000, long.MaxValue / 4);
    private static ForgeAction[] Actions() => [new("clone-widen", "A"), new("clone-widen", "B"), new("output-deepen", "A")];
    private static TrialProtocol Protocol(double minimum = 0) => new("adaptive-test", "train", "accept", "fixed-v1", 2, 100, long.MaxValue / 2, minimum, new());
    private static string PathFor(string file) => Path.Combine(Path.GetTempPath(), "adaptive-tests", Guid.NewGuid().ToString("N"), file);
    private static ForgeModel Model()
    {
        var model = ForgeModel.Create(ForgeTopology.CreateDiamond(7, false));
        model.Network.BindText(new TokenBindings("unit-tokenizer", ["I"], ["O"], [new double[] { 0 }, new double[] { .5 }, new double[] { 1 }], seed: 4));
        return model;
    }

    [TestMethod]
    public void GrowthDesireDoesNotDependOnCandidateCount()
    {
        var options = Options() with { GrowthProbability = .37 };
        var empty = new AdaptivePolicy(options).Propose(0, []);
        var full = new AdaptivePolicy(options).Propose(0, Actions());
        Assert.AreEqual((empty.GrowthProbability, empty.GrowthDraw, empty.WantsGrowth),
            (full.GrowthProbability, full.GrowthDraw, full.WantsGrowth));
    }

    [TestMethod]
    public void HierarchicalProbabilityIncludesGrowthKindAndPosition()
    {
        var proposal = new AdaptivePolicy(Options()).Propose(0, Actions());
        Assert.AreEqual(proposal.Action?.Kind == "clone-widen" ? .25 : .5, proposal.JointProbability, 1e-12);
    }

    [TestMethod]
    public void FixedRandomGrowthFrequencyMatchesConfiguredProbability()
    {
        var proposals = Enumerable.Range(0, 2000).Select(seed => new AdaptivePolicy(Options() with
            { Seed = (ulong)seed, GrowthProbability = .3 }).Propose(0, Actions())).ToArray();
        Assert.IsTrue(Math.Abs(proposals.Count(p => p.WantsGrowth) / 2000.0 - .3) < .04);
    }

    [TestMethod]
    public void CooldownSuppressesNextOpportunity()
    {
        var policy = new AdaptivePolicy(Options() with { CooldownUpdates = 3 });
        policy.Propose(0, Actions());
        Assert.AreEqual(0, policy.Propose(1, Actions()).GrowthProbability);
    }

    [TestMethod]
    public void TrialBudgetSuppressesFurtherGrowth()
    {
        var policy = new AdaptivePolicy(Options() with { MaxTrials = 1 });
        policy.Propose(0, Actions());
        Assert.AreEqual(0, policy.Propose(10, Actions()).GrowthProbability);
    }

    [TestMethod]
    public void HeuristicUsesRecentLossPlateau()
    {
        var policy = new AdaptivePolicy(Options("A2"));
        policy.Observe(2);
        policy.Observe(2);
        Assert.AreEqual(1, policy.Propose(0, Actions()).GrowthProbability);
    }

    [TestMethod]
    public void HeuristicSuppressesGrowthDuringRapidImprovement()
    {
        var policy = new AdaptivePolicy(Options("A2"));
        policy.Observe(2);
        policy.Observe(1);
        Assert.AreEqual(0, policy.Propose(0, Actions()).GrowthProbability);
    }

    [DataTestMethod]
    [DataRow("fault")]
    [DataRow("uncertain")]
    public void IncompleteTrialDoesNotTrainScorer(string status)
    {
        var policy = new AdaptivePolicy(Options("A3"));
        var proposal = policy.Propose(0, Actions());
        policy.Record(proposal, status);
        CollectionAssert.AreEqual(new double[8], policy.Snapshot().Weights);
    }

    [TestMethod]
    public void ActualRejectedPairGainTrainsScorer()
    {
        var policy = new AdaptivePolicy(Options("A3"));
        var proposal = policy.Propose(0, Actions());
        policy.Record(proposal, "rejected", -.2);
        Assert.IsTrue(policy.Snapshot().Weights[0] < 0);
    }

    [TestMethod]
    public void UnattemptedProposalCannotTrainScorer()
    {
        var policy = new AdaptivePolicy(Options("A3") with { GrowthProbability = 0 });
        var proposal = policy.Propose(0, Actions());
        Assert.ThrowsException<InvalidOperationException>(() => policy.Record(proposal, "rejected", -1));
    }

    [TestMethod]
    public void PolicyRestorePreservesNextSampleAndProbabilities()
    {
        var options = Options("A3");
        var policy = new AdaptivePolicy(options);
        policy.Observe(2);
        var proposal = policy.Propose(0, Actions());
        policy.Record(proposal, "accepted", .2);
        var restored = new AdaptivePolicy(options, JsonSerializer.Deserialize<AdaptivePolicyState>(JsonSerializer.Serialize(policy.Snapshot())));
        Assert.AreEqual(JsonSerializer.Serialize(policy.Propose(1, Actions())), JsonSerializer.Serialize(restored.Propose(1, Actions())));
    }

    [TestMethod]
    public void A0PerformsNormalTrainingWithoutGrowing()
    {
        var session = new AdaptiveSession(Model(), Options("A0"), Protocol(), Actions());
        var before = session.Model.Network.EvaluateTokens([0, 1, 2]);
        session.Advance([0, 1, 2]);
        Assert.AreNotEqual(before, session.Model.Network.EvaluateTokens([0, 1, 2]));
    }

    [TestMethod]
    public void A0NeverConstructsCandidate()
    {
        var session = new AdaptiveSession(Model(), Options("A0"), Protocol(), Actions());
        session.Advance([0, 1, 2]);
        Assert.AreEqual((false, 4, 0L), (session.HasPendingTrial, session.Model.MeasureCost().ComputeNodes, session.PairTokens));
    }

    [TestMethod]
    public void PendingWindowRestoreMatchesUninterruptedRealAction()
    {
        var source = Model();
        var options = Options("A3");
        var protocol = Protocol(-100);
        ForgeAction[] actions = [new("clone-widen", "A")];
        var session = new AdaptiveSession(source, options, protocol, actions);
        session.Advance([0, 1, 2]);
        var path = PathFor("adaptive.json");
        session.Save(path);
        var restored = AdaptiveSession.Load(path, options, protocol, actions);
        session.Advance([2, 1, 0]);
        restored.Advance([2, 1, 0]);
        session.Evaluate("accept", [new int[] { 0, 2, 1 }]);
        restored.Evaluate("accept", [new int[] { 0, 2, 1 }]);
        Assert.AreEqual(ForgeArchive.Fingerprint(ForgeArchive.Export(session.Model)), ForgeArchive.Fingerprint(ForgeArchive.Export(restored.Model)));
    }

    [DataTestMethod]
    [DataRow("clone-widen", "A", 5)]
    [DataRow("output-deepen", "A", 6)]
    [DataRow("add-feedback", "O", 4)]
    public void AcceptedTrialPublishesActualTopologyAction(string kind, string target, int nodes)
    {
        var session = new AdaptiveSession(Model(), Options(), Protocol(-100), [new(kind, target, Destination: "A", Weight: .01)]);
        session.Advance([0, 1, 2]);
        session.Advance([2, 1, 0]);
        session.Evaluate("accept", [new int[] { 0, 2, 1 }]);
        Assert.AreEqual(nodes, session.Model.MeasureCost().ComputeNodes);
    }

    [TestMethod]
    public void RejectedTrialPublishesTrainedKeepNotSource()
    {
        var session = new AdaptiveSession(Model(), Options(), Protocol(100), [new("clone-widen", "A")]);
        var before = session.Model.Network.EvaluateTokens([0, 1, 2]);
        session.Advance([0, 1, 2]);
        session.Advance([2, 1, 0]);
        session.Evaluate("accept", [new int[] { 0, 2, 1 }]);
        Assert.AreNotEqual(before, session.Model.Network.EvaluateTokens([0, 1, 2]));
    }

    [TestMethod]
    public void CandidateBudgetFilteringDoesNotCreateNegativeLearningLabel()
    {
        var session = new AdaptiveSession(Model(), Options("A3") with { MaxComputeNodes = 4 }, Protocol(), [new("clone-widen", "A")]);
        session.Advance([0, 1, 2]);
        Assert.AreEqual((true, "no-candidates", 0), (session.Policy.Snapshot().Proposals[0].WantsGrowth,
            session.Policy.Snapshot().Proposals[0].Reason, session.Policy.Snapshot().Outcomes.Length));
    }

    [TestMethod]
    public void AbandonedWindowIsUncertainRatherThanRejected()
    {
        var session = new AdaptiveSession(Model(), Options("A3"), Protocol(), Actions());
        session.Advance([0, 1, 2]);
        session.Abandon(false, "bounded-stop");
        Assert.AreEqual("uncertain", session.Policy.Snapshot().Outcomes.Single().Status);
    }

    [TestMethod]
    public void RestoreRejectsDifferentPolicyContract()
    {
        var session = new AdaptiveSession(Model(), Options(), Protocol(), Actions());
        var path = PathFor("adaptive.json");
        session.Save(path);
        Assert.ThrowsException<InvalidDataException>(() => AdaptiveSession.Load(path, Options() with { Exploration = .4 }, Protocol(), Actions()));
    }

    [TestMethod]
    public void ExplorationPreservesProbabilityFloorAfterLearning()
    {
        var options = Options("A3");
        var policy = new AdaptivePolicy(options);
        var first = policy.Propose(0, Actions());
        policy.Record(first, "accepted", 1);
        var next = policy.Propose(1, Actions());
        Assert.IsTrue(next.KindProbability >= options.Exploration / 2 &&
            next.PositionProbability >= options.Exploration / 2);
    }

    [TestMethod]
    public void FeedbackTrialAddsRealPhysicalConnection()
    {
        var session = new AdaptiveSession(Model(), Options(), Protocol(-100), [new("add-feedback", "O", Destination: "A", Weight: .01)]);
        var connections = session.Model.MeasureCost().Connections;
        session.Advance([0, 1, 2]);
        session.Advance([2, 1, 0]);
        session.Evaluate("accept", [new int[] { 0, 2, 1 }]);
        Assert.AreEqual(connections + 1, session.Model.MeasureCost().Connections);
    }

    [TestMethod]
    public void RestoreRejectsDifferentAcceptanceRule()
    {
        var session = new AdaptiveSession(Model(), Options(), Protocol(), Actions());
        var path = PathFor("adaptive.json");
        session.Save(path);
        Assert.ThrowsException<InvalidDataException>(() => AdaptiveSession.Load(path, Options(), Protocol(1), Actions()));
    }

    [TestMethod]
    public void FaultedWindowPublishesAlignedKeepWithoutLearning()
    {
        var session = new AdaptiveSession(Model(), Options("A3"), Protocol(), Actions());
        session.Advance([0, 1, 2]);
        session.Abandon(true, "training-fault");
        Assert.AreEqual((1L, "fault", 0.0), (session.Position, session.Policy.Snapshot().Outcomes.Single().Status,
            session.Policy.Snapshot().Weights.Sum(Math.Abs)));
    }

    [TestMethod]
    public async Task EntryRejectsMissingArguments()
    {
        Assert.AreEqual(2, await AdaptiveRunner.RunAsync([], TextWriter.Null, TextWriter.Null));
    }

    [TestMethod]
    public async Task EntryReturnsCancellationCode()
    {
        Assert.AreEqual(130, await AdaptiveRunner.RunAsync([], TextWriter.Null, TextWriter.Null, new CancellationToken(true)));
    }
}
