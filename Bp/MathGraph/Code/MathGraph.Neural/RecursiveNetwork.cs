using MathGraphs;

namespace MathGraphs.Neural;

public enum ScalarActivation { Tanh, Identity, Relu }

public sealed class ScalarParameter
{
    /// <summary>创建具有独立稳定身份的有限标量参数。</summary>
    public ScalarParameter(double value = 0, bool decay = true)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(double.IsFinite(value), false);
        Value = value;
        Decay = decay;
    }

    public Guid Id { get; internal init; } = Guid.NewGuid();
    public double Value { get; internal set; }
    public bool Decay { get; }
    public double FirstMoment { get; internal set; }
    public double SecondMoment { get; internal set; }
    public long Updates { get; internal set; }
}

public sealed class Neuron
{
    /// <summary>创建标量神经元；偏置可通过同一参数对象共享。</summary>
    public Neuron(ScalarParameter? bias = null, ScalarActivation activation = ScalarActivation.Tanh)
    {
        if (!Enum.IsDefined(activation)) throw new ArgumentOutOfRangeException(nameof(activation));
        Bias = bias ?? new ScalarParameter();
        Activation = activation;
    }

    public ScalarParameter Bias { get; }
    public ScalarActivation Activation { get; }
    public ScalarParameter? Gate { get; internal set; }
    public FiveNodeGraph? Child { get; internal set; }
    public NodeStatistics Statistics { get; internal set; } = new();
}

public sealed class NeuralConnection
{
    /// <summary>创建具有稳定连接身份的权重引用，延迟不得为负。</summary>
    public NeuralConnection(ScalarParameter weight, int delay = 0)
    {
        ArgumentNullException.ThrowIfNull(weight);
        ArgumentOutOfRangeException.ThrowIfNegative(delay);
        Weight = weight;
        Delay = delay;
    }

    public Guid Id { get; internal init; } = Guid.NewGuid();
    public ScalarParameter Weight { get; }
    public int Delay { get; }
}

public sealed class FiveNodeGraph
{
    internal FiveNodeGraph(MathGraph<Neuron, NeuralConnection> graph) => Graph = graph;
    public MathGraph<Neuron, NeuralConnection> Graph { get; }
    public MathGraphElement<Neuron, NeuralConnection> Input => Graph.ElementList[0];
    public MathGraphElement<Neuron, NeuralConnection> Output => Graph.ElementList[4];

    /// <summary>建立 I、A、B、M、O 及延迟一步的 O→I；种子只控制数值初始化。</summary>
    public static FiveNodeGraph Create(int seed = 0)
    {
        var random = new Random(seed);
        var graph = new MathGraph<Neuron, NeuralConnection>();
        var nodes = Enumerable.Range(0, 5).Select(_ => graph.CreateAndAddElement(new Neuron(), Guid.NewGuid().ToString("N"))).ToArray();
        foreach (var (from, to, delay) in new[] { (0, 1, 0), (0, 2, 0), (1, 3, 0), (2, 3, 0), (3, 4, 0), (4, 0, 1) })
            graph.AddEdge(nodes[from], nodes[to], new NeuralConnection(new ScalarParameter(random.NextDouble() - 0.5), delay));
        return new FiveNodeGraph(graph);
    }
}

public sealed record NetworkBudget(int MaxNodes = 10000, int MaxDepth = 32, int MaxDelay = 128,
    int MaxSteps = 1024, long MaxTapeCells = 4000000, int MaxConnections = 100000);

public sealed record AdamWOptions(double LearningRate = 0.001, double Beta1 = 0.9,
    double Beta2 = 0.999, double Epsilon = 1e-8, double WeightDecay = 0.01);

public sealed partial class RecursiveNetwork
{
    private readonly object _sync = new();

    /// <summary>使用 MathGraph 作为唯一拓扑来源，并立即验证结构和预算。</summary>
    public RecursiveNetwork(MathGraph<Neuron, NeuralConnection> graph, NetworkBudget? budget = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        Graph = graph;
        Budget = budget ?? new NetworkBudget();
        if (Budget.MaxNodes <= 0 || Budget.MaxDepth < 0 || Budget.MaxDelay < 0 || Budget.MaxSteps <= 0 || Budget.MaxTapeCells <= 0 || Budget.MaxConnections < 0)
            throw new ArgumentOutOfRangeException(nameof(budget));
        _ = ExecutionLayout.Create(Graph, Budget);
    }

    public MathGraph<Neuron, NeuralConnection> Graph { get; }
    public NetworkBudget Budget { get; }
    public long Version { get; private set; }

    /// <summary>在零门下发布五点修正分支；失败不修改父节点或旧参数。</summary>
    public void Fractalize(string nodeId, int seed = 0, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        lock (_sync)
        {
            if (_controller?.Pending is not null) throw new InvalidOperationException();
            var measurementStart = System.Diagnostics.Stopwatch.GetTimestamp();
            var allocatedStart = GC.GetAllocatedBytesForCurrentThread();
            var current = ExecutionLayout.Create(Graph, Budget, cancellationToken: cancellationToken);
            var node = current.Nodes.SingleOrDefault(n => n.Id == nodeId) ?? throw new ArgumentException(nameof(nodeId));
            if (node.Value.Child is not null) throw new InvalidOperationException();
            var child = FiveNodeGraph.Create(seed);
            var gate = new ScalarParameter(decay: false);
            _ = ExecutionLayout.Create(Graph, Budget, node.Value, child, gate, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            node.Value.Gate = gate;
            node.Value.Child = child;
            Version++;
            _lastStructureMeasurement = new StructureMeasurement(Version,
                System.Diagnostics.Stopwatch.GetElapsedTime(measurementStart), GC.GetAllocatedBytesForCurrentThread() - allocatedStart);
        }
    }

    /// <summary>捕获独立结构及参数快照；旧快照在新结构发布后仍可使用。</summary>
    public NetworkExecutor CreateExecutor(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync) return new NetworkExecutor(ExecutionLayout.Create(Graph, Budget, cancellationToken: cancellationToken), Budget, Version);
    }

    /// <summary>在一个完整窗口内训练指定读出，提交全部有限 AdamW 更新或不提交任何更新。</summary>
    public double TrainSquaredError(IReadOnlyList<IReadOnlyDictionary<string, double>> inputs,
        string outputId, IReadOnlyList<double> targets, AdamWOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(targets);
        if (targets.Count != inputs.Count || targets.Count == 0 || targets.Any(t => !double.IsFinite(t)))
            throw new ArgumentException(nameof(targets));
        lock (_sync)
        {
            if (_trainingJob is not null) throw new InvalidOperationException();
            var layout = ExecutionLayout.Create(Graph, Budget, cancellationToken: cancellationToken);
            var result = new NetworkExecutor(layout, Budget, Version).Run(inputs, cancellationToken: cancellationToken);
            var seeds = new Dictionary<string, double>[targets.Count];
            double loss = 0;
            for (var q = 0; q < targets.Count; q++)
            {
                var difference = result.Outputs[q][outputId] - targets[q];
                loss += difference * difference / (2 * targets.Count);
                seeds[q] = new Dictionary<string, double> { [outputId] = difference / targets.Count };
            }
            Numeric.RequireFinite(loss);
            var statistics = new Dictionary<Neuron, NodeStatistics>();
            var gradients = result.BackwardCore(seeds, statistics, cancellationToken);
            var nextTrainingUpdates = checked(_trainingUpdates + 1);
            ApplyAdamW(layout.Parameters, gradients, options ?? new AdamWOptions(), cancellationToken);
            foreach (var (neuron, updated) in statistics) neuron.Statistics = updated;
            _trainingUpdates = nextTrainingUpdates;
            Version++;
            return loss;
        }
    }

    private static void ValidateAdamW(AdamWOptions options)
    {
        if (!double.IsFinite(options.LearningRate) || options.LearningRate <= 0 ||
            !double.IsFinite(options.Beta1) || options.Beta1 < 0 || options.Beta1 >= 1 ||
            !double.IsFinite(options.Beta2) || options.Beta2 < 0 || options.Beta2 >= 1 ||
            !double.IsFinite(options.Epsilon) || options.Epsilon <= 0 ||
            !double.IsFinite(options.WeightDecay) || options.WeightDecay < 0)
            throw new ArgumentOutOfRangeException(nameof(options));
    }

    private static void ApplyAdamW(IReadOnlyDictionary<Guid, ScalarParameter> parameters,
        IReadOnlyDictionary<Guid, double> gradients, AdamWOptions options, CancellationToken cancellationToken)
    {
        ValidateAdamW(options);
        var updates = new List<(ScalarParameter Parameter, double Value, double M, double V, long Step)>(parameters.Count);
        long correctionStep = -1;
        double firstCorrection = 0;
        double secondCorrection = 0;
        foreach (var (id, parameter) in parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var gradient = gradients.GetValueOrDefault(id);
            Numeric.RequireFinite(gradient);
            var step = checked(parameter.Updates + 1);
            var m = options.Beta1 * parameter.FirstMoment + (1 - options.Beta1) * gradient;
            var v = options.Beta2 * parameter.SecondMoment + (1 - options.Beta2) * gradient * gradient;
            if (step != correctionStep)
            {
                firstCorrection = 1 - Math.Pow(options.Beta1, step);
                secondCorrection = 1 - Math.Pow(options.Beta2, step);
                correctionStep = step;
            }
            var value = parameter.Value - options.LearningRate *
                (m / firstCorrection / (Math.Sqrt(v / secondCorrection) + options.Epsilon)
                + (parameter.Decay ? options.WeightDecay * parameter.Value : 0));
            Numeric.RequireFinite(m);
            Numeric.RequireFinite(v);
            Numeric.RequireFinite(value);
            updates.Add((parameter, value, m, v, step));
        }
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var update in updates)
        {
            update.Parameter.Value = update.Value;
            update.Parameter.FirstMoment = update.M;
            update.Parameter.SecondMoment = update.V;
            update.Parameter.Updates = update.Step;
        }
    }
}

internal static class Numeric
{
    private static readonly System.Resources.ResourceManager Resources = new("MathGraph.Neural.NeuralErrors", typeof(Numeric).Assembly);

    internal static string Message(string key) => Resources.GetString(key, System.Globalization.CultureInfo.CurrentUICulture) ?? key;

    internal static double RequireFinite(double value, string location = "")
    {
        if (!double.IsFinite(value))
            throw new ArithmeticException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Message("NonFinite"), location));
        return value;
    }
}
