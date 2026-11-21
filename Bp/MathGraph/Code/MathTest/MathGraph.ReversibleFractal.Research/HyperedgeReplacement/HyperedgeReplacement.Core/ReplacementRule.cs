namespace MathGraph.HyperedgeReplacement;

/// <summary>A rule with a nonterminal left-hand label and an ordered right-hand interface.</summary>
public sealed class ReplacementRule
{
    /// <summary>Creates a rule whose distinct external vertices match the left-hand rank.</summary>
    public ReplacementRule(EdgeLabel left, Hypergraph right, IEnumerable<Vertex> externalVertices)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(externalVertices);
        if (left.IsTerminal)
        {
            throw new ArgumentException("The left-hand label must be nonterminal.", nameof(left));
        }

        var positions = externalVertices.ToArray();
        var vertices = right.Vertices.ToHashSet();
        if (positions.Length != left.Rank || positions.Distinct().Count() != positions.Length ||
            positions.Any(vertex => !vertices.Contains(vertex)))
        {
            throw new ArgumentException("External vertices must be distinct right-hand vertices and match the left-hand rank.", nameof(externalVertices));
        }

        Left = left;
        Right = right;
        ExternalVertices = Array.AsReadOnly(positions);
    }

    public EdgeLabel Left { get; }
    public Hypergraph Right { get; }
    public IReadOnlyList<Vertex> ExternalVertices { get; }

    /// <summary>Removes the selected edge and inserts a fresh copy of the right-hand side, glued by position.</summary>
    public Hypergraph Apply(Hypergraph host, Hyperedge selected)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(selected);
        if (!host.Edges.Contains(selected))
        {
            throw new ArgumentException("The selected edge must belong to the host graph.", nameof(selected));
        }

        if (selected.Label != Left)
        {
            throw new ArgumentException("The selected edge must match the left-hand label and rank.", nameof(selected));
        }

        var images = new Dictionary<Vertex, Vertex>(Right.Vertices.Count);
        for (var index = 0; index < ExternalVertices.Count; index++)
        {
            images.Add(ExternalVertices[index], selected.Attachments[index]);
        }

        var vertices = new List<Vertex>(host.Vertices);
        foreach (var vertex in Right.Vertices)
        {
            if (!images.ContainsKey(vertex))
            {
                var copy = new Vertex();
                images.Add(vertex, copy);
                vertices.Add(copy);
            }
        }

        var copies = Right.Edges.Select(edge => new Hyperedge(edge.Label, edge.Attachments.Select(vertex => images[vertex])));
        return new Hypergraph(vertices, host.Edges.Where(edge => edge != selected).Concat(copies));
    }
}
