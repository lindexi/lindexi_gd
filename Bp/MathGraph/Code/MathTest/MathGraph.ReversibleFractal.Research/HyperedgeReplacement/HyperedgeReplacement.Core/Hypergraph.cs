namespace MathGraph.HyperedgeReplacement;

/// <summary>A vertex whose identity is independent of its position in a graph.</summary>
public sealed class Vertex;

/// <summary>A symbol with fixed arity and terminal status.</summary>
public sealed record EdgeLabel
{
    /// <summary>Creates a ranked symbol.</summary>
    public EdgeLabel(string name, int rank, bool isTerminal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(rank);
        Name = name;
        Rank = rank;
        IsTerminal = isTerminal;
    }

    public string Name { get; }
    public int Rank { get; }
    public bool IsTerminal { get; }
}

/// <summary>An edge with its own identity and an ordered attachment list.</summary>
public sealed class Hyperedge
{
    /// <summary>Creates an edge and checks that its attachment count matches the label rank.</summary>
    public Hyperedge(EdgeLabel label, IEnumerable<Vertex> attachments)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(attachments);
        var positions = attachments.ToArray();
        if (positions.Length != label.Rank || positions.Contains(null))
        {
            throw new ArgumentException("Attachments must be non-null and match the label rank.", nameof(attachments));
        }

        Label = label;
        Attachments = Array.AsReadOnly(positions);
    }

    public EdgeLabel Label { get; }
    public IReadOnlyList<Vertex> Attachments { get; }
}

/// <summary>An immutable snapshot of identified vertices and hyperedges.</summary>
public sealed class Hypergraph
{
    /// <summary>Creates a graph and checks that each attachment belongs to it.</summary>
    public Hypergraph(IEnumerable<Vertex> vertices, IEnumerable<Hyperedge> edges)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(edges);
        var vertexList = vertices.ToArray();
        var edgeList = edges.ToArray();
        var vertexSet = vertexList.ToHashSet();
        if (vertexList.Any(vertex => vertex is null) || vertexSet.Count != vertexList.Length)
        {
            throw new ArgumentException("Vertices must be non-null and unique.", nameof(vertices));
        }

        if (edgeList.Distinct().Count() != edgeList.Length ||
            edgeList.Any(edge => edge is null || edge.Attachments.Any(vertex => !vertexSet.Contains(vertex))))
        {
            throw new ArgumentException("Edges must be non-null and unique, with all attachments in the graph.", nameof(edges));
        }

        Vertices = Array.AsReadOnly(vertexList);
        Edges = Array.AsReadOnly(edgeList);
    }

    public IReadOnlyList<Vertex> Vertices { get; }
    public IReadOnlyList<Hyperedge> Edges { get; }
}
