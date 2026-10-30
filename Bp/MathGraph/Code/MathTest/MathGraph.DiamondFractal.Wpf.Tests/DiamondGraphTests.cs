using MathGraph.DiamondFractal.Wpf.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraph.DiamondFractal.Wpf.Tests;

[TestClass]
public class DiamondGraphTests
{
    [TestMethod]
    public void InitialGraphHasFourRealVertices() => Assert.AreEqual(4, new DiamondGraph().RealVertexCount);

    [TestMethod]
    public void InitialGraphHasSixEdges() => Assert.AreEqual(6, new DiamondGraph().Edges.Count);

    [TestMethod]
    public void BoundaryVerticesAreExcludedFromRealCount()
    {
        var graph = DiamondGraph.CreateDefault();
        Assert.AreEqual(2, graph.Vertices.Count - graph.RealVertexCount);
    }

    [TestMethod]
    public void DefaultGraphHasSevenRealVertices() => Assert.AreEqual(7, DiamondGraph.CreateDefault().RealVertexCount);

    [TestMethod]
    public void DefaultGraphHasTenEdges() => Assert.AreEqual(10, DiamondGraph.CreateDefault().Edges.Count);

    [TestMethod]
    public void DefaultOnlyExpandsUpperBranch() => CollectionAssert.AreEqual(new[] { "root/U" }, DiamondGraph.CreateDefault().ExpandedPaths.ToArray());

    [TestMethod]
    public void ReplacementRemovesOldVertex() => Assert.IsFalse(DiamondGraph.CreateDefault().Vertices.Any(v => v.Id == "root/U"));

    [TestMethod]
    public void ReplacementLeavesNoDanglingEdges()
    {
        var graph = DiamondGraph.CreateDefault();
        var ids = graph.Vertices.Select(v => v.Id).ToHashSet();
        Assert.IsTrue(graph.Edges.All(e => ids.Contains(e.From) && ids.Contains(e.To)));
    }

    [TestMethod]
    public void IncomingEdgeConnectsChildEntrance() => Assert.IsTrue(DiamondGraph.CreateDefault().Edges.Contains(new Edge("root/L", "root/U/L")));

    [TestMethod]
    public void OutgoingEdgeConnectsChildExit() => Assert.IsTrue(DiamondGraph.CreateDefault().Edges.Contains(new Edge("root/U/R", "root/R")));

    [TestMethod]
    public void ModuleHasOneIncomingAndOneOutgoing() => Assert.AreEqual(new Degree(1, 1), DiamondGraph.CreateDefault().GetModuleDegree("root/U"));

    [DataTestMethod]
    [DataRow("root/L", 1, 2)]
    [DataRow("root/R", 2, 1)]
    [DataRow("root/D", 1, 1)]
    [DataRow("root/U/L", 1, 2)]
    [DataRow("root/U/R", 2, 1)]
    [DataRow("S", 0, 1)]
    [DataRow("T", 1, 0)]
    public void TrueVertexDegreesArePreserved(string id, int incoming, int outgoing) =>
        Assert.AreEqual(new Degree(incoming, outgoing), DiamondGraph.CreateDefault().GetDegree(id));

    [DataTestMethod]
    [DataRow(0, false, 0)]
    [DataRow(1, false, 1)]
    [DataRow(6, false, 6)]
    [DataRow(0, true, 0)]
    [DataRow(1, true, 2)]
    [DataRow(3, true, 14)]
    [DataRow(6, true, 126)]
    public void GenerationHasExpectedVertexCount(int depth, bool both, int count)
    {
        var graph = new DiamondGraph();
        graph.Generate(depth, both);
        Assert.AreEqual(4 + 3 * count, graph.RealVertexCount);
    }

    [DataTestMethod]
    [DataRow(6, false, 6)]
    [DataRow(6, true, 126)]
    public void GenerationHasExpectedEdgeCount(int depth, bool both, int count)
    {
        var graph = new DiamondGraph();
        graph.Generate(depth, both);
        Assert.AreEqual(6 + 4 * count, graph.Edges.Count);
    }

    [TestMethod]
    public void EveryVertexIsReachableFromSource()
    {
        var graph = new DiamondGraph();
        graph.Generate(6, true);
        Assert.AreEqual(graph.Vertices.Count, Reach(graph, "S", false).Count);
    }

    [TestMethod]
    public void EveryVertexCanReachSink()
    {
        var graph = new DiamondGraph();
        graph.Generate(6, true);
        Assert.AreEqual(graph.Vertices.Count, Reach(graph, "T", true).Count);
    }

    [TestMethod]
    public void GeneratedGraphIsAcyclic()
    {
        var graph = new DiamondGraph();
        graph.Generate(6, true);
        var incoming = graph.Vertices.ToDictionary(v => v.Id, v => graph.GetDegree(v.Id).Incoming);
        var queue = new Queue<string>(incoming.Where(p => p.Value == 0).Select(p => p.Key));
        int visited = 0;
        while (queue.TryDequeue(out var id))
        {
            visited++;
            foreach (var edge in graph.Edges.Where(e => e.From == id))
                if (--incoming[edge.To] == 0) queue.Enqueue(edge.To);
        }
        Assert.AreEqual(graph.Vertices.Count, visited);
    }

    [TestMethod]
    public void CollapsingSubtreeRestoresOriginalEdges()
    {
        var graph = new DiamondGraph();
        graph.Generate(4, true);
        graph.Collapse("root/U");
        Assert.IsTrue(graph.Edges.Contains(new("root/L", "root/U")) && graph.Edges.Contains(new("root/U", "root/R")));
    }

    [TestMethod]
    public void CollapsingSubtreeRemovesAllDescendants()
    {
        var graph = new DiamondGraph();
        graph.Generate(4, true);
        graph.Collapse("root/U");
        Assert.IsFalse(graph.Vertices.Any(v => v.Id.StartsWith("root/U/", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CollapseAllRestoresInitialTopology()
    {
        var graph = DiamondGraph.CreateDefault();
        graph.CollapseAll();
        CollectionAssert.AreEquivalent(new DiamondGraph().Edges.ToArray(), graph.Edges.ToArray());
    }

    [TestMethod]
    public void ArbitraryLowerLeafCanExpand()
    {
        var graph = DiamondGraph.CreateDefault();
        graph.Expand("root/U/D");
        Assert.IsTrue(graph.Vertices.Any(v => v.Id == "root/U/D/L"));
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("root/L/U")]
    [DataRow("root/U/X")]
    [DataRow("root/D/U")]
    [DataRow("root/U")]
    public void InvalidOrNonLeafPathsAreRejected(string path) =>
        Assert.ThrowsException<ArgumentException>(() => DiamondGraph.CreateDefault().Expand(path));

    [TestMethod]
    public void NullPathIsRejected() => Assert.ThrowsException<ArgumentNullException>(() => new DiamondGraph().Expand(null!));

    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    public void InvalidDepthIsRejected(int depth) => Assert.ThrowsException<ArgumentOutOfRangeException>(() => new DiamondGraph().Generate(depth, true));

    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(127)]
    public void InvalidBudgetIsRejected(int budget) => Assert.ThrowsException<ArgumentOutOfRangeException>(() => new DiamondGraph().Generate(1, true, budget));

    [TestMethod]
    public void InsufficientBudgetDoesNotMutateGraph()
    {
        var graph = DiamondGraph.CreateDefault();
        Assert.ThrowsException<InvalidOperationException>(() => graph.Generate(6, true, 125));
        CollectionAssert.AreEqual(new[] { "root/U" }, graph.ExpandedPaths.ToArray());
    }

    [TestMethod]
    public void InsufficientBudgetIsRejected() => Assert.ThrowsException<InvalidOperationException>(() => new DiamondGraph().Generate(6, true, 125));

    [TestMethod]
    public void ManualExpansionCannotExceedDepth()
    {
        var graph = new DiamondGraph();
        graph.Generate(6, false);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => graph.Expand("root/U/U/U/U/U/U/U"));
    }

    [TestMethod]
    public void UnknownModuleCannotCollapse() => Assert.ThrowsException<ArgumentException>(() => new DiamondGraph().Collapse("root/U"));

    [TestMethod]
    public void UnknownVertexDegreeIsRejected() => Assert.ThrowsException<ArgumentException>(() => new DiamondGraph().GetDegree("missing"));

    [TestMethod]
    public void UnknownModuleDegreeIsRejected() => Assert.ThrowsException<ArgumentException>(() => new DiamondGraph().GetModuleDegree("root/U"));

    [DataTestMethod]
    [DataRow("root/L", NodeKind.Split, 1, 2)]
    [DataRow("root/R", NodeKind.Merge, 2, 1)]
    public void SplitAndMergePreserveModuleDegree(string path, NodeKind kind, int incoming, int outgoing)
    {
        var graph = new DiamondGraph();
        graph.Expand(path);
        Assert.AreEqual(new Degree(incoming, outgoing), graph.GetModuleDegree(path));
    }

    [DataTestMethod]
    [DataRow("root/L", NodeKind.Split, 1)]
    [DataRow("root/L", NodeKind.Merge, 0)]
    [DataRow("root/L", NodeKind.Transfer, 2)]
    [DataRow("root/R", NodeKind.Split, 0)]
    [DataRow("root/R", NodeKind.Merge, 1)]
    [DataRow("root/R", NodeKind.Transfer, 2)]
    public void SplitAndMergeHaveTypedChildren(string path, NodeKind kind, int count)
    {
        var graph = new DiamondGraph();
        graph.Expand(path);
        Assert.AreEqual(count, graph.Vertices.Count(v => v.Id.StartsWith(path + "/", StringComparison.Ordinal) && v.Kind == kind));
    }

    [DataTestMethod]
    [DataRow("root/L")]
    [DataRow("root/R")]
    public void SplitAndMergeRemoveOriginalVertex(string path)
    {
        var graph = new DiamondGraph();
        graph.Expand(path);
        Assert.IsFalse(graph.Vertices.Any(v => v.Id == path));
    }

    [DataTestMethod]
    [DataRow("root/L", 6, 8)]
    [DataRow("root/R", 6, 8)]
    public void SplitAndMergeHaveExpectedCounts(string path, int vertices, int edges)
    {
        var graph = new DiamondGraph();
        graph.Expand(path);
        Assert.AreEqual((vertices, edges), (graph.RealVertexCount, graph.Edges.Count));
    }

    [DataTestMethod]
    [DataRow("root/L", "S", "root/L/A")]
    [DataRow("root/L", "root/L/A", "root/L/B")]
    [DataRow("root/L", "root/L/A", "root/L/C")]
    [DataRow("root/L", "root/L/B", "root/U")]
    [DataRow("root/L", "root/L/C", "root/D")]
    [DataRow("root/R", "root/U", "root/R/B")]
    [DataRow("root/R", "root/D", "root/R/C")]
    [DataRow("root/R", "root/R/B", "root/R/A")]
    [DataRow("root/R", "root/R/C", "root/R/A")]
    [DataRow("root/R", "root/R/A", "T")]
    public void SplitAndMergeConnectExpectedPorts(string path, string from, string to)
    {
        var graph = new DiamondGraph();
        graph.Expand(path);
        Assert.IsTrue(graph.Edges.Contains(new Edge(from, to)));
    }

    private static DiamondGraph MixedGraph()
    {
        var graph = new DiamondGraph();
        foreach (var path in new[] { "root/L", "root/R", "root/L/A", "root/R/A", "root/L/B", "root/L/C", "root/R/B", "root/R/C", "root/U", "root/D", "root/U/R", "root/D/L" })
            graph.Expand(path);
        return graph;
    }

    [DataTestMethod]
    [DataRow("root/L/A/B", "root/L/B/L")]
    [DataRow("root/L/A/C", "root/L/C/L")]
    [DataRow("root/R/B/R", "root/R/A/B")]
    [DataRow("root/R/C/R", "root/R/A/C")]
    [DataRow("root/L/B/R", "root/U/L")]
    [DataRow("root/L/C/R", "root/D/L/A")]
    [DataRow("root/U/R/A", "root/R/B/L")]
    [DataRow("root/D/R", "root/R/C/L")]
    public void NestedSameAndDifferentTypesPreservePortOrder(string from, string to) =>
        Assert.IsTrue(MixedGraph().Edges.Contains(new Edge(from, to)));

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MixedGraphHasNoDanglingEdges(bool collapse)
    {
        var graph = MixedGraph();
        if (collapse) graph.Collapse("root/L");
        var ids = graph.Vertices.Select(v => v.Id).ToHashSet();
        Assert.IsTrue(graph.Edges.All(e => ids.Contains(e.From) && ids.Contains(e.To)));
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MixedGraphPreservesEveryTypedDegree(bool collapse)
    {
        var graph = MixedGraph();
        if (collapse) graph.Collapse("root/R");
        var expected = new Dictionary<NodeKind, Degree>
        {
            [NodeKind.Transfer] = new(1, 1), [NodeKind.Split] = new(1, 2), [NodeKind.Merge] = new(2, 1)
        };
        Assert.IsTrue(graph.Vertices.Where(v => !v.IsBoundary).All(v => graph.GetDegree(v.Id) == expected[v.Kind]));
    }

    [TestMethod]
    public void EveryMixedModulePreservesTypedBoundaryDegree()
    {
        var graph = MixedGraph();
        var expected = new Dictionary<NodeKind, Degree>
        {
            [NodeKind.Transfer] = new(1, 1), [NodeKind.Split] = new(1, 2), [NodeKind.Merge] = new(2, 1)
        };
        Assert.IsTrue(graph.ExpandedPaths.All(p => graph.GetModuleDegree(p) == expected[graph.GetKind(p)]));
    }

    [DataTestMethod]
    [DataRow("S", false, false)]
    [DataRow("T", true, false)]
    [DataRow("S", false, true)]
    [DataRow("T", true, true)]
    public void MixedGraphRemainsSourceSinkReachable(string start, bool reverse, bool collapse)
    {
        var graph = MixedGraph();
        if (collapse) graph.Collapse("root/L");
        Assert.AreEqual(graph.Vertices.Count, Reach(graph, start, reverse).Count);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MixedGraphRemainsAcyclic(bool collapse)
    {
        var graph = MixedGraph();
        if (collapse) graph.Collapse("root/R");
        var incoming = graph.Vertices.ToDictionary(v => v.Id, v => graph.GetDegree(v.Id).Incoming);
        var queue = new Queue<string>(incoming.Where(p => p.Value == 0).Select(p => p.Key));
        int count = 0;
        while (queue.TryDequeue(out var id))
        {
            count++;
            foreach (var edge in graph.Edges.Where(e => e.From == id))
                if (--incoming[edge.To] == 0) queue.Enqueue(edge.To);
        }
        Assert.AreEqual(graph.Vertices.Count, count);
    }

    [TestMethod]
    public void CollapsingLRAndDescendantsRestoresInitialEdges()
    {
        var graph = MixedGraph();
        graph.Collapse("root/L");
        graph.Collapse("root/R");
        graph.Collapse("root/U");
        graph.Collapse("root/D");
        CollectionAssert.AreEquivalent(new DiamondGraph().Edges.ToArray(), graph.Edges.ToArray());
    }

    [DataTestMethod]
    [DataRow("S")]
    [DataRow("T")]
    [DataRow("root/A")]
    [DataRow("root/L/U")]
    [DataRow("root/R/L")]
    [DataRow("root/U/A")]
    public void BoundaryAndInvalidTypedPathsCannotExpand(string path) =>
        Assert.ThrowsException<ArgumentException>(() => new DiamondGraph().Expand(path));

    [TestMethod]
    public void EveryRealLeafCanExpand() => Assert.IsTrue(new DiamondGraph().Vertices.Where(v => !v.IsBoundary).All(v => v.CanExpand));

    [TestMethod]
    public void ExternalVerticesCannotExpand() => Assert.IsTrue(new DiamondGraph().Vertices.Where(v => v.IsBoundary).All(v => !v.CanExpand));

    [DataTestMethod]
    [DataRow(0, 0, 4, 6)]
    [DataRow(1, 4, 14, 18)]
    [DataRow(2, 18, 50, 62)]
    [DataRow(3, 68, 178, 218)]
    public void AllTypesGenerationHasExactCounts(int depth, int expansions, int vertices, int edges)
    {
        var graph = new DiamondGraph();
        graph.Generate(depth, GenerationMode.AllTypes);
        Assert.AreEqual((expansions, vertices, edges), (graph.ExpandedPaths.Count, graph.RealVertexCount, graph.Edges.Count));
    }

    [DataTestMethod]
    [DataRow(4, 126)]
    [DataRow(6, 126)]
    [DataRow(3, 67)]
    [DataRow(1, 3)]
    public void AllTypesBudgetFailurePreservesFullGraph(int depth, int budget)
    {
        var graph = MixedGraph();
        var before = (string.Join(";", graph.Vertices), string.Join(";", graph.Edges), string.Join(";", graph.ExpandedPaths));
        Assert.ThrowsException<InvalidOperationException>(() => graph.Generate(depth, GenerationMode.AllTypes, budget));
        Assert.AreEqual(before, (string.Join(";", graph.Vertices), string.Join(";", graph.Edges), string.Join(";", graph.ExpandedPaths)));
    }

    [TestMethod]
    public void ManualBudgetFailurePreservesFullGraph()
    {
        var graph = new DiamondGraph();
        graph.Generate(6, true);
        var before = (string.Join(";", graph.Vertices), string.Join(";", graph.Edges), string.Join(";", graph.ExpandedPaths));
        Assert.ThrowsException<InvalidOperationException>(() => graph.Expand("root/L"));
        Assert.AreEqual(before, (string.Join(";", graph.Vertices), string.Join(";", graph.Edges), string.Join(";", graph.ExpandedPaths)));
    }

    [TestMethod]
    public void InvalidGenerationModeIsRejected() => Assert.ThrowsException<ArgumentOutOfRangeException>(() => new DiamondGraph().Generate(1, (GenerationMode)99));

    [TestMethod]
    public void AllTypesExactBudgetIsAccepted()
    {
        var graph = new DiamondGraph();
        graph.Generate(3, GenerationMode.AllTypes, 68);
        Assert.AreEqual(68, graph.ExpandedPaths.Count);
    }

    [DataTestMethod]
    [DataRow("root/L", "root/L/A/A/B", "root/L/A/B")]
    [DataRow("root/L", "root/L/A/A/C", "root/L/A/C")]
    [DataRow("root/R", "root/R/A/B", "root/R/A/A/B")]
    [DataRow("root/R", "root/R/A/C", "root/R/A/A/C")]
    public void RepeatedSameTypeARecursionPreservesBranches(string path, string from, string to)
    {
        var graph = new DiamondGraph();
        graph.Expand(path);
        graph.Expand(path + "/A");
        graph.Expand(path + "/A/A");
        Assert.IsTrue(graph.Edges.Contains(new Edge(from, to)));
    }

    [TestMethod]
    public void AllTypesGeneratedVerticesHaveCorrectDegree()
    {
        var graph = new DiamondGraph();
        graph.Generate(3, GenerationMode.AllTypes);
        var expected = new Dictionary<NodeKind, Degree>
        {
            [NodeKind.Transfer] = new(1, 1), [NodeKind.Split] = new(1, 2), [NodeKind.Merge] = new(2, 1)
        };
        Assert.IsTrue(graph.Vertices.Where(v => !v.IsBoundary).All(v => graph.GetDegree(v.Id) == expected[v.Kind]));
    }

    [TestMethod]
    public void MixedTypesRespectActualVertexCountAtExpansionLimit()
    {
        var graph = new DiamondGraph();
        graph.Generate(6, true);
        graph.Collapse("root/U/U/U/U/U/U");
        graph.Expand("root/L");
        Assert.AreEqual(381, graph.RealVertexCount);
    }

    [TestMethod]
    public void ExpansionLimitIsEnforcedEvenBelowVertexLimit()
    {
        var graph = new DiamondGraph();
        graph.Generate(6, true);
        graph.Collapse("root/U/U/U/U/U/U");
        graph.Expand("root/L");
        var before = (string.Join(";", graph.Vertices), string.Join(";", graph.Edges), string.Join(";", graph.ExpandedPaths));
        Assert.ThrowsException<InvalidOperationException>(() => graph.Expand("root/R"));
        Assert.AreEqual(before, (string.Join(";", graph.Vertices), string.Join(";", graph.Edges), string.Join(";", graph.ExpandedPaths)));
    }

    private static HashSet<string> Reach(DiamondGraph graph, string start, bool reverse)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(start);
        while (queue.TryDequeue(out var id))
        {
            if (!visited.Add(id)) continue;
            foreach (var edge in graph.Edges.Where(e => reverse ? e.To == id : e.From == id))
                queue.Enqueue(reverse ? edge.From : edge.To);
        }
        return visited;
    }
}
