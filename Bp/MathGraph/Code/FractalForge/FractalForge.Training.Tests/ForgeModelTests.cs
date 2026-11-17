using FractalForge.Core;
using FractalForge.Training;
using MathGraphs.Neural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FractalForge.Training.Tests;

[TestClass]
public class ForgeModelTests
{
    private static ForgeModel Model()
    {
        var model = ForgeModel.Create(ForgeTopology.CreateDiamond(7));
        model.Network.BindText(new TokenBindings("test-v1", ["I"], ["O"], new double[][] { [-1], [0], [1] }, seed: 4));
        return model;
    }
    private static IReadOnlyList<IReadOnlyDictionary<string, double>> Inputs() => new IReadOnlyDictionary<string, double>[]
    { new Dictionary<string, double> { ["I"] = .7 }, new Dictionary<string, double> { ["I"] = -.4 }, new Dictionary<string, double> { ["I"] = .2 } };
    private static TrialProtocol Protocol() => new("run-test", "data-v1", "accept-v1", "fixed-endpoint-v1", 1, 20, 1000000, 100, new AdamWOptions());

    [TestMethod]
    public void RootExecutionMatchesIndependentInterpreter()
    {
        var model = Model();
        Assert.AreEqual(GraphInterpreter.Run(model.Network, Inputs())[2]["O"], model.Network.CreateExecutor().Run(Inputs()).Outputs[2]["O"], 1e-12);
    }

    [TestMethod]
    public void CloneWidenPreservesForwardFunction()
    {
        var model = Model(); var candidate = model.Apply(new("clone-widen", "A", .3));
        Assert.AreEqual(model.Network.CreateExecutor().Run(Inputs()).Outputs[2]["O"], candidate.Network.CreateExecutor().Run(Inputs()).Outputs[2]["O"], 1e-12);
    }

    [TestMethod]
    public void OutputDeepenPreservesMultipleSources()
    {
        var model = Model(); var candidate = model.Apply(new("output-deepen", "O"));
        Assert.AreEqual(model.Network.CreateExecutor().Run(Inputs()).Outputs[2]["O"], candidate.Network.CreateExecutor().Run(Inputs()).Outputs[2]["O"], 1e-12);
    }

    [TestMethod]
    public void RecursiveOutputCanBeDeepenedAgain()
    {
        var model = Model().Apply(new("output-deepen", "O"));
        var target = model.Topology.Units[0].A;
        Assert.IsTrue(model.Apply(new("output-deepen", target)).MeasureCost().ComputeNodes > model.MeasureCost().ComputeNodes);
    }

    [TestMethod]
    public void UnchangedParameterRetainsOptimizerHistory()
    {
        var model = Model(); model.Network.TrainTokens(new[] { 0, 1, 2, 0 });
        var old = model.Network.Graph.ElementList.Single(n => n.Id == "B").Value.Bias;
        var next = model.Apply(new("clone-widen", "A")).Network.Graph.ElementList.Single(n => n.Id == "B").Value.Bias;
        Assert.AreEqual((old.Id, old.Value, old.FirstMoment, old.SecondMoment, old.Updates), (next.Id, next.Value, next.FirstMoment, next.SecondMoment, next.Updates));
    }

    [TestMethod]
    public void NewParametersStartWithZeroMoments()
    {
        var model = Model(); model.Network.TrainTokens(new[] { 0, 1, 2, 0 });
        var candidate = model.Apply(new("clone-widen", "A"));
        var original = model.Topology.Biases.Values.Select(b => b.ParameterId).ToHashSet();
        Assert.IsTrue(candidate.Network.Graph.ElementList.Select(n => n.Value.Bias).Where(p => !original.Contains(p.Id)).All(p => p.Updates == 0 && p.FirstMoment == 0 && p.SecondMoment == 0));
    }

    [TestMethod]
    public void FailedActionDoesNotMutateSource()
    {
        var model = Model(); var before = ForgeArchive.Export(model);
        Assert.ThrowsException<InvalidOperationException>(() => model.Apply(new("clone-widen", "O")));
        CollectionAssert.AreEqual(before, ForgeArchive.Export(model));
    }

    [TestMethod]
    public void TokenTrainingContinuesExactlyAfterRestore()
    {
        var model = Model(); model.Network.TrainTokens(new[] { 0, 1, 2, 0 });
        var restored = ForgeArchive.Import(ForgeArchive.Export(model));
        model.Network.TrainTokens(new[] { 2, 1, 0, 2 }); restored.Network.TrainTokens(new[] { 2, 1, 0, 2 });
        CollectionAssert.AreEqual(model.Network.ExportCheckpoint(), restored.Network.ExportCheckpoint());
    }

    [TestMethod]
    public void GenerationSessionRestoresRandomAndHistory()
    {
        var model = Model(); var executor = model.Network.CreateTokenExecutor();
        var session = executor.CreateGenerationSession(new[] { 0, 1 }, 3, seed: 42);
        session.NextToken();
        var restored = model.Network.CreateTokenExecutor().RestoreGenerationSession(session.ExportCheckpoint());
        Assert.AreEqual(session.NextToken(), restored.NextToken());
    }

    [TestMethod]
    public void RejectedTrialKeepsConsumedWindow()
    {
        var trial = PairedTrial.Begin(Model(), new("clone-widen", "A"), Protocol(), 8, 17);
        trial.Advance(new[] { 0, 1, 2 }, 8, 19);
        trial.Evaluate("accept-v1", new IReadOnlyList<int>[] { new[] { 0, 1, 2 } });
        Assert.AreSame(trial.Keep, trial.Complete());
    }

    [TestMethod]
    public void CancelledTrialPreservesBothBranchesAndCursor()
    {
        var trial = PairedTrial.Begin(Model(), new("clone-widen", "A"), Protocol());
        var before = ForgeArchive.Export(trial.Keep);
        Assert.ThrowsException<OperationCanceledException>(() => trial.Advance(new[] { 0, 1 }, 0, 2, new CancellationToken(true)));
        CollectionAssert.AreEqual(before, ForgeArchive.Export(trial.Keep));
        Assert.AreEqual(0L, trial.Progress.Position);
    }

    [TestMethod]
    public void ManifestRestoresExplicitPairAndProgress()
    {
        var trial = PairedTrial.Begin(Model(), new("clone-widen", "A"), Protocol(), 3, 9);
        trial.Advance(new[] { 0, 1, 2 }, 3, 12);
        var path = Path.Combine(Path.GetTempPath(), "forge-tests", Guid.NewGuid().ToString("N"), "trial.json");
        trial.Save(path);
        Assert.AreEqual(trial.Progress, PairedTrial.Load(path, Protocol()).Progress);
    }

    [TestMethod]
    public void UnknownModelSemanticsAreRejected()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(ForgeArchive.Export(Model())).Replace(ForgeTopology.Semantics, "unknown"));
        Assert.ThrowsException<InvalidDataException>(() => ForgeArchive.Import(bytes));
    }

    [TestMethod]
    public void FeedbackActionAddsPhysicalDelayedConnection()
    {
        var candidate = Model().Apply(new("add-feedback", "A", Destination: "B", Delay: 2, Weight: .2));
        Assert.AreEqual(2, candidate.Topology.Expand().Single(e => e.From == "A" && e.To == "B").Delay);
    }

    [TestMethod]
    public void DelayedFeedbackGradientMatchesFiniteDifference()
    {
        var model = Model().Apply(new("add-feedback", "A", Destination: "B", Delay: 2, Weight: .2));
        var route = model.Topology.Routes.Single(r => r.Path[0] == "A" && r.Path[^1] == "B");
        var seeds = new IReadOnlyDictionary<string, double>[] { new Dictionary<string, double>(), new Dictionary<string, double>(), new Dictionary<string, double> { ["O"] = 1 } };
        var gradient = model.Network.CreateExecutor().Run(Inputs()).Backward(seeds)[route.ParameterId];
        double Value(double delta)
        {
            var topology = new ForgeTopology(model.Topology.Graph.ElementList.ToDictionary(n => n.Id, n => n.Value), model.Topology.Biases,
                model.Topology.Routes.Select(r => r.Id == route.Id ? r with { InitialValue = r.InitialValue + delta } : r), model.Topology.Units);
            return ForgeModel.Create(topology).Network.CreateExecutor().Run(Inputs()).Outputs[2]["O"];
        }
        Assert.AreEqual((Value(1e-6) - Value(-1e-6)) / 2e-6, gradient, 1e-8);
    }

    [TestMethod]
    public void OverBudgetActionLeavesOriginalModelUntouched()
    {
        var model = ForgeModel.Create(ForgeTopology.CreateDiamond(), new NetworkBudget(MaxNodes: 4));
        var before = ForgeArchive.Export(model);
        Assert.ThrowsException<InvalidOperationException>(() => model.Apply(new("clone-widen", "A")));
        CollectionAssert.AreEqual(before, ForgeArchive.Export(model));
    }

    [TestMethod]
    public void CorruptReferencedBranchIsRejected()
    {
        var trial = PairedTrial.Begin(Model(), new("clone-widen", "A"), Protocol());
        var directory = Path.Combine(Path.GetTempPath(), "forge-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "trial.json"); trial.Save(path);
        var branch = Directory.GetFiles(directory, "*.forge")[0]; File.WriteAllBytes(branch, new byte[] { 1, 2 });
        Assert.ThrowsException<InvalidDataException>(() => PairedTrial.Load(path, Protocol()));
    }

    [TestMethod]
    public void OldFivePointFractalCheckpointRetainsSemantics()
    {
        var graph = FiveNodeGraph.Create(3); var network = new RecursiveNetwork(graph.Graph);
        network.Fractalize(graph.Graph.ElementList[1].Id, 4);
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint());
        Assert.AreEqual(5, restored.Graph.ElementList[1].Value.Child!.Graph.ElementList.Count);
    }
}
