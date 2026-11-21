namespace MathGraphs.Neural;

/// <summary>成功训练所见内部步的累计统计；方差为总体方差，梯度为节点输出伴随值的平均绝对值。</summary>
public sealed record NodeStatistics(long Samples = 0, double ActivationMean = 0,
    double ActivationM2 = 0, double MeanAbsoluteGradient = 0, double RecentStructureReward = 0, WindowStatistics? LastWindow = null)
{
    public double ActivationVariance => Samples == 0 ? 0 : ActivationM2 / Samples;

    internal void Validate()
    {
        if (LastWindow is { } window && (window.Samples <= 0 || window.Samples > Samples ||
            !double.IsFinite(window.Mean) || !double.IsFinite(window.Variance) || window.Variance < 0 ||
            !double.IsFinite(window.MeanAbsoluteGradient) || window.MeanAbsoluteGradient < 0)) throw new InvalidDataException();
        if (Samples < 0 || !double.IsFinite(ActivationMean) || !double.IsFinite(ActivationM2) || ActivationM2 < 0 ||
            !double.IsFinite(MeanAbsoluteGradient) || MeanAbsoluteGradient < 0 || !double.IsFinite(RecentStructureReward) ||
            (Samples == 0 && (ActivationMean != 0 || ActivationM2 != 0 || MeanAbsoluteGradient != 0)) ||
            (Samples == 1 && ActivationM2 != 0)) throw new InvalidDataException();
    }
}

/// <summary>图上节点的统计快照和当前结构成本描述。</summary>
public sealed record NodeObservation(string NodeId, NodeStatistics Statistics, int Depth,
    int IncomingConnections, int OutgoingConnections, int EstimatedAddedNodes);

public sealed partial class RecursiveNetwork
{
    /// <summary>读取所有节点的持久统计及当前深度和度数，不执行训练或修改状态。</summary>
    public IReadOnlyList<NodeObservation> GetNodeObservations(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _ = ExecutionLayout.Create(Graph, Budget, cancellationToken: cancellationToken);
            var observations = new List<NodeObservation>();
            Visit(Graph, 0);
            return observations.AsReadOnly();

            void Visit(MathGraph<Neuron, NeuralConnection> graph, int depth)
            {
                foreach (var node in graph.ElementList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    observations.Add(new NodeObservation(node.Id, node.Value.Statistics, depth,
                        node.InEdgeList.Count, node.OutEdgeList.Count, node.Value.Child is null ? 5 : 0));
                    if (node.Value.Child is { } child) Visit(child.Graph, depth + 1);
                }
            }
        }
    }
}
