using FractalForge.Core;
using MathGraphs;
using MathGraphs.Neural;

namespace FractalForge.Training;

public sealed record ForgeCost(int LogicalNodes, int ComputeNodes, int Connections, int Parameters, long HistoryCells, long WindowTapeCells);
public sealed record ForgeAction(string Kind, string Target, double Alpha = .5, string? Destination = null, int Delay = 1, double Weight = 0);

/// <summary>四点逻辑定义与普通 Neural 根模型的受控组合；所有结构动作返回独立候选。</summary>
public sealed class ForgeModel
{
    public ForgeTopology Topology { get; }
    public RecursiveNetwork Network { get; }
    private ForgeModel(ForgeTopology topology, RecursiveNetwork network) { Topology = topology; Network = network; }

    /// <summary>编译显式边界路径并检查实际 Neural 布局及预算。</summary>
    public static ForgeModel Create(ForgeTopology topology, NetworkBudget? budget = null) => Compile(topology, null, budget);

    internal static ForgeModel Restore(ForgeTopology topology, RecursiveNetwork network)
    {
        var edges = topology.Expand();
        var actual = network.Graph.ElementList.SelectMany(n => n.OutEdgeList).ToArray();
        if (network.Graph.ElementList.Any(n => n.Value.Child is not null) ||
            !network.Graph.ElementList.Select(n => n.Id).ToHashSet().SetEquals(topology.Biases.Keys) || actual.Length != edges.Count)
            throw new InvalidDataException();
        foreach (var node in network.Graph.ElementList)
        {
            var logical = topology.Graph.ElementList.Single(n => n.Id == node.Id);
            if (node.Value.Bias.Id != topology.Biases[node.Id].ParameterId || (int)node.Value.Activation != (int)logical.Value.Activation) throw new InvalidDataException();
        }
        foreach (var edge in edges)
            if (!actual.Any(e => e.EdgeInfo!.Id == edge.Id && e.From.Id == edge.From && e.To.Id == edge.To && e.EdgeInfo.Weight.Id == edge.ParameterId && e.EdgeInfo.Delay == edge.Delay)) throw new InvalidDataException();
        return new(topology, network);
    }

    private static ForgeModel Compile(ForgeTopology topology, RecursiveNetwork? source, NetworkBudget? budget = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        var edges = topology.Expand();
        var old = source?.Graph.ElementList.Select(n => n.Value.Bias).Concat(source.Graph.ElementList.SelectMany(n => n.OutEdgeList).Select(e => e.EdgeInfo!.Weight)).DistinctBy(p => p.Id).ToDictionary(p => p.Id) ?? [];
        var parameters = new Dictionary<Guid, ScalarParameter>();
        ScalarParameter Parameter(Guid id, double value)
        {
            if (!parameters.TryGetValue(id, out var parameter))
            {
                parameter = old.TryGetValue(id, out var previous) ? RecursiveNetwork.CopyParameterState(previous) : new ScalarParameter(value);
                // Fresh identities are assigned by the narrow bridge, not public mutable setters.
                if (!old.ContainsKey(id)) parameter = RecursiveNetwork.CreateRootParameter(id, value);
                parameters.Add(id, parameter);
            }
            return parameter;
        }
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var nodes = topology.Graph.ElementList.Where(n => n.Value.Role == ForgeRole.Compute).ToDictionary(n => n.Id,
            n => graph.CreateAndAddElement(new Neuron(Parameter(topology.Biases[n.Id].ParameterId, topology.Biases[n.Id].InitialValue), (ScalarActivation)n.Value.Activation), n.Id));
        foreach (var edge in edges) graph.AddEdge(nodes[edge.From], nodes[edge.To], RecursiveNetwork.CreateRootConnection(edge.Id, Parameter(edge.ParameterId, edge.InitialValue), edge.Delay));
        return new(topology, source is null ? new RecursiveNetwork(graph, budget) : source.RebuildRoot(graph));
    }

    /// <summary>报告真实展开连接、参数、历史与最大窗口 tape 成本（以标量单元计）。</summary>
    public ForgeCost MeasureCost()
    {
        var edges = Topology.Expand();
        var count = Topology.Biases.Count;
        var delay = edges.Select(e => e.Delay).DefaultIfEmpty().Max();
        return new(Topology.Graph.ElementList.Count, count, edges.Count,
            Topology.Biases.Values.Select(b => b.ParameterId).Concat(edges.Select(e => e.ParameterId)).Distinct().Count() + (Network.TextBindings?.Parameters.Count ?? 0),
            checked((long)count * 2 * delay), checked((long)count * 2 * Network.Budget.MaxSteps));
    }

    /// <summary>执行版本一动作：clone-widen、output-deepen、add-feedback；失败不修改源模型。</summary>
    public ForgeModel Apply(ForgeAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        // Capture a consistent source including optimizer state before constructing a candidate.
        var source = RecursiveNetwork.ImportCheckpoint(Network.ExportCheckpoint(), Network.Budget);
        var nodes = Topology.Graph.ElementList.ToDictionary(n => n.Id, n => n.Value);
        var biases = Topology.Biases.ToDictionary(p => p.Key, p => p.Value);
        var routes = Topology.Routes.ToList();
        var units = Topology.Units.ToList();
        var expanded = Topology.Expand();
        if (!nodes.TryGetValue(action.Target, out var target) || target.Role != ForgeRole.Compute) throw new ArgumentException(nameof(action));
        var values = source.Graph.ElementList.Select(n => n.Value.Bias).Concat(source.Graph.ElementList.SelectMany(n => n.OutEdgeList).Select(e => e.EdgeInfo!.Weight)).DistinctBy(p => p.Id).ToDictionary(p => p.Id, p => p.Value);
        if (action.Kind == "add-feedback")
        {
            if (action.Delay <= 0 || action.Destination is null || !biases.ContainsKey(action.Destination) || !double.IsFinite(action.Weight)) throw new ArgumentException(nameof(action));
            routes.Add(new(Guid.NewGuid(), [action.Target, action.Destination], Guid.NewGuid(), action.Weight, action.Delay));
        }
        else if (action.Kind == "clone-widen")
        {
            if (!(action.Alpha > 0 && action.Alpha < 1) || source.TextBindings is { } binding && binding.InputNodes.Concat(binding.ReadoutNodes).Contains(action.Target)) throw new InvalidOperationException();
            var affected = expanded.Where(e => e.From == action.Target || e.To == action.Target).ToArray();
            if (affected.Length == 0 || affected.Any(e => e.From == e.To || e.Delay != 0) ||
                affected.Any(e => expanded.Count(x => x.ParameterId == e.ParameterId) != 1 || biases.Values.Any(b => b.ParameterId == e.ParameterId)) ||
                biases.Values.Count(b => b.ParameterId == biases[action.Target].ParameterId) != 1) throw new InvalidOperationException();
            var prefix = action.Target + "." + Guid.NewGuid().ToString("N");
            var i = prefix + ".I"; var a = prefix + ".A"; var b = prefix + ".B"; var o = prefix + ".O";
            nodes.Remove(action.Target); biases.Remove(action.Target);
            nodes.Add(i, new(ForgeRole.InputBoundary)); nodes.Add(o, new(ForgeRole.OutputBoundary));
            nodes.Add(a, target); nodes.Add(b, target);
            var oldBias = source.Graph.ElementList.Single(n => n.Id == action.Target).Value.Bias.Value;
            biases.Add(a, new(Guid.NewGuid(), oldBias)); biases.Add(b, new(Guid.NewGuid(), oldBias));
            routes = routes.Where(r => r.Path[0] != action.Target && r.Path[^1] != action.Target).ToList();
            foreach (var edge in affected)
            {
                var original = Topology.Routes.Single(r => r.Id == edge.Id);
                foreach (var (branch, fraction) in new[] { (a, action.Alpha), (b, 1 - action.Alpha) })
                {
                    var path = edge.To == action.Target ? original.Path[..^1].Concat(new[] { i, branch }).ToArray() : new[] { branch, o }.Concat(original.Path.Skip(1)).ToArray();
                    routes.Add(new(Guid.NewGuid(), path, Guid.NewGuid(), values[edge.ParameterId] * (edge.From == action.Target ? fraction : 1)));
                }
            }
            units.Add(new(action.Target, i, a, b, o, action.Kind));
        }
        else if (action.Kind == "output-deepen")
        {
            var incoming = routes.Where(r => r.Path[^1] == action.Target).ToArray();
            if (incoming.Length == 0 || incoming.Any(r => r.Path[0] == action.Target || r.Delay != 0 ||
                expanded.Count(e => e.ParameterId == r.ParameterId) != 1 || biases.Values.Any(bias => bias.ParameterId == r.ParameterId))) throw new InvalidOperationException();
            foreach (var route in incoming)
            {
                var prefix = action.Target + "." + Guid.NewGuid().ToString("N");
                var i = prefix + ".I"; var a = prefix + ".A"; var b = prefix + ".B"; var o = prefix + ".O";
                nodes.Add(i, new(ForgeRole.InputBoundary)); nodes.Add(o, new(ForgeRole.OutputBoundary));
                nodes.Add(a, new(ForgeRole.Compute, ForgeActivation.Identity)); nodes.Add(b, new(ForgeRole.Compute, ForgeActivation.Identity));
                biases.Add(a, new(Guid.NewGuid())); biases.Add(b, new(Guid.NewGuid()));
                routes.Remove(route);
                routes.Add(new(Guid.NewGuid(), route.Path[..^1].Concat(new[] { i, a }).ToArray(), Guid.NewGuid(), values[route.ParameterId]));
                routes.Add(new(Guid.NewGuid(), route.Path[..^1].Concat(new[] { i, b }).ToArray(), Guid.NewGuid(), values[route.ParameterId]));
                routes.Add(new(Guid.NewGuid(), [a, o, action.Target], Guid.NewGuid(), .5));
                routes.Add(new(Guid.NewGuid(), [b, o, action.Target], Guid.NewGuid(), .5));
                units.Add(new(action.Target, i, a, b, o, action.Kind));
            }
        }
        else throw new NotSupportedException();
        var result = Compile(new ForgeTopology(nodes, biases, routes, units), source);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
