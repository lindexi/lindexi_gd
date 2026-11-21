using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraph.HyperedgeReplacement.Tests;

[TestClass]
public sealed class ReplacementRuleTests
{
    [TestMethod]
    public void WhenExternalOrderIsReversedThenAttachmentsFollowPositions()
    {
        var first = new Vertex();
        var second = new Vertex();
        var x = new Vertex();
        var y = new Vertex();
        var label = new EdgeLabel("A", 2, false);
        var selected = new Hyperedge(label, [first, second]);
        var template = new Hyperedge(new EdgeLabel("link", 2, true), [x, y]);
        var rule = new ReplacementRule(label, new Hypergraph([x, y], [template]), [y, x]);

        var result = rule.Apply(new Hypergraph([first, second], [selected]), selected);

        CollectionAssert.AreEqual(new[] { second, first }, result.Edges.Single().Attachments.ToArray());
    }

    [TestMethod]
    public void WhenEdgeRankDoesNotMatchAttachmentCountThenConstructionFails()
    {
        var label = new EdgeLabel("A", 2, false);
        var vertex = new Vertex();

        Assert.ThrowsException<ArgumentException>(() => new Hyperedge(label, [vertex]));
    }

    [TestMethod]
    public void WhenRuleRankDoesNotMatchExternalCountThenConstructionFails()
    {
        var label = new EdgeLabel("A", 2, false);
        var vertex = new Vertex();
        var right = new Hypergraph([vertex], []);

        Assert.ThrowsException<ArgumentException>(() => new ReplacementRule(label, right, [vertex]));
    }

    [TestMethod]
    public void WhenSelectedRankDiffersDespiteMatchingNameThenReplacementFails()
    {
        var vertex = new Vertex();
        var selected = new Hyperedge(new EdgeLabel("A", 1, false), [vertex]);
        var host = new Hypergraph([vertex], [selected]);
        var rule = new ReplacementRule(new EdgeLabel("A", 0, false), new Hypergraph([], []), []);

        Assert.ThrowsException<ArgumentException>(() => rule.Apply(host, selected));
    }

    [TestMethod]
    public void WhenLeftLabelIsTerminalThenRuleConstructionFails()
    {
        var label = new EdgeLabel("link", 0, true);

        Assert.ThrowsException<ArgumentException>(() => new ReplacementRule(label, new Hypergraph([], []), []));
    }

    [TestMethod]
    public void WhenHostAttachmentRepeatsThenBothExternalPositionsGlueToIt()
    {
        var vertex = new Vertex();
        var x = new Vertex();
        var y = new Vertex();
        var label = new EdgeLabel("A", 2, false);
        var selected = new Hyperedge(label, [vertex, vertex]);
        var template = new Hyperedge(new EdgeLabel("link", 2, true), [x, y]);
        var rule = new ReplacementRule(label, new Hypergraph([x, y], [template]), [x, y]);

        var result = rule.Apply(new Hypergraph([vertex], [selected]), selected);

        CollectionAssert.AreEqual(new[] { vertex, vertex }, result.Edges.Single().Attachments.ToArray());
    }

    [TestMethod]
    public void WhenSelectedEdgesShareBoundaryThenBothOrdersYieldTheSamePortFixedShape()
    {
        var start = new Vertex();
        var joint = new Vertex();
        var end = new Vertex();
        var label = new EdgeLabel("A", 2, false);
        var terminal = new EdgeLabel("link", 2, true);
        var left = new Hyperedge(label, [start, joint]);
        var right = new Hyperedge(label, [joint, end]);
        var host = new Hypergraph([start, joint, end], [left, right]);
        var x = new Vertex();
        var middle = new Vertex();
        var y = new Vertex();
        var templateFirst = new Hyperedge(terminal, [x, middle]);
        var templateSecond = new Hyperedge(terminal, [middle, y]);
        var rule = new ReplacementRule(label, new Hypergraph([x, middle, y], [templateFirst, templateSecond]), [x, y]);

        var leftFirst = rule.Apply(rule.Apply(host, left), right);
        var rightFirst = rule.Apply(rule.Apply(host, right), left);

        var expected = new[] { (0, 3, terminal), (3, 1, terminal), (1, 4, terminal), (4, 2, terminal) };
        CollectionAssert.AreEquivalent(expected, Describe(leftFirst));
        CollectionAssert.AreEquivalent(expected, Describe(rightFirst));
        var originalVertices = new[] { start, joint, end };
        var leftCopies = leftFirst.Vertices.Except(originalVertices).ToArray();
        var rightCopies = rightFirst.Vertices.Except(originalVertices).ToArray();
        Assert.AreEqual(2, leftCopies.Length);
        Assert.AreEqual(2, rightCopies.Length);
        Assert.IsFalse(leftCopies.Concat(rightCopies).Contains(middle));
        Assert.IsFalse(leftCopies.Intersect(rightCopies).Any());
        Assert.IsFalse(leftFirst.Edges.Concat(rightFirst.Edges).Intersect([templateFirst, templateSecond]).Any());
        CollectionAssert.AreEqual(new[] { left, right }, host.Edges.ToArray());

        (int, int, EdgeLabel)[] Describe(Hypergraph graph)
        {
            var leftMiddle = graph.Edges.Single(edge => edge.Attachments[0] == start).Attachments[1];
            var rightMiddle = graph.Edges.Single(edge => edge.Attachments[0] == joint).Attachments[1];
            var ids = new Dictionary<Vertex, int>
            {
                [start] = 0, [joint] = 1, [end] = 2, [leftMiddle] = 3, [rightMiddle] = 4
            };
            return graph.Edges.Select(edge => (ids[edge.Attachments[0]], ids[edge.Attachments[1]], edge.Label)).ToArray();
        }
    }
}
