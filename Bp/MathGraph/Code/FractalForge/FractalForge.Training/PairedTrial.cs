using System.Text.Json;
using MathGraphs.Neural;

namespace FractalForge.Training;

public sealed record TrialProtocol(string RunId, string DataIdentity, string EvaluationIdentity, string RuleVersion,
    int MaxUpdates, long MaxTokens, long MaxPairTapeCells, double MinimumGain, AdamWOptions Optimizer);
public sealed record TrialProgress(long Position, long Tokens, int Updates, ulong RandomState);
public sealed record PairedLoss(double Keep, double Candidate, long Tokens);

/// <summary>同源、同消费位置的串行配对试训；每次双分支更新完整成功后才提交。</summary>
public sealed class PairedTrial
{
    private sealed record TrialData(string Semantics, TrialProtocol Protocol, ForgeAction Action, string SourceFingerprint,
        TrialProgress Progress, string Decision, PairedLoss? Evaluation, string KeepFile, string KeepHash, string CandidateFile, string CandidateHash);
    public const string Semantics = "fractalforge.paired-trial.v1";
    public TrialProtocol Protocol { get; }
    public ForgeAction Action { get; }
    public string SourceFingerprint { get; }
    public ForgeModel Keep { get; private set; }
    public ForgeModel Candidate { get; private set; }
    public TrialProgress Progress { get; private set; }
    public string Decision { get; private set; } = "pending";
    public PairedLoss? Evaluation { get; private set; }

    private PairedTrial(TrialProtocol protocol, ForgeAction action, string fingerprint, ForgeModel keep, ForgeModel candidate, TrialProgress progress)
    { Protocol = protocol; Action = action; SourceFingerprint = fingerprint; Keep = keep; Candidate = candidate; Progress = progress; Validate(); }

    /// <summary>从同一个不可变模型快照开始，记录外部顺序流位置和随机状态。</summary>
    public static PairedTrial Begin(ForgeModel source, ForgeAction action, TrialProtocol protocol, long position = 0, ulong randomState = 1)
    {
        var bytes = ForgeArchive.Export(source);
        var keep = ForgeArchive.Import(bytes, source.Network.Budget);
        return new(protocol, action, ForgeArchive.Fingerprint(bytes), keep, keep.Apply(action), new(position, 0, 0, randomState));
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Protocol.RunId) || string.IsNullOrWhiteSpace(Protocol.DataIdentity) || string.IsNullOrWhiteSpace(Protocol.EvaluationIdentity) ||
            string.IsNullOrWhiteSpace(Protocol.RuleVersion) || Protocol.MaxUpdates <= 0 || Protocol.MaxTokens <= 0 || !double.IsFinite(Protocol.MinimumGain) ||
            Protocol.Optimizer is null || Progress.Position < 0 || Progress.Tokens < 0 || Progress.Updates < 0 || Progress.Updates > Protocol.MaxUpdates || Progress.Tokens > Protocol.MaxTokens ||
            Keep.MeasureCost().WindowTapeCells + Candidate.MeasureCost().WindowTapeCells > Protocol.MaxPairTapeCells) throw new InvalidDataException();
    }

    /// <summary>消费同一完整 token 样本；失败、取消或超预算均保留两侧原状态和游标。</summary>
    public void Advance(IReadOnlyList<int> tokens, long expectedPosition, ulong nextRandomState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (Decision != "pending" || Evaluation is not null || expectedPosition != Progress.Position || tokens.Count < 2 ||
            Progress.Updates >= Protocol.MaxUpdates || tokens.Count - 1 > Protocol.MaxTokens - Progress.Tokens) throw new InvalidOperationException();
        var keep = ForgeArchive.Import(ForgeArchive.Export(Keep), Keep.Network.Budget);
        var candidate = ForgeArchive.Import(ForgeArchive.Export(Candidate), Candidate.Network.Budget);
        keep.Network.TrainTokens(tokens, options: Protocol.Optimizer, cancellationToken: cancellationToken);
        candidate.Network.TrainTokens(tokens, options: Protocol.Optimizer, cancellationToken: cancellationToken);
        var progress = new TrialProgress(checked(Progress.Position + 1), checked(Progress.Tokens + tokens.Count - 1), checked(Progress.Updates + 1), nextRandomState);
        cancellationToken.ThrowIfCancellationRequested();
        Keep = keep; Candidate = candidate; Progress = progress;
    }

    /// <summary>固定终点、token 加权评价；身份必须与预登记接受集一致。</summary>
    public PairedLoss Evaluate(string identity, IReadOnlyList<IReadOnlyList<int>> samples, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (identity != Protocol.EvaluationIdentity || Decision != "pending" || Evaluation is not null || Progress.Updates != Protocol.MaxUpdates || samples.Count == 0) throw new InvalidOperationException();
        double keep = 0, candidate = 0; long tokens = 0;
        foreach (var sample in samples)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sample.Count < 2) throw new ArgumentException(nameof(samples));
            var count = sample.Count - 1;
            keep += Keep.Network.EvaluateTokens(sample, cancellationToken: cancellationToken) * count;
            candidate += Candidate.Network.EvaluateTokens(sample, cancellationToken: cancellationToken) * count;
            tokens = checked(tokens + count);
        }
        var result = new PairedLoss(keep / tokens, candidate / tokens, tokens);
        if (!double.IsFinite(result.Keep) || !double.IsFinite(result.Candidate)) throw new ArithmeticException();
        cancellationToken.ThrowIfCancellationRequested();
        Evaluation = result;
        return result;
    }

    /// <summary>逐样本流式执行固定终点接受评价；全部成功后一次发布 token 加权结果。</summary>
    public async Task<PairedLoss> EvaluateAsync(string identity, IAsyncEnumerable<IReadOnlyList<int>> samples,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (identity != Protocol.EvaluationIdentity || Decision != "pending" || Evaluation is not null || Progress.Updates != Protocol.MaxUpdates)
            throw new InvalidOperationException();
        double keep = 0, candidate = 0;
        long tokens = 0;
        await foreach (var sample in samples.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sample is null || sample.Count < 2) throw new ArgumentException(nameof(samples));
            var count = sample.Count - 1;
            keep += Keep.Network.EvaluateTokens(sample, cancellationToken: cancellationToken) * count;
            candidate += Candidate.Network.EvaluateTokens(sample, cancellationToken: cancellationToken) * count;
            tokens = checked(tokens + count);
        }
        if (tokens == 0) throw new ArgumentException(nameof(samples));
        var result = new PairedLoss(keep / tokens, candidate / tokens, tokens);
        if (!double.IsFinite(result.Keep) || !double.IsFinite(result.Candidate)) throw new ArithmeticException();
        cancellationToken.ThrowIfCancellationRequested();
        Evaluation = result;
        return result;
    }

    /// <summary>根据预登记最小收益选择完整候选或已走完同一窗口的保持分支。</summary>
    public ForgeModel Complete()
    {
        if (Evaluation is null || Decision != "pending") throw new InvalidOperationException();
        Decision = Evaluation.Keep - Evaluation.Candidate > Protocol.MinimumGain ? "candidate" : "keep";
        return Decision == "candidate" ? Candidate : Keep;
    }

    /// <summary>写入不可变内容文件后最后发布指定清单；不使用文件时间选择恢复点。</summary>
    public void Save(string manifestPath)
    {
        Validate();
        var directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        Directory.CreateDirectory(directory);
        var keep = ForgeArchive.Export(Keep); var candidate = ForgeArchive.Export(Candidate);
        var kh = ForgeArchive.Fingerprint(keep); var ch = ForgeArchive.Fingerprint(candidate);
        var kf = kh + ".forge"; var cf = ch + ".forge";
        Store(Path.Combine(directory, kf), keep); Store(Path.Combine(directory, cf), candidate);
        var data = new TrialData(Semantics, Protocol, Action, SourceFingerprint, Progress, Decision, Evaluation, kf, kh, cf, ch);
        var temporary = manifestPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { stream.Write(JsonSerializer.SerializeToUtf8Bytes(data, ForgeArchive.JsonOptions)); stream.Flush(true); }
        File.Move(temporary, manifestPath, true);
        static void Store(string path, byte[] bytes)
        {
            if (File.Exists(path))
            {
                if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) throw new InvalidDataException();
                return;
            }
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            stream.Write(bytes); stream.Flush(true);
        }
    }

    /// <summary>只恢复显式清单引用且哈希一致的配对状态，校验运行协议身份。</summary>
    public static PairedTrial Load(string manifestPath, TrialProtocol expected, NetworkBudget? limits = null)
    {
        var manifest = ReadBounded(manifestPath);
        var data = JsonSerializer.Deserialize<TrialData>(manifest, ForgeArchive.JsonOptions) ?? throw new InvalidDataException();
        if (data.Semantics != Semantics || data.Protocol != expected || data.SourceFingerprint.Length != 64 ||
            data.Decision is not ("pending" or "keep" or "candidate")) throw new InvalidDataException();
        var directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        ForgeModel Read(string file, string hash)
        {
            if (hash.Length != 64 || file != hash + ".forge" || hash.Any(c => !Uri.IsHexDigit(c))) throw new InvalidDataException();
            var bytes = ReadBounded(Path.Combine(directory, file));
            if (ForgeArchive.Fingerprint(bytes) != hash) throw new InvalidDataException();
            return ForgeArchive.Import(bytes, limits);
        }
        var result = new PairedTrial(data.Protocol, data.Action, data.SourceFingerprint, Read(data.KeepFile, data.KeepHash), Read(data.CandidateFile, data.CandidateHash), data.Progress);
        if (data.Evaluation is { } evaluation && (evaluation.Tokens <= 0 || !double.IsFinite(evaluation.Keep) || !double.IsFinite(evaluation.Candidate) || data.Progress.Updates != expected.MaxUpdates)) throw new InvalidDataException();
        if (data.Decision != "pending" && (data.Evaluation is null ||
            data.Decision != (data.Evaluation.Keep - data.Evaluation.Candidate > expected.MinimumGain ? "candidate" : "keep"))) throw new InvalidDataException();
        result.Evaluation = data.Evaluation; result.Decision = data.Decision;
        return result;
    }

    private static byte[] ReadBounded(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 67108864) throw new InvalidDataException();
        var bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes); return bytes;
    }
}
