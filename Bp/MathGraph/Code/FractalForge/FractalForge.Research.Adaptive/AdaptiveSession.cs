using System.Text.Json;
using System.Text.Json.Serialization;
using FractalForge.Training;

namespace FractalForge.Research.Adaptive;

/// <summary>候选预验证结果；拒绝、预算过滤和构造故障与配对后的收益拒绝分开。</summary>
public sealed record AdaptiveFilter(long Update, ForgeAction Action, string Status, string Detail);

/// <summary>方向内调度器：普通训练与真实同源窗口交替；完整样本重置历史，不迁移跨结构历史。</summary>
public sealed class AdaptiveSession
{
    private const string Semantics = "fractalforge.adaptive.v1";
    private sealed record Checkpoint(string Semantics, string ContractHash, string ModelFile, string ModelHash,
        string? TrialFile, string? TrialHash, long Updates, long Position, long TrainingTokens, long PairTokens,
        int EvaluationCount, AdaptivePolicyState Policy, AdaptiveFilter[] Filters);
    private static readonly JsonSerializerOptions Json = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    private readonly AdaptiveOptions options;
    private readonly TrialProtocol protocol;
    private readonly ForgeAction[] actions;
    private readonly string contractHash;
    private readonly List<AdaptiveFilter> filters = [];
    private PairedTrial? trial;
    private AdaptiveProposal? pending;

    /// <summary>当前已发布模型；pending 时为配对起点，不发布候选中间态。</summary>
    public ForgeModel Model { get; private set; }
    /// <summary>可恢复策略及审计。</summary>
    public AdaptivePolicy Policy { get; }
    /// <summary>主线完整样本更新数，配对两侧一次共同更新仅计一次。</summary>
    public long Updates { get; private set; }
    /// <summary>顺序完整样本游标。</summary>
    public long Position { get; private set; }
    /// <summary>主线监督 token 总量。</summary>
    public long TrainingTokens { get; private set; }
    /// <summary>候选分支额外训练 token 总量。</summary>
    public long PairTokens { get; private set; }
    /// <summary>已完成接受集评价次数；重复接受评价存在选择偏差风险。</summary>
    public int EvaluationCount { get; private set; }
    /// <summary>是否存在尚未完成评价的真实配对窗口。</summary>
    public bool HasPendingTrial => trial is not null;
    /// <summary>待决窗口是否到达固定终点。</summary>
    public bool ReadyToEvaluate => trial?.Progress.Updates == protocol.MaxUpdates;
    /// <summary>预验证过滤审计的隔离副本。</summary>
    public IReadOnlyList<AdaptiveFilter> Filters => filters.ToArray();

    /// <summary>从明确源模型创建运行；动作集合及规则作为恢复契约固定，不消费训练随机流。</summary>
    public AdaptiveSession(ForgeModel model, AdaptiveOptions options, TrialProtocol protocol, IReadOnlyList<ForgeAction> actions)
        : this(model, options, protocol, actions, null) { }

    private AdaptiveSession(ForgeModel model, AdaptiveOptions options, TrialProtocol protocol,
        IReadOnlyList<ForgeAction> actions, AdaptivePolicyState? restored)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(actions);
        Policy = new(options, restored);
        if (actions.Count == 0 || actions.Any(a => a is null || a.Kind is not ("clone-widen" or "output-deepen" or "add-feedback")) ||
            protocol.MaxUpdates <= 0 || protocol.MaxTokens <= 0 || protocol.MaxPairTapeCells <= 0 ||
            !double.IsFinite(protocol.MinimumGain) || protocol.Optimizer is null ||
            string.IsNullOrWhiteSpace(protocol.RunId) || string.IsNullOrWhiteSpace(protocol.RuleVersion) ||
            string.IsNullOrWhiteSpace(protocol.DataIdentity) || string.IsNullOrWhiteSpace(protocol.EvaluationIdentity) ||
            protocol.DataIdentity == protocol.EvaluationIdentity) throw new ArgumentException(AdaptiveText.Get("Invalid"));
        Model = model;
        this.options = options;
        this.protocol = protocol;
        this.actions = actions.Distinct().ToArray();
        contractHash = ForgeArchive.Fingerprint(JsonSerializer.SerializeToUtf8Bytes(new { options, protocol, Actions = this.actions }));
    }

    /// <summary>消费一个完整样本；待决窗口只在双分支成功后提交，普通更新同样先在副本上训练。</summary>
    public void Advance(IReadOnlyList<int> tokens, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        cancellationToken.ThrowIfCancellationRequested();
        if (tokens.Count < 2 || ReadyToEvaluate) throw new InvalidOperationException(AdaptiveText.Get("Invalid"));
        if (trial is null && Updates >= options.WarmupUpdates && Updates % options.OpportunityInterval == 0 &&
            !Policy.Snapshot().Proposals.Any(p => p.Opportunity == Updates))
        {
            var candidates = new List<ForgeAction>();
            foreach (var action in options.Branch == "A0" ? Array.Empty<ForgeAction>() : actions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var candidate = Model.Apply(action, cancellationToken);
                    var cost = candidate.MeasureCost();
                    var fits = cost.ComputeNodes <= options.MaxComputeNodes && cost.Parameters <= options.MaxParameters &&
                        cost.WindowTapeCells <= options.MaxTapeCells &&
                        cost.WindowTapeCells + Model.MeasureCost().WindowTapeCells <= protocol.MaxPairTapeCells;
                    if (fits) candidates.Add(action);
                    else filters.Add(new(Updates, action, "budget", "model-or-pair-budget"));
                }
                catch (ArgumentException ex) { filters.Add(new(Updates, action, "inapplicable", ex.GetType().Name)); }
                catch (InvalidOperationException ex) { filters.Add(new(Updates, action, "inapplicable", ex.GetType().Name)); }
                catch (InvalidDataException ex) { filters.Add(new(Updates, action, "fault", ex.GetType().Name)); }
            }
            pending = Policy.Propose(Updates, candidates);
            if (pending.Action is { } selected)
            {
                try { trial = PairedTrial.Begin(Model, selected, protocol, Position, Policy.Snapshot().RandomState); }
                catch (ArgumentException ex) { Policy.Record(pending, "fault", detail: ex.GetType().Name); }
                catch (InvalidOperationException ex) { Policy.Record(pending, "fault", detail: ex.GetType().Name); }
                catch (InvalidDataException ex) { Policy.Record(pending, "fault", detail: ex.GetType().Name); }
            }
        }
        var observedModel = trial?.Keep ?? Model;
        var loss = observedModel.Network.EvaluateTokens(tokens, cancellationToken: cancellationToken);
        if (trial is not null)
        {
            trial.Advance(tokens, Position, trial.Progress.RandomState, cancellationToken);
            PairTokens = checked(PairTokens + tokens.Count - 1);
        }
        else
        {
            var next = ForgeArchive.Import(ForgeArchive.Export(Model), Model.Network.Budget);
            next.Network.TrainTokens(tokens, options: protocol.Optimizer, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Model = next;
        }
        Policy.Observe(loss);
        Position = checked(Position + 1);
        Updates = checked(Updates + 1);
        TrainingTokens = checked(TrainingTokens + tokens.Count - 1);
    }

    /// <summary>仅固定终点评价并选择完整保持/候选状态；只已尝试配对的真实收益更新评分器。</summary>
    public void Evaluate(string identity, IReadOnlyList<IReadOnlyList<int>> samples, CancellationToken cancellationToken = default)
    {
        if (trial is null || pending is null || !ReadyToEvaluate) throw new InvalidOperationException(AdaptiveText.Get("Invalid"));
        var loss = trial.Evaluate(identity, samples, cancellationToken);
        Model = trial.Complete();
        EvaluationCount++;
        Policy.Record(pending, trial.Decision == "candidate" ? "accepted" : "rejected", loss.Keep - loss.Candidate);
        trial = null;
        pending = null;
    }

    /// <summary>逐样本评价完整接受流，成功后才提交选择与策略收益。</summary>
    public async Task EvaluateAsync(string identity, IAsyncEnumerable<IReadOnlyList<int>> samples, CancellationToken cancellationToken = default)
    {
        if (trial is null || pending is null || !ReadyToEvaluate) throw new InvalidOperationException(AdaptiveText.Get("Invalid"));
        var loss = await trial.EvaluateAsync(identity, samples, cancellationToken).ConfigureAwait(false);
        Model = trial.Complete();
        EvaluationCount++;
        Policy.Record(pending, trial.Decision == "candidate" ? "accepted" : "rejected", loss.Keep - loss.Candidate);
        trial = null;
        pending = null;
    }

    /// <summary>显式结束未完成窗口，发布同游标保持分支；不确定或故障不训练评分器，不冒充收益拒绝。</summary>
    public void Abandon(bool fault, string detail)
    {
        if (trial is null || pending is null) throw new InvalidOperationException(AdaptiveText.Get("Invalid"));
        Model = trial.Keep;
        Policy.Record(pending, fault ? "fault" : "uncertain", detail: detail);
        trial = null;
        pending = null;
    }

    /// <summary>先落盘不可变哈希模型/窗口，再原子发布清单；调用方须持有运行独占写锁。</summary>
    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? throw new ArgumentException(nameof(path));
        Directory.CreateDirectory(directory);
        var modelBytes = ForgeArchive.Export(Model);
        var modelHash = ForgeArchive.Fingerprint(modelBytes);
        Store(Path.Combine(directory, modelHash + ".forge"), modelBytes);
        string? trialHash = null;
        if (trial is not null)
        {
            var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".trial.json");
            trial.Save(temporary);
            var trialBytes = File.ReadAllBytes(temporary);
            trialHash = ForgeArchive.Fingerprint(trialBytes);
            Store(Path.Combine(directory, trialHash + ".trial.json"), trialBytes);
        }
        var checkpoint = new Checkpoint(Semantics, contractHash, modelHash + ".forge", modelHash,
            trialHash is null ? null : trialHash + ".trial.json", trialHash, Updates, Position, TrainingTokens,
            PairTokens, EvaluationCount, Policy.Snapshot(), filters.ToArray());
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        Store(temp, JsonSerializer.SerializeToUtf8Bytes(checkpoint));
        File.Move(temp, path, true);
    }

    /// <summary>恢复明确清单并核对配置与所有内容身份，不查找最新文件，不重新抽样。</summary>
    public static AdaptiveSession Load(string path, AdaptiveOptions options, TrialProtocol protocol, IReadOnlyList<ForgeAction> actions)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? throw new ArgumentException(nameof(path));
        var data = JsonSerializer.Deserialize<Checkpoint>(Read(path, 16777216), Json) ?? throw new InvalidDataException(AdaptiveText.Get("Invalid"));
        byte[] Content(string? name, string? hash, string suffix, int limit)
        {
            if (hash is not { Length: 64 } || hash.Any(c => !char.IsAsciiHexDigit(c)) || name != hash + suffix)
                throw new InvalidDataException(AdaptiveText.Get("Invalid"));
            var bytes = Read(Path.Combine(directory, name), limit);
            if (ForgeArchive.Fingerprint(bytes) != hash) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
            return bytes;
        }
        var model = ForgeArchive.Import(Content(data.ModelFile, data.ModelHash, ".forge", 67108864));
        var session = new AdaptiveSession(model, options, protocol, actions, data.Policy);
        if (data.Semantics != Semantics || data.ContractHash != session.contractHash || data.Updates < 0 ||
            data.Position != data.Updates || data.TrainingTokens < data.Updates || data.PairTokens < 0 ||
            data.PairTokens > data.TrainingTokens || data.EvaluationCount < 0 ||
            data.EvaluationCount != data.Policy.Outcomes.Count(o => o.Status is "accepted" or "rejected"))
            throw new InvalidDataException(AdaptiveText.Get("Invalid"));
        session.Updates = data.Updates;
        session.Position = data.Position;
        session.TrainingTokens = data.TrainingTokens;
        session.PairTokens = data.PairTokens;
        session.EvaluationCount = data.EvaluationCount;
        session.filters.AddRange(data.Filters);
        var outstanding = data.Policy.Proposals.Where(p => p.Action is not null && !data.Policy.Outcomes.Any(o => o.Opportunity == p.Opportunity)).ToArray();
        if (data.TrialFile is not null)
        {
            Content(data.TrialFile, data.TrialHash, ".trial.json", 1048576);
            session.trial = PairedTrial.Load(Path.Combine(directory, data.TrialFile), protocol);
            if (outstanding.Length != 1 || session.trial.Action != outstanding[0].Action || session.trial.Decision != "pending" ||
                session.trial.Progress.Position != data.Position || session.trial.Progress.Updates != data.Updates - outstanding[0].Opportunity ||
                session.trial.SourceFingerprint != data.ModelHash) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
            session.pending = outstanding[0];
        }
        else if (outstanding.Length != 0 || data.TrialHash is not null) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
        return session;
    }

    internal static byte[] Read(string path, int limit)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length <= 0 || stream.Length > limit) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static void Store(string path, byte[] bytes)
    {
        if (File.Exists(path))
        {
            if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
            return;
        }
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(bytes);
        stream.Flush(true);
    }
}
