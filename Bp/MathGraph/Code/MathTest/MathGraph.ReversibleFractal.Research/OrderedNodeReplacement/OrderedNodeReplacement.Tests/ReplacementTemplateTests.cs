using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OrderedNodeReplacement.Tests;

[TestClass]
public sealed class ReplacementTemplateTests
{
    [TestMethod]
    public void ThreeNodeTemplateReplacesTheTargetWithoutExtraNodes()
    {
        var source = new NodeId("source");
        var target = new NodeId("target");
        var sink = new NodeId("sink");
        var graph = new OrderedGraph(
            [new(source, 0, 1), new(target, 1, 1), new(sink, 1, 0)],
            [Connect(source, target), Connect(target, sink)]);
        var a = target.Child("a");
        var b = target.Child("b");
        var c = target.Child("c");
        var expected = new OrderedGraph(
            [new(source, 0, 1), new(sink, 1, 0), new(a, 1, 1), new(b, 1, 1), new(c, 1, 1)],
            [Connect(source, a), Connect(a, b), Connect(b, c), Connect(c, sink)]);

        var result = Chain().Apply(graph, target);

        AssertGraph(expected, result);
    }

    [TestMethod]
    public void FiveNodeTemplateUsesBoundaryOrderForDifferentSignature()
    {
        var left = new NodeId("left");
        var right = new NodeId("right");
        var middle = new NodeId("middle");
        var upper = new NodeId("upper");
        var lower = new NodeId("lower");
        var fragment = new OrderedGraph(
            [new(left, 1, 1), new(right, 1, 1), new(middle, 2, 2), new(upper, 1, 1), new(lower, 1, 1)],
            [Connect(left, middle, input: 1), Connect(right, middle),
                Connect(middle, upper, output: 1), Connect(middle, lower)]);
        var template = new ReplacementTemplate(fragment, [new(right, 0), new(left, 0)],
            [new(lower, 0), new(upper, 0)]);
        var source = new NodeId("source");
        var target = new NodeId("target");
        var sink = new NodeId("sink");
        var graph = new OrderedGraph(
            [new(source, 0, 2), new(target, 2, 2), new(sink, 2, 0)],
            [Connect(source, target), Connect(source, target, 1, 1),
                Connect(target, sink), Connect(target, sink, 1, 1)]);
        var l = target.Child("left");
        var r = target.Child("right");
        var m = target.Child("middle");
        var u = target.Child("upper");
        var d = target.Child("lower");
        var expected = new OrderedGraph(
            [new(source, 0, 2), new(sink, 2, 0), new(l, 1, 1), new(r, 1, 1), new(m, 2, 2), new(u, 1, 1), new(d, 1, 1)],
            [Connect(source, r), Connect(source, l, output: 1), Connect(l, m, input: 1), Connect(r, m),
                Connect(m, u, output: 1), Connect(m, d), Connect(d, sink), Connect(u, sink, input: 1)]);

        var result = template.Apply(graph, target);

        AssertGraph(expected, result);
    }

    [TestMethod]
    public void AdjacentReplacementsProduceTheSameGraphInEitherOrder()
    {
        var first = new NodeId("first");
        var second = new NodeId("second");
        var graph = new OrderedGraph([new(first, 1, 1), new(second, 1, 1)], [Connect(first, second)]);
        var template = Chain();

        var firstThenSecond = template.Apply(template.Apply(graph, first), second);
        var secondThenFirst = template.Apply(template.Apply(graph, second), first);

        AssertGraph(firstThenSecond, secondThenFirst);
    }

    [TestMethod]
    public void AdjacentReplacementsConnectTheTwoScopedBoundaryNodes()
    {
        var first = new NodeId("first");
        var second = new NodeId("second");
        var graph = new OrderedGraph([new(first, 1, 1), new(second, 1, 1)], [Connect(first, second)]);
        var template = Chain();

        var result = template.Apply(template.Apply(graph, first), second);

        Assert.IsTrue(result.Edges.Contains(Connect(first.Child("c"), second.Child("a"))));
    }

    [TestMethod]
    public void ReplacementRemapsBothEndpointsOfASelfLoop()
    {
        var target = new NodeId("target");
        var graph = new OrderedGraph([new(target, 1, 1)], [Connect(target, target)]);

        var result = Chain().Apply(graph, target);

        CollectionAssert.AreEquivalent(
            new[] { Connect(target.Child("a"), target.Child("b")), Connect(target.Child("b"), target.Child("c")),
                Connect(target.Child("c"), target.Child("a")) }, result.Edges.ToArray());
    }

    [DataTestMethod]
    [DataRow(-1, 0)]
    [DataRow(1, 0)]
    [DataRow(0, 1)]
    public void GraphRejectsInvalidEdgePort(int output, int input)
    {
        var a = new NodeId("a");
        var b = new NodeId("b");

        Assert.ThrowsException<ArgumentException>(() =>
            new OrderedGraph([new(a, 0, 1), new(b, 1, 0)], [Connect(a, b, output, input)]));
    }

    [TestMethod]
    public void TemplateRejectsPortInTheWrongDirection()
    {
        var a = new NodeId("a");
        var fragment = new OrderedGraph([new(a, 0, 1)], []);

        Assert.ThrowsException<ArgumentException>(() => new ReplacementTemplate(fragment, [new(a, 0)], []));
    }

    [TestMethod]
    public void TemplateRejectsBoundaryPortUsedByAnInternalEdge()
    {
        var a = new NodeId("a");
        var b = new NodeId("b");
        var fragment = new OrderedGraph([new(a, 0, 1), new(b, 1, 0)], [Connect(a, b)]);

        Assert.ThrowsException<ArgumentException>(() => new ReplacementTemplate(fragment, [], [new(a, 0)]));
    }

    [TestMethod]
    public void TemplateRejectsRepeatedBoundaryPort()
    {
        var a = new NodeId("a");
        var fragment = new OrderedGraph([new(a, 1, 0)], []);

        Assert.ThrowsException<ArgumentException>(() => new ReplacementTemplate(fragment, [new(a, 0), new(a, 0)], []));
    }

    [TestMethod]
    public void ReplacementRejectsSignatureMismatchEvenWhenPortsAreUnconnected()
    {
        var target = new NodeId("target");
        var graph = new OrderedGraph([new(target, 2, 1)], []);

        Assert.ThrowsException<ArgumentException>(() => Chain().Apply(graph, target));
    }

    private static ReplacementTemplate Chain()
    {
        var a = new NodeId("a");
        var b = new NodeId("b");
        var c = new NodeId("c");
        var fragment = new OrderedGraph([new(a, 1, 1), new(b, 1, 1), new(c, 1, 1)],
            [Connect(a, b), Connect(b, c)]);
        return new ReplacementTemplate(fragment, [new(a, 0)], [new(c, 0)]);
    }

    private static Edge Connect(NodeId source, NodeId target, int output = 0, int input = 0) =>
        new(new(source, output), new(target, input));

    private static void AssertGraph(OrderedGraph expected, OrderedGraph actual)
    {
        CollectionAssert.AreEquivalent(expected.Nodes.ToArray(), actual.Nodes.ToArray());
        CollectionAssert.AreEquivalent(expected.Edges.ToArray(), actual.Edges.ToArray());
    }
}
