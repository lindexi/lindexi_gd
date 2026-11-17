using MathGraphs;

namespace FractalForge.Core;

public enum ForgeRole { Compute, InputBoundary, OutputBoundary }
public enum ForgeActivation { Tanh, Identity, Relu }
public sealed record ForgeNode(ForgeRole Role, ForgeActivation Activation = ForgeActivation.Tanh);
public sealed record ForgeLink(Guid Id);
public sealed record ForgeRoute(Guid Id, string[] Path, Guid ParameterId, double InitialValue, int Delay = 0);
public sealed record ForgeBias(Guid ParameterId, double InitialValue = 0);
public sealed record FourPointUnit(string Target, string Input, string A, string B, string Output, string Kind);
public sealed record ExpandedConnection(Guid Id, string From, string To, Guid ParameterId, double InitialValue, int Delay);

/// <summary>逻辑 MathGraph、显式逐来源边界路径和参数归属；编译不在透明点求和。</summary>
public sealed class ForgeTopology
{
    public const string Semantics = "fractalforge.explicit-routes.v1";
    public MathGraph<ForgeNode, ForgeLink> Graph { get; }
    public IReadOnlyList<ForgeRoute> Routes { get; }
    public IReadOnlyDictionary<string, ForgeBias> Biases { get; }
    public IReadOnlyList<FourPointUnit> Units { get; }

    /// <summary>从节点及完整信号路径建立逻辑图；每条路径只有首尾允许计算。</summary>
    public ForgeTopology(IReadOnlyDictionary<string, ForgeNode> nodes, IReadOnlyDictionary<string, ForgeBias> biases,
        IEnumerable<ForgeRoute> routes, IEnumerable<FourPointUnit>? units = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(biases);
        ArgumentNullException.ThrowIfNull(routes);
        Graph = new();
        var map = nodes.ToDictionary(p => p.Key, p => Graph.CreateAndAddElement(p.Value, p.Key));
        Biases = new System.Collections.ObjectModel.ReadOnlyDictionary<string, ForgeBias>(new Dictionary<string, ForgeBias>(biases));
        Routes = Array.AsReadOnly(routes.Select(r => r with { Path = r.Path.ToArray() }).ToArray());
        Units = Array.AsReadOnly((units ?? []).ToArray());
        var links = new HashSet<(string, string)>();
        foreach (var route in Routes)
        {
            if (route.Path.Length < 2) throw new ArgumentException(nameof(routes));
            for (var i = 1; i < route.Path.Length; i++)
            {
                if (!map.ContainsKey(route.Path[i - 1]) || !map.ContainsKey(route.Path[i])) throw new ArgumentException(nameof(routes));
                if (links.Add((route.Path[i - 1], route.Path[i])))
                    Graph.AddEdge(map[route.Path[i - 1]], map[route.Path[i]], new ForgeLink(Guid.NewGuid()));
            }
        }
        _ = Expand();
    }

    /// <summary>验证全部来源路径，消去透明边界；多来源保留为独立连接及独立梯度归属。</summary>
    public IReadOnlyList<ExpandedConnection> Expand()
    {
        var nodes = Graph.ElementList.ToDictionary(n => n.Id, n => n.Value);
        if (nodes.Count == 0 || nodes.Any(p => string.IsNullOrWhiteSpace(p.Key) || !Enum.IsDefined(p.Value.Role) || !Enum.IsDefined(p.Value.Activation)))
            throw new InvalidDataException();
        if (!nodes.Where(p => p.Value.Role == ForgeRole.Compute).Select(p => p.Key).ToHashSet().SetEquals(Biases.Keys)) throw new InvalidDataException();
        var ids = new HashSet<Guid>();
        var parameters = new Dictionary<Guid, double>();
        foreach (var bias in Biases.Values) Register(bias.ParameterId, bias.InitialValue);
        var result = new List<ExpandedConnection>();
        foreach (var route in Routes)
        {
            if (route.Id == Guid.Empty || !ids.Add(route.Id) || route.Delay < 0 || route.Path.Length < 2 || route.Path.Any(id => !nodes.ContainsKey(id))) throw new InvalidDataException();
            if (nodes[route.Path[0]].Role != ForgeRole.Compute || nodes[route.Path[^1]].Role != ForgeRole.Compute ||
                route.Path.Skip(1).SkipLast(1).Any(id => nodes[id].Role == ForgeRole.Compute)) throw new InvalidDataException();
            for (var i = 1; i < route.Path.Length; i++)
                if (!Graph.ElementList.Single(n => n.Id == route.Path[i - 1]).OutEdgeList.Any(e => e.To.Id == route.Path[i])) throw new InvalidDataException();
            Register(route.ParameterId, route.InitialValue);
            result.Add(new(route.Id, route.Path[0], route.Path[^1], route.ParameterId, route.InitialValue, route.Delay));
        }
        var expectedLinks = Routes.SelectMany(r => r.Path.Zip(r.Path.Skip(1))).ToHashSet();
        var actualLinks = Graph.ElementList.SelectMany(n => n.OutEdgeList).Select(e => (e.From.Id, e.To.Id)).ToArray();
        if (actualLinks.Length != expectedLinks.Count || !expectedLinks.SetEquals(actualLinks)) throw new InvalidDataException();
        foreach (var unit in Units)
        {
            if (unit.Kind is not ("clone-widen" or "output-deepen") ||
                !nodes.TryGetValue(unit.Input, out var input) || input.Role != ForgeRole.InputBoundary ||
                !nodes.TryGetValue(unit.Output, out var output) || output.Role != ForgeRole.OutputBoundary ||
                unit.A == unit.B || string.IsNullOrWhiteSpace(unit.Target)) throw new InvalidDataException();
            if ((!nodes.ContainsKey(unit.A) && !Units.Any(u => u.Target == unit.A)) ||
                (!nodes.ContainsKey(unit.B) && !Units.Any(u => u.Target == unit.B))) throw new InvalidDataException();
        }
        return result.AsReadOnly();
        void Register(Guid id, double value)
        {
            if (id == Guid.Empty || !double.IsFinite(value) || (parameters.TryGetValue(id, out var previous) && previous != value)) throw new InvalidDataException();
            parameters[id] = value;
        }
    }

    /// <summary>创建真正的四计算点 I/A/B/O 基线；反馈是独立显式延迟边。</summary>
    public static ForgeTopology CreateDiamond(int seed = 0, bool feedback = true)
    {
        var nodes = new[] { "I", "A", "B", "O" }.ToDictionary(id => id, _ => new ForgeNode(ForgeRole.Compute));
        var biases = nodes.Keys.ToDictionary(id => id, _ => new ForgeBias(Guid.NewGuid()));
        var random = new Random(seed);
        var routes = new List<ForgeRoute>();
        foreach (var (from, to) in new[] { ("I", "A"), ("I", "B"), ("A", "O"), ("B", "O") })
            routes.Add(new(Guid.NewGuid(), [from, to], Guid.NewGuid(), random.NextDouble() - .5));
        if (feedback) routes.Add(new(Guid.NewGuid(), ["O", "I"], Guid.NewGuid(), random.NextDouble() - .5, 1));
        return new(nodes, biases, routes);
    }
}
