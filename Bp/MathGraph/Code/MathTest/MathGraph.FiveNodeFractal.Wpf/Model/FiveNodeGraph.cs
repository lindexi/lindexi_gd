namespace MathGraph.FiveNodeFractal.Wpf.Model;

/// <summary>Three illustrative, non-exhaustive five-node connection patterns.</summary>
public enum Prototype { FullCross, SingleCross, Parallel }

/// <summary>A real leaf or one of the three external boundary vertices.</summary>
public sealed record Vertex(string Id, bool IsBoundary, bool IsAdapter, int Depth);

/// <summary>A stable, ordered port on an actual leaf vertex.</summary>
public sealed record Port(string VertexId, int Index);

/// <summary>A directed connection between explicit ordered ports.</summary>
public sealed record Edge(Port Source, Port Target);

/// <summary>The actual incoming and outgoing arity.</summary>
public readonly record struct Degree(int Incoming, int Outgoing);

/// <summary>Ordered boundary references resolved through every nested replacement.</summary>
public sealed record Boundary(IReadOnlyList<Port> Inputs, IReadOnlyList<Port> Outputs);

/// <summary>A UI-independent finite, port-preserving five-node substitution graph.</summary>
public sealed class FiveNodeGraph
{
    /// <summary>Default resource budget for whole-graph generation only; manual expansion is not limited.</summary>
    public const int DefaultExpansionBudget = 1000;
    private HashSet<string> expanded = new(StringComparer.Ordinal);
    private List<Vertex> vertices = [];
    private List<Edge> edges = [];
    private Dictionary<string, Degree> signatures = [];
    private Dictionary<string, Boundary> boundaries = [];

    /// <summary>Creates an unexpanded prototype with independent S0 and S1 inputs.</summary>
    public FiveNodeGraph(Prototype prototype = Prototype.FullCross)
    {
        if (!Enum.IsDefined(prototype)) throw new ArgumentOutOfRangeException(nameof(prototype));
        Prototype = prototype;
        Rebuild();
    }

    /// <summary>The immutable prototype used by all replacements in this graph.</summary>
    public Prototype Prototype { get; }
    /// <summary>Actual vertices, including S0, S1 and T.</summary>
    public IReadOnlyList<Vertex> Vertices => vertices.AsReadOnly();
    /// <summary>Actual edges; intersections do not create vertices.</summary>
    public IReadOnlyList<Edge> Edges => edges.AsReadOnly();
    /// <summary>Stable paths of expanded containers, which are not vertices.</summary>
    public IReadOnlyList<string> ExpandedPaths => expanded.Order(StringComparer.Ordinal).ToArray();
    /// <summary>Number of real leaves, including all adapters.</summary>
    public int RealVertexCount => vertices.Count - 3;
    /// <summary>Maximum replacement depth, excluding visualization groups.</summary>
    public int Depth => expanded.Count == 0 ? 0 : expanded.Max(PathDepth);

    /// <summary>Gets a real leaf's actual degree from its incident edges.</summary>
    public Degree GetDegree(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!vertices.Any(v => v.Id == path)) throw new ArgumentException(null, nameof(path));
        return new(edges.Count(e => e.Target.VertexId == path), edges.Count(e => e.Source.VertexId == path));
    }

    /// <summary>Gets a current leaf or expanded module's preserved port signature.</summary>
    public Degree GetSignature(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return signatures.TryGetValue(path, out var degree) ? degree : throw new ArgumentException(null, nameof(path));
    }

    /// <summary>Resolves ordered module or leaf ports to actual terminal vertices.</summary>
    public Boundary GetBoundary(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return boundaries.TryGetValue(path, out var ports) ? ports : throw new ArgumentException(null, nameof(path));
    }

    /// <summary>Atomically replaces one real leaf, including adapter leaves.</summary>
    public void Expand(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!vertices.Any(v => v.Id == path && !v.IsBoundary)) throw new ArgumentException(null, nameof(path));
        var candidate = new HashSet<string>(expanded, StringComparer.Ordinal) { path };
        Commit(candidate);
    }

    /// <summary>Restores a module's original vertex and removes its entire expanded subtree.</summary>
    public void Collapse(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!expanded.Contains(path)) throw new ArgumentException(null, nameof(path));
        Commit(expanded.Where(p => p != path && !p.StartsWith(path + "/", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal));
    }

    /// <summary>Restores the base five-node prototype.</summary>
    public void CollapseAll() => Commit(new(StringComparer.Ordinal));

    /// <summary>Atomically generates all leaves from the base graph within an explicit expansion budget, checked before construction.</summary>
    public void Generate(int depth, int expansionBudget = DefaultExpansionBudget)
    {
        if (depth < 0) throw new ArgumentOutOfRangeException(nameof(depth));
        if (expansionBudget < 0) throw new ArgumentOutOfRangeException(nameof(expansionBudget));
        long leaves = 5;
        long required = 0;
        for (int level = 0; level < depth; level++)
        {
            required += leaves;
            if (required > expansionBudget) throw new InvalidOperationException();
            // All three prototypes have the same aggregate leaf growth, including adapters.
            leaves = 6 * leaves - 1;
        }
        var candidate = new FiveNodeGraph(Prototype);
        for (int level = 0; level < depth; level++)
        {
            candidate.expanded.UnionWith(candidate.vertices.Where(v => !v.IsBoundary).Select(v => v.Id));
            candidate.Rebuild();
        }
        Adopt(candidate);
    }

    private static int PathDepth(string path) => path.Count(c => c == '/');

    private void Commit(HashSet<string> paths)
    {
        var candidate = new FiveNodeGraph(Prototype) { expanded = paths };
        candidate.Rebuild();
        Adopt(candidate);
    }

    private void Adopt(FiveNodeGraph candidate)
    {
        expanded = candidate.expanded;
        vertices = candidate.vertices;
        edges = candidate.edges;
        signatures = candidate.signatures;
        boundaries = candidate.boundaries;
    }

    private List<(string From, string To)> CoreEdges()
    {
        List<(string, string)> result = [("A", "C"), ("B", "D"), ("C", "E"), ("D", "E")];
        if (Prototype != Prototype.Parallel) result.Insert(1, ("A", "D"));
        if (Prototype == Prototype.FullCross) result.Insert(3, ("B", "C"));
        return result;
    }

    private void Rebuild()
    {
        vertices = [];
        edges = [];
        signatures = [];
        boundaries = [];
        foreach (var id in new[] { "S0", "S1", "T" }) vertices.Add(new(id, true, false, 0));
        var root = BuildTemplate("root", new(2, 1), false);
        edges.Add(new(new("S0", 0), root.Inputs[0]));
        edges.Add(new(new("S1", 0), root.Inputs[1]));
        edges.Add(new(root.Outputs[0], new("T", 0)));
    }

    private Boundary BuildLeaf(string path, Degree degree, bool adapter)
    {
        signatures[path] = degree;
        if (expanded.Contains(path)) return BuildTemplate(path, degree, true);
        vertices.Add(new(path, false, adapter, PathDepth(path) - 1));
        var ports = new Boundary(Array.AsReadOnly(Enumerable.Range(0, degree.Incoming).Select(i => new Port(path, i)).ToArray()),
            Array.AsReadOnly(Enumerable.Range(0, degree.Outgoing).Select(i => new Port(path, i)).ToArray()));
        boundaries[path] = ports;
        return ports;
    }

    private Boundary BuildTemplate(string path, Degree degree, bool replacement)
    {
        var links = CoreEdges();
        var names = new List<string> { "A", "B", "C", "D", "E" };
        string[] inputs;
        string[] outputs;
        if (replacement && degree.Incoming == 1)
        {
            names.Add("I");
            links.InsertRange(0, [("I", "A"), ("I", "B")]);
            inputs = ["I"];
        }
        else inputs = ["A", "B"];
        if (replacement && degree.Outgoing == 2)
        {
            names.Add("O");
            links.Add(("E", "O"));
            outputs = ["O", "O"];
        }
        else outputs = ["E"];
        var children = new Dictionary<string, Boundary>();
        foreach (string name in names)
        {
            var signature = new Degree(links.Count(e => e.To == name) + inputs.Count(n => n == name),
                links.Count(e => e.From == name) + outputs.Count(n => n == name));
            children[name] = BuildLeaf(path + "/" + name, signature, name is "I" or "O");
        }
        var inIndex = names.ToDictionary(n => n, _ => 0);
        var outIndex = names.ToDictionary(n => n, _ => 0);
        // Template edge order fixes port identity; current leaf IDs never reorder it.
        foreach (var (from, to) in links)
            edges.Add(new(children[from].Outputs[outIndex[from]++], children[to].Inputs[inIndex[to]++]));
        var boundary = new Boundary(Array.AsReadOnly(inputs.Select(n => children[n].Inputs[inIndex[n]++]).ToArray()),
            Array.AsReadOnly(outputs.Select(n => children[n].Outputs[outIndex[n]++]).ToArray()));
        boundaries[path] = boundary;
        signatures[path] = degree;
        return boundary;
    }
}
