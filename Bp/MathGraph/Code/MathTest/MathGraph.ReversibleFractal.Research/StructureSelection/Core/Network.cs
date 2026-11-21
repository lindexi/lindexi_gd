namespace MathGraph.StructureSelection;

internal readonly record struct Sample(double X0, double X1, double Y);

internal sealed class Network
{
    private sealed record Node(Leaf Leaf, int[] Inputs, int[] Outputs, int Offset);
    private readonly Node[] nodes;
    private readonly int target;

    internal Network(FivePointGraph graph)
    {
        Graph = graph;
        var available = new Dictionary<Port, int> { [new("S0", 0)] = 0, [new("S1", 0)] = 1 };
        var sources = graph.Edges.ToDictionary(e => e.To, e => e.From);
        var pending = graph.Leaves.ToList();
        var sorted = new List<Node>();
        int slots = 2;
        int offset = 0;
        while (pending.Count > 0)
        {
            int ready = pending.FindIndex(l => Enumerable.Range(0, l.Inputs)
                .All(i => available.ContainsKey(sources[new(l.Id, i)])));
            if (ready < 0) throw new InvalidOperationException("The graph is not acyclic.");
            var leaf = pending[ready];
            pending.RemoveAt(ready);
            var inputs = Enumerable.Range(0, leaf.Inputs).Select(i => available[sources[new(leaf.Id, i)]]).ToArray();
            var outputs = new int[leaf.Outputs];
            for (int o = 0; o < outputs.Length; o++) available.Add(new(leaf.Id, o), outputs[o] = slots++);
            sorted.Add(new(leaf, inputs, outputs, offset));
            offset += ParameterCount(leaf);
        }
        target = available[sources[new("T", 0)]];
        nodes = sorted.ToArray();
        Weights = new double[offset];
        SlotCount = slots;
    }

    internal FivePointGraph Graph { get; }
    internal double[] Weights { get; }
    internal int UnitsPerSample => SlotCount - 2;
    private int SlotCount { get; }
    private static int ParameterCount(Leaf leaf) => leaf.Outputs * (leaf.Inputs + 2);

    internal Network Fork(string? point)
    {
        var fork = new Network(point is null ? Graph : Graph.Expand(point));
        var original = nodes.ToDictionary(n => n.Leaf.Id, StringComparer.Ordinal);
        foreach (var child in fork.nodes)
        {
            var leaf = child.Leaf;
            if (original.TryGetValue(leaf.Id, out var same))
            {
                Array.Copy(Weights, same.Offset, fork.Weights, child.Offset, ParameterCount(leaf));
                continue;
            }

            var parent = original[point ?? throw new InvalidOperationException()];
            bool forward = parent.Leaf.Inputs == 2;
            char name = leaf.Id[^1];
            // C carries the old operator. Other vertices relay ports; the unused D path starts nonzero.
            if (name == 'C')
                Array.Copy(Weights, parent.Offset, fork.Weights, child.Offset, ParameterCount(leaf));
            else if (forward && name == 'D')
            {
                fork.Weights[child.Offset] = 0.5;
                fork.Weights[child.Offset + 1] = 0.5;
            }
            else
                for (int o = 0; o < leaf.Outputs; o++)
                    fork.Weights[child.Offset + o * (leaf.Inputs + 2)] = 1;
        }
        return fork;
    }

    internal Dictionary<string, double> Scores(double[] gradient)
    {
        return nodes.ToDictionary(n => n.Leaf.Id, n =>
        {
            int count = ParameterCount(n.Leaf);
            double sum = 0;
            for (int i = n.Offset; i < n.Offset + count; i++) sum += gradient[i] * gradient[i];
            return Math.Sqrt(sum / count);
        }, StringComparer.Ordinal);
    }

    internal double Evaluate(double x0, double x1) => Forward(x0, x1, new double[SlotCount], new double[SlotCount]);

    // Returns MSE and optionally accumulates its mean gradient through the real port edges.
    internal double Measure(Sample[] data, ReadOnlySpan<int> batch, double[]? gradient = null)
    {
        int count = batch.IsEmpty ? data.Length : batch.Length;
        var values = new double[SlotCount];
        var nonlinear = new double[SlotCount];
        double[] adjoints = gradient is null ? [] : new double[SlotCount];
        double sum = 0;
        if (gradient is not null) Array.Clear(gradient);
        for (int i = 0; i < count; i++)
        {
            var sample = data[batch.IsEmpty ? i : batch[i]];
            double error = Forward(sample.X0, sample.X1, values, nonlinear) - sample.Y;
            sum += error * error;
            if (gradient is null) continue;
            Array.Clear(adjoints);
            adjoints[target] = 2 * error / count;
            for (int n = nodes.Length - 1; n >= 0; n--)
            {
                var node = nodes[n];
                int p = node.Offset;
                foreach (int output in node.Outputs)
                {
                    int bias = p + node.Inputs.Length;
                    int amplitude = bias + 1;
                    double adjoint = adjoints[output];
                    double tanh = nonlinear[output];
                    double linearAdjoint = adjoint * (1 + Weights[amplitude] * (1 - tanh * tanh));
                    foreach (int input in node.Inputs)
                    {
                        gradient[p] += linearAdjoint * values[input];
                        adjoints[input] += linearAdjoint * Weights[p++];
                    }
                    gradient[bias] += linearAdjoint;
                    gradient[amplitude] += adjoint * tanh;
                    p = amplitude + 1;
                }
            }
        }
        return sum / count;
    }

    private double Forward(double x0, double x1, double[] values, double[] nonlinear)
    {
        values[0] = x0;
        values[1] = x1;
        foreach (var node in nodes)
        {
            int p = node.Offset;
            foreach (int output in node.Outputs)
            {
                double linear = 0;
                foreach (int input in node.Inputs) linear += Weights[p++] * values[input];
                linear += Weights[p++];
                double tanh = nonlinear[output] = Math.Tanh(linear);
                values[output] = linear + Weights[p++] * tanh;
            }
        }
        return values[target];
    }
}
