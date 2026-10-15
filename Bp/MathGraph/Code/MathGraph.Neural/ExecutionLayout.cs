using MathGraphs;

namespace MathGraphs.Neural;

internal enum OperationKind { Sum, Activate, Merge }
internal sealed record InputTerm(int Source, int Delay, Guid Weight)
{
    internal int WeightIndex { get; init; }
}
internal sealed record Operation(OperationKind Kind, string NodeId, Guid Parameter,
    ScalarActivation Activation, int Source, int Child, InputTerm[] Terms)
{
    internal int ParameterIndex { get; init; }
    internal double GateValue { get; init; }
}

internal sealed class ExecutionLayout
{
    internal List<MathGraphElement<Neuron, NeuralConnection>> Nodes { get; } = [];
    internal List<Operation> Operations { get; } = [];
    internal Dictionary<string, int> Outputs { get; } = [];
    internal Dictionary<Guid, ScalarParameter> Parameters { get; } = [];
    internal Dictionary<Guid, double> Values { get; } = [];
    internal Guid[] ParameterIds { get; private set; } = [];
    internal double[] ParameterValues { get; private set; } = [];
    internal int MaxDelay { get; private set; }
    internal int ConnectionCount { get; private set; }

    internal static ExecutionLayout Create(MathGraph<Neuron, NeuralConnection> graph, NetworkBudget budget,
        Neuron? replacement = null, FiveNodeGraph? replacementChild = null, ScalarParameter? replacementGate = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var layout = new ExecutionLayout();
        var graphs = new HashSet<MathGraph<Neuron, NeuralConnection>>();
        var ids = new HashSet<string>();
        var connections = new HashSet<Guid>();
        var pending = new List<(int Index, MathGraphElement<Neuron, NeuralConnection> Node)>();
        var neuronValues = new HashSet<Neuron>();
        AddGraph(graph, 0, -1, null);
        foreach (var (index, node) in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var terms = node.InEdgeList.Select(edge =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new InputTerm(layout.Outputs[edge.From.Id], edge.EdgeInfo!.Delay, edge.EdgeInfo.Weight.Id);
            }).ToArray();
            layout.Operations[index] = layout.Operations[index] with { Terms = terms };
        }
        if ((long)layout.Operations.Count * Math.Max(1, layout.MaxDelay) > budget.MaxTapeCells)
            throw new InvalidOperationException();
        layout.ParameterIds = layout.Parameters.Keys.ToArray();
        layout.ParameterValues = layout.ParameterIds.Select(id => layout.Values[id]).ToArray();
        var parameterIndices = layout.ParameterIds.Select((id, index) => (id, index))
            .ToDictionary(pair => pair.id, pair => pair.index);
        for (var i = 0; i < layout.Operations.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var operation = layout.Operations[i];
            layout.Operations[i] = operation with
            {
                ParameterIndex = operation.Kind == OperationKind.Activate ? -1 : parameterIndices[operation.Parameter],
                GateValue = operation.Kind == OperationKind.Merge ? Math.Tanh(layout.Values[operation.Parameter]) : 0,
                Terms = operation.Terms.Select(term => term with { WeightIndex = parameterIndices[term.Weight] }).ToArray()
            };
        }
        cancellationToken.ThrowIfCancellationRequested();
        return layout;

        void Register(ScalarParameter parameter)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Numeric.RequireFinite(parameter.Value);
            if (layout.Parameters.TryGetValue(parameter.Id, out var existing) && !ReferenceEquals(existing, parameter))
                throw new InvalidOperationException();
            layout.Parameters[parameter.Id] = parameter;
            layout.Values[parameter.Id] = parameter.Value;
        }

        void AddGraph(MathGraph<Neuron, NeuralConnection> current, int depth, int injection, string? entry)
        {
            if (depth > budget.MaxDepth || !graphs.Add(current) || current.ElementList.Count == 0)
                throw new InvalidOperationException();
            cancellationToken.ThrowIfCancellationRequested();
            if (current.ElementList.Count > budget.MaxNodes - layout.Nodes.Count)
                throw new InvalidOperationException();
            var members = new HashSet<MathGraphElement<Neuron, NeuralConnection>>();
            foreach (var member in current.ElementList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                members.Add(member);
            }
            foreach (var node in current.ElementList)
            {
                if (!ids.Add(node.Id) || !neuronValues.Add(node.Value) || !ReferenceEquals(node.MathGraph, current))
                    throw new InvalidOperationException();
                layout.Nodes.Add(node);
                if (layout.Nodes.Count > budget.MaxNodes) throw new InvalidOperationException();
                Register(node.Value.Bias);
                if (node.OutEdgeList.Count > budget.MaxConnections - layout.ConnectionCount)
                    throw new InvalidOperationException();
                layout.ConnectionCount += node.OutEdgeList.Count;
                foreach (var edge in node.OutEdgeList)
                {
                    var info = edge.EdgeInfo ?? throw new InvalidOperationException();
                    if (!members.Contains(edge.To) || !connections.Add(info.Id) || info.Delay > budget.MaxDelay)
                        throw new InvalidOperationException();
                    Register(info.Weight);
                    layout.MaxDelay = Math.Max(layout.MaxDelay, info.Delay);
                }
            }
            var degrees = members.ToDictionary(n => n, n => n.InEdgeList.Count(e =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return (e.EdgeInfo ?? throw new InvalidOperationException()).Delay == 0;
            }));
            var ready = new Queue<MathGraphElement<Neuron, NeuralConnection>>(current.ElementList.Where(n => degrees[n] == 0));
            var count = 0;
            while (ready.TryDequeue(out var node))
            {
                cancellationToken.ThrowIfCancellationRequested();
                count++;
                var neuron = node.Value;
                var sum = layout.Operations.Count;
                layout.Operations.Add(new Operation(OperationKind.Sum, node.Id, neuron.Bias.Id,
                    neuron.Activation, node.Id == entry ? injection : -1, -1, []));
                pending.Add((sum, node));
                var activated = layout.Operations.Count;
                layout.Operations.Add(new Operation(OperationKind.Activate, node.Id, Guid.Empty,
                    neuron.Activation, sum, -1, []));
                var child = ReferenceEquals(neuron, replacement) ? replacementChild : neuron.Child;
                var gate = ReferenceEquals(neuron, replacement) ? replacementGate : neuron.Gate;
                if ((child is null) != (gate is null)) throw new InvalidOperationException();
                if (child is not null && gate is not null)
                {
                    Register(gate);
                    AddGraph(child.Graph, depth + 1, sum, child.Input.Id);
                    activated = layout.Operations.Count;
                    layout.Operations.Add(new Operation(OperationKind.Merge, node.Id, gate.Id,
                        neuron.Activation, sum + 1, layout.Outputs[child.Output.Id], []));
                }
                layout.Outputs.Add(node.Id, activated);
                foreach (var edge in node.OutEdgeList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (edge.EdgeInfo!.Delay == 0 && --degrees[edge.To] == 0) ready.Enqueue(edge.To);
                }
            }
            if (count != current.ElementList.Count) throw new InvalidOperationException();
        }
    }
}
