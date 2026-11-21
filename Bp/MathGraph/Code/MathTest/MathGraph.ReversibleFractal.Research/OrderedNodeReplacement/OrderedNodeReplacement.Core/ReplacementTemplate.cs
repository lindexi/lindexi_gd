namespace OrderedNodeReplacement;

/// <summary>A graph fragment with ordered input and output boundary mappings.</summary>
public sealed class ReplacementTemplate
{
    /// <summary>Creates a template whose boundary ports are distinct and free of internal edges.</summary>
    public ReplacementTemplate(OrderedGraph fragment, IEnumerable<Port> inputs, IEnumerable<Port> outputs)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(outputs);

        if (fragment.Nodes.Count == 0 || fragment.Nodes.Any(node => node.Id.Parent is not null))
        {
            throw new ArgumentException("A fragment must contain nodes with local, single-part IDs.", nameof(fragment));
        }

        Fragment = fragment;
        Inputs = Array.AsReadOnly(inputs.ToArray());
        Outputs = Array.AsReadOnly(outputs.ToArray());
        ValidateBoundary(Inputs, output: false, nameof(inputs));
        ValidateBoundary(Outputs, output: true, nameof(outputs));
    }

    public OrderedGraph Fragment { get; }
    public IReadOnlyList<Port> Inputs { get; }
    public IReadOnlyList<Port> Outputs { get; }

    /// <summary>Replaces one node, remapping each incident endpoint by its direction and port index.</summary>
    public OrderedGraph Apply(OrderedGraph graph, NodeId nodeId)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(nodeId);

        if (!graph.TryGetNode(nodeId, out var node) || node is null)
        {
            throw new ArgumentException("The replacement target is not in the graph.", nameof(nodeId));
        }

        if (node.InputCount != Inputs.Count || node.OutputCount != Outputs.Count)
        {
            throw new ArgumentException("The target and template must have the same input and output counts.", nameof(nodeId));
        }

        Port Scope(Port port) => new(nodeId.Child(port.Node.Name), port.Index);

        var nodes = graph.Nodes.Where(candidate => candidate.Id != nodeId)
            .Concat(Fragment.Nodes.Select(local => local with { Id = nodeId.Child(local.Id.Name) }));
        var edges = graph.Edges.Select(edge => new Edge(
                edge.Source.Node == nodeId ? Scope(Outputs[edge.Source.Index]) : edge.Source,
                edge.Target.Node == nodeId ? Scope(Inputs[edge.Target.Index]) : edge.Target))
            .Concat(Fragment.Edges.Select(edge => new Edge(Scope(edge.Source), Scope(edge.Target))));

        return new OrderedGraph(nodes, edges);
    }

    private void ValidateBoundary(IReadOnlyList<Port> ports, bool output, string parameterName)
    {
        var seen = new HashSet<Port>();
        foreach (var port in ports)
        {
            Fragment.ValidatePort(port, output, parameterName);
            if (!seen.Add(port) || Fragment.IsUsed(port, output))
            {
                throw new ArgumentException("Boundary ports must be distinct and unused by internal edges.", parameterName);
            }
        }
    }
}
