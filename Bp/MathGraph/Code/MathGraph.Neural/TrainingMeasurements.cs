namespace MathGraphs.Neural;

/// <summary>最近成功 token 训练的 CPU 测量；分配量为当前线程累计托管分配，不是峰值内存。</summary>
public sealed record TrainingMeasurement(long ModelVersion, int SupervisedTokens, TimeSpan Total,
    TimeSpan LayoutAndSnapshot, TimeSpan Forward, TimeSpan LossAndReadoutBackward,
    TimeSpan GraphBackwardAndStatistics, TimeSpan OptimizerCommit, long AllocatedBytes,
    long LayoutAllocatedBytes = 0, long ForwardAllocatedBytes = 0, long ReadoutBackwardAllocatedBytes = 0,
    long GraphBackwardAllocatedBytes = 0, long OptimizerAllocatedBytes = 0);

/// <summary>最近成功分形的 CPU 布局验证、子图创建及发布总耗时。</summary>
public sealed record StructureMeasurement(long ModelVersion, TimeSpan Total, long AllocatedBytes);

/// <summary>按有效监督 token 加权的整集交叉熵；时间包含各样本布局构建。</summary>
public sealed record DatasetEvaluation(string DatasetIdentity, long ModelVersion, long SupervisedTokens,
    double MeanCrossEntropy, TimeSpan Total, long AllocatedBytes);

public sealed partial class RecursiveNetwork
{
    private TrainingMeasurement? _lastTrainingMeasurement;
    private StructureMeasurement? _lastStructureMeasurement;

    public TrainingMeasurement? LastTrainingMeasurement
    {
        get { lock (_sync) return _lastTrainingMeasurement; }
    }

    public StructureMeasurement? LastStructureMeasurement
    {
        get { lock (_sync) return _lastStructureMeasurement; }
    }

    /// <summary>在一致模型版本下评估整集；每个样本独立零状态，按监督 token 加权，不更新模型。</summary>
    public DatasetEvaluation EvaluateDataset(TokenDataset dataset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bindings = TextBindings ?? throw new InvalidOperationException();
            if (dataset.TokenizerIdentity != bindings.TokenizerIdentity) throw new ArgumentException(nameof(dataset));
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            long total = 0;
            double mean = 0;
            for (var i = 0; i < dataset.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var labels = dataset.GetLabelMask(i);
                var count = labels is null ? dataset[i].Count - 1 : labels.Skip(1).Count(value => value);
                var loss = EvaluateTokens(dataset[i], cancellationToken: cancellationToken, labelMask: labels);
                total = checked(total + count);
                mean = Numeric.RequireFinite(mean + (loss - mean) * ((double)count / total));
            }
            cancellationToken.ThrowIfCancellationRequested();
            return new DatasetEvaluation(dataset.Identity, Version, total, mean,
                System.Diagnostics.Stopwatch.GetElapsedTime(start), GC.GetAllocatedBytesForCurrentThread() - allocated);
        }
    }
}
