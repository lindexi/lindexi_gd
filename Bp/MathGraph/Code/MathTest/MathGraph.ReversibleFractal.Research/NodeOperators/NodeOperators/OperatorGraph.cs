namespace MathGraph.ReversibleFractal.Research.NodeOperators;

/// <summary>A node output, or a graph input when NodeIndex is -1.</summary>
public readonly record struct SourcePort(int NodeIndex, int PortIndex);

/// <summary>A node and its source bindings in input port order.</summary>
public sealed record OperatorNode(AffineOperator Operator, IReadOnlyList<SourcePort> Inputs);

/// <summary>Forward outputs and gradients of the output vector dotted with the supplied seed.</summary>
public sealed record GraphGradient(double[] Outputs, double[] Inputs, double[] Parameters);

/// <summary>A compiled DAG with immutable wiring and per-call forward and reverse storage.</summary>
public sealed class OperatorGraph
{
    private sealed record CompiledNode(AffineOperator Operator, int[] Inputs, int FirstOutput, int FirstParameter);

    private readonly CompiledNode[] nodes;
    private readonly int[] order;
    private readonly int[] outputs;
    private readonly int valueCount;

    /// <summary>Compiles the graph; node indices and parameter blocks follow the supplied list, not topological order.</summary>
    public OperatorGraph(int inputCount, IReadOnlyList<OperatorNode> nodes, IReadOnlyList<SourcePort> outputs)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputCount);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(outputs);
        InputCount = inputCount;
        this.nodes = new CompiledNode[nodes.Count];
        valueCount = inputCount;
        for (int n = 0; n < nodes.Count; n++)
        {
            var node = nodes[n];
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(node.Operator);
            ArgumentNullException.ThrowIfNull(node.Inputs);
            if (node.Inputs.Count != node.Operator.InputCount)
                throw new ArgumentException($"Node {n} has the wrong number of input bindings.", nameof(nodes));
            this.nodes[n] = new(node.Operator, new int[node.Inputs.Count], valueCount, ParameterCount);
            valueCount = checked(valueCount + node.Operator.OutputCount);
            ParameterCount = checked(ParameterCount + node.Operator.ParameterCount);
        }

        var successors = new List<int>[nodes.Count];
        var remaining = new int[nodes.Count];
        for (int n = 0; n < successors.Length; n++) successors[n] = [];
        for (int n = 0; n < nodes.Count; n++)
        {
            for (int i = 0; i < this.nodes[n].Inputs.Length; i++)
            {
                var source = nodes[n].Inputs[i];
                this.nodes[n].Inputs[i] = Resolve(source, nameof(nodes));
                if (source.NodeIndex < 0) continue;
                successors[source.NodeIndex].Add(n);
                remaining[n]++;
            }
        }
        this.outputs = outputs.Select(port => Resolve(port, nameof(outputs))).ToArray();

        var ready = new Queue<int>();
        for (int n = 0; n < remaining.Length; n++)
            if (remaining[n] == 0) ready.Enqueue(n);
        order = new int[nodes.Count];
        int visited = 0;
        while (ready.TryDequeue(out int n))
        {
            order[visited++] = n;
            foreach (int successor in successors[n])
                if (--remaining[successor] == 0) ready.Enqueue(successor);
        }
        if (visited != nodes.Count) throw new ArgumentException("The graph contains a cycle.", nameof(nodes));
    }

    /// <summary>Number of external input ports.</summary>
    public int InputCount { get; }
    /// <summary>Number of exposed output ports, including any aliases.</summary>
    public int OutputCount => outputs.Length;
    /// <summary>Number of operator nodes.</summary>
    public int NodeCount => nodes.Length;
    /// <summary>Total length of all node parameter blocks.</summary>
    public int ParameterCount { get; }

    /// <summary>Evaluates the graph and returns outputs in boundary order.</summary>
    public double[] Forward(ReadOnlySpan<double> inputs, ReadOnlySpan<double> parameters) =>
        ReadOutputs(Evaluate(inputs, parameters));

    /// <summary>Evaluates the graph and computes the vector–Jacobian product for the supplied output seed.</summary>
    public GraphGradient Differentiate(ReadOnlySpan<double> inputs, ReadOnlySpan<double> parameters,
        ReadOnlySpan<double> outputSeed)
    {
        if (outputSeed.Length != OutputCount)
            throw new ArgumentException("Expected one seed per graph output.", nameof(outputSeed));
        var values = Evaluate(inputs, parameters);
        var adjoints = new double[valueCount];
        var gradients = new double[ParameterCount];
        for (int i = 0; i < outputs.Length; i++) adjoints[outputs[i]] += outputSeed[i];
        for (int i = order.Length - 1; i >= 0; i--)
        {
            var node = nodes[order[i]];
            node.Operator.Backward(node.Inputs, node.FirstOutput, values, adjoints,
                parameters.Slice(node.FirstParameter, node.Operator.ParameterCount),
                gradients.AsSpan(node.FirstParameter, node.Operator.ParameterCount));
        }
        return new(ReadOutputs(values), adjoints[..InputCount], gradients);
    }

    private int Resolve(SourcePort source, string parameterName)
    {
        if (source.NodeIndex == -1 && (uint)source.PortIndex < (uint)InputCount)
            return source.PortIndex;
        if ((uint)source.NodeIndex < (uint)nodes.Length)
        {
            var node = nodes[source.NodeIndex];
            if ((uint)source.PortIndex < (uint)node.Operator.OutputCount)
                return node.FirstOutput + source.PortIndex;
        }
        throw new ArgumentException($"Unknown source port {source}.", parameterName);
    }

    private double[] Evaluate(ReadOnlySpan<double> inputs, ReadOnlySpan<double> parameters)
    {
        if (inputs.Length != InputCount) throw new ArgumentException("Wrong input vector length.", nameof(inputs));
        if (parameters.Length != ParameterCount)
            throw new ArgumentException("Wrong parameter vector length.", nameof(parameters));
        var values = new double[valueCount];
        inputs.CopyTo(values);
        foreach (int n in order)
        {
            var node = nodes[n];
            node.Operator.Forward(node.Inputs, node.FirstOutput, values,
                parameters.Slice(node.FirstParameter, node.Operator.ParameterCount));
        }
        return values;
    }

    private double[] ReadOutputs(double[] values)
    {
        var result = new double[outputs.Length];
        for (int i = 0; i < outputs.Length; i++) result[i] = values[outputs[i]];
        return result;
    }
}
