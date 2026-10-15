using System.Buffers.Binary;
using System.Security.Cryptography;

namespace MathGraphs.Neural;

/// <summary>按精确 token 内容组分割；比例作用于组数而非 token 数。</summary>
public sealed record DatasetPartitionOptions(double StructureSelectionFraction = 0.1,
    double TestFraction = 0.1, ulong Seed = 1);

/// <summary>三个互斥内容组集合及可审计的源样本索引；只读对象不会修改源数据。</summary>
public sealed class DatasetPartition
{
    private DatasetPartition(TokenDataset source, DatasetPartitionOptions options,
        TokenDataset training, TokenDataset selection, TokenDataset test,
        int[] trainingIndices, int[] selectionIndices, int[] testIndices)
    {
        SourceIdentity = source.Identity;
        Options = options;
        Training = training;
        StructureSelection = selection;
        Test = test;
        TrainingIndices = Array.AsReadOnly(trainingIndices);
        StructureSelectionIndices = Array.AsReadOnly(selectionIndices);
        TestIndices = Array.AsReadOnly(testIndices);
    }

    public string SourceIdentity { get; }
    public DatasetPartitionOptions Options { get; }
    public TokenDataset Training { get; }
    public TokenDataset StructureSelection { get; }
    public TokenDataset Test { get; }
    public IReadOnlyList<int> TrainingIndices { get; }
    public IReadOnlyList<int> StructureSelectionIndices { get; }
    public IReadOnlyList<int> TestIndices { get; }

    /// <summary>相同 token 内容不跨集合，标签掩码完整保留；组不足或比例不能产生三个非空集合时拒绝。</summary>
    public static DatasetPartition Create(TokenDataset source, DatasetPartitionOptions? options = null,
        int maxTotalTokens = 1000000, CancellationToken cancellationToken = default, IProgress<DatasetProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTotalTokens);
        var configuration = options ?? new DatasetPartitionOptions();
        if (!double.IsFinite(configuration.StructureSelectionFraction) || configuration.StructureSelectionFraction <= 0 ||
            !double.IsFinite(configuration.TestFraction) || configuration.TestFraction <= 0 ||
            configuration.StructureSelectionFraction + configuration.TestFraction >= 1)
            throw new ArgumentOutOfRangeException(nameof(options));
        cancellationToken.ThrowIfCancellationRequested();
        var groups = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var total = 0;
        var lastReport = System.Diagnostics.Stopwatch.GetTimestamp();
        progress?.Report(new DatasetProgress(DatasetPreparationStage.Grouping, 0, null, 0, 0));
        for (var i = 0; i < source.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sample = source[i];
            if (sample.Count > maxTotalTokens - total) throw new ArgumentOutOfRangeException(nameof(maxTotalTokens));
            total += sample.Count;
            var key = ContentHash(sample, cancellationToken);
            if (!groups.TryGetValue(key, out var members)) groups.Add(key, members = []);
            // 哈希碰撞不能把不相同的内容视为相同样本。
            if (members.Count != 0 && !source[members[0]].SequenceEqual(sample)) throw new InvalidOperationException();
            members.Add(i);
            if (System.Diagnostics.Stopwatch.GetElapsedTime(lastReport).TotalSeconds >= 2)
            {
                progress?.Report(new DatasetProgress(DatasetPreparationStage.Grouping, 0, null, i + 1, total));
                lastReport = System.Diagnostics.Stopwatch.GetTimestamp();
            }
        }
        if (groups.Count < 3) throw new ArgumentException(Numeric.Message("SplitInsufficient"), nameof(source));
        var selectionCount = Math.Max(1, (int)Math.Floor(groups.Count * configuration.StructureSelectionFraction));
        var testCount = Math.Max(1, (int)Math.Floor(groups.Count * configuration.TestFraction));
        if (selectionCount + testCount >= groups.Count)
            throw new ArgumentException(Numeric.Message("SplitInsufficient"), nameof(options));
        progress?.Report(new DatasetProgress(DatasetPreparationStage.SortingGroups, 0, null, source.Count, total));
        var ordered = groups.Select(pair => (Key: pair.Key, Members: pair.Value,
                Rank: Rank(pair.Key, configuration.Seed)))
            .OrderBy(group => group.Rank, StringComparer.Ordinal).ThenBy(group => group.Key, StringComparer.Ordinal).ToArray();
        var selection = ordered.Take(selectionCount).SelectMany(g => g.Members).Order().ToArray();
        var test = ordered.Skip(selectionCount).Take(testCount).SelectMany(g => g.Members).Order().ToArray();
        var training = ordered.Skip(selectionCount + testCount).SelectMany(g => g.Members).Order().ToArray();
        progress?.Report(new DatasetProgress(DatasetPreparationStage.BuildingPartitions, 0, null, source.Count, total));
        var trainingData = Subset(training);
        var selectionData = Subset(selection);
        var testData = Subset(test);
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new DatasetProgress(DatasetPreparationStage.Completed, 0, null, source.Count, total));
        return new DatasetPartition(source, configuration, trainingData, selectionData, testData, training, selection, test);

        TokenDataset Subset(int[] indices)
        {
            var samples = new List<IReadOnlyList<int>>();
            var masks = source.GetLabelMask(0) is null ? null : new List<IReadOnlyList<bool>>();
            foreach (var index in indices)
            {
                cancellationToken.ThrowIfCancellationRequested();
                samples.Add(source[index]);
                if (masks is not null) masks.Add(source.GetLabelMask(index) ?? throw new InvalidOperationException());
            }
            return new TokenDataset(source.TokenizerIdentity, samples, maxTotalTokens, masks, cancellationToken);
        }
    }

    private static string ContentHash(IReadOnlyList<int> sample, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, sample.Count);
        hash.AppendData(buffer);
        foreach (var token in sample)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BinaryPrimitives.WriteInt32LittleEndian(buffer, token);
            hash.AppendData(buffer);
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static string Rank(string contentHash, ulong seed)
    {
        Span<byte> bytes = stackalloc byte[40];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, seed);
        Convert.FromHexString(contentHash).CopyTo(bytes[8..]);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
