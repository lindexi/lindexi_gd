using System.Security.Cryptography;
using System.Text;

namespace MathGraphs.Neural;

/// <summary>固定顺序的独立 token 样本集合，复制输入并按内容生成稳定身份。</summary>
public sealed class TokenDataset
{
    private readonly IReadOnlyList<int>[] _samples;
    private readonly IReadOnlyList<bool>[]? _labelMasks;

    /// <summary>创建有界不可变数据集；每个样本至少包含两个 token。</summary>
    public TokenDataset(string tokenizerIdentity, IEnumerable<IReadOnlyList<int>> samples, int maxTokens = 1000000,
        IReadOnlyList<IReadOnlyList<bool>>? labelMasks = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenizerIdentity);
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTokens);
        TokenizerIdentity = tokenizerIdentity;
        var copied = new List<IReadOnlyList<int>>();
        var total = 0;
        foreach (var sample in samples)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(sample);
            if (sample.Count < 2 || sample.Count > maxTokens - total) throw new ArgumentException(nameof(samples));
            var copy = sample.ToArray();
            if (copy.Any(token => token < 0)) throw new ArgumentException(nameof(samples));
            total += copy.Length;
            copied.Add(Array.AsReadOnly(copy));
        }
        if (copied.Count == 0) throw new ArgumentException(nameof(samples));
        _samples = copied.ToArray();
        if (labelMasks is not null)
        {
            if (labelMasks.Count != _samples.Length) throw new ArgumentException(nameof(labelMasks));
            _labelMasks = new IReadOnlyList<bool>[_samples.Length];
            for (var i = 0; i < _samples.Length; i++)
            {
                var labels = labelMasks[i];
                if (labels is null || labels.Count != _samples[i].Count) throw new ArgumentException(nameof(labelMasks));
                var copy = labels.ToArray();
                if (!copy.Skip(1).Any(value => value)) throw new ArgumentException(nameof(labelMasks));
                _labelMasks[i] = Array.AsReadOnly(copy);
            }
        }
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var identityBytes = Encoding.UTF8.GetBytes(tokenizerIdentity);
        var hashBuffer = new byte[4096];
        var buffered = 0;
        Append(identityBytes.Length);
        Flush();
        hash.AppendData(identityBytes);
        Append(_samples.Length);
        foreach (var sample in _samples)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Append(sample.Count);
            foreach (var token in sample) Append(token);
        }
        if (_labelMasks is not null)
        {
            Append(-1);
            foreach (var labels in _labelMasks)
                foreach (var label in labels) Append(label ? 1 : 0);
        }
        Flush();
        cancellationToken.ThrowIfCancellationRequested();
        Identity = Convert.ToHexString(hash.GetHashAndReset());

        void Append(int value)
        {
            if (buffered == hashBuffer.Length) Flush();
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(hashBuffer.AsSpan(buffered, 4), value);
            buffered += 4;
        }

        void Flush()
        {
            cancellationToken.ThrowIfCancellationRequested();
            hash.AppendData(hashBuffer.AsSpan(0, buffered));
            buffered = 0;
        }
    }

    public string TokenizerIdentity { get; }
    public string Identity { get; }
    public int Count => _samples.Length;
    public IReadOnlyList<int> this[int index] => _samples[index];

    /// <summary>返回样本的目标位置掩码；null 表示所有有效下一 token 均参与训练。</summary>
    public IReadOnlyList<bool>? GetLabelMask(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
        return _labelMasks?[index];
    }
}

/// <summary>固定顺序独立样本训练的恢复状态；下一样本游标在参数成功更新后推进。</summary>
public sealed record TrainingJobState(string DatasetIdentity, string TokenizerIdentity, int SampleCount,
    AdamWOptions Optimizer, int Epochs, int MaxSampleTokens, long CompletedSamples = 0)
{
    public long TotalSamples => (long)SampleCount * Epochs;
    public bool IsCompleted => CompletedSamples == TotalSamples;
    public long CompletedEpochs => CompletedSamples / SampleCount;
    public int NextSampleIndex => (int)(CompletedSamples % SampleCount);
}
