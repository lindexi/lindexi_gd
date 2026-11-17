using System.Text.Json;
using FractalForge.Training;

namespace FractalForge.Research.Adaptive;

/// <summary>预登记策略参数；四分支共享动作集、窗口和接受规则，预算按已启动配对窗口计。</summary>
public sealed record AdaptiveOptions(string Branch, int OpportunityInterval, int WarmupUpdates, int CooldownUpdates,
    int MaxTrials, double GrowthProbability, double Exploration, double LearningRate, double PlateauScale,
    ulong Seed, int MaxComputeNodes, int MaxParameters, long MaxTapeCells);

/// <summary>分层抽样记录；JointProbability 是增长、类别、位置的乘积，过滤前后均保留增长意愿。</summary>
public sealed record AdaptiveProposal(long Opportunity, double GrowthProbability, double GrowthDraw, bool WantsGrowth,
    ForgeAction? Action, double KindProbability, double PositionProbability, double JointProbability,
    double[] Features, string Reason);

/// <summary>已尝试窗口结果；uncertain 表示未到终点，不作为负收益标签。</summary>
public sealed record AdaptiveOutcome(long Opportunity, string Status, double? Gain, string Detail);

/// <summary>可恢复的近期统计、线性参数、随机流和完整决策审计。</summary>
public sealed record AdaptivePolicyState(ulong RandomState, int Trials, long LastTrialUpdate, long Opportunities,
    double? RecentLoss, double RecentImprovement, int LossObservations, double[] Weights,
    AdaptiveProposal[] Proposals, AdaptiveOutcome[] Outcomes);

/// <summary>A0 不增长、A1 固定概率、A2 近期损失平台启发式、A3 线性评分与显式 epsilon 探索。</summary>
public sealed class AdaptivePolicy
{
    private readonly AdaptiveOptions options;
    private AdaptivePolicyState state;

    /// <summary>创建或恢复策略；拒绝无效配置及不兼容维度。</summary>
    public AdaptivePolicy(AdaptiveOptions options, AdaptivePolicyState? restored = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Branch is not ("A0" or "A1" or "A2" or "A3") || options.OpportunityInterval <= 0 ||
            options.WarmupUpdates < 0 || options.CooldownUpdates < 0 || options.MaxTrials < 0 ||
            !Probability(options.GrowthProbability) || !Probability(options.Exploration) ||
            !double.IsFinite(options.LearningRate) || options.LearningRate <= 0 ||
            !double.IsFinite(options.PlateauScale) || options.PlateauScale <= 0 || options.MaxComputeNodes <= 0 ||
            options.MaxParameters <= 0 || options.MaxTapeCells <= 0) throw new ArgumentException(AdaptiveText.Get("Invalid"));
        this.options = options;
        state = restored is null ? new(options.Seed, 0, -1, 0, null, 0, 0, new double[8], [], []) : Copy(restored);
        if (state.Weights.Length != 8 || state.Weights.Any(w => !double.IsFinite(w)) || state.Trials < 0 ||
            state.Trials > options.MaxTrials || state.LastTrialUpdate < -1 || state.Opportunities < 0 ||
            state.LossObservations < 0 || state.RecentLoss is { } loss && (!double.IsFinite(loss) || loss < 0) ||
            !double.IsFinite(state.RecentImprovement) || state.Proposals.Length != state.Opportunities ||
            state.Proposals.Count(p => p.Action is not null) != state.Trials ||
            state.Proposals.Select(p => p.Opportunity).Distinct().Count() != state.Proposals.Length ||
            state.Outcomes.Select(o => o.Opportunity).Distinct().Count() != state.Outcomes.Length ||
            state.Outcomes.Any(o => !state.Proposals.Any(p => p.Opportunity == o.Opportunity && p.Action is not null)))
            throw new InvalidDataException(AdaptiveText.Get("Invalid"));
    }

    /// <summary>返回隔离快照，调用方不能修改活跃策略的数组。</summary>
    public AdaptivePolicyState Snapshot() => Copy(state);

    /// <summary>以更新前训练样本损失更新近期 EWMA，不读取接受集或最终测试集。</summary>
    public void Observe(double loss)
    {
        if (!double.IsFinite(loss) || loss < 0) throw new ArgumentOutOfRangeException(nameof(loss));
        var previous = state.RecentLoss ?? loss;
        var improvement = (previous - loss) / Math.Max(Math.Abs(previous), 1e-8);
        state = state with { RecentLoss = .8 * previous + .2 * loss,
            RecentImprovement = .8 * state.RecentImprovement + .2 * improvement,
            LossObservations = checked(state.LossObservations + 1) };
    }

    /// <summary>仅在预登记机会调用；增长抽样独立于候选数量，不合法/空候选不会提高增长率。</summary>
    public AdaptiveProposal Propose(long updates, IReadOnlyList<ForgeAction> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (updates < options.WarmupUpdates || updates % options.OpportunityInterval != 0 ||
            state.Proposals.Any(p => p.Opportunity == updates)) throw new InvalidOperationException(AdaptiveText.Get("Invalid"));
        var plateau = Math.Clamp(1 - Math.Max(0, state.RecentImprovement) / options.PlateauScale, 0, 1);
        var probability = options.Branch == "A0" ? 0 : options.GrowthProbability;
        if (options.Branch == "A2") probability *= state.LossObservations < 2 ? 0 : plateau;
        var eligible = state.Trials < options.MaxTrials &&
            (state.LastTrialUpdate < 0 || updates - state.LastTrialUpdate >= options.CooldownUpdates);
        if (!eligible) probability = 0;
        var draw = Next();
        var wants = draw < probability;
        var reason = !eligible ? "cooldown-or-budget" : !wants ? "no-growth" : candidates.Count == 0 ? "no-candidates" : "selected";
        ForgeAction? selected = null;
        double kindProbability = 0, positionProbability = 0;
        double[] features = [];
        if (wants && candidates.Count > 0)
        {
            var groups = candidates.Distinct().GroupBy(a => a.Kind).OrderBy(g => g.Key, StringComparer.Ordinal).ToArray();
            var kindWeights = groups.Select(g => options.Branch is "A2" or "A3" ?
                g.Average(a => Score(Features(a, plateau))) : 1).ToArray();
            var kindDistribution = Distribution(kindWeights);
            var k = Sample(kindDistribution);
            kindProbability = kindDistribution[k];
            var positions = groups[k].OrderBy(a => JsonSerializer.Serialize(a), StringComparer.Ordinal).ToArray();
            var positionDistribution = Distribution(positions.Select(a => options.Branch is "A2" or "A3" ? Score(Features(a, plateau)) : 1).ToArray());
            var p = Sample(positionDistribution);
            selected = positions[p];
            positionProbability = positionDistribution[p];
            features = Features(selected, plateau);
            state = state with { Trials = state.Trials + 1, LastTrialUpdate = updates };
        }
        var proposal = new AdaptiveProposal(updates, probability, draw, wants, selected, kindProbability,
            positionProbability, probability * kindProbability * positionProbability, features, reason);
        state = state with { Opportunities = state.Opportunities + 1, Proposals = [.. state.Proposals, proposal] };
        return proposal;
    }

    /// <summary>记录一次窗口结果；仅终点真实配对收益训练线性评分，未尝试/故障/不确定不加负标签。</summary>
    public void Record(AdaptiveProposal proposal, string status, double? gain = null, string detail = "")
    {
        ArgumentNullException.ThrowIfNull(proposal);
        var attempted = state.Proposals.SingleOrDefault(p => p.Opportunity == proposal.Opportunity);
        if (attempted?.Action is null || attempted.Action != proposal.Action ||
            state.Outcomes.Any(o => o.Opportunity == proposal.Opportunity) ||
            status is not ("accepted" or "rejected" or "uncertain" or "fault") ||
            (status is "accepted" or "rejected" && (gain is null || !double.IsFinite(gain.Value))) ||
            (status is "uncertain" or "fault" && gain is not null)) throw new InvalidOperationException(AdaptiveText.Get("Invalid"));
        var weights = state.Weights.ToArray();
        if (options.Branch == "A3" && gain is { } actual)
        {
            var prediction = Dot(weights, attempted.Features);
            var residual = Math.Clamp(actual, -1, 1) - prediction;
            var norm = 1 + attempted.Features.Sum(x => x * x);
            for (var i = 0; i < weights.Length; i++) weights[i] += options.LearningRate * residual * attempted.Features[i] / norm;
        }
        state = state with { Weights = weights, Outcomes = [.. state.Outcomes, new(proposal.Opportunity, status, gain, detail)] };
    }

    private double[] Features(ForgeAction action, double plateau) => [1, plateau,
        Math.Clamp(state.RecentImprovement / options.PlateauScale, -1, 1),
        action.Kind == "clone-widen" ? 1 : 0, action.Kind == "output-deepen" ? 1 : 0,
        action.Kind == "add-feedback" ? 1 : 0, PositionFeature(action.Target), PositionFeature(action.Destination ?? action.Target)];

    private static double PositionFeature(string target)
    {
        uint hash = 2166136261;
        foreach (var character in target) hash = unchecked((hash ^ character) * 16777619);
        return hash / (double)uint.MaxValue * 2 - 1;
    }

    private double Score(double[] features) => options.Branch == "A3" ? Math.Exp(Math.Clamp(Dot(state.Weights, features), -20, 20)) :
        1 + features[1] * (features[3] + .5 * features[4]) + Math.Max(0, -features[2]) * features[5];

    private double[] Distribution(double[] weights)
    {
        var sum = weights.Sum();
        var epsilon = options.Branch == "A3" ? options.Exploration : 0;
        return weights.Select(w => (1 - epsilon) * w / sum + epsilon / weights.Length).ToArray();
    }

    private int Sample(double[] probabilities)
    {
        var draw = Next();
        for (var i = 0; i < probabilities.Length - 1; i++)
        {
            draw -= probabilities[i];
            if (draw < 0) return i;
        }
        return probabilities.Length - 1;
    }

    private double Next()
    {
        var value = unchecked(state.RandomState + 0x9E3779B97F4A7C15UL);
        state = state with { RandomState = value };
        value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
        value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
        return ((value ^ (value >> 31)) >> 11) * (1.0 / (1UL << 53));
    }

    private static double Dot(double[] weights, double[] features) => weights.Zip(features, (w, x) => w * x).Sum();
    private static bool Probability(double p) => double.IsFinite(p) && p is >= 0 and <= 1;
    private static AdaptivePolicyState Copy(AdaptivePolicyState value) => value with
    {
        Weights = value.Weights.ToArray(), Proposals = value.Proposals.Select(p => p with { Features = p.Features.ToArray() }).ToArray(),
        Outcomes = value.Outcomes.ToArray()
    };
}
