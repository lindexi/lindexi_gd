using MathGraphs.Neural;

namespace MathGraphs.Neural.Cli;

public sealed record ApplicationConfig
{
    public string TokenizerPath { get; init; } = string.Empty;
    public string TokenizerConfigPath { get; init; } = string.Empty;
    public string DatasetPath { get; init; } = string.Empty;
    public string DatasetFormat { get; init; } = "pretrain";
    public string ArchiveRoot { get; init; } = "runs";
    public int Epochs { get; init; } = 1;
    public int Seed { get; init; } = 17;
    public int InternalSteps { get; init; } = 1;
    public int PrefixLines { get; init; } = 0;
    public bool Pipeline { get; init; } = true;
    public int PrefetchCapacity { get; init; } = 64;
    public int SelectionSamples { get; init; } = 64;
    public int WarmupUpdates { get; init; } = 2;
    public int MaxNewTokens { get; init; } = 32;
    public NetworkBudget Budget { get; init; } = new(MaxTapeCells: 16000000);
    public JsonlDatasetOptions DataLimits { get; init; } = new(MaxLineBytes: 1048576, MaxFileBytes: long.MaxValue,
        MaxSamples: int.MaxValue, MaxSampleTokens: 512, MaxTotalTokens: int.MaxValue);
    public DatasetPartitionOptions Partition { get; init; } = new(0.01, 0.01, 7);
    public AdamWOptions Optimizer { get; init; } = new();
    public StructureOptions? Controller { get; init; } = new(1000, UseNodeFeatures: true);
    public TrainingArchiveOptions Archive { get; init; } = new();
    public SamplingOptions Sampling { get; init; } = new();
}
