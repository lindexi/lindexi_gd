namespace MathGraphs.Neural;

/// <summary>流水线配置；在线内容哈希划分不同于全量排序划分。选择集固定后才开始结构观察。</summary>
public sealed record PipelineOptions(int Epochs = 1, int WarmupUpdates = 2, int PrefetchCapacity = 64,
    int SelectionSamples = 64, int PrefixLines = 0);

public sealed record PipelineSelectionSample(string ContentHash, int[] Tokens, bool[]? Labels);

/// <summary>只记录消费提交点，不记录生产者预取位置；源文件在训练及恢复期间应保持不变。</summary>
public sealed record PipelineTrainingState(string SourcePath, long SourceLength, long SourceWriteTicks,
    string Format, string TokenizerIdentity, PipelineOptions Options, JsonlDatasetOptions Limits,
    DatasetPartitionOptions Partition, AdamWOptions Optimizer, StructureOptions? ControllerOptions,
    ulong ControllerSeed, int Epoch = 0, long ByteOffset = 0, long Line = 0,
    long TrainedSamples = 0, int WarmupCompleted = 0, long EpochSamples = 0, long EpochTokens = 0,
    bool SelectionFrozen = false, PipelineSelectionSample[]? Selection = null);

/// <summary>生产与消费进度分开报告；消费字节是可恢复位置，队列容量不代表总数据限制。</summary>
public sealed record PipelineProgress(string Stage, int Epoch, long ByteOffset, long SourceLength,
    long TrainedSamples, int WarmupCompleted, int SelectionSamples, string? Checkpoint = null, PipelineTimings? Timings = null, PipelineLearning? Learning = null);

/// <summary>本次运行已计算的训练与结构评价统计；不触发额外模型执行。</summary>
public sealed record PipelineLearning(double? LastTrainingLoss, double WeightedLossSum, long SupervisedTokens,
    long TrainingSamples, double? SelectionLoss, StructureDecision? Decision, StructureFeedback? Feedback);

/// <summary>本次运行累计耗时；写盘含刷盘与发布，保留清理单列，不是CPU使用率。</summary>
public sealed record PipelineTimings(double ElapsedSeconds, double TrainingSeconds,
    double EvaluationSeconds, double SerializationSeconds, double WriteSeconds, double RetentionSeconds,
    long Saves, long LastCheckpointBytes);
