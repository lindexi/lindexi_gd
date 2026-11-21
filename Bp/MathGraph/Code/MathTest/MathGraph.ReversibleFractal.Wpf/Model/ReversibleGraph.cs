namespace MathGraph.ReversibleFractal.Wpf.Model;

/// <summary>The orientation of a five-node FullCross module.</summary>
public enum Direction { Forward, Reverse }
/// <summary>A real leaf or external boundary vertex.</summary>
public sealed record Vertex(string Id, bool IsBoundary, int Depth);
/// <summary>An ordered port on a real leaf.</summary>
public sealed record Port(string VertexId, int Index);
/// <summary>A directed connection between real leaf ports.</summary>
public sealed record Edge(Port Source, Port Target);
/// <summary>Actual incoming and outgoing arity.</summary>
public readonly record struct Degree(int Incoming, int Outgoing);
/// <summary>Ordered module boundaries resolved to actual leaf ports.</summary>
public sealed record Boundary(IReadOnlyList<Port> Inputs, IReadOnlyList<Port> Outputs);

/// <summary>A UI-independent, finite, port-preserving two-type substitution graph.</summary>
public sealed class ReversibleGraph
{
    /// <summary>Resource budget for batch generation only, not manual expansion.</summary>
    public const int DefaultExpansionBudget = 1000;
    private HashSet<string> expanded = new(StringComparer.Ordinal);
    private List<Vertex> vertices = [];
    private List<Edge> edges = [];
    private Dictionary<string, Degree> signatures = [];
    private Dictionary<string, Boundary> boundaries = [];
    private static readonly string[] Names = ["A", "B", "C", "D", "E"];
    private static readonly (string From, string To)[] Links =
        [("A", "C"), ("A", "D"), ("B", "C"), ("B", "D"), ("C", "E"), ("D", "E")];

    /// <summary>Creates the forward base graph with S0, S1 and T boundaries.</summary>
    public ReversibleGraph() => Rebuild();
    /// <summary>Actual leaves and the three external vertices; containers are excluded.</summary>
    public IReadOnlyList<Vertex> Vertices => vertices.AsReadOnly();
    /// <summary>Actual directed port edges.</summary>
    public IReadOnlyList<Edge> Edges => edges.AsReadOnly();
    /// <summary>Stable expanded container paths.</summary>
    public IReadOnlyList<string> ExpandedPaths => expanded.Order(StringComparer.Ordinal).ToArray();
    /// <summary>Number of real leaves.</summary>
    public int RealVertexCount => vertices.Count - 3;
    /// <summary>Maximum replacement depth.</summary>
    public int Depth => expanded.Count == 0 ? 0 : expanded.Max(p => p.Count(c => c == '/'));

    /// <summary>Reads a current vertex's actual incident degree.</summary>
    public Degree GetDegree(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!vertices.Any(v => v.Id == path)) throw new ArgumentException(null, nameof(path));
        return new(edges.Count(e => e.Target.VertexId == path), edges.Count(e => e.Source.VertexId == path));
    }
    /// <summary>Reads the preserved signature of a leaf or container.</summary>
    public Degree GetSignature(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return signatures.TryGetValue(path, out var value) ? value : throw new ArgumentException(null, nameof(path));
    }
    /// <summary>Chooses orientation from the actual preserved arity.</summary>
    public Direction GetDirection(string path) => GetSignature(path) == new Degree(2, 1) ? Direction.Forward : Direction.Reverse;
    /// <summary>Resolves ordered boundary ports through all nested modules.</summary>
    public Boundary GetBoundary(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return boundaries.TryGetValue(path, out var value) ? value : throw new ArgumentException(null, nameof(path));
    }
    /// <summary>Atomically replaces one real leaf without a manual expansion budget.</summary>
    public void Expand(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!vertices.Any(v => v.Id == path && !v.IsBoundary)) throw new ArgumentException(null, nameof(path));
        Commit(new HashSet<string>(expanded, StringComparer.Ordinal) { path });
    }
    /// <summary>Restores a module's original leaf and removes its expanded subtree.</summary>
    public void Collapse(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!expanded.Contains(path)) throw new ArgumentException(null, nameof(path));
        Commit(expanded.Where(p => p != path && !p.StartsWith(path + "/", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal));
    }
    /// <summary>Restores the base graph.</summary>
    public void CollapseAll() => Commit(new(StringComparer.Ordinal));
    /// <summary>Atomically generates whole-leaf rounds from the base, with an explicit batch budget.</summary>
    public void Generate(int depth, int expansionBudget = DefaultExpansionBudget)
    {
        if (depth < 0) throw new ArgumentOutOfRangeException(nameof(depth));
        if (expansionBudget < 0) throw new ArgumentOutOfRangeException(nameof(expansionBudget));
        long leaves = 5, required = 0;
        for (int level = 0; level < depth; level++)
        {
            required += leaves;
            if (required > expansionBudget) throw new InvalidOperationException();
            leaves *= 5;
        }
        var candidate = new ReversibleGraph();
        for (int level = 0; level < depth; level++)
        {
            candidate.expanded.UnionWith(candidate.vertices.Where(v => !v.IsBoundary).Select(v => v.Id));
            candidate.Rebuild();
        }
        Adopt(candidate);
    }
    private void Commit(HashSet<string> paths)
    {
        var candidate = new ReversibleGraph { expanded = paths };
        candidate.Rebuild();
        Adopt(candidate);
    }
    private void Adopt(ReversibleGraph candidate)
    {
        expanded = candidate.expanded; vertices = candidate.vertices; edges = candidate.edges;
        signatures = candidate.signatures; boundaries = candidate.boundaries;
    }
    private void Rebuild()
    {
        vertices = []; edges = []; signatures = []; boundaries = [];
        foreach (string id in new[] { "S0", "S1", "T" }) vertices.Add(new(id, true, 0));
        var root = BuildTemplate("root", new(2, 1));
        edges.Add(new(new("S0", 0), root.Inputs[0]));
        edges.Add(new(new("S1", 0), root.Inputs[1]));
        edges.Add(new(root.Outputs[0], new("T", 0)));
    }
    private Boundary BuildLeaf(string path, Degree degree)
    {
        signatures[path] = degree;
        if (expanded.Contains(path)) return BuildTemplate(path, degree);
        vertices.Add(new(path, false, path.Count(c => c == '/') - 1));
        var boundary = new Boundary(Array.AsReadOnly(Enumerable.Range(0, degree.Incoming).Select(i => new Port(path, i)).ToArray()),
            Array.AsReadOnly(Enumerable.Range(0, degree.Outgoing).Select(i => new Port(path, i)).ToArray()));
        return boundaries[path] = boundary;
    }
    private Boundary BuildTemplate(string path, Degree degree)
    {
        bool forward = degree == new Degree(2, 1);
        if (!forward && degree != new Degree(1, 2)) throw new InvalidOperationException();
        var links = Links.Select(e => forward ? e : (From: e.To, To: e.From)).ToArray();
        string[] inputs = forward ? ["A", "B"] : ["E"];
        string[] outputs = forward ? ["E"] : ["A", "B"];
        var children = new Dictionary<string, Boundary>();
        foreach (string name in Names)
            children[name] = BuildLeaf(path + "/" + name, new(
                links.Count(e => e.To == name) + inputs.Count(n => n == name),
                links.Count(e => e.From == name) + outputs.Count(n => n == name)));
        var incoming = Names.ToDictionary(n => n, _ => 0);
        var outgoing = Names.ToDictionary(n => n, _ => 0);
        // Fixed template order, never descendant ID sorting, defines external edge identity.
        foreach (var (from, to) in links)
            edges.Add(new(children[from].Outputs[outgoing[from]++], children[to].Inputs[incoming[to]++]));
        var boundary = new Boundary(Array.AsReadOnly(inputs.Select(n => children[n].Inputs[incoming[n]++]).ToArray()),
            Array.AsReadOnly(outputs.Select(n => children[n].Outputs[outgoing[n]++]).ToArray()));
        signatures[path] = degree;
        return boundaries[path] = boundary;
    }
}
