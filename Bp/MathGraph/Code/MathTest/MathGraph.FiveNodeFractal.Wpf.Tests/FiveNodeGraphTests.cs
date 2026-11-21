using MathGraph.FiveNodeFractal.Wpf.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraph.FiveNodeFractal.Wpf.Tests;

[TestClass]
public class FiveNodeGraphTests
{
    private static string[] Snapshot(FiveNodeGraph graph) => graph.Vertices.Select(v => $"V:{v}")
        .Concat(graph.Edges.Select(e => $"E:{e}"))
        .Concat(graph.ExpandedPaths.Select(p => "M:" + p)).Order(StringComparer.Ordinal).ToArray();
    private static string[] Connections(FiveNodeGraph graph) => graph.Edges.Select(e => e.Source.VertexId + ">" + e.Target.VertexId).Order(StringComparer.Ordinal).ToArray();

    [DataTestMethod]
    [DataRow(Prototype.FullCross, 9)]
    [DataRow(Prototype.SingleCross, 8)]
    [DataRow(Prototype.Parallel, 7)]
    public void InitialEdgeCountIsExact(Prototype prototype, int count) => Assert.AreEqual(count, new FiveNodeGraph(prototype).Edges.Count);

    [DataTestMethod]
    [DataRow(Prototype.FullCross, "S0>root/A,S1>root/B,root/A>root/C,root/A>root/D,root/B>root/C,root/B>root/D,root/C>root/E,root/D>root/E,root/E>T")]
    [DataRow(Prototype.SingleCross, "S0>root/A,S1>root/B,root/A>root/C,root/A>root/D,root/B>root/D,root/C>root/E,root/D>root/E,root/E>T")]
    [DataRow(Prototype.Parallel, "S0>root/A,S1>root/B,root/A>root/C,root/B>root/D,root/C>root/E,root/D>root/E,root/E>T")]
    public void InitialEdgeSetMatchesPrototype(Prototype prototype, string expected) =>
        CollectionAssert.AreEqual(expected.Split(',').Order(StringComparer.Ordinal).ToArray(), Connections(new(prototype)));

    [DataTestMethod]
    [DataRow(Prototype.FullCross)]
    [DataRow(Prototype.SingleCross)]
    [DataRow(Prototype.Parallel)]
    public void InitialGraphHasOnlyFiveRealAndThreeExternalVertices(Prototype prototype) => CollectionAssert.AreEquivalent(
        new[] { "S0", "S1", "T", "root/A", "root/B", "root/C", "root/D", "root/E" }, new FiveNodeGraph(prototype).Vertices.Select(v => v.Id).ToArray());

    [DataTestMethod]
    [DataRow(Prototype.FullCross, "12,12,21,21,21")]
    [DataRow(Prototype.SingleCross, "12,11,11,21,21")]
    [DataRow(Prototype.Parallel, "11,11,11,11,21")]
    public void DegreesComeFromActualPrototype(Prototype prototype, string expected)
    {
        var graph = new FiveNodeGraph(prototype);
        CollectionAssert.AreEqual(expected.Split(','), new[] { "A", "B", "C", "D", "E" }.Select(n => graph.GetDegree("root/" + n))
            .Select(d => $"{d.Incoming}{d.Outgoing}").ToArray());
    }

    [DataTestMethod]
    [DataRow(Prototype.FullCross, "root/C", 5)]
    [DataRow(Prototype.FullCross, "root/A", 7)]
    [DataRow(Prototype.SingleCross, "root/C", 6)]
    [DataRow(Prototype.Parallel, "root/A", 6)]
    public void ReplacementHasExactTemplateVertices(Prototype prototype, string path, int count)
    {
        var graph = new FiveNodeGraph(prototype);
        graph.Expand(path);
        Assert.AreEqual(count, graph.Vertices.Count(v => v.Id.StartsWith(path + "/", StringComparison.Ordinal)));
    }

    [DataTestMethod]
    [DataRow(Prototype.FullCross, "root/C", 6)]
    [DataRow(Prototype.FullCross, "root/A", 9)]
    [DataRow(Prototype.SingleCross, "root/C", 7)]
    [DataRow(Prototype.Parallel, "root/A", 6)]
    public void ReplacementEdgeGrowthEqualsTemplateInternalEdges(Prototype prototype, string path, int growth)
    {
        var graph = new FiveNodeGraph(prototype);
        int before = graph.Edges.Count;
        graph.Expand(path);
        Assert.AreEqual(before + growth, graph.Edges.Count);
    }

    [TestMethod]
    public void ReplacementRemovesOldVertex()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/C");
        Assert.IsFalse(graph.Vertices.Any(v => v.Id == "root/C"));
    }

    [TestMethod]
    public void IndependentIncomingEdgesUseAAndBWithoutMerge()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/C");
        CollectionAssert.AreEquivalent(new[] { "root/A>root/C/A", "root/B>root/C/B" },
            Connections(graph).Where(e => e.StartsWith("root/A>root/C/", StringComparison.Ordinal) || e.StartsWith("root/B>root/C/", StringComparison.Ordinal)).ToArray());
    }

    [TestMethod]
    public void SplitReplacementRewiresBothOutputsInOriginalOrder()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/A");
        CollectionAssert.AreEqual(new[] { new Edge(new("root/A/O", 0), new("root/C", 0)), new Edge(new("root/A/O", 1), new("root/D", 0)) },
            graph.Edges.Where(e => e.Source.VertexId == "root/A/O").ToArray());
    }

    [TestMethod]
    public void AdaptersAreRealAndAtSameDepthAsCore()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/A");
        CollectionAssert.AreEquivalent(new[] { "root/A/I", "root/A/O" }, graph.Vertices.Where(v => v.IsAdapter && !v.IsBoundary && v.Depth == 1).Select(v => v.Id).ToArray());
    }

    [DataTestMethod]
    [DataRow("I")]
    [DataRow("O")]
    public void AdapterCanBeRecursivelyReplaced(string adapter)
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/A"); graph.Expand("root/A/" + adapter);
        Assert.AreEqual(17, graph.RealVertexCount);
    }

    [TestMethod]
    public void NestedOutputPortsRetainIdentityRegardlessOfExpansionOrder()
    {
        var graph = new FiveNodeGraph();
        graph.Expand("root/A"); graph.Expand("root/D"); graph.Expand("root/A/O"); graph.Expand("root/C");
        CollectionAssert.AreEqual(new[] { new Edge(new("root/A/O/O", 0), new("root/C/A", 0)), new Edge(new("root/A/O/O", 1), new("root/D/A", 0)) },
            graph.Edges.Where(e => e.Source.VertexId == "root/A/O/O").ToArray());
    }

    [TestMethod]
    public void BoundaryApiRecursivelyResolvesInputPortOrder()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/C"); graph.Expand("root/C/A"); graph.Expand("root/C/B");
        CollectionAssert.AreEqual(new[] { new Port("root/C/A/I", 0), new Port("root/C/B/I", 0) }, graph.GetBoundary("root/C").Inputs.ToArray());
    }

    [TestMethod]
    public void ModuleSignaturePreservesOriginalArity()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/A"); graph.Expand("root/A/O");
        Assert.AreEqual(new Degree(1, 2), graph.GetSignature("root/A"));
    }

    [TestMethod]
    public void CollapseRestoresExactOriginalGraph()
    {
        var graph = new FiveNodeGraph(); var original = Snapshot(graph);
        graph.Expand("root/A"); graph.Expand("root/A/I"); graph.Collapse("root/A");
        CollectionAssert.AreEqual(original, Snapshot(graph));
    }

    [TestMethod]
    public void CollapseSubtreeLeavesSiblingExpanded()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/A"); graph.Expand("root/B"); graph.Expand("root/A/I"); graph.Collapse("root/A");
        CollectionAssert.AreEqual(new[] { "root/B" }, graph.ExpandedPaths.ToArray());
    }

    [TestMethod]
    public void CollapseAllRestoresBase()
    {
        var graph = new FiveNodeGraph(); var expected = Snapshot(graph); graph.Generate(2); graph.CollapseAll();
        CollectionAssert.AreEqual(expected, Snapshot(graph));
    }

    [DataTestMethod]
    [DataRow(Prototype.FullCross, 0, 5, 9, 0)]
    [DataRow(Prototype.FullCross, 1, 29, 45, 5)]
    [DataRow(Prototype.FullCross, 2, 173, 261, 34)]
    [DataRow(Prototype.SingleCross, 1, 29, 40, 5)]
    [DataRow(Prototype.SingleCross, 2, 173, 232, 34)]
    [DataRow(Prototype.Parallel, 1, 29, 35, 5)]
    [DataRow(Prototype.Parallel, 2, 173, 203, 34)]
    public void WholeLeafGenerationHasIndependentExactCounts(Prototype prototype, int depth, int real, int edgeCount, int expansions)
    {
        var graph = new FiveNodeGraph(prototype); graph.Generate(depth);
        CollectionAssert.AreEqual(new[] { real, edgeCount, expansions, depth }, new[] { graph.RealVertexCount, graph.Edges.Count, graph.ExpandedPaths.Count, graph.Depth });
    }

    [TestMethod]
    public void GenerateZeroReplacesPreviousGraphWithBase()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/A"); graph.Generate(0);
        CollectionAssert.AreEqual(Snapshot(new()), Snapshot(graph));
    }

    [DataTestMethod]
    [DataRow("root/Z")]
    [DataRow("root/A/C")]
    [DataRow("S0")]
    [DataRow("")]
    public void InvalidExpansionIsAtomic(string path)
    {
        var graph = new FiveNodeGraph(); var before = Snapshot(graph);
        Assert.ThrowsException<ArgumentException>(() => graph.Expand(path));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }

    [TestMethod]
    public void NullPathIsRejected() => Assert.ThrowsException<ArgumentNullException>(() => new FiveNodeGraph().Expand(null!));

    [TestMethod]
    public void ManualChainCanExpandBeyondOldDepthLimit()
    {
        var graph = new FiveNodeGraph(); string path = "root/C";
        for (int i = 0; i < 12; i++) { graph.Expand(path); path += "/C"; }
        Assert.AreEqual(12, graph.Depth);
    }

    [TestMethod]
    public void ManualExpansionCanExceedOldExpansionLimit()
    {
        var graph = new FiveNodeGraph(); graph.Generate(2);
        foreach (var leaf in graph.Vertices.Where(v => !v.IsBoundary).Take(70).ToArray()) graph.Expand(leaf.Id);
        Assert.AreEqual(104, graph.ExpandedPaths.Count);
    }

    [TestMethod]
    public void ManualExpansionCanExceedOldVertexLimit()
    {
        var graph = new FiveNodeGraph(); graph.Generate(2);
        foreach (var leaf in graph.Vertices.Where(v => !v.IsBoundary).Take(83).ToArray()) graph.Expand(leaf.Id);
        Assert.IsTrue(graph.RealVertexCount > 500);
    }

    [TestMethod]
    public void DefaultBatchBudgetAllowsThirdRound()
    {
        var graph = new FiveNodeGraph(); graph.Generate(3);
        Assert.AreEqual(1037, graph.RealVertexCount);
    }

    [TestMethod]
    public void ExplicitBudgetCanExceedDefaultBudget()
    {
        var graph = new FiveNodeGraph(); graph.Generate(4, 1244);
        Assert.AreEqual(1244, graph.ExpandedPaths.Count);
    }

    [TestMethod]
    public void ZeroBudgetAllowsZeroRounds()
    {
        var graph = new FiveNodeGraph(); graph.Generate(0, 0);
        Assert.AreEqual(5, graph.RealVertexCount);
    }

    [TestMethod]
    public void ExactBudgetAllowsAllRequestedExpansions()
    {
        var graph = new FiveNodeGraph(); graph.Generate(2, 34);
        Assert.AreEqual(34, graph.ExpandedPaths.Count);
    }

    [TestMethod]
    public void ExplicitBatchBudgetRejectionIsAtomic()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/A"); var before = Snapshot(graph);
        Assert.ThrowsException<InvalidOperationException>(() => graph.Generate(3, 206));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }

    [TestMethod]
    public void ExplicitExpansionBudgetRejectionIsAtomic()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/C"); var before = Snapshot(graph);
        Assert.ThrowsException<InvalidOperationException>(() => graph.Generate(1, 4));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }

    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void InvalidGenerationDepthIsAtomic(int depth)
    {
        var graph = new FiveNodeGraph(); var before = Snapshot(graph);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => graph.Generate(depth));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }

    [TestMethod]
    public void NegativeExpansionBudgetIsRejected() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new FiveNodeGraph().Generate(1, -1));

    [DataTestMethod]
    [DataRow(6)]
    [DataRow(int.MaxValue)]
    public void LargeDepthIsRejectedByExplicitBudgetAtomically(int depth)
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/A"); var before = Snapshot(graph);
        Assert.ThrowsException<InvalidOperationException>(() => graph.Generate(depth, 5));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }

    [TestMethod]
    public void DefaultBatchBudgetRejectionIsAtomic()
    {
        var graph = new FiveNodeGraph(); graph.Expand("root/A"); var before = Snapshot(graph);
        Assert.ThrowsException<InvalidOperationException>(() => graph.Generate(4));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }

    [TestMethod]
    public void InvalidCollapseIsAtomic()
    {
        var graph = new FiveNodeGraph(); var before = Snapshot(graph);
        Assert.ThrowsException<ArgumentException>(() => graph.Collapse("root/A"));
        CollectionAssert.AreEqual(before, Snapshot(graph));
    }

    [DataTestMethod]
    [DataRow(Prototype.FullCross)]
    [DataRow(Prototype.SingleCross)]
    [DataRow(Prototype.Parallel)]
    public void GeneratedEdgesHaveNoDanglingEndpoints(Prototype prototype)
    {
        var graph = new FiveNodeGraph(prototype); graph.Generate(2);
        var ids = graph.Vertices.Select(v => v.Id).ToHashSet();
        Assert.AreEqual(0, graph.Edges.Count(e => !ids.Contains(e.Source.VertexId) || !ids.Contains(e.Target.VertexId)));
    }

    [DataTestMethod]
    [DataRow(Prototype.FullCross)]
    [DataRow(Prototype.SingleCross)]
    [DataRow(Prototype.Parallel)]
    public void EveryRealVertexHasUniqueContiguousPorts(Prototype prototype)
    {
        var graph = new FiveNodeGraph(prototype); graph.Generate(2);
        foreach (var vertex in graph.Vertices.Where(v => !v.IsBoundary))
        {
            var signature = graph.GetSignature(vertex.Id);
            CollectionAssert.AreEqual(Enumerable.Range(0, signature.Incoming).ToArray(), graph.Edges.Where(e => e.Target.VertexId == vertex.Id).Select(e => e.Target.Index).Order().ToArray());
            CollectionAssert.AreEqual(Enumerable.Range(0, signature.Outgoing).ToArray(), graph.Edges.Where(e => e.Source.VertexId == vertex.Id).Select(e => e.Source.Index).Order().ToArray());
        }
    }

    private static HashSet<string> Reach(FiveNodeGraph graph, IEnumerable<string> starts, bool reverse)
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

    [DataTestMethod]
    [DataRow(Prototype.FullCross)]
    [DataRow(Prototype.SingleCross)]
    [DataRow(Prototype.Parallel)]
    public void EveryVertexIsReachableFromTheTwoSources(Prototype prototype)
    {
        var graph = new FiveNodeGraph(prototype); graph.Generate(2);
        Assert.AreEqual(graph.Vertices.Count, Reach(graph, ["S0", "S1"], false).Count);
    }

    [DataTestMethod]
    [DataRow(Prototype.FullCross)]
    [DataRow(Prototype.SingleCross)]
    [DataRow(Prototype.Parallel)]
    public void EveryVertexCanReachSink(Prototype prototype)
    {
        var graph = new FiveNodeGraph(prototype); graph.Generate(2);
        Assert.AreEqual(graph.Vertices.Count, Reach(graph, ["T"], true).Count);
    }

    [DataTestMethod]
    [DataRow(Prototype.FullCross)]
    [DataRow(Prototype.SingleCross)]
    [DataRow(Prototype.Parallel)]
    public void GeneratedGraphIsAcyclic(Prototype prototype)
    {
        var graph = new FiveNodeGraph(prototype); graph.Generate(2);
        var degree = graph.Vertices.ToDictionary(v => v.Id, v => graph.GetDegree(v.Id).Incoming);
        var queue = new Queue<string>(degree.Where(p => p.Value == 0).Select(p => p.Key)); int visited = 0;
        while (queue.TryDequeue(out var current))
        {
            visited++;
            foreach (var edge in graph.Edges.Where(e => e.Source.VertexId == current))
                if (--degree[edge.Target.VertexId] == 0) queue.Enqueue(edge.Target.VertexId);
        }
        Assert.AreEqual(graph.Vertices.Count, visited);
    }
}
