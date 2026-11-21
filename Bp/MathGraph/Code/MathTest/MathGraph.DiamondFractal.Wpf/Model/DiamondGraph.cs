namespace MathGraph.DiamondFractal.Wpf.Model;

/// <summary>Port signatures of real vertices; Boundary identifies external vertices.</summary>
public enum NodeKind { Boundary, Transfer, Split, Merge }

/// <summary>Recursive generation policy.</summary>
public enum GenerationMode { Upper, BothBranches, AllTypes }

/// <summary>A vertex in the actual directed graph; containers are not vertices.</summary>
public sealed record Vertex(string Id, bool IsBoundary, bool CanExpand)
{
    /// <summary>Ordered input/output port signature.</summary>
    public NodeKind Kind { get; init; } = NodeKind.Transfer;
}

/// <summary>A directed edge between actual vertices.</summary>
public sealed record Edge(string From, string To);

/// <summary>Incoming and outgoing edge counts.</summary>
public readonly record struct Degree(int Incoming, int Outgoing);

/// <summary>A finite port-preserving substitution graph with stable path identifiers.</summary>
public sealed class DiamondGraph
{
    /// <summary>Maximum supported nesting depth.</summary>
    public const int MaximumDepth = 6;
    /// <summary>Hard limit on simultaneously expanded modules.</summary>
    public const int MaximumExpansions = 126;
    /// <summary>Hard limit on actual non-boundary vertices.</summary>
    public const int MaximumVertices = 382;
    private readonly HashSet<string> expanded = new(StringComparer.Ordinal);
    private readonly List<Vertex> vertices = [];
    private readonly List<Edge> edges = [];
    private sealed record Ports(string[] Inputs, string[] Outputs);

    /// <summary>Creates the unexpanded four-vertex diamond.</summary>
    public DiamondGraph() => Rebuild();
    /// <summary>Actual vertices, including external boundary vertices.</summary>
    public IReadOnlyList<Vertex> Vertices => vertices.AsReadOnly();
    /// <summary>Actual directed edges.</summary>
    public IReadOnlyList<Edge> Edges => edges.AsReadOnly();
    /// <summary>Expanded containers, not actual vertices.</summary>
    public IReadOnlyCollection<string> ExpandedPaths => expanded.Order(StringComparer.Ordinal).ToArray();
    /// <summary>Number of actual non-boundary vertices.</summary>
    public int RealVertexCount => vertices.Count - 2;
    /// <summary>Maximum replacement depth; fixed template groups do not add levels.</summary>
    public int Depth => expanded.Count == 0 ? 0 : expanded.Max(PathDepth);

    /// <summary>Creates the reference image with only root/U expanded.</summary>
    public static DiamondGraph CreateDefault()
    {
        var graph = new DiamondGraph();
        graph.Expand("root/U");
        return graph;
    }

    /// <summary>Returns the type of a valid stable path, including collapsed descendants.</summary>
    public NodeKind GetKind(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return ResolvePath(path).Kind;
    }

    /// <summary>Replaces an existing real leaf, preserving ordered ports.</summary>
    public void Expand(string path)
    {
        var kind = GetKind(path);
        if (!vertices.Any(v => v.Id == path && v.CanExpand))
            throw new ArgumentException("The path must identify an existing real leaf.", nameof(path));
        if (PathDepth(path) > MaximumDepth) throw new ArgumentOutOfRangeException(nameof(path));
        if (expanded.Count >= MaximumExpansions || RealVertexCount + Growth(kind) > MaximumVertices)
            throw new InvalidOperationException("The graph budget would be exceeded.");
        expanded.Add(path);
        Rebuild();
    }

    /// <summary>Restores a replaced vertex and removes all descendant modules.</summary>
    public void Collapse(string path)
    {
        GetKind(path);
        if (!expanded.Contains(path)) throw new ArgumentException("Unknown module.", nameof(path));
        expanded.RemoveWhere(p => p == path || p.StartsWith(path + "/", StringComparison.Ordinal));
        Rebuild();
    }

    /// <summary>Restores the initial diamond.</summary>
    public void CollapseAll()
    {
        expanded.Clear();
        Rebuild();
    }

    /// <summary>Generates the original upper-only or two-middle-branch approximation.</summary>
    public void Generate(int depth, bool bothBranches, int expansionBudget = MaximumExpansions) =>
        Generate(depth, bothBranches ? GenerationMode.BothBranches : GenerationMode.Upper, expansionBudget);

    /// <summary>Generates a complete approximation atomically, rejecting rather than truncating over-budget requests.</summary>
    public void Generate(int depth, GenerationMode mode, int expansionBudget = MaximumExpansions)
    {
        if (depth < 0 || depth > MaximumDepth) throw new ArgumentOutOfRangeException(nameof(depth));
        if (expansionBudget < 0 || expansionBudget > MaximumExpansions) throw new ArgumentOutOfRangeException(nameof(expansionBudget));
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var candidate = new HashSet<string>(StringComparer.Ordinal);
        int realCount = 4;
        Add("root", NodeKind.Transfer, depth);
        expanded.Clear();
        expanded.UnionWith(candidate);
        Rebuild();

        void Add(string module, NodeKind kind, int remaining)
        {
            if (remaining == 0) return;
            foreach (var (name, childKind) in Children(kind))
            {
                if (mode != GenerationMode.AllTypes && name != "U" && !(name == "D" && mode == GenerationMode.BothBranches)) continue;
                string path = module + "/" + name;
                realCount += Growth(childKind);
                if (candidate.Count >= expansionBudget || realCount > MaximumVertices)
                    throw new InvalidOperationException("The graph budget would be exceeded.");
                candidate.Add(path);
                Add(path, childKind, remaining - 1);
            }
        }
    }

    /// <summary>Returns a real vertex's degree.</summary>
    public Degree GetDegree(string id)
    {
        if (!vertices.Any(v => v.Id == id)) throw new ArgumentException("Unknown vertex.", nameof(id));
        return new(edges.Count(e => e.To == id), edges.Count(e => e.From == id));
    }

    /// <summary>Counts only edges crossing a container boundary.</summary>
    public Degree GetModuleDegree(string path)
    {
        if (!expanded.Contains(path)) throw new ArgumentException("Unknown module.", nameof(path));
        bool Inside(string id) => id.StartsWith(path + "/", StringComparison.Ordinal);
        return new(edges.Count(e => !Inside(e.From) && Inside(e.To)), edges.Count(e => Inside(e.From) && !Inside(e.To)));
    }

    private static (NodeKind Kind, int Depth) ResolvePath(string path)
    {
        var parts = path.Split('/');
        if (parts.Length < 2 || parts[0] != "root") throw new ArgumentException("Invalid path.", nameof(path));
        var kind = NodeKind.Transfer;
        int depth = 0;
        for (int index = 1; index < parts.Length; index++)
        {
            string name = parts[index];
            if (kind is NodeKind.Split or NodeKind.Merge && name is "B" or "C")
            {
                if (++index >= parts.Length) throw new ArgumentException("Incomplete template path.", nameof(path));
                name += "/" + parts[index];
            }
            var child = Children(kind).FirstOrDefault(c => c.Name == name);
            if (child.Name is null) throw new ArgumentException("Invalid typed path.", nameof(path));
            kind = child.Kind;
            depth++;
        }
        return (kind, depth);
    }

    private static int PathDepth(string path) => ResolvePath(path).Depth;
    private static int Growth(NodeKind kind) => kind == NodeKind.Transfer ? 3 : 8;
    private static (string Name, NodeKind Kind)[] Children(NodeKind kind) => kind switch
    {
        NodeKind.Transfer => [("L", NodeKind.Split), ("U", NodeKind.Transfer), ("D", NodeKind.Transfer), ("R", NodeKind.Merge)],
        NodeKind.Split or NodeKind.Merge => [
            ("A", kind),
            ("B/L", NodeKind.Split), ("B/U", NodeKind.Transfer), ("B/D", NodeKind.Transfer), ("B/R", NodeKind.Merge),
            ("C/L", NodeKind.Split), ("C/U", NodeKind.Transfer), ("C/D", NodeKind.Transfer), ("C/R", NodeKind.Merge)],
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private void Rebuild()
    {
        vertices.Clear();
        edges.Clear();
        vertices.Add(new("S", true, false) { Kind = NodeKind.Boundary });
        vertices.Add(new("T", true, false) { Kind = NodeKind.Boundary });
        var root = BuildModule("root", NodeKind.Transfer);
        edges.Add(new("S", root.Inputs[0]));
        edges.Add(new(root.Outputs[0], "T"));
    }

    private Ports BuildLeaf(string path, NodeKind kind)
    {
        if (expanded.Contains(path)) return BuildModule(path, kind);
        vertices.Add(new(path, false, true) { Kind = kind });
        return new(Enumerable.Repeat(path, kind == NodeKind.Merge ? 2 : 1).ToArray(),
            Enumerable.Repeat(path, kind == NodeKind.Split ? 2 : 1).ToArray());
    }

    private Ports BuildModule(string path, NodeKind kind)
    {
        var children = Children(kind).ToDictionary(c => c.Name, c => BuildLeaf(path + "/" + c.Name, c.Kind));
        void Connect(string from, int output, string to, int input) =>
            edges.Add(new(children[from].Outputs[output], children[to].Inputs[input]));
        // Port indices belong to the template, never to sorted current endpoint IDs.
        if (kind == NodeKind.Transfer)
        {
            Connect("L", 0, "U", 0);
            Connect("L", 1, "D", 0);
            Connect("U", 0, "R", 0);
            Connect("D", 0, "R", 1);
            return new(children["L"].Inputs, children["R"].Outputs);
        }
        foreach (string group in new[] { "B", "C" })
        {
            Connect(group + "/L", 0, group + "/U", 0);
            Connect(group + "/L", 1, group + "/D", 0);
            Connect(group + "/U", 0, group + "/R", 0);
            Connect(group + "/D", 0, group + "/R", 1);
        }
        if (kind == NodeKind.Split)
        {
            Connect("A", 0, "B/L", 0);
            Connect("A", 1, "C/L", 0);
            return new(children["A"].Inputs, [children["B/R"].Outputs[0], children["C/R"].Outputs[0]]);
        }
        Connect("B/R", 0, "A", 0);
        Connect("C/R", 0, "A", 1);
        return new([children["B/L"].Inputs[0], children["C/L"].Inputs[0]], children["A"].Outputs);
    }
}
