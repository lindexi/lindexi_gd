namespace MathGraphs.Neural;

public sealed record StructureOptions(int ObservationUpdates = 10, double LearningRate = 0.01,
    double CostPerNode = 0.001, double BaselineRate = 0.1, bool UseNodeFeatures = false);

public sealed record StructureDecision(string? NodeId, double Probability, int AddedNodes);
public sealed record StructureFeedback(double Reward, double Advantage, double Baseline);

public sealed partial class RecursiveNetwork
{
    private sealed record PendingStructure(string[] Actions, double[] Probabilities, int Selected,
        double InitialLoss, int Cost, long TrainingUpdates, double[][]? Features = null);
    private sealed record ControllerData(StructureOptions Options, ulong RandomState, double Baseline,
        Dictionary<string, double> Scores, double KeepScore, PendingStructure? Pending, double[]? FeatureWeights = null);

    private ControllerData? _controller;
    private long _trainingUpdates;

    /// <summary>启用一次结构策略；评分、随机状态、奖励基线和观察进度随模型检查点保存。</summary>
    public void ConfigureStructureController(StructureOptions? options = null, ulong seed = 1)
    {
        var configuration = options ?? new StructureOptions(UseNodeFeatures: true);
        ValidateStructureOptions(configuration);
        lock (_sync)
        {
            if (_controller is not null) throw new InvalidOperationException();
            _controller = new ControllerData(configuration, seed, 0, [], 0, null, new double[StructureFeatureCount]);
        }
    }

    /// <summary>从保持和合法终端节点中采样并提交动作；损失必须来自专用结构选择数据。</summary>
    public StructureDecision BeginStructureObservation(double selectionLoss, CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(selectionLoss) || selectionLoss < 0) throw new ArgumentOutOfRangeException(nameof(selectionLoss));
        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var controller = _controller ?? throw new InvalidOperationException();
            if (controller.Pending is not null) throw new InvalidOperationException();
            var layout = ExecutionLayout.Create(Graph, Budget, cancellationToken: cancellationToken);
            var actions = new List<string> { string.Empty };
            var scores = new Dictionary<string, double>(controller.Scores);
            var depths = new Dictionary<string, int>();
            CollectDepths(Graph, 0);
            foreach (var node in layout.Nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                scores.TryAdd(node.Id, 0);
                // 分形增加五个节点、十个标量操作和一个父节点合并操作。
                if (node.Value.Child is null && depths[node.Id] < Budget.MaxDepth &&
                    layout.Nodes.Count <= Budget.MaxNodes - 5 && Budget.MaxDelay >= 1 &&
                    layout.ConnectionCount <= Budget.MaxConnections - 6 &&
                    (long)(layout.Operations.Count + 11) * Math.Max(1, layout.MaxDelay) <= Budget.MaxTapeCells)
                    actions.Add(node.Id);
            }
            var byId = layout.Nodes.ToDictionary(n => n.Id);
            var features = actions.Select(id => id.Length == 0 || !controller.Options.UseNodeFeatures
                ? new double[StructureFeatureCount] : NodeFeatures(byId[id], depths[id])).ToArray();
            var logits = actions.Select((id, i) => Numeric.RequireFinite((id.Length == 0 ? controller.KeepScore : scores[id]) +
                (controller.Options.UseNodeFeatures ? features[i].Select((value, f) => value * controller.FeatureWeights![f]).Sum() : 0))).ToArray();
            var maximum = logits.Max();
            var probabilities = logits.Select(value => Math.Exp(value - maximum)).ToArray();
            var total = probabilities.Sum();
            for (var i = 0; i < probabilities.Length; i++) probabilities[i] /= total;
            var randomState = controller.RandomState;
            var draw = NextRandom(ref randomState);
            var selected = probabilities.Length - 1;
            double cumulative = 0;
            for (var i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];
                if (draw < cumulative) { selected = i; break; }
            }
            var cost = selected == 0 ? 0 : 5;
            var pending = new PendingStructure(actions.ToArray(), probabilities, selected, selectionLoss, cost, _trainingUpdates, features);
            var next = controller with { Scores = scores, RandomState = randomState, Pending = pending };
            cancellationToken.ThrowIfCancellationRequested();
            if (selected != 0)
            {
                // Fractalize 验证整个候选布局；成功前不发布控制器或随机状态。
                Fractalize(actions[selected], unchecked((int)randomState), cancellationToken);
            }
            _controller = next;
            return new StructureDecision(selected == 0 ? null : actions[selected], probabilities[selected], cost);

            void CollectDepths(MathGraph<Neuron, NeuralConnection> graph, int depth)
            {
                foreach (var node in graph.ElementList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    depths.Add(node.Id, depth);
                    if (node.Value.Child is { } child) CollectDepths(child.Graph, depth + 1);
                }
            }
        }
    }

    /// <summary>达到约定成功训练次数后，以损失改善减节点成本计算奖励，执行 REINFORCE 更新。</summary>
    public StructureFeedback CompleteStructureObservation(double selectionLoss)
    {
        if (!double.IsFinite(selectionLoss) || selectionLoss < 0) throw new ArgumentOutOfRangeException(nameof(selectionLoss));
        lock (_sync)
        {
            var controller = _controller ?? throw new InvalidOperationException();
            var pending = controller.Pending ?? throw new InvalidOperationException();
            if (_trainingUpdates - pending.TrainingUpdates < controller.Options.ObservationUpdates)
                throw new InvalidOperationException();
            var reward = Numeric.RequireFinite(pending.InitialLoss - selectionLoss - controller.Options.CostPerNode * pending.Cost);
            var advantage = Numeric.RequireFinite(reward - controller.Baseline);
            var scores = new Dictionary<string, double>(controller.Scores);
            var keep = controller.KeepScore;
            for (var i = 0; i < pending.Actions.Length; i++)
            {
                var id = pending.Actions[i];
                var change = controller.Options.LearningRate * advantage * ((i == pending.Selected ? 1 : 0) - pending.Probabilities[i]);
                var updated = Numeric.RequireFinite((i == 0 ? keep : scores[id]) + change);
                if (i == 0) keep = updated;
                else scores[id] = updated;
            }
            var weights = UpdateFeatureWeights(controller, pending, advantage);
            var baseline = Numeric.RequireFinite(controller.Baseline + controller.Options.BaselineRate * advantage);
            Neuron? target = null;
            NodeStatistics? updatedStatistics = null;
            if (pending.Selected != 0)
            {
                target = ExecutionLayout.Create(Graph, Budget).Nodes.Single(n => n.Id == pending.Actions[pending.Selected]).Value;
                updatedStatistics = target.Statistics with { RecentStructureReward = reward };
                updatedStatistics.Validate();
            }
            _controller = controller with { Scores = scores, KeepScore = keep, Baseline = baseline, Pending = null, FeatureWeights = weights };
            if (target is not null && updatedStatistics is not null) target.Statistics = updatedStatistics;
            return new StructureFeedback(reward, advantage, baseline);
        }
    }

    private static void ValidateStructureOptions(StructureOptions options)
    {
        if (options.ObservationUpdates <= 0 || !double.IsFinite(options.LearningRate) || options.LearningRate <= 0 ||
            !double.IsFinite(options.CostPerNode) || options.CostPerNode < 0 ||
            !double.IsFinite(options.BaselineRate) || options.BaselineRate <= 0 || options.BaselineRate > 1)
            throw new ArgumentOutOfRangeException(nameof(options));
    }

    private static double NextRandom(ref ulong state)
    {
        // SplitMix64 的状态显式持久化，避免依赖 System.Random 的私有实现。
        state = unchecked(state + 0x9E3779B97F4A7C15UL);
        var value = state;
        value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
        value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
        value ^= value >> 31;
        return (value >> 11) * (1.0 / (1UL << 53));
    }

    private void RestoreController(ControllerData? controller, long trainingUpdates)
    {
        if (trainingUpdates < 0) throw new InvalidDataException();
        if (controller is not null)
        {
            if (controller.Options is null || controller.Scores is null || !double.IsFinite(controller.Baseline) ||
                !double.IsFinite(controller.KeepScore)) throw new InvalidDataException();
            ValidateStructureOptions(controller.Options);
            ValidateFeatureState(controller);
            var nodes = ExecutionLayout.Create(Graph, Budget).Nodes.ToDictionary(n => n.Id);
            foreach (var (id, score) in controller.Scores)
                if (!nodes.ContainsKey(id) || !double.IsFinite(score)) throw new InvalidDataException();
            if (controller.Pending is { } pending)
            {
                if (pending.Actions is null || pending.Probabilities is null || pending.Actions.Length == 0 ||
                    pending.Actions.Length != pending.Probabilities.Length || pending.Actions[0] != string.Empty ||
                    pending.Actions.Distinct().Count() != pending.Actions.Length ||
                    pending.Selected < 0 || pending.Selected >= pending.Actions.Length ||
                    pending.Cost != (pending.Selected == 0 ? 0 : 5) ||
                    !double.IsFinite(pending.InitialLoss) || pending.InitialLoss < 0 ||
                    pending.TrainingUpdates < 0 || pending.TrainingUpdates > trainingUpdates ||
                    pending.Probabilities.Any(p => !double.IsFinite(p) || p < 0 || p > 1) ||
                    Math.Abs(pending.Probabilities.Sum() - 1) > 1e-12 ||
                    pending.Probabilities[pending.Selected] <= 0 ||
                    pending.Actions.Skip(1).Any(id => id is null || !nodes.ContainsKey(id) || !controller.Scores.ContainsKey(id)))
                    throw new InvalidDataException();
                if (pending.Selected != 0 && nodes[pending.Actions[pending.Selected]].Value.Child is null)
                    throw new InvalidDataException();
            }
        }
        _controller = controller;
        _trainingUpdates = trainingUpdates;
    }
}
