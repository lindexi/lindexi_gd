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
    [DataRow("root/L")]
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
