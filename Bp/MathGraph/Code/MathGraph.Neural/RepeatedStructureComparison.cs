namespace MathGraphs.Neural;

/// <summary>跨结构决策种子的描述性统计，不是显著性检验；初始模型及数据划分固定。</summary>
public sealed record ComparisonSummary(StructureComparisonMode Mode, int Runs, double MeanTestLoss,
    double SampleStandardDeviation, double MeanNodeCount, double MeanAllocatedBytes, double MeanElapsedMilliseconds);

/// <summary>固定初始模型下的多种子报告，保留每次运行，避免只报告最佳结果。</summary>
public sealed record RepeatedComparisonReport(IReadOnlyList<ulong> Seeds,
    IReadOnlyList<StructureComparisonReport> Runs, IReadOnlyList<ComparisonSummary> Summary);

public sealed partial class RecursiveNetwork
{
    /// <summary>固定初始模型和数据划分，按不同结构决策种子运行四组实验并汇总；失败不返回部分成功报告。</summary>
    public RepeatedComparisonReport CompareStructureSeeds(DatasetPartition partition, IReadOnlyList<int> presetCandidateOrdinals,
        IReadOnlyList<ulong> seeds, StructureComparisonOptions? options = null, AdamWOptions? optimizer = null,
        StructureOptions? controller = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partition);
        ArgumentNullException.ThrowIfNull(presetCandidateOrdinals);
        ArgumentNullException.ThrowIfNull(seeds);
        var configuration = options ?? new StructureComparisonOptions();
        if (seeds.Count < 2 || seeds.Count > 64 || configuration.Rounds <= 0 || configuration.UpdatesPerRound <= 0 ||
            (long)configuration.Rounds * configuration.UpdatesPerRound > Budget.MaxSteps / (long)seeds.Count / 4)
            throw new ArgumentOutOfRangeException(nameof(seeds));
        var seedArray = seeds.ToArray();
        if (seedArray.Distinct().Count() != seedArray.Length) throw new ArgumentException(nameof(seeds));
        var schedule = presetCandidateOrdinals.ToArray();
        byte[] checkpoint;
        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_trainingJob is not null || _controller is not null || TextBindings is null) throw new InvalidOperationException();
            checkpoint = ExportCheckpoint();
        }
        var source = ImportCheckpoint(checkpoint, Budget, Math.Max(1, checkpoint.Length));
        var runs = new List<StructureComparisonReport>();
        foreach (var seed in seedArray)
        {
            cancellationToken.ThrowIfCancellationRequested();
            runs.Add(source.CompareStructures(partition, schedule, configuration with { Seed = seed }, optimizer, controller, cancellationToken));
        }
        var summaries = new List<ComparisonSummary>();
        foreach (var mode in Enum.GetValues<StructureComparisonMode>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var arms = runs.Select(run => run.Arms.Single(arm => arm.Mode == mode)).ToArray();
            double mean = 0, m2 = 0;
            for (var i = 0; i < arms.Length; i++)
            {
                var delta = arms[i].FinalTest.MeanCrossEntropy - mean;
                mean += delta / (i + 1);
                m2 += delta * (arms[i].FinalTest.MeanCrossEntropy - mean);
            }
            summaries.Add(new ComparisonSummary(mode, arms.Length, Numeric.RequireFinite(mean),
                Numeric.RequireFinite(Math.Sqrt(m2 / (arms.Length - 1))), arms.Average(a => a.FinalNodeCount),
                arms.Average(a => (double)a.AllocatedBytes), arms.Average(a => a.EndToEnd.TotalMilliseconds)));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new RepeatedComparisonReport(Array.AsReadOnly(seedArray), runs.AsReadOnly(), summaries.AsReadOnly());
    }
}
