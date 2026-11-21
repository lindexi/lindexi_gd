namespace OrderedNodeReplacement;

/// <summary>Identifies a node by its root name and optional template-local path.</summary>
public sealed record NodeId
{
    /// <summary>Creates a root identifier.</summary>
    public NodeId(string name) : this(null, name) { }

    private NodeId(NodeId? parent, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Parent = parent;
        Name = name;
    }

    public NodeId? Parent { get; }
    public string Name { get; }

    /// <summary>Creates an identifier scoped to this node.</summary>
    public NodeId Child(string name) => new(this, name);
}

/// <summary>A node with separate zero-based input and output port ranges.</summary>
public sealed record Node(NodeId Id, int InputCount, int OutputCount);

/// <summary>A node and port index; the surrounding edge or boundary determines direction.</summary>
public readonly record struct Port(NodeId Node, int Index);

/// <summary>A directed edge from an output port to an input port.</summary>
public readonly record struct Edge(Port Source, Port Target);

/// <summary>A directed graph with at most one edge per port in each direction.</summary>
public sealed class OrderedGraph
{
    private readonly Dictionary<NodeId, Node> nodesById = new();
    private readonly HashSet<Port> usedOutputs = new();
    private readonly HashSet<Port> usedInputs = new();

    /// <summary>Snapshots the nodes and edges and validates their endpoints.</summary>
    public OrderedGraph(IEnumerable<Node> nodes, IEnumerable<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        Nodes = Array.AsReadOnly(nodes.ToArray());
        Edges = Array.AsReadOnly(edges.ToArray());

        foreach (var node in Nodes)
        {
            if (node is null || node.Id is null || node.InputCount < 0 || node.OutputCount < 0 ||
                !nodesById.TryAdd(node.Id, node))
            {
                throw new ArgumentException("Nodes must have unique IDs and nonnegative port counts.", nameof(nodes));
            }
        }

        foreach (var node in Nodes)
        {
            for (var ancestor = node.Id.Parent; ancestor is not null; ancestor = ancestor.Parent)
            {
                if (nodesById.ContainsKey(ancestor))
                {
                    throw new ArgumentException("A node's scope cannot contain another live node.", nameof(nodes));
                }
            }
        }

        foreach (var edge in Edges)
        {
            ValidatePort(edge.Source, output: true, nameof(edges));
            ValidatePort(edge.Target, output: false, nameof(edges));
            if (!usedOutputs.Add(edge.Source) || !usedInputs.Add(edge.Target))
            {
                throw new ArgumentException("A port cannot carry multiple edges in the same direction.", nameof(edges));
            }
        }
    }

    public IReadOnlyList<Node> Nodes { get; }
    public IReadOnlyList<Edge> Edges { get; }

    internal bool TryGetNode(NodeId id, out Node? node) => nodesById.TryGetValue(id, out node);

    internal void ValidatePort(Port port, bool output, string parameterName)
    {
        if (port.Node is null || !nodesById.TryGetValue(port.Node, out var node) || port.Index < 0 ||
            port.Index >= (output ? node.OutputCount : node.InputCount))
        {
            throw new ArgumentException("Port does not exist in the specified direction.", parameterName);
        }
    }

    internal bool IsUsed(Port port, bool output) => (output ? usedOutputs : usedInputs).Contains(port);
}
