using System.Resources;
using FractalForge.Core;
using FractalForge.Training;
using MathGraphs;
using MathGraphs.Neural;

namespace FractalForge.Research.Deepening;

public enum DeepeningBranch { D0, D1, D2, D3 }
public sealed record DeepeningProposal(ForgeModel Model, string Target, Guid[] Gates, double InitialPreactivationBound);

internal static class Messages
{
    private static readonly ResourceManager Resources = new("FractalForge.Research.Deepening.Messages", typeof(Messages).Assembly);
    internal static string Get(string key) => Resources.GetString(key) ?? key;
    internal static InvalidOperationException Invalid() => new(Get("Invalid"));
}

/// <summary>逐来源加深策略；所有动作返回独立模型，结构切换采用零历史。</summary>
public static class DeepeningStrategy
{
    /// <summary>创建保持、Identity 接续或保留父路径的 Tanh 修正。D3 的界仅约束初始化时目标预激活扰动。</summary>
    public static DeepeningProposal Propose(ForgeModel source, DeepeningBranch branch, string target,
        double perturbationBound = 0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();
        if (!Enum.IsDefined(branch) || !double.IsFinite(perturbationBound) || perturbationBound < 0 ||
            branch is DeepeningBranch.D0 or DeepeningBranch.D1 && perturbationBound != 0) throw Messages.Invalid();
        var snapshot = ForgeArchive.Import(ForgeArchive.Export(source), source.Network.Budget);
        if (branch == DeepeningBranch.D0) return new(snapshot, target, [], 0);
        // Transparent local O is a locator for its recorded computational merge, never an implicit aggregator.
        var unit = snapshot.Topology.Units.SingleOrDefault(u => u.Output == target);
        target = unit?.Target ?? target;
        var nodes = snapshot.Topology.Graph.ElementList.ToDictionary(n => n.Id, n => n.Value);
        if (!nodes.TryGetValue(target, out var node) || node.Role != ForgeRole.Compute ||
            snapshot.Network.TextBindings is { } text && text.InputNodes.Concat(text.ReadoutNodes).Contains(target)) throw Messages.Invalid();
        if (branch == DeepeningBranch.D1)
            return new(snapshot.Apply(new("output-deepen", target), cancellationToken), target, [], 0);
        if (branch == DeepeningBranch.D3 && perturbationBound <= 0 || branch == DeepeningBranch.D2 && perturbationBound != 0) throw Messages.Invalid();
        var incoming = snapshot.Topology.Routes.Where(r => r.Path[^1] == target).ToArray();
        if (incoming.Length == 0 || incoming.Any(r => r.Delay != 0 || r.Path[0] == target)) throw Messages.Invalid();
        var parameters = Parameters(snapshot);
        var biases = snapshot.Topology.Biases.ToDictionary(p => p.Key, p => p.Value);
        var routes = snapshot.Topology.Routes.ToList();
        var units = snapshot.Topology.Units.ToList();
        var gates = new List<Guid>();
        var gate = perturbationBound / (2 * incoming.Length);
        foreach (var route in incoming)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var prefix = target + "." + Guid.NewGuid().ToString("N");
            var i = prefix + ".I"; var a = prefix + ".A"; var b = prefix + ".B"; var o = prefix + ".O";
            nodes.Add(i, new(ForgeRole.InputBoundary)); nodes.Add(o, new(ForgeRole.OutputBoundary));
            nodes.Add(a, new(ForgeRole.Compute, ForgeActivation.Tanh)); nodes.Add(b, new(ForgeRole.Compute, ForgeActivation.Tanh));
            biases.Add(a, new(Guid.NewGuid())); biases.Add(b, new(Guid.NewGuid()));
            // Different scales break symmetry without consuming the corpus random stream.
            foreach (var (id, scale) in new[] { (a, 1.0), (b, .5) })
            {
                routes.Add(new(Guid.NewGuid(), route.Path[..^1].Concat(new[] { i, id }).ToArray(), Guid.NewGuid(), parameters[route.ParameterId].Value * scale));
                var parameterId = Guid.NewGuid();
                gates.Add(parameterId);
                routes.Add(new(Guid.NewGuid(), [id, o, target], parameterId, gate));
            }
            // Core v1 reserves Units.Kind for its two built-in transformations. Direction-specific
            // correction routes remain explicit; gates and merge identity live in the experiment state.
        }
        var candidate = Rebuild(snapshot, new(nodes, biases, routes, units));
        cancellationToken.ThrowIfCancellationRequested();
        return new(candidate, target, gates.ToArray(), perturbationBound);
    }

    internal static Dictionary<Guid, ScalarParameter> Parameters(ForgeModel model) => model.Network.Graph.ElementList
        .Select(n => n.Value.Bias).Concat(model.Network.Graph.ElementList.SelectMany(n => n.OutEdgeList).Select(e => e.EdgeInfo!.Weight))
        .DistinctBy(p => p.Id).ToDictionary(p => p.Id);

    internal static ForgeModel Rebuild(ForgeModel source, ForgeTopology topology)
    {
        var old = Parameters(source);
        var parameters = new Dictionary<Guid, ScalarParameter>();
        ScalarParameter Parameter(Guid id, double initial)
        {
            if (!parameters.TryGetValue(id, out var value))
            {
                value = old.TryGetValue(id, out var previous) ? RecursiveNetwork.CopyParameterState(previous) : RecursiveNetwork.CreateRootParameter(id, initial);
                parameters.Add(id, value);
            }
            return value;
        }
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var nodes = topology.Graph.ElementList.Where(n => n.Value.Role == ForgeRole.Compute).ToDictionary(n => n.Id,
            n => graph.CreateAndAddElement(new Neuron(Parameter(topology.Biases[n.Id].ParameterId, topology.Biases[n.Id].InitialValue), (ScalarActivation)n.Value.Activation), n.Id));
        foreach (var edge in topology.Expand())
            graph.AddEdge(nodes[edge.From], nodes[edge.To], RecursiveNetwork.CreateRootConnection(edge.Id, Parameter(edge.ParameterId, edge.InitialValue), edge.Delay));
        // ForgeArchive validates the logical topology against the rebuilt network and retains trained text bindings.
        var template = ForgeArchive.Export(ForgeModel.Create(topology, source.Network.Budget));
        var root = System.Text.Json.Nodes.JsonNode.Parse(template) ?? throw Messages.Invalid();
        root["Network"] = System.Text.Json.JsonSerializer.SerializeToNode(source.Network.RebuildRoot(graph).ExportCheckpoint());
        return ForgeArchive.Import(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(root), source.Network.Budget);
    }

    /// <summary>消融指定修正门；保留节点、连接及其他参数状态，不把零权重当作删边。</summary>
    public static ForgeModel Ablate(ForgeModel source, IReadOnlyCollection<Guid> gates)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(gates);
        var nodes = source.Topology.Graph.ElementList.ToDictionary(n => n.Id, n => n.Value);
        var routes = source.Topology.Routes.Select(r => gates.Contains(r.ParameterId) ? r with { ParameterId = Guid.NewGuid(), InitialValue = 0 } : r);
        return Rebuild(source, new(nodes, source.Topology.Biases, routes, source.Topology.Units));
    }

    /// <summary>返回忽略正延迟边的最长实际计算路径节点数；零延迟环由编译器拒绝。</summary>
    public static int LongestPath(ForgeModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var edges = model.Topology.Expand().Where(e => e.Delay == 0).ToArray();
        var cache = new Dictionary<string, int>();
        int Depth(string id)
        {
            if (cache.TryGetValue(id, out var value)) return value;
            return cache[id] = 1 + edges.Where(e => e.To == id).Select(e => Depth(e.From)).DefaultIfEmpty(0).Max();
        }
        return model.Topology.Biases.Keys.Select(Depth).Max();
    }
}
