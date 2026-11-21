using MathGraphs;

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
    private MathGraph<Vertex, EdgePorts> topology = new();
    private Dictionary<string, MathGraphElement<Vertex, EdgePorts>> elementsById = new(StringComparer.Ordinal);
    private IReadOnlyList<Vertex> vertices = [];
    private IReadOnlyList<Edge> edges = [];
    private int nextEdgeIndex;
    private readonly record struct EdgePorts(int SourceIndex, int TargetIndex, int ViewIndex);
    private Dictionary<string, Degree> signatures = [];
    private Dictionary<string, Boundary> boundaries = [];
    private static readonly string[] Names = ["A", "B", "C", "D", "E"];
    private static readonly (string From, string To)[] Links =
        [("A", "C"), ("A", "D"), ("B", "C"), ("B", "D"), ("C", "E"), ("D", "E")];

    /// <summary>Creates the forward base graph with S0, S1 and T boundaries.</summary>
    public ReversibleGraph() => Rebuild();
    /// <summary>Actual leaves and the three external vertices; containers are excluded.</summary>
    public IReadOnlyList<Vertex> Vertices => vertices;
    /// <summary>Actual directed port edges.</summary>
    public IReadOnlyList<Edge> Edges => edges;
    /// <summary>Stable expanded container paths.</summary>
    public IReadOnlyList<string> ExpandedPaths => expanded.Order(StringComparer.Ordinal).ToArray();
    /// <summary>Number of real leaves.</summary>
    public int RealVertexCount => topology.ElementList.Count - 3;
    /// <summary>Maximum replacement depth.</summary>
    public int Depth => expanded.Count == 0 ? 0 : expanded.Max(p => p.Count(c => c == '/'));

    /// <summary>Reads a current vertex's actual incident degree.</summary>
    public Degree GetDegree(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!elementsById.TryGetValue(path, out var element)) throw new ArgumentException(null, nameof(path));
        return new(element.InEdgeList.Count, element.OutEdgeList.Count);
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
        if (!elementsById.TryGetValue(path, out var element) || element.Value.IsBoundary) throw new ArgumentException(null, nameof(path));
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
        expanded = candidate.expanded;
        topology = candidate.topology;
        elementsById = candidate.elementsById;
        vertices = candidate.vertices; edges = candidate.edges;
        signatures = candidate.signatures; boundaries = candidate.boundaries;
    }
    private void Rebuild()
    {
        topology = new();
        elementsById = new(StringComparer.Ordinal);
        nextEdgeIndex = 0;
        signatures = []; boundaries = [];
        foreach (string id in new[] { "S0", "S1", "T" }) AddVertex(new(id, true, 0));
        var root = BuildTemplate("root", new(2, 1));
        AddEdge(new("S0", 0), root.Inputs[0]);
        AddEdge(new("S1", 0), root.Inputs[1]);
        AddEdge(root.Outputs[0], new("T", 0));

        vertices = Array.AsReadOnly(topology.ElementList.Select(element => element.Value).ToArray());
        // Preserve template insertion order for the view, independently of adjacency order.
        var edgeView = new Edge[nextEdgeIndex];
        foreach (var element in topology.ElementList)
        {
            foreach (var edge in element.OutEdgeList)
            {
                var ports = edge.EdgeInfo;
                edgeView[ports.ViewIndex] = new(new(edge.From.Id, ports.SourceIndex), new(edge.To.Id, ports.TargetIndex));
            }
        }
        edges = Array.AsReadOnly(edgeView);
    }
    private void AddVertex(Vertex vertex) =>
        elementsById.Add(vertex.Id, topology.CreateAndAddElement(vertex, vertex.Id));

    private void AddEdge(Port source, Port target) =>
        topology.AddEdge(elementsById[source.VertexId], elementsById[target.VertexId],
            new EdgePorts(source.Index, target.Index, nextEdgeIndex++));

    private Boundary BuildLeaf(string path, Degree degree)
    {
        signatures[path] = degree;
        if (expanded.Contains(path)) return BuildTemplate(path, degree);
        AddVertex(new(path, false, path.Count(c => c == '/') - 1));
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
            AddEdge(children[from].Outputs[outgoing[from]++], children[to].Inputs[incoming[to]++]);
        var boundary = new Boundary(Array.AsReadOnly(inputs.Select(n => children[n].Inputs[incoming[n]++]).ToArray()),
            Array.AsReadOnly(outputs.Select(n => children[n].Outputs[outgoing[n]++]).ToArray()));
        signatures[path] = degree;
        return boundaries[path] = boundary;
    }
}
