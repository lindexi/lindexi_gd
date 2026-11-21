namespace MathGraph.FunctionMorphism;

/// <summary>A pure ordered 2-to-1 or 1-to-2 function, represented by a leaf or a five-point module.</summary>
public sealed class PortFunction<T>
{
    private readonly record struct Wire(int From, int Output, int To, int Input);

    private static readonly Wire[] ForwardWires =
    [
        new(0, 0, 2, 0), new(0, 1, 3, 0), new(1, 0, 2, 1),
        new(1, 1, 3, 1), new(2, 0, 4, 0), new(3, 0, 4, 1)
    ];
    private static readonly Wire[] ReverseWires = ForwardWires
        .Select(wire => new Wire(wire.To, wire.Input, wire.From, wire.Output)).ToArray();
    private static readonly int[] ForwardOrder = [0, 1, 2, 3, 4];
    private static readonly int[] ReverseOrder = [4, 2, 3, 0, 1];

    private readonly bool isMerge;
    private readonly Func<T[], T[]>? operation;
    private readonly PortFunction<T>[]? children;

    private PortFunction(bool isMerge, Func<T[], T[]> operation) =>
        (this.isMerge, this.operation) = (isMerge, operation);

    private PortFunction(PortFunction<T>[] children) =>
        (isMerge, this.children) = (children[4].isMerge, children);

    /// <summary>Creates a leaf taking two inputs in port order and returning one output.</summary>
    public static PortFunction<T> Merge(Func<T, T, T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new(true, input => [operation(input[0], input[1])]);
    }

    /// <summary>Creates a leaf taking one input and returning two outputs in tuple order.</summary>
    public static PortFunction<T> Split(Func<T, (T First, T Second)> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new(false, input =>
        {
            var (first, second) = operation(input[0]);
            return [first, second];
        });
    }

    /// <summary>Wires five children in A–E order; E determines the orientation and boundary arity.</summary>
    public static PortFunction<T> FivePoint(PortFunction<T> a, PortFunction<T> b,
        PortFunction<T> c, PortFunction<T> d, PortFunction<T> e)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(c);
        ArgumentNullException.ThrowIfNull(d);
        ArgumentNullException.ThrowIfNull(e);
        if (a.isMerge == e.isMerge || b.isMerge == e.isMerge ||
            c.isMerge != e.isMerge || d.isMerge != e.isMerge)
        {
            throw new ArgumentException();
        }

        return new([a, b, c, d, e]);
    }

    /// <summary>Replaces the leaf at the given A–E path (empty for the root), keeping its operator at E.</summary>
    public PortFunction<T> Expand(params char[] path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return ExpandAt(path);
    }

    private PortFunction<T> ExpandAt(ReadOnlySpan<char> path)
    {
        if (path.IsEmpty)
        {
            if (children is not null)
            {
                throw new InvalidOperationException();
            }

            var copy = Split(value => (value, value));
            var first = Merge((left, _) => left);
            var second = Merge((_, right) => right);
            return isMerge
                ? FivePoint(copy, copy, first, second, this)
                : FivePoint(first, second, copy, copy, this);
        }

        int index = path[0] - 'A';
        if (children is null || index < 0 || index >= children.Length)
        {
            throw new ArgumentException(null, nameof(path));
        }

        var updated = (PortFunction<T>[])children.Clone();
        updated[index] = updated[index].ExpandAt(path[1..]);
        return new(updated);
    }

    /// <summary>Evaluates the finite DAG with values in boundary input and output port order.</summary>
    public T[] Evaluate(params T[] input)
    {
        ValidateInput(input);
        return EvaluateCore(input);
    }

    /// <summary>Evaluates and records the immediate children and wires of the root module; a leaf has neither.</summary>
    public EvaluationTrace EvaluateWithTrace(params T[] input)
    {
        ValidateInput(input);
        var children = new List<ChildTrace>();
        var wires = new List<WireTrace>();
        var output = EvaluateCore(input, children, wires);
        return new(Array.AsReadOnly(output), children.AsReadOnly(), wires.AsReadOnly());
    }

    /// <summary>Boundary outputs and the root module's observed execution.</summary>
    public sealed record EvaluationTrace(IReadOnlyList<T> Outputs, IReadOnlyList<ChildTrace> Children,
        IReadOnlyList<WireTrace> Wires);

    /// <summary>The inputs and outputs of one immediate child, in port order.</summary>
    public sealed record ChildTrace(char Vertex, IReadOnlyList<T> Inputs, IReadOnlyList<T> Outputs);

    /// <summary>A value transferred from a child output port to a child input port.</summary>
    public sealed record WireTrace(char From, int OutputPort, char To, int InputPort, T Value);

    private void ValidateInput(T[] input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Length != (isMerge ? 2 : 1))
        {
            throw new ArgumentException(null, nameof(input));
        }
    }

    private T[] EvaluateCore(T[] input, List<ChildTrace>? childTrace = null, List<WireTrace>? wireTrace = null)
    {
        if (operation is not null)
        {
            return operation(input);
        }

        if (children is null)
        {
            throw new InvalidOperationException();
        }

        var incoming = new T[children.Length][];
        var outgoing = new T[children.Length][];
        for (int i = 0; i < children.Length; i++)
        {
            incoming[i] = new T[children[i].isMerge ? 2 : 1];
        }

        if (isMerge)
        {
            incoming[0][0] = input[0];
            incoming[1][0] = input[1];
        }
        else
        {
            incoming[4][0] = input[0];
        }

        var wires = isMerge ? ForwardWires : ReverseWires;
        foreach (int vertex in isMerge ? ForwardOrder : ReverseOrder)
        {
            var output = outgoing[vertex] = children[vertex].EvaluateCore(incoming[vertex]);
            childTrace?.Add(new((char)('A' + vertex), Array.AsReadOnly(incoming[vertex]), Array.AsReadOnly(output)));
            foreach (var wire in wires)
            {
                if (wire.From == vertex)
                {
                    var value = output[wire.Output];
                    incoming[wire.To][wire.Input] = value;
                    wireTrace?.Add(new((char)('A' + wire.From), wire.Output, (char)('A' + wire.To), wire.Input, value));
                }
            }
        }

        return isMerge ? outgoing[4] : [outgoing[0][0], outgoing[1][0]];
    }
}
