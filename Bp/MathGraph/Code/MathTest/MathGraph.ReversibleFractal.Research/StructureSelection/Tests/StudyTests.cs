using MathGraph.StructureSelection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraph.StructureSelection.Tests;

[TestClass]
public sealed class StudyTests
{
    [DataTestMethod]
    [DataRow("A", "C>A,D>A,C>B,D>B,E>C,E>D")]
    [DataRow("C", "A>C,A>D,B>C,B>D,C>E,D>E")]
    public void ExpansionUsesExactlyTheOrientedFivePointTemplate(string point, string expected)
    {
        var graph = FivePointGraph.Create().Expand(point);
        string prefix = point + "/";
        var actual = graph.Edges.Where(e => e.From.Vertex.StartsWith(prefix, StringComparison.Ordinal) &&
            e.To.Vertex.StartsWith(prefix, StringComparison.Ordinal)).Select(e => $"{e.From.Vertex[^1]}>{e.To.Vertex[^1]}");
        Assert.AreEqual(expected, string.Join(",", actual));
    }

    [TestMethod]
    public void NestedOppositeDirectionsPreserveOrderedPortsRegardlessOfExpansionOrder()
    {
        var root = FivePointGraph.Create();
        var left = root.Expand("A").Expand("C").Expand("A/A");
        var right = root.Expand("C").Expand("A").Expand("A/A");
        CollectionAssert.AreEquivalent(right.Edges.ToArray(), left.Edges.ToArray());
    }

    [TestMethod]
    public void NestedExpansionReconnectsTheOriginalAtoCAtOrderedBoundaryPorts()
    {
        var graph = FivePointGraph.Create().Expand("A").Expand("C").Expand("A/A");
        CollectionAssert.Contains(graph.Edges.ToArray(), new Edge(new("A/A/E", 0), new("C/A", 0)));
    }

    [DataTestMethod]
    [DataRow("forward")]
    [DataRow("reverse")]
    public void BothDirectionsUseTheSameExpansionBudget(string direction)
    {
        var candidates = Experiment.Run().Trials.SelectMany(t => t.Candidates).Where(c => c.Direction == direction).ToArray();
        Assert.IsTrue(candidates.Length > 0 && candidates.All(c => c is
            { Leaves: 9, Edges: 15, Parameters: 44, Updates: 64, TrainingExamples: 1024, TrainingPortVisits: 26624 }));
    }

    [TestMethod]
    public void CandidatesChoosingTheSamePointHaveIdenticalOutcomes()
    {
        var collisions = Experiment.Run().Trials.Where(t => t.SamePoint).ToArray();
        Assert.IsTrue(collisions.Length > 0 && collisions.All(t =>
            (t.Candidates[1] with { Strategy = "signal" }) == t.Candidates[2]));
    }

    [TestMethod]
    public void ContinuationMatchesTheUpdateBudgetWithoutPayingExpansionCost()
    {
        var trials = Experiment.Run().Trials;
        Assert.IsTrue(trials.All(t => t.Candidates[0] is
            { Point: null, Leaves: 5, Edges: 9, Parameters: 24, Updates: 64, TrainingExamples: 1024, TrainingPortVisits: 14336 }));
    }

    [TestMethod]
    public void BothExpansionsPreservePredictionsBeforeTraining()
    {
        double drift = Experiment.Run().Trials.SelectMany(t => t.Candidates).Max(c => c.InitialPredictionDrift);
        Assert.IsTrue(drift <= 1e-12);
    }

    [TestMethod]
    public void RetentionIsTheStableMaximumOfValidationScores()
    {
        var study = Experiment.Run();
        Assert.IsTrue(study.Trials.All(t => t.RetainedStrategy == t.Candidates.OrderByDescending(c => c.RetentionScore).First().Strategy));
    }

    [DataTestMethod]
    [DataRow(1.0, 0.98, 25, false)]
    [DataRow(1.0, 0.96, 25, true)]
    [DataRow(0.0, 0.0, 25, false)]
    public void RetentionRequiresMoreThanMarginAndCapacityPenalty(double baseline, double expanded, int extra, bool expected)
    {
        Assert.AreEqual(expected, Experiment.Score(baseline, expanded, extra) > 0);
    }

    [TestMethod]
    public void FixedStudyReproducesAllPairedRecords()
    {
        var first = Experiment.Run();
        var second = Experiment.Run();
        Assert.AreEqual(System.Text.Json.JsonSerializer.Serialize(first), System.Text.Json.JsonSerializer.Serialize(second));
    }

    [TestMethod]
    public void TestMetricsAreFiniteForAllConfiguredPairs()
    {
        var trials = Experiment.Run().Trials;
        Assert.AreEqual(144, trials.SelectMany(t => t.Candidates).Count(c => double.IsFinite(c.TestMse)));
    }
}
