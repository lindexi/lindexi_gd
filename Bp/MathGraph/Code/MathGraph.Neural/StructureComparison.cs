using System.Diagnostics;
using System.Security.Cryptography;

namespace MathGraphs.Neural;

public enum StructureComparisonMode { FixedInitial, RandomGrowth, PresetGrowth, LearnedGrowth }

/// <summary>同上限、同训练步数的 CPU 对照配置；预设索引指向每轮按图顺序排列的合法终端节点，-1 表示保持。</summary>
public sealed record StructureComparisonOptions(int Rounds = 2, int UpdatesPerRound = 2, ulong Seed = 1);

/// <summary>单轮动作、结构选择集损失与实际资源；不包含最终测试集观测。</summary>
public sealed record ComparisonRound(int Round, int? CandidateOrdinal, int AddedNodes, int NodeCount,
    double SelectionLossBefore, double SelectionLossAfter, StructureFeedback? Feedback,
    IReadOnlyList<TrainingMeasurement> Training, StructureMeasurement? Structure,
    int CandidateCount = 0, double ActionProbability = 1);

/// <summary>单组报告；EndToEnd 包含克隆、布局重建、动作、训练和评估，不包含源数据加载。</summary>
public sealed record ComparisonArm(StructureComparisonMode Mode, IReadOnlyList<ComparisonRound> Rounds,
    DatasetEvaluation FinalTest, int FinalNodeCount, int ParameterCount, TimeSpan EndToEnd, long AllocatedBytes);

/// <summary>可审计的实验配置和数据身份；CPU 时间和分配量不是跨设备性能或峰值内存报告。</summary>
public sealed record StructureComparisonReport(string InitialCheckpointSha256, string SourceDatasetIdentity,
    string TrainingIdentity, string SelectionIdentity, string TestIdentity, DatasetPartitionOptions PartitionOptions,
    StructureComparisonOptions Options, AdamWOptions Optimizer, StructureOptions Controller,
    NetworkBudget Budget, IReadOnlyList<int> PresetSchedule, string Runtime, string OperatingSystem,
    IReadOnlyList<ComparisonArm> Arms);

public sealed partial class RecursiveNetwork
{
    /// <summary>从同一模型快照运行四组 CPU 实验；源模型不变，最终测试集仅在每组训练结束后读取。</summary>
    public StructureComparisonReport CompareStructures(DatasetPartition partition,
        IReadOnlyList<int> presetCandidateOrdinals, StructureComparisonOptions? options = null,
        AdamWOptions? optimizer = null, StructureOptions? controller = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partition);
        ArgumentNullException.ThrowIfNull(presetCandidateOrdinals);
        var configuration = options ?? new StructureComparisonOptions();
        if (configuration.Rounds <= 0 || configuration.UpdatesPerRound <= 0 ||
            (long)configuration.Rounds * configuration.UpdatesPerRound > Budget.MaxSteps ||
            presetCandidateOrdinals.Count != configuration.Rounds || presetCandidateOrdinals.Any(i => i < -1))
            throw new ArgumentOutOfRangeException(nameof(options));
        var adam = optimizer ?? new AdamWOptions();
        var policy = controller ?? new StructureOptions(ObservationUpdates: configuration.UpdatesPerRound);
        ValidateAdamW(adam);
        ValidateStructureOptions(policy);
        if (policy.ObservationUpdates != configuration.UpdatesPerRound) throw new ArgumentException(nameof(controller));
        var schedule = presetCandidateOrdinals.ToArray();
        byte[] checkpoint;
        NetworkBudget budget;
        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_trainingJob is not null || _controller is not null || TextBindings is null) throw new InvalidOperationException();
            if (partition.Training.TokenizerIdentity != TextBindings.TokenizerIdentity ||
                partition.StructureSelection.TokenizerIdentity != TextBindings.TokenizerIdentity ||
                partition.Test.TokenizerIdentity != TextBindings.TokenizerIdentity) throw new ArgumentException(nameof(partition));
            checkpoint = ExportCheckpoint();
            budget = Budget;
        }
        var arms = new List<ComparisonArm>();
        foreach (var mode in Enum.GetValues<StructureComparisonMode>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var start = Stopwatch.GetTimestamp();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var model = ImportCheckpoint(checkpoint, budget, Math.Max(1, checkpoint.Length));
            if (mode == StructureComparisonMode.LearnedGrowth) model.ConfigureStructureController(policy, configuration.Seed);
            var random = configuration.Seed;
            var rounds = new List<ComparisonRound>();
            long updates = 0;
            for (var round = 0; round < configuration.Rounds; round++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var before = model.EvaluateDataset(partition.StructureSelection, cancellationToken);
                var candidates = model.LegalComparisonCandidates(cancellationToken);
                int? ordinal = null;
                var added = 0;
                double actionProbability = 1;
                if (mode == StructureComparisonMode.LearnedGrowth)
                {
                    var decision = model.BeginStructureObservation(before.MeanCrossEntropy, cancellationToken);
                    if (decision.NodeId is not null) ordinal = Array.IndexOf(candidates, decision.NodeId);
                    added = decision.AddedNodes;
                    actionProbability = decision.Probability;
                }
                else if (mode != StructureComparisonMode.FixedInitial)
                {
                    actionProbability = mode == StructureComparisonMode.RandomGrowth ? 1.0 / (candidates.Length + 1) : 1;
                    var choice = mode == StructureComparisonMode.RandomGrowth
                        ? (int)(NextRandom(ref random) * (candidates.Length + 1)) - 1 : schedule[round];
                    if (choice >= candidates.Length) throw new ArgumentOutOfRangeException(nameof(presetCandidateOrdinals));
                    if (choice >= 0)
                    {
                        ordinal = choice;
                        model.Fractalize(candidates[choice], unchecked((int)random), cancellationToken);
                        added = 5;
                    }
                }
                var structure = added > 0 ? model.LastStructureMeasurement : null;
                var measurements = new List<TrainingMeasurement>();
                for (var step = 0; step < configuration.UpdatesPerRound; step++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var index = (int)(updates % partition.Training.Count);
                    model.TrainTokens(partition.Training[index], options: adam, cancellationToken: cancellationToken,
                        labelMask: partition.Training.GetLabelMask(index));
                    measurements.Add(model.LastTrainingMeasurement ?? throw new InvalidOperationException());
                    updates++;
                }
                var after = model.EvaluateDataset(partition.StructureSelection, cancellationToken);
                var feedback = mode == StructureComparisonMode.LearnedGrowth
                    ? model.CompleteStructureObservation(after.MeanCrossEntropy) : null;
                rounds.Add(new ComparisonRound(round, ordinal, added, model.GetNodeObservations(cancellationToken).Count,
                    before.MeanCrossEntropy, after.MeanCrossEntropy, feedback, measurements.AsReadOnly(), structure,
                    candidates.Length, actionProbability));
            }
            var test = model.EvaluateDataset(partition.Test, cancellationToken);
            var layout = ExecutionLayout.Create(model.Graph, model.Budget, cancellationToken: cancellationToken);
            var parameters = layout.Parameters.Count + (model.TextBindings?.Parameters.Count ?? 0);
            arms.Add(new ComparisonArm(mode, rounds.AsReadOnly(), test, layout.Nodes.Count, parameters,
                Stopwatch.GetElapsedTime(start), GC.GetAllocatedBytesForCurrentThread() - allocated));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new StructureComparisonReport(Convert.ToHexString(SHA256.HashData(checkpoint)), partition.SourceIdentity,
            partition.Training.Identity, partition.StructureSelection.Identity, partition.Test.Identity, partition.Options,
            configuration, adam, policy, budget, Array.AsReadOnly(schedule),
            System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            System.Runtime.InteropServices.RuntimeInformation.OSDescription, arms.AsReadOnly());
    }

    private string[] LegalComparisonCandidates(CancellationToken cancellationToken)
    {
        var layout = ExecutionLayout.Create(Graph, Budget, cancellationToken: cancellationToken);
        var depths = GetNodeObservations(cancellationToken).ToDictionary(n => n.NodeId, n => n.Depth);
        var candidates = new List<string>();
        foreach (var node in layout.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node.Value.Child is null && depths[node.Id] < Budget.MaxDepth &&
                layout.Nodes.Count <= Budget.MaxNodes - 5 && Budget.MaxDelay >= 1 &&
                layout.ConnectionCount <= Budget.MaxConnections - 6 &&
                (long)(layout.Operations.Count + 11) * Math.Max(1, layout.MaxDelay) <= Budget.MaxTapeCells)
                candidates.Add(node.Id);
        }
        return candidates.ToArray();
    }
}
