using MathGraphs;
using System.Collections.ObjectModel;

namespace MathGraphs.Neural;

public static partial class GraphInterpreter
{
    /// <summary>直接递归读取图计算零初始状态序列，用作扁平执行的独立前向参照。</summary>
    public static IReadOnlyList<IReadOnlyDictionary<string, double>> Run(RecursiveNetwork network,
        IReadOnlyList<IReadOnlyDictionary<string, double>> inputs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(network);
        ArgumentNullException.ThrowIfNull(inputs);
        var validation = ExecutionLayout.Create(network.Graph, network.Budget, cancellationToken: cancellationToken);
        if (inputs.Count > network.Budget.MaxSteps || (long)inputs.Count * validation.Operations.Count > network.Budget.MaxTapeCells)
            throw new ArgumentOutOfRangeException(nameof(inputs));
        var outputs = new List<IReadOnlyDictionary<string, double>>();
        for (var q = 0; q < inputs.Count; q++)
        {
            var current = new Dictionary<string, double>();
            foreach (var (id, value) in inputs[q])
            {
                if (!validation.Outputs.ContainsKey(id)) throw new ArgumentException(nameof(inputs));
                Numeric.RequireFinite(value);
            }
            EvaluateGraph(network.Graph, null, 0);
            outputs.Add(new ReadOnlyDictionary<string, double>(current));

            void EvaluateGraph(MathGraph<Neuron, NeuralConnection> graph, string? entry, double injection)
            {
                foreach (var node in graph.ElementList) EvaluateNode(node);

                double EvaluateNode(MathGraphElement<Neuron, NeuralConnection> node)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (current.TryGetValue(node.Id, out var computed)) return computed;
                    var sum = node.Value.Bias.Value + inputs[q].GetValueOrDefault(node.Id);
                    if (node.Id == entry) sum += injection;
                    foreach (var edge in node.InEdgeList)
                    {
                        var connection = edge.EdgeInfo ?? throw new InvalidOperationException();
                        var source = connection.Delay == 0 ? EvaluateNode(edge.From) :
                            q >= connection.Delay ? outputs[q - connection.Delay][edge.From.Id] : 0;
                        sum += connection.Weight.Value * source;
                    }
                    Numeric.RequireFinite(sum);
                    var value = NetworkExecutor.Activate(node.Value.Activation, sum);
                    if (node.Value.Child is { } child && node.Value.Gate is { } gate)
                    {
                        EvaluateGraph(child.Graph, child.Input.Id, sum);
                        value += Math.Tanh(gate.Value) * current[child.Output.Id];
                    }
                    current[node.Id] = Numeric.RequireFinite(value);
                    return value;
                }
            }
        }
        return outputs.AsReadOnly();
    }
}
