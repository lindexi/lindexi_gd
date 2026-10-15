using System.Collections.ObjectModel;

namespace MathGraphs.Neural;

public static partial class GraphInterpreter
{
    private sealed record Differential(double Value, double[] Derivatives);

    /// <summary>直接按图传播参数前向导数，返回指定线性输出目标的梯度；仅作有界小图正确性参照。</summary>
    public static IReadOnlyDictionary<Guid, double> Differentiate(RecursiveNetwork network,
        IReadOnlyList<IReadOnlyDictionary<string, double>> inputs,
        IReadOnlyList<IReadOnlyDictionary<string, double>> outputGradients,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(network);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(outputGradients);
        cancellationToken.ThrowIfCancellationRequested();
        if (inputs.Count != outputGradients.Count) throw new ArgumentException(nameof(outputGradients));
        var layout = ExecutionLayout.Create(network.Graph, network.Budget, cancellationToken: cancellationToken);
        var parameters = layout.Parameters.Keys.ToArray();
        if (inputs.Count > network.Budget.MaxSteps ||
            (long)Math.Max(1, inputs.Count) * layout.Operations.Count > network.Budget.MaxTapeCells / Math.Max(1, parameters.Length + 1))
            throw new ArgumentOutOfRangeException(nameof(inputs));
        var indices = parameters.Select((id, index) => (id, index)).ToDictionary(p => p.id, p => p.index);
        var gradient = new double[parameters.Length];
        var history = new List<Dictionary<string, Differential>>();
        for (var q = 0; q < inputs.Count; q++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var (id, value) in inputs[q])
            {
                if (!layout.Outputs.ContainsKey(id)) throw new ArgumentException(nameof(inputs));
                Numeric.RequireFinite(value);
            }
            var current = new Dictionary<string, Differential>();
            EvaluateGraph(network.Graph, null, null);
            foreach (var (id, seed) in outputGradients[q])
            {
                Numeric.RequireFinite(seed);
                if (!current.TryGetValue(id, out var output)) throw new ArgumentException(nameof(outputGradients));
                for (var p = 0; p < gradient.Length; p++)
                    gradient[p] = Numeric.RequireFinite(gradient[p] + seed * output.Derivatives[p]);
            }
            history.Add(current);

            void EvaluateGraph(MathGraph<Neuron, NeuralConnection> graph, string? entry, Differential? injection)
            {
                foreach (var node in graph.ElementList) Evaluate(node);

                Differential Evaluate(MathGraphElement<Neuron, NeuralConnection> node)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (current.TryGetValue(node.Id, out var cached)) return cached;
                    var derivatives = new double[parameters.Length];
                    derivatives[indices[node.Value.Bias.Id]] = 1;
                    var sum = layout.Values[node.Value.Bias.Id] + inputs[q].GetValueOrDefault(node.Id);
                    if (node.Id == entry && injection is not null)
                    {
                        sum += injection.Value;
                        for (var p = 0; p < derivatives.Length; p++) derivatives[p] += injection.Derivatives[p];
                    }
                    foreach (var edge in node.InEdgeList)
                    {
                        var connection = edge.EdgeInfo ?? throw new InvalidOperationException();
                        Differential? source = connection.Delay == 0 ? Evaluate(edge.From) :
                            q >= connection.Delay ? history[q - connection.Delay][edge.From.Id] : null;
                        if (source is null) continue;
                        var weight = layout.Values[connection.Weight.Id];
                        sum += weight * source.Value;
                        for (var p = 0; p < derivatives.Length; p++) derivatives[p] += weight * source.Derivatives[p];
                        derivatives[indices[connection.Weight.Id]] += source.Value;
                    }
                    Numeric.RequireFinite(sum);
                    var value = node.Value.Activation switch
                    {
                        ScalarActivation.Identity => sum,
                        ScalarActivation.Tanh => Math.Tanh(sum),
                        ScalarActivation.Relu => Math.Max(0, sum),
                        _ => throw new InvalidOperationException()
                    };
                    var slope = node.Value.Activation switch
                    {
                        ScalarActivation.Identity => 1,
                        ScalarActivation.Tanh => 1 - value * value,
                        ScalarActivation.Relu => sum > 0 ? 1 : 0,
                        _ => throw new InvalidOperationException()
                    };
                    var outputDerivatives = derivatives.Select(d => Numeric.RequireFinite(d * slope)).ToArray();
                    if (node.Value.Child is { } child && node.Value.Gate is { } gate)
                    {
                        EvaluateGraph(child.Graph, child.Input.Id, new Differential(sum, derivatives));
                        var correction = current[child.Output.Id];
                        var gateValue = Math.Tanh(layout.Values[gate.Id]);
                        value += gateValue * correction.Value;
                        for (var p = 0; p < outputDerivatives.Length; p++)
                            outputDerivatives[p] += gateValue * correction.Derivatives[p];
                        outputDerivatives[indices[gate.Id]] += (1 - gateValue * gateValue) * correction.Value;
                    }
                    foreach (var derivative in outputDerivatives) Numeric.RequireFinite(derivative);
                    var result = new Differential(Numeric.RequireFinite(value), outputDerivatives);
                    current.Add(node.Id, result);
                    return result;
                }
            }
        }
        return new ReadOnlyDictionary<Guid, double>(parameters.Select((id, p) => (id, value: gradient[p]))
            .ToDictionary(p => p.id, p => p.value));
    }
}
