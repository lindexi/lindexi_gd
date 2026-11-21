namespace MathGraph.GdIfs.Wpf.Model;

/// <summary>A UI-independent Cartesian point.</summary>
public readonly record struct GraphPoint(double X, double Y);

/// <summary>An edge between two local vertex indices; containers are not vertices.</summary>
public readonly record struct CycleEdge(int From, int To);

/// <summary>A C5 at one level of the containment hierarchy.</summary>
public sealed record CycleRing(int Level, GraphPoint Center, double Radius, IReadOnlyList<GraphPoint> Nodes);

/// <summary>A finite, single-branch graph-directed similarity model with no inter-level edges.</summary>
public sealed class CycleModel
{
    private CycleModel(int selectedNode, double contraction, IReadOnlyList<CycleRing> rings)
    {
        SelectedNode = selectedNode;
        Contraction = contraction;
        Rings = rings;
    }

    /// <summary>The selected local vertex index, repeated at every level.</summary>
    public int SelectedNode { get; }
    /// <summary>The similarity ratio between successive rings.</summary>
    public double Contraction { get; }
    /// <summary>All rings, ordered from the outermost to the innermost.</summary>
    public IReadOnlyList<CycleRing> Rings { get; }
    /// <summary>The five edges of every ring, including the closing edge.</summary>
    public static IReadOnlyList<CycleEdge> Edges { get; } = Array.AsReadOnly(new[]
    {
        new CycleEdge(0, 1), new CycleEdge(1, 2), new CycleEdge(2, 3),
        new CycleEdge(3, 4), new CycleEdge(4, 0)
    });
    /// <summary>The number of vertices, counting levels independently.</summary>
    public int NodeCount => Rings.Count * 5;
    /// <summary>The number of edges; containment adds no edges.</summary>
    public int EdgeCount => Rings.Count * 5;

    /// <summary>Creates depth + 1 rings. Each child is centered on the selected parent vertex.</summary>
    public static CycleModel Create(int selectedNode, int depth, double contraction)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(selectedNode);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(selectedNode, 4);
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(depth, 8);
        if (!double.IsFinite(contraction) || contraction < 0.12 || contraction > 0.36)
            throw new ArgumentOutOfRangeException(nameof(contraction));

        var rings = new List<CycleRing>(depth + 1);
        var center = new GraphPoint(0, 0);
        double radius = 1;
        for (int level = 0; level <= depth; level++)
        {
            var nodes = new GraphPoint[5];
            for (int index = 0; index < nodes.Length; index++)
            {
                double angle = -Math.PI / 2 + index * Math.Tau / 5;
                nodes[index] = new GraphPoint(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
            }
            rings.Add(new CycleRing(level, center, radius, Array.AsReadOnly(nodes)));
            center = nodes[selectedNode];
            radius *= contraction;
        }
        return new CycleModel(selectedNode, contraction, rings.AsReadOnly());
    }
}
