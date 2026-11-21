using System.Windows;

namespace OrderedNodeReplacement.Wpf;

internal sealed record ReplacementExample(
    OrderedGraph Graph,
    NodeId Target,
    ReplacementTemplate Template,
    IReadOnlyDictionary<NodeId, Point> BeforeLayout,
    IReadOnlyDictionary<NodeId, Point> AfterLayout)
{
    internal static ReplacementExample ThreeNode()
    {
        var a = new NodeId("a");
        var b = new NodeId("b");
        var c = new NodeId("c");
        var fragment = new OrderedGraph([new(a, 1, 1), new(b, 1, 1), new(c, 1, 1)],
            [Connect(a, b), Connect(b, c)]);
        return Create(new(fragment, [new(a, 0)], [new(c, 0)]),
            [(a, new(189, 73)), (b, new(354, 73)), (c, new(519, 73))]);
    }

    internal static ReplacementExample FiveNode()
    {
        var a = new NodeId("a");
        var b = new NodeId("b");
        var c = new NodeId("c");
        var d = new NodeId("d");
        var e = new NodeId("e");
        var fragment = new OrderedGraph(
            [new(a, 1, 1), new(b, 1, 1), new(c, 2, 2), new(d, 1, 1), new(e, 1, 1)],
            [Connect(a, c, input: 1), Connect(b, c), Connect(c, d, output: 1), Connect(c, e)]);
        return Create(new(fragment, [new(b, 0), new(a, 0)], [new(e, 0), new(d, 0)]),
            [(a, new(189, 132)), (b, new(189, 14)), (c, new(354, 73)),
                (d, new(519, 132)), (e, new(519, 14))]);
    }

    private static ReplacementExample Create(ReplacementTemplate template,
        (NodeId Local, Point Position)[] fragmentLayout)
    {
        var source = new NodeId("source");
        var target = new NodeId("target");
        var sink = new NodeId("sink");
        var inputs = template.Inputs.Count;
        var outputs = template.Outputs.Count;
        var graph = new OrderedGraph([new(source, 0, inputs), new(target, inputs, outputs), new(sink, outputs, 0)],
            Enumerable.Range(0, inputs).Select(index => Connect(source, target, index, index))
                .Concat(Enumerable.Range(0, outputs).Select(index => Connect(target, sink, index, index))));

        var afterLayout = new Dictionary<NodeId, Point> { [source] = new(24, 73), [sink] = new(684, 73) };
        var beforeLayout = new Dictionary<NodeId, Point>(afterLayout) { [target] = new(354, 73) };
        foreach (var (local, position) in fragmentLayout)
        {
            afterLayout.Add(target.Child(local.Name), position);
        }

        return new(graph, target, template, beforeLayout, afterLayout);
    }

    private static Edge Connect(NodeId source, NodeId target, int output = 0, int input = 0) =>
        new(new(source, output), new(target, input));
}
