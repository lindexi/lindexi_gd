namespace MathGraph.ReversibleFractal.Research.NodeOperators;

/// <summary>Builds the ordered forward and reverse five-node FullCross templates without external project dependencies.</summary>
public static class FullCross
{
    private static readonly string[] Names = ["A", "B", "C", "D", "E"];
    private static readonly (int From, int To)[] ForwardLinks = [(0, 2), (0, 3), (1, 2), (1, 3), (2, 4), (3, 4)];
    private readonly record struct TargetPort(int NodeIndex, int PortIndex);
    private sealed record Boundary(TargetPort[] Inputs, SourcePort[] Outputs);

    /// <summary>Creates a two-input, one-output graph. Expansion paths omit the root, e.g. A, C or A/A.</summary>
    public static OperatorGraph Create(IEnumerable<string> expandedPaths, Func<string, Activation> selectActivation)
    {
        ArgumentNullException.ThrowIfNull(expandedPaths);
        ArgumentNullException.ThrowIfNull(selectActivation);
        var pending = new HashSet<string>(expandedPaths, StringComparer.Ordinal);
        var leaves = new List<OperatorNode>();
        var bindings = new List<SourcePort[]>();
        var root = BuildTemplate("", forward: true);
        if (pending.Count != 0)
            throw new ArgumentException("Expansion paths must identify template nodes and include their expanded ancestors.",
                nameof(expandedPaths));
        for (int i = 0; i < root.Inputs.Length; i++) Connect(new(-1, i), root.Inputs[i]);
        return new(root.Inputs.Length, leaves, root.Outputs);

        Boundary BuildLeaf(string path, int inputCount, int outputCount)
        {
            if (pending.Remove(path)) return BuildTemplate(path, forward: inputCount == 2);
            int nodeIndex = leaves.Count;
            var sources = new SourcePort[inputCount];
            bindings.Add(sources);
            leaves.Add(new(new(inputCount, outputCount, selectActivation(path)), sources));
            return new(Enumerable.Range(0, inputCount).Select(i => new TargetPort(nodeIndex, i)).ToArray(),
                Enumerable.Range(0, outputCount).Select(i => new SourcePort(nodeIndex, i)).ToArray());
        }

        Boundary BuildTemplate(string path, bool forward)
        {
            var links = forward ? ForwardLinks : ForwardLinks.Select(link => (From: link.To, To: link.From)).ToArray();
            int[] inputNodes = forward ? [0, 1] : [4];
            int[] outputNodes = forward ? [4] : [0, 1];
            var children = new Boundary[Names.Length];
            for (int i = 0; i < children.Length; i++)
            {
                string childPath = path.Length == 0 ? Names[i] : $"{path}/{Names[i]}";
                children[i] = BuildLeaf(childPath, links.Count(link => link.To == i) + inputNodes.Count(n => n == i),
                    links.Count(link => link.From == i) + outputNodes.Count(n => n == i));
            }

            // Assign internal ports in template edge order before exposing the remaining boundary ports.
            var nextInput = new int[Names.Length];
            var nextOutput = new int[Names.Length];
            foreach (var (from, to) in links)
                Connect(children[from].Outputs[nextOutput[from]++], children[to].Inputs[nextInput[to]++]);
            return new(inputNodes.Select(n => children[n].Inputs[nextInput[n]++]).ToArray(),
                outputNodes.Select(n => children[n].Outputs[nextOutput[n]++]).ToArray());
        }

        void Connect(SourcePort source, TargetPort target) => bindings[target.NodeIndex][target.PortIndex] = source;
    }
}
