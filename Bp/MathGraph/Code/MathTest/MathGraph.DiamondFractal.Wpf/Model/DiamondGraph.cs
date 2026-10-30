namespace MathGraph.DiamondFractal.Wpf.Model;

/// <summary>A vertex in the actual directed graph; containers are not vertices.</summary>
public sealed record Vertex(string Id, bool IsBoundary, bool CanExpand);

/// <summary>A directed edge between actual vertices.</summary>
public sealed record Edge(string From, string To);

/// <summary>Incoming and outgoing edge counts.</summary>
public readonly record struct Degree(int Incoming, int Outgoing);

/// <summary>A finite diamond substitution graph with stable path identifiers.</summary>
public sealed class DiamondGraph
{
    /// <summary>Maximum supported nesting depth.</summary>
    public const int MaximumDepth = 6;
    /// <summary>Hard limit on simultaneously expanded modules.</summary>
    public const int MaximumExpansions = 126;
    /// <summary>Hard limit on actual vertices, excluding the two boundary vertices.</summary>
    public const int MaximumVertices = 382;

    private readonly HashSet<string> expanded = new(StringComparer.Ordinal);
    private readonly List<Vertex> vertices = [];
    private readonly List<Edge> edges = [];

    /// <summary>Creates the unexpanded four-vertex diamond.</summary>
    public DiamondGraph() => Rebuild();

    /// <summary>Actual vertices, including the two external boundary vertices.</summary>
    public IReadOnlyList<Vertex> Vertices => vertices.AsReadOnly();
    /// <summary>Actual edges, including both external boundary edges.</summary>
    public IReadOnlyList<Edge> Edges => edges.AsReadOnly();
    /// <summary>Visual modules; these identifiers do not occur as actual vertices.</summary>
    public IReadOnlyCollection<string> ExpandedPaths => expanded.Order(StringComparer.Ordinal).ToArray();
    /// <summary>Number of actual non-boundary vertices.</summary>
    public int RealVertexCount => vertices.Count - 2;
    /// <summary>Current maximum expanded depth.</summary>
    public int Depth => expanded.Count == 0 ? 0 : expanded.Max(PathDepth);

    /// <summary>Creates the reference image: only the root upper vertex is expanded.</summary>
    public static DiamondGraph CreateDefault()
    {
        var graph = new DiamondGraph();
        graph.Expand("root/U");
        return graph;
    }

    /// <summary>Replaces a leaf middle vertex with a connected diamond.</summary>
    public void Expand(string path)
    {
        ValidatePath(path);
        if (!vertices.Any(v => v.Id == path && v.CanExpand))
            throw new ArgumentException("The path must identify an existing middle leaf.", nameof(path));
        if (PathDepth(path) > MaximumDepth)
            throw new ArgumentOutOfRangeException(nameof(path));
        if (expanded.Count >= MaximumExpansions || RealVertexCount + 3 > MaximumVertices)
            throw new InvalidOperationException("The graph budget would be exceeded.");
        expanded.Add(path);
        Rebuild();
    }

    /// <summary>Collapses a module and all its descendants, reconnecting the parent edges.</summary>
    public void Collapse(string path)
    {
        ValidatePath(path);
        if (!expanded.Contains(path))
            throw new ArgumentException("The path must identify an expanded module.", nameof(path));
        expanded.RemoveWhere(p => p == path || p.StartsWith(path + "/", StringComparison.Ordinal));
        Rebuild();
    }

    /// <summary>Removes all expansions.</summary>
    public void CollapseAll()
    {
        expanded.Clear();
        Rebuild();
    }

    /// <summary>Generates a finite approximation, validating the full budget before mutation.</summary>
    public void Generate(int depth, bool bothBranches, int expansionBudget = MaximumExpansions)
    {
        if (depth < 0 || depth > MaximumDepth) throw new ArgumentOutOfRangeException(nameof(depth));
        if (expansionBudget < 0 || expansionBudget > MaximumExpansions)
            throw new ArgumentOutOfRangeException(nameof(expansionBudget));
        int count = bothBranches ? (1 << (depth + 1)) - 2 : depth;
        if (count > expansionBudget || 4 + 3 * count > MaximumVertices)
            throw new InvalidOperationException("The graph budget would be exceeded.");
        expanded.Clear();
        Add("root", depth);
        Rebuild();

        void Add(string module, int remaining)
        {
            if (remaining == 0) return;
            expanded.Add(module + "/U");
            Add(module + "/U", remaining - 1);
            if (!bothBranches) return;
            expanded.Add(module + "/D");
            Add(module + "/D", remaining - 1);
        }
    }

    /// <summary>Returns the true degree of a vertex.</summary>
    public Degree GetDegree(string id)
    {
        if (!vertices.Any(v => v.Id == id)) throw new ArgumentException("Unknown vertex.", nameof(id));
        return new(edges.Count(e => e.To == id), edges.Count(e => e.From == id));
    }

    /// <summary>Counts only edges crossing a module boundary, not internal vertex degrees.</summary>
    public Degree GetModuleDegree(string path)
    {
        if (!expanded.Contains(path)) throw new ArgumentException("Unknown module.", nameof(path));
        bool Inside(string id) => id.StartsWith(path + "/", StringComparison.Ordinal);
        return new(edges.Count(e => !Inside(e.From) && Inside(e.To)),
            edges.Count(e => Inside(e.From) && !Inside(e.To)));
    }

    private static int PathDepth(string path) => path.Count(c => c == '/');

    private static void ValidatePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var parts = path.Split('/');
        if (parts.Length < 2 || parts[0] != "root" || parts.Skip(1).Any(p => p is not ("U" or "D")))
            throw new ArgumentException("Invalid middle-vertex path.", nameof(path));
    }

    private void Rebuild()
    {
        vertices.Clear();
        edges.Clear();
        vertices.Add(new("S", true, false));
        vertices.Add(new("T", true, false));
        var root = Build("root");
        edges.Add(new("S", root.Left));
        edges.Add(new(root.Right, "T"));
    }

    private (string Left, string Right) Build(string module)
    {
        string left = module + "/L", right = module + "/R";
        vertices.Add(new(left, false, false));
        vertices.Add(new(right, false, false));
        foreach (string branch in new[] { "U", "D" })
        {
            string path = module + "/" + branch;
            if (expanded.Contains(path))
            {
                var child = Build(path);
                edges.Add(new(left, child.Left));
                edges.Add(new(child.Right, right));
            }
            else
            {
                vertices.Add(new(path, false, true));
                edges.Add(new(left, path));
                edges.Add(new(path, right));
            }
        }
        return (left, right);
    }
}
