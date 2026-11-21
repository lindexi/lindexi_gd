using MathGraph.ReversibleFractal.Wpf.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraph.ReversibleFractal.Wpf.Tests;

[TestClass]
public class ReversibleGraphTests
{
    private static string[] Snapshot(ReversibleGraph graph) => graph.Vertices.Select(v => $"V:{v}")
        .Concat(graph.Edges.Select(e => $"E:{e}"))
        .Concat(graph.ExpandedPaths.Select(p => "M:" + p)).Order(StringComparer.Ordinal).ToArray();
    private static string[] Connections(ReversibleGraph graph) => graph.Edges
        .Select(e => $"{e.Source.VertexId}:{e.Source.Index}>{e.Target.VertexId}:{e.Target.Index}").Order(StringComparer.Ordinal).ToArray();
    private static ReversibleGraph Expanded(params string[] paths)
    {
        var graph = new ReversibleGraph();
        foreach (string path in paths) graph.Expand(path);
        return graph;
    }
    private static ReversibleGraph Generated(int depth)
    {
        var graph = new ReversibleGraph(); graph.Generate(depth); return graph;
    }

    [TestMethod]
    public void BaseEdgesAreExact() => CollectionAssert.AreEqual(new[]
    {
        "S0:0>root/A:0", "S1:0>root/B:0", "root/A:0>root/C:0", "root/A:1>root/D:0",
        "root/B:0>root/C:1", "root/B:1>root/D:1", "root/C:0>root/E:0", "root/D:0>root/E:1", "root/E:0>T:0"
    }.Order(StringComparer.Ordinal).ToArray(), Connections(new()));

    [DataTestMethod]
    [DataRow("A", 1, 2)] [DataRow("B", 1, 2)] [DataRow("C", 2, 1)] [DataRow("D", 2, 1)] [DataRow("E", 2, 1)]
    public void BaseDegreesAreActual(string name, int incoming, int outgoing) =>
        Assert.AreEqual(new Degree(incoming, outgoing), new ReversibleGraph().GetDegree("root/" + name));

    [TestMethod]
    public void ForwardCReplacementHasExactInternalAndExternalPorts() => CollectionAssert.AreEqual(new[]
    {
        "S0:0>root/A:0", "S1:0>root/B:0", "root/A:0>root/C/A:0", "root/A:1>root/D:0",
        "root/B:0>root/C/B:0", "root/B:1>root/D:1", "root/C/A:0>root/C/C:0", "root/C/A:1>root/C/D:0",
        "root/C/B:0>root/C/C:1", "root/C/B:1>root/C/D:1", "root/C/C:0>root/C/E:0", "root/C/D:0>root/C/E:1",
        "root/C/E:0>root/E:0", "root/D:0>root/E:1", "root/E:0>T:0"
    }.Order(StringComparer.Ordinal).ToArray(), Connections(Expanded("root/C")));

    [TestMethod]
    public void ReverseAReplacementHasExactInternalAndExternalPorts() => CollectionAssert.AreEqual(new[]
    {
        "S0:0>root/A/E:0", "S1:0>root/B:0", "root/A/A:0>root/C:0", "root/A/B:0>root/D:0",
        "root/A/C:0>root/A/A:0", "root/A/D:0>root/A/A:1", "root/A/C:1>root/A/B:0", "root/A/D:1>root/A/B:1",
        "root/A/E:0>root/A/C:0", "root/A/E:1>root/A/D:0", "root/B:0>root/C:1", "root/B:1>root/D:1",
        "root/C:0>root/E:0", "root/D:0>root/E:1", "root/E:0>T:0"
    }.Order(StringComparer.Ordinal).ToArray(), Connections(Expanded("root/A")));

    [TestMethod]
    public void ReverseBPreservesIndependentSourceAndOutputOrder() => CollectionAssert.AreEquivalent(new[]
    {
        new Edge(new("S1", 0), new("root/B/E", 0)),
        new Edge(new("root/B/A", 0), new("root/C", 1)), new Edge(new("root/B/B", 0), new("root/D", 1))
    }, Expanded("root/B").Edges.Where(e => e.Source.VertexId == "S1" ||
        (e.Source.VertexId.StartsWith("root/B/", StringComparison.Ordinal) && !e.Target.VertexId.StartsWith("root/B/", StringComparison.Ordinal))).ToArray());

    [TestMethod]
    public void ForwardEPreservesInputOrderAndSink() => CollectionAssert.AreEquivalent(new[]
    {
        new Edge(new("root/C", 0), new("root/E/A", 0)), new Edge(new("root/D", 0), new("root/E/B", 0)),
        new Edge(new("root/E/E", 0), new("T", 0))
    }, Expanded("root/E").Edges.Where(e => e.Source.VertexId == "root/C" || e.Source.VertexId == "root/D" || e.Target.VertexId == "T").ToArray());

    [DataTestMethod]
    [DataRow("A")] [DataRow("B")] [DataRow("C")] [DataRow("D")] [DataRow("E")]
    public void EveryReplacementContainsOnlyFiveNamedRealChildren(string name) => CollectionAssert.AreEquivalent(
        new[] { "A", "B", "C", "D", "E" }.Select(n => $"root/{name}/{n}").ToArray(),
        Expanded("root/" + name).Vertices.Where(v => v.Id.StartsWith($"root/{name}/", StringComparison.Ordinal)).Select(v => v.Id).ToArray());

    [DataTestMethod]
    [DataRow("A")] [DataRow("B")] [DataRow("C")] [DataRow("D")] [DataRow("E")]
    public void EveryReplacementGrowsByFourVerticesAndSixEdges(string name)
    {
        var graph = Expanded("root/" + name);
        CollectionAssert.AreEqual(new[] { 9, 15 }, new[] { graph.RealVertexCount, graph.Edges.Count });
    }

    [TestMethod]
    public void MixedDoubleEndedReconnectionRetainsOriginalEdges() => CollectionAssert.AreEqual(new[]
    {
        new Edge(new("root/A/A/E", 0), new("root/C/A/E", 0)),
        new Edge(new("root/A/B", 0), new("root/D/A", 0))
    }, Expanded("root/A", "root/C", "root/A/A", "root/C/A", "root/D").Edges
        .Where(e => e.Source.VertexId == "root/A/A/E" || e.Source.VertexId == "root/A/B").ToArray());

    [TestMethod]
    public void DifferentValidExpansionOrdersProduceIdenticalGraphs() => CollectionAssert.AreEqual(
        Snapshot(Expanded("root/A", "root/C", "root/A/A", "root/C/A", "root/D")),
        Snapshot(Expanded("root/D", "root/C", "root/C/A", "root/A", "root/A/A")));

    [TestMethod]
    public void ForwardBoundaryRecursivelyPreservesInputOrder() => CollectionAssert.AreEqual(
        new[] { new Port("root/C/A/E", 0), new Port("root/C/B/E", 0) },
        Expanded("root/C", "root/C/B", "root/C/A").GetBoundary("root/C").Inputs.ToArray());

    [TestMethod]
    public void ReverseBoundaryRecursivelyPreservesOutputOrder() => CollectionAssert.AreEqual(
        new[] { new Port("root/A/A/E", 0), new Port("root/A/B/E", 0) },
        Expanded("root/A", "root/A/B", "root/A/A").GetBoundary("root/A").Outputs.ToArray());

    [DataTestMethod]
    [DataRow("root", Direction.Forward)] [DataRow("root/A", Direction.Reverse)]
    [DataRow("root/A/A", Direction.Forward)] [DataRow("root/A/C", Direction.Reverse)]
    [DataRow("root/A/E", Direction.Reverse)] [DataRow("root/C/A", Direction.Reverse)]
    [DataRow("root/C/E", Direction.Forward)]
    public void OrientationFollowsActualSignature(string path, Direction expected) =>
        Assert.AreEqual(expected, Expanded("root/A", "root/C").GetDirection(path));

    [TestMethod]
    public void AllRealLeavesRemainInTheTwoClosedDegreeTypes() => Assert.IsTrue(Generated(3).Vertices
        .Where(v => !v.IsBoundary).All(v => v.Id.Split('/').Skip(1).All(n => "ABCDE".Contains(n, StringComparison.Ordinal))));

    [TestMethod]
    public void AllActualDegreesMatchClosedSignatures()
    {
        var graph = Generated(2);
        Assert.IsTrue(graph.Vertices.Where(v => !v.IsBoundary).All(v =>
            graph.GetDegree(v.Id) == graph.GetSignature(v.Id) && graph.GetDegree(v.Id) is { Incoming: 1, Outgoing: 2 } or { Incoming: 2, Outgoing: 1 }));
    }

    [DataTestMethod]
    [DataRow(0, 5, 9, 0)] [DataRow(1, 25, 39, 5)] [DataRow(2, 125, 189, 30)] [DataRow(3, 625, 939, 155)]
    public void WholeRoundsHaveExactCounts(int depth, int real, int edges, int expansions)
    {
        var graph = Generated(depth);
        CollectionAssert.AreEqual(new[] { real, edges, expansions, depth }, new[] { graph.RealVertexCount, graph.Edges.Count, graph.ExpandedPaths.Count, graph.Depth });
    }

    [TestMethod]
    public void EveryPortIsUniqueAndContiguous()
    {
        var graph = Generated(2);
        var actual = graph.Vertices.Where(v => !v.IsBoundary).Select(v =>
            string.Join(',', graph.Edges.Where(e => e.Target.VertexId == v.Id).Select(e => e.Target.Index).Order()) + ";" +
            string.Join(',', graph.Edges.Where(e => e.Source.VertexId == v.Id).Select(e => e.Source.Index).Order())).ToArray();
        Assert.IsTrue(actual.All(p => p is "0;0,1" or "0,1;0"));
    }

    [TestMethod]
    public void EveryEdgeEndpointExists()
    {
        var graph = Generated(2); var ids = graph.Vertices.Select(v => v.Id).ToHashSet();
        Assert.IsTrue(graph.Edges.All(e => ids.Contains(e.Source.VertexId) && ids.Contains(e.Target.VertexId)));
    }
    private static HashSet<string> Reach(ReversibleGraph graph, IEnumerable<string> starts, bool reverse)
    {
        var reached = starts.ToHashSet(); var queue = new Queue<string>(reached);
        while (queue.TryDequeue(out var current))
            foreach (var edge in graph.Edges.Where(e => (reverse ? e.Target.VertexId : e.Source.VertexId) == current))
            {
                string next = reverse ? edge.Source.VertexId : edge.Target.VertexId;
                if (reached.Add(next)) queue.Enqueue(next);
            }
        return reached;
    }
    [TestMethod]
    public void EveryVertexIsReachableFromSources()
    {
        var graph = Generated(2); Assert.AreEqual(graph.Vertices.Count, Reach(graph, ["S0", "S1"], false).Count);
    }
    [TestMethod]
    public void EveryVertexCanReachSink()
    {
        var graph = Generated(2); Assert.AreEqual(graph.Vertices.Count, Reach(graph, ["T"], true).Count);
    }
    [TestMethod]
    public void GraphIsAcyclic()
    {
        var graph = Generated(2); var degrees = graph.Vertices.ToDictionary(v => v.Id, v => graph.GetDegree(v.Id).Incoming);
        var queue = new Queue<string>(degrees.Where(d => d.Value == 0).Select(d => d.Key)); int visited = 0;
        while (queue.TryDequeue(out var current))
        {
            visited++;
            foreach (var edge in graph.Edges.Where(e => e.Source.VertexId == current))
                if (--degrees[edge.Target.VertexId] == 0) queue.Enqueue(edge.Target.VertexId);
        }
        Assert.AreEqual(graph.Vertices.Count, visited);
    }
    [TestMethod]
    public void NestedCollapseRestoresExactGraph()
    {
        var graph = Expanded("root/B"); var before = Snapshot(graph);
        graph.Expand("root/A"); graph.Expand("root/A/A"); graph.Expand("root/A/A/C"); graph.Collapse("root/A");
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }
    [TestMethod]
    public void CollapseAllRestoresBase()
    {
        var graph = Generated(2); graph.CollapseAll(); CollectionAssert.AreEqual(Snapshot(new()), Snapshot(graph));
    }
    [TestMethod]
    public void GenerateZeroRestoresBase()
    {
        var graph = Generated(2); graph.Generate(0, 0); CollectionAssert.AreEqual(Snapshot(new()), Snapshot(graph));
    }
    [TestMethod]
    public void ManualChainExceedsOldDepthLimit()
    {
        var graph = new ReversibleGraph(); string path = "root/A";
        for (int i = 0; i < 40; i++) { graph.Expand(path); path += "/A"; }
        Assert.AreEqual(40, graph.Depth);
    }
    [TestMethod]
    public void ExplicitBudgetCanExceedDefault()
    {
        var graph = new ReversibleGraph(); graph.Generate(5, 3905); Assert.AreEqual(3905, graph.ExpandedPaths.Count);
    }
    [TestMethod]
    public void ExactBudgetIsAccepted()
    {
        var graph = new ReversibleGraph(); graph.Generate(2, 30); Assert.AreEqual(125, graph.RealVertexCount);
    }
    [DataTestMethod]
    [DataRow(1, 4)] [DataRow(2, 29)] [DataRow(5, 1000)] [DataRow(int.MaxValue, 5)]
    public void BudgetRejectionIsAtomic(int depth, int budget)
    {
        var graph = Expanded("root/C"); var before = Snapshot(graph);
        Assert.ThrowsException<InvalidOperationException>(() => graph.Generate(depth, budget));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }
    [TestMethod]
    public void DefaultBudgetRejectionIsAtomic()
    {
        var graph = Expanded("root/A"); var before = Snapshot(graph);
        Assert.ThrowsException<InvalidOperationException>(() => graph.Generate(5));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }
    [DataTestMethod]
    [DataRow(-1, 1000)] [DataRow(1, -1)]
    public void InvalidBatchArgumentsAreAtomic(int depth, int budget)
    {
        var graph = Expanded("root/A"); var before = Snapshot(graph);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => graph.Generate(depth, budget));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }
    [DataTestMethod]
    [DataRow("S0")] [DataRow("root/Z")] [DataRow("root/A/C")] [DataRow("")]
    public void InvalidExpansionIsAtomic(string path)
    {
        var graph = new ReversibleGraph(); var before = Snapshot(graph);
        Assert.ThrowsException<ArgumentException>(() => graph.Expand(path));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }
    [TestMethod]
    public void InvalidCollapseIsAtomic()
    {
        var graph = Expanded("root/A"); var before = Snapshot(graph);
        Assert.ThrowsException<ArgumentException>(() => graph.Collapse("root/B"));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }
    [TestMethod]
    public void ContainersAreNeverActualVertices()
    {
        var graph = Generated(2);
        Assert.IsFalse(graph.Vertices.Any(v => graph.ExpandedPaths.Contains(v.Id)));
    }
    [TestMethod]
    public void AllModuleBoundariesResolveToActualLeafPorts()
    {
        var graph = Generated(2);
        var leaves = graph.Vertices.Where(v => !v.IsBoundary).Select(v => v.Id).ToHashSet();
        Assert.IsTrue(graph.ExpandedPaths.Append("root").Select(graph.GetBoundary)
            .All(b => b.Inputs.Concat(b.Outputs).All(p => leaves.Contains(p.VertexId))));
    }
    [TestMethod]
    public void BoundaryListsPreserveEveryModuleSignature()
    {
        var graph = Generated(2);
        Assert.IsTrue(graph.ExpandedPaths.Append("root").All(path =>
            new Degree(graph.GetBoundary(path).Inputs.Count, graph.GetBoundary(path).Outputs.Count) == graph.GetSignature(path)));
    }
    [TestMethod]
    public void CollapsingOneEndpointReconnectsToStillExpandedNeighbor()
    {
        var graph = Expanded("root/A", "root/C", "root/A/A", "root/C/A");
        graph.Collapse("root/A");
        CollectionAssert.AreEqual(Snapshot(Expanded("root/C", "root/C/A")), Snapshot(graph));
    }
    [TestMethod]
    public void ManualExpansionIgnoresBatchBudget()
    {
        var graph = new ReversibleGraph();
        graph.Generate(0, 0); graph.Expand("root/A");
        Assert.AreEqual(9, graph.RealVertexCount);
    }
    [TestMethod]
    public void NullExpansionIsRejected() => Assert.ThrowsException<ArgumentNullException>(() => new ReversibleGraph().Expand(null!));
}
