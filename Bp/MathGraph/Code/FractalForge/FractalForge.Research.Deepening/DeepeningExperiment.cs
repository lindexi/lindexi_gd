using System.Text.Json;
using System.Text.Json.Serialization;
using FractalForge.Training;
using MathGraphs.Neural;

namespace FractalForge.Research.Deepening;

public sealed record DeepeningRules(string Identity, string TrainingIdentity, string AcceptanceIdentity, int Updates,
    long MaxTokens, long MaxPairTapeCells, double MinimumGain, int CooldownUpdates, AdamWOptions Optimizer);
public sealed record DeepeningEvaluation(double KeepLoss, double CandidateLoss, double AblatedLoss, long Tokens);
public sealed record DeepeningState(string Semantics, DeepeningRules Rules, DeepeningBranch Branch, string Target,
    string SourceHash, byte[] Keep, byte[] Candidate, Guid[] Gates, long Position, long Tokens, int Updates,
    int CooldownRemaining, DeepeningEvaluation? Evaluation, string Decision);

/// <summary>方向内同源配对实验；更新、评价和冷却均在一致边界提交，可独立恢复。</summary>
public sealed class DeepeningExperiment
{
    public const string Semantics = "fractalforge.deepening.experiment.v1";
    private static readonly JsonSerializerOptions Json = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    private DeepeningState state;
    private DeepeningExperiment(DeepeningState state) { this.state = state; Validate(); }
    public DeepeningState State => state with { Keep = state.Keep.ToArray(), Candidate = state.Candidate.ToArray(), Gates = state.Gates.ToArray() };
    public ForgeModel Keep => ForgeArchive.Import(state.Keep);
    public ForgeModel Candidate => ForgeArchive.Import(state.Candidate);
    public ForgeModel Selected => state.Decision switch { "candidate" => Candidate, "keep" => Keep, _ => throw Messages.Invalid() };

    /// <summary>固定规则、源快照与目标；D0 两侧均保持结构。</summary>
    public static DeepeningExperiment Begin(ForgeModel source, DeepeningBranch branch, string target, double bound,
        DeepeningRules rules, long position = 0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(rules);
        var bytes = ForgeArchive.Export(source);
        var proposal = DeepeningStrategy.Propose(source, branch, target, bound, cancellationToken);
        return new(new(Semantics, rules, branch, proposal.Target, ForgeArchive.Fingerprint(bytes), bytes,
            ForgeArchive.Export(proposal.Model), proposal.Gates, position, 0, 0, rules.CooldownUpdates, null, "pending"));
    }

    private void Validate()
    {
        var r = state.Rules;
        if (state.Semantics != Semantics || r is null || string.IsNullOrWhiteSpace(r.Identity) ||
            string.IsNullOrWhiteSpace(r.TrainingIdentity) || string.IsNullOrWhiteSpace(r.AcceptanceIdentity) ||
            r.TrainingIdentity == r.AcceptanceIdentity || r.Updates <= 0 || r.MaxTokens <= 0 || r.CooldownUpdates < 0 ||
            !double.IsFinite(r.MinimumGain) || r.Optimizer is null || state.Position < 0 || state.Updates < 0 || state.Updates > r.Updates ||
            state.Tokens < 0 || state.Tokens > r.MaxTokens || state.CooldownRemaining < 0 || state.CooldownRemaining > r.CooldownUpdates ||
            !Enum.IsDefined(state.Branch) || state.Decision is not ("pending" or "keep" or "candidate") ||
            (state.Evaluation is null) != (state.Decision == "pending") ||
            Keep.MeasureCost().WindowTapeCells + Candidate.MeasureCost().WindowTapeCells > r.MaxPairTapeCells) throw Messages.Invalid();
    }

    /// <summary>两侧消费相同完整样本；任一侧失败或取消不发布模型与游标。</summary>
    public void Advance(IReadOnlyList<int> tokens, long expectedPosition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (state.Decision != "pending" || state.Updates >= state.Rules.Updates || expectedPosition != state.Position ||
            tokens.Count < 2 || tokens.Count - 1 > state.Rules.MaxTokens - state.Tokens) throw Messages.Invalid();
        var keep = Keep; var candidate = Candidate;
        keep.Network.TrainTokens(tokens, options: state.Rules.Optimizer, cancellationToken: cancellationToken);
        candidate.Network.TrainTokens(tokens, options: state.Rules.Optimizer, cancellationToken: cancellationToken);
        var next = state with { Keep = ForgeArchive.Export(keep), Candidate = ForgeArchive.Export(candidate),
            Position = checked(state.Position + 1), Tokens = checked(state.Tokens + tokens.Count - 1), Updates = state.Updates + 1 };
        cancellationToken.ThrowIfCancellationRequested();
        state = next;
    }

    /// <summary>固定终点评价，token 加权；门消融损失差为路径贡献诊断，不是梯度或因果证明。</summary>
    public DeepeningEvaluation Evaluate(string identity, IEnumerable<IReadOnlyList<int>> samples, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (identity != state.Rules.AcceptanceIdentity || state.Updates != state.Rules.Updates || state.Decision != "pending") throw Messages.Invalid();
        var keep = Keep; var candidate = Candidate;
        var ablated = DeepeningStrategy.Ablate(candidate, state.Gates);
        double k = 0, c = 0, a = 0; long count = 0;
        foreach (var sample in samples)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var weight = sample.Count - 1;
            if (weight <= 0 || weight > state.Rules.MaxTokens - count) throw Messages.Invalid();
            k += keep.Network.EvaluateTokens(sample, cancellationToken: cancellationToken) * weight;
            c += candidate.Network.EvaluateTokens(sample, cancellationToken: cancellationToken) * weight;
            a += ablated.Network.EvaluateTokens(sample, cancellationToken: cancellationToken) * weight;
            count = checked(count + weight);
        }
        if (count == 0) throw Messages.Invalid();
        var result = new DeepeningEvaluation(k / count, c / count, a / count, count);
        cancellationToken.ThrowIfCancellationRequested();
        state = state with { Evaluation = result, Decision = result.KeepLoss - result.CandidateLoss > state.Rules.MinimumGain ? "candidate" : "keep" };
        return result;
    }

    /// <summary>流式固定终点评价；全流成功后才发布接受决策。</summary>
    public async Task<DeepeningEvaluation> EvaluateAsync(string identity, IAsyncEnumerable<IReadOnlyList<int>> samples,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (identity != state.Rules.AcceptanceIdentity || state.Updates != state.Rules.Updates || state.Decision != "pending") throw Messages.Invalid();
        var keep = Keep; var candidate = Candidate;
        var ablated = DeepeningStrategy.Ablate(candidate, state.Gates);
        double k = 0, c = 0, a = 0; long count = 0;
        await foreach (var sample in samples.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var weight = sample.Count - 1;
            if (weight <= 0 || weight > state.Rules.MaxTokens - count) throw Messages.Invalid();
            k += keep.Network.EvaluateTokens(sample, cancellationToken: cancellationToken) * weight;
            c += candidate.Network.EvaluateTokens(sample, cancellationToken: cancellationToken) * weight;
            a += ablated.Network.EvaluateTokens(sample, cancellationToken: cancellationToken) * weight;
            count = checked(count + weight);
        }
        if (count == 0) throw Messages.Invalid();
        var result = new DeepeningEvaluation(k / count, c / count, a / count, count);
        cancellationToken.ThrowIfCancellationRequested();
        state = state with { Evaluation = result, Decision = result.KeepLoss - result.CandidateLoss > state.Rules.MinimumGain ? "candidate" : "keep" };
        return result;
    }

    /// <summary>接受之后继续训练已选模型，逐样本减少冷却；失败不推进。</summary>
    public void Cooldown(IReadOnlyList<int> tokens, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (state.CooldownRemaining <= 0 || tokens.Count < 2 || tokens.Count - 1 > state.Rules.MaxTokens - state.Tokens) throw Messages.Invalid();
        var selected = Selected;
        selected.Network.TrainTokens(tokens, options: state.Rules.Optimizer, cancellationToken: cancellationToken);
        var bytes = ForgeArchive.Export(selected);
        cancellationToken.ThrowIfCancellationRequested();
        state = state with { Keep = state.Decision == "keep" ? bytes : state.Keep,
            Candidate = state.Decision == "candidate" ? bytes : state.Candidate,
            Position = checked(state.Position + 1), Tokens = checked(state.Tokens + tokens.Count - 1), CooldownRemaining = state.CooldownRemaining - 1 };
    }

    /// <summary>冷却结束后从完整选中模型开始下一次显式目标实验，不把透明 O 当计算点。</summary>
    public DeepeningExperiment Next(DeepeningBranch branch, string target, double bound, DeepeningRules rules, CancellationToken cancellationToken = default)
    {
        if (state.CooldownRemaining != 0 || rules.TrainingIdentity != state.Rules.TrainingIdentity) throw Messages.Invalid();
        return Begin(Selected, branch, target, bound, rules, state.Position, cancellationToken);
    }

    /// <summary>单文件原子发布两侧模型与进度；不搜索最新检查点。</summary>
    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full) ?? throw Messages.Invalid());
        var temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, state, Json, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, full, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    /// <summary>从指定检查点恢复，严格匹配预登记规则，限制文件大小。</summary>
    public static async Task<DeepeningExperiment> LoadAsync(string path, DeepeningRules expectedRules, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 192 * 1024 * 1024) throw Messages.Invalid();
        var data = await JsonSerializer.DeserializeAsync<DeepeningState>(stream, Json, cancellationToken).ConfigureAwait(false) ?? throw Messages.Invalid();
        if (data.Rules != expectedRules) throw Messages.Invalid();
        return new(data);
    }
}
