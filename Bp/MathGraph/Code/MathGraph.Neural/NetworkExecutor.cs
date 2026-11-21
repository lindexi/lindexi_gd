using System.Collections.ObjectModel;

namespace MathGraphs.Neural;

public sealed class RecurrentState
{
    internal RecurrentState(Dictionary<string, double[]> history, long step, Guid snapshotId)
    {
        History = history;
        Step = step;
        SnapshotId = snapshotId;
    }

    internal Guid SnapshotId { get; }
    internal Dictionary<string, double[]> History { get; }
    public long Step { get; }
}

public sealed partial class NetworkExecutor
{
    private readonly ExecutionLayout _layout;
    private readonly NetworkBudget _budget;
    private readonly Guid _snapshotId = Guid.NewGuid();
    internal NetworkExecutor(ExecutionLayout layout, NetworkBudget budget, long version)
    {
        _layout = layout;
        _budget = budget;
        Version = version;
    }

    public long Version { get; }

    /// <summary>运行一个有界窗口；输入按内部步提供。返回状态可续推，但跨窗口梯度明确截断。</summary>
    public ExecutionResult Run(IReadOnlyList<IReadOnlyDictionary<string, double>> inputs,
        RecurrentState? initialState = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (initialState is not null && initialState.SnapshotId != _snapshotId)
            throw new InvalidOperationException(Numeric.Message("StateMismatch"));
        if (inputs.Count > _budget.MaxSteps ||
            (long)inputs.Count * _layout.Operations.Count > _budget.MaxTapeCells)
            throw new ArgumentOutOfRangeException(nameof(inputs));
        var tape = new double[inputs.Count][];
        var outputs = new IReadOnlyDictionary<string, double>[inputs.Count];
        for (var q = 0; q < inputs.Count; q++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var (id, value) in inputs[q])
            {
                if (!_layout.Outputs.ContainsKey(id)) throw new ArgumentException(nameof(inputs));
                Numeric.RequireFinite(value);
            }
            var row = tape[q] = new double[_layout.Operations.Count];
            for (var i = 0; i < row.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var op = _layout.Operations[i];
                double value;
                switch (op.Kind)
                {
                    case OperationKind.Sum:
                        value = _layout.ParameterValues[op.ParameterIndex] + inputs[q].GetValueOrDefault(op.NodeId);
                        if (op.Source >= 0) value += row[op.Source];
                        foreach (var term in op.Terms)
                            value += _layout.ParameterValues[term.WeightIndex] * Read(q - term.Delay, term.Source);
                        break;
                    case OperationKind.Activate:
                        value = Activate(op.Activation, row[op.Source]);
                        break;
                    default:
                        value = row[op.Source] + op.GateValue * row[op.Child];
                        break;
                }
                if (!double.IsFinite(value))
                    Numeric.RequireFinite(value, $"node={op.NodeId}, q={(initialState?.Step ?? 0) + q}, operation={op.Kind}");
                row[i] = value;
            }
            outputs[q] = new ExecutionOutputs(_layout.Outputs, row);
        }
        var history = new Dictionary<string, double[]>();
        foreach (var (id, index) in _layout.Outputs)
        {
            var values = new double[_layout.MaxDelay];
            for (var age = 0; age < values.Length; age++) values[age] = Read(inputs.Count - 1 - age, index);
            history[id] = values;
        }
        return new ExecutionResult(_layout, tape, outputs, initialState,
            new RecurrentState(history, checked((initialState?.Step ?? 0) + inputs.Count), _snapshotId));

        double Read(int q, int index)
        {
            if (q >= 0) return tape[q][index];
            var id = _layout.Operations[index].NodeId;
            return initialState is not null && initialState.History.TryGetValue(id, out var old) && -q <= old.Length
                ? Numeric.RequireFinite(old[-q - 1]) : 0;
        }
    }

    internal static double Activate(ScalarActivation activation, double value) => activation switch
    {
        ScalarActivation.Tanh => Math.Tanh(value),
        ScalarActivation.Identity => value,
        ScalarActivation.Relu => Math.Max(0, value),
        _ => throw new ArgumentOutOfRangeException(nameof(activation))
    };
}

public sealed class ExecutionResult
{
    private readonly ExecutionLayout _layout;
    private readonly double[][] _tape;
    private readonly RecurrentState? _initial;

    internal ExecutionResult(ExecutionLayout layout, double[][] tape,
        IReadOnlyDictionary<string, double>[] outputs, RecurrentState? initial, RecurrentState state)
    {
        _layout = layout;
        _tape = tape;
        _initial = initial;
        Outputs = Array.AsReadOnly(outputs);
        State = state;
    }

    public IReadOnlyList<IReadOnlyDictionary<string, double>> Outputs { get; }
    public RecurrentState State { get; }

    /// <summary>对当前窗口执行完整 BPTT；共享参数梯度按参数身份合并，不修改模型。</summary>
    public IReadOnlyDictionary<Guid, double> Backward(IReadOnlyList<IReadOnlyDictionary<string, double>> outputGradients,
        CancellationToken cancellationToken = default) => BackwardCore(outputGradients, null, cancellationToken);

    internal IReadOnlyDictionary<Guid, double> BackwardCore(IReadOnlyList<IReadOnlyDictionary<string, double>> outputGradients,
        Dictionary<Neuron, NodeStatistics>? statistics, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outputGradients);
        if (outputGradients.Count != _tape.Length) throw new ArgumentException(nameof(outputGradients));
        var adjoints = _tape.Select(row => new double[row.Length]).ToArray();
        var parameterGradients = new double[_layout.ParameterIds.Length];
        for (var q = 0; q < _tape.Length; q++)
            foreach (var (id, gradient) in outputGradients[q])
                adjoints[q][_layout.Outputs[id]] += Numeric.RequireFinite(gradient);
        for (var q = _tape.Length - 1; q >= 0; q--)
        {
            for (var i = _layout.Operations.Count - 1; i >= 0; i--)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var op = _layout.Operations[i];
                var gradient = Numeric.RequireFinite(adjoints[q][i]);
                switch (op.Kind)
                {
                    case OperationKind.Sum:
                        Add(op.ParameterIndex, gradient);
                        if (op.Source >= 0) adjoints[q][op.Source] += gradient;
                        foreach (var term in op.Terms)
                        {
                            var time = q - term.Delay;
                            double source = 0;
                            if (time >= 0) source = _tape[time][term.Source];
                            else if (_initial is not null && _initial.History.TryGetValue(_layout.Operations[term.Source].NodeId, out var old)
                                && -time <= old.Length) source = old[-time - 1];
                            Add(term.WeightIndex, gradient * source);
                            if (time >= 0) adjoints[time][term.Source] += gradient * _layout.ParameterValues[term.WeightIndex];
                        }
                        break;
                    case OperationKind.Activate:
                        var derivative = op.Activation switch
                        {
                            ScalarActivation.Tanh => 1 - _tape[q][i] * _tape[q][i],
                            ScalarActivation.Identity => 1,
                            ScalarActivation.Relu => _tape[q][op.Source] > 0 ? 1 : 0,
                            _ => throw new InvalidOperationException()
                        };
                        adjoints[q][op.Source] += gradient * derivative;
                        break;
                    case OperationKind.Merge:
                        var gate = op.GateValue;
                        Add(op.ParameterIndex, gradient * (1 - gate * gate) * _tape[q][op.Child]);
                        adjoints[q][op.Source] += gradient;
                        adjoints[q][op.Child] += gradient * gate;
                        break;
                }
            }
        }
        if (statistics is not null)
        {
            foreach (var node in _layout.Nodes)
            {
                var updated = node.Value.Statistics;
                var cumulative = new NodeStatisticsAccumulator(updated);
                var window = new NodeStatisticsAccumulator();
                var index = _layout.Outputs[node.Id];
                for (var q = 0; q < _tape.Length; q++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    cumulative.Observe(_tape[q][index], adjoints[q][index]);
                    window.Observe(_tape[q][index], adjoints[q][index]);
                }
                if (window.Samples > 0) updated = updated with
                {
                    Samples = cumulative.Samples,
                    ActivationMean = cumulative.ActivationMean,
                    ActivationM2 = cumulative.ActivationM2,
                    MeanAbsoluteGradient = cumulative.MeanAbsoluteGradient,
                    LastWindow = new WindowStatistics(window.Samples, window.ActivationMean,
                        window.ActivationM2 / window.Samples, window.MeanAbsoluteGradient)
                };
                updated.Validate();
                statistics.Add(node.Value, updated);
            }
        }
        var gradients = new Dictionary<Guid, double>(parameterGradients.Length);
        for (var i = 0; i < parameterGradients.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            gradients.Add(_layout.ParameterIds[i], parameterGradients[i]);
        }
        return new ReadOnlyDictionary<Guid, double>(gradients);

        void Add(int index, double gradient) => parameterGradients[index] = Numeric.RequireFinite(parameterGradients[index] + gradient);
    }
}
