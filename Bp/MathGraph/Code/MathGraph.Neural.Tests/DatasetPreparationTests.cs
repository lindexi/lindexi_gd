using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class DatasetPreparationTests
{
    [TestMethod]
    public void BufferedHashMatchesOriginalByteSequence()
    {
        var samples = new[] { Enumerable.Range(0, 2049).ToArray(), new[] { 3, 2, 1 } };
        var labels = samples.Select(row => (IReadOnlyList<bool>)row.Select(i => i % 2 == 0).ToArray()).ToArray();
        var actual = new TokenDataset("test", samples, labelMasks: labels);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var identity = Encoding.UTF8.GetBytes("test");
        Append(identity.Length);
        hash.AppendData(identity);
        Append(samples.Length);
        foreach (var row in samples) { Append(row.Length); foreach (var value in row) Append(value); }
        Append(-1);
        foreach (var row in labels) foreach (var value in row) Append(value ? 1 : 0);
        Assert.AreEqual(Convert.ToHexString(hash.GetHashAndReset()), actual.Identity);
        void Append(int value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            hash.AppendData(bytes);
        }
    }

    [TestMethod]
    public async Task LoadingReportsActualBytesAndStageTransitionsAsync()
    {
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json"),
            Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json"));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"text\":\"Hello\"}\n{\"text\":\"你好\"}"));
        var progress = new Capture();
        await PretrainingJsonl.LoadAsync(stream, tokenizer, progress: progress);
        CollectionAssert.AreEqual(new[] { DatasetPreparationStage.Tokenizing, DatasetPreparationStage.BuildingDataset, DatasetPreparationStage.Completed },
            progress.Values.Select(v => v.Stage).ToArray());
        Assert.AreEqual(stream.Length, progress.Values[^1].Bytes);
        Assert.AreEqual(2L, progress.Values[^1].Samples);
    }

    [TestMethod]
    public void PartitionReportsStagesWithoutChangingMembership()
    {
        var data = new TokenDataset("test", new[] { new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 3 } });
        var progress = new Capture();
        var partition = DatasetPartition.Create(data, progress: progress);
        Assert.AreEqual(DatasetPartition.Create(data).Training.Identity, partition.Training.Identity);
        CollectionAssert.AreEqual(new[] { DatasetPreparationStage.Grouping, DatasetPreparationStage.SortingGroups,
            DatasetPreparationStage.BuildingPartitions, DatasetPreparationStage.Completed }, progress.Values.Select(v => v.Stage).ToArray());
    }

    private sealed class Capture : IProgress<DatasetProgress>
    {
        internal List<DatasetProgress> Values { get; } = [];
        public void Report(DatasetProgress value) => Values.Add(value);
    }
}
