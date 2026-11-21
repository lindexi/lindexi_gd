namespace MathGraph.StructureSelection;

/// <summary>A real vertex's ordered incoming and outgoing arity.</summary>
public sealed record Leaf(string Id, int Inputs, int Outputs);
/// <summary>An ordered port on a real leaf or external boundary.</summary>
public sealed record Port(string Vertex, int Index);
/// <summary>A directed connection between actual ports, not containers.</summary>
public sealed record Edge(Port From, Port To);

/// <summary>An immutable FullCross graph with arity-preserving five-point substitutions.</summary>
public sealed class FivePointGraph
{
    private const string Names = "ABCDE";
    private static readonly (char From, char To)[] Links =
        [('A', 'C'), ('A', 'D'), ('B', 'C'), ('B', 'D'), ('C', 'E'), ('D', 'E')];

    private FivePointGraph(IEnumerable<Leaf> leaves, IEnumerable<Edge> edges)
    {
        Leaves = Array.AsReadOnly(leaves.OrderBy(l => l.Id, StringComparer.Ordinal).ToArray());
        Edges = Array.AsReadOnly(edges.ToArray());
    }

    /// <summary>All actual computational vertices in stable ordinal order.</summary>
    public IReadOnlyList<Leaf> Leaves { get; }
    /// <summary>Actual directed edges, including the three external connections.</summary>
    public IReadOnlyList<Edge> Edges { get; }

    /// <summary>Creates the positive root with S0, S1 and T external boundaries.</summary>
    public static FivePointGraph Create()
    {
        var root = Template("", true);
        return new(root.Leaves, root.Edges.Concat([
            new(new("S0", 0), root.Inputs[0]), new(new("S1", 0), root.Inputs[1]), new(root.Outputs[0], new("T", 0))]));
    }

    /// <summary>Replaces one actual leaf and reconnects its boundary by port number.</summary>
    public FivePointGraph Expand(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var leaf = Leaves.SingleOrDefault(l => l.Id == id) ?? throw new ArgumentException(null, nameof(id));
        var module = Template(id + "/", leaf.Inputs == 2);
        return new(Leaves.Where(l => l.Id != id).Concat(module.Leaves),
            Edges.Select(e => new Edge(e.From.Vertex == id ? module.Outputs[e.From.Index] : e.From,
                e.To.Vertex == id ? module.Inputs[e.To.Index] : e.To)).Concat(module.Edges));
    }

    private static (Leaf[] Leaves, Edge[] Edges, Port[] Inputs, Port[] Outputs) Template(string prefix, bool forward)
    {
        var incoming = new int[Names.Length];
        var outgoing = new int[Names.Length];
        Port Input(char c) => new(prefix + c, incoming[c - 'A']++);
        Port Output(char c) => new(prefix + c, outgoing[c - 'A']++);
        // Counting internal edges first makes each ordered boundary independent of expansion order.
        var edges = Links.Select(e => forward ? new Edge(Output(e.From), Input(e.To)) :
            new Edge(Output(e.To), Input(e.From))).ToArray();
        var inputs = (forward ? "AB" : "E").Select(Input).ToArray();
        var outputs = (forward ? "E" : "AB").Select(Output).ToArray();
        return (Names.Select(c => new Leaf(prefix + c, incoming[c - 'A'], outgoing[c - 'A'])).ToArray(), edges, inputs, outputs);
    }
}
