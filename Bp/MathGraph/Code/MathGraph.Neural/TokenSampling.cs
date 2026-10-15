namespace MathGraphs.Neural;

/// <summary>与仓库 MiniMind 顺序一致的采样配置；重复 token 的 logit 均除以惩罚系数。</summary>
public sealed record SamplingOptions(double Temperature = 1, double RepetitionPenalty = 1,
    int TopK = 0, double TopP = 1, bool DoSample = true);

public static class TokenSampling
{
    /// <summary>计算经过温度、重复惩罚及 Top-K/Top-P 筛选的归一化概率，不修改输入。</summary>
    public static IReadOnlyList<double> Probabilities(IReadOnlyList<double> logits,
        IReadOnlyList<int> context, SamplingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(logits);
        ArgumentNullException.ThrowIfNull(context);
        var configuration = options ?? new SamplingOptions();
        Validate(configuration);
        if (logits.Count == 0 || logits.Any(v => !double.IsFinite(v))) throw new ArgumentException(nameof(logits));
        if (context.Any(id => (uint)id >= (uint)logits.Count)) throw new ArgumentException(nameof(context));
        var values = logits.Select(v => Numeric.RequireFinite(v / configuration.Temperature)).ToArray();
        foreach (var id in context.Distinct()) values[id] = Numeric.RequireFinite(values[id] / configuration.RepetitionPenalty);
        var ordered = Enumerable.Range(0, values.Length).OrderByDescending(i => values[i]).ToArray();
        if (configuration.TopK > 0 && configuration.TopK < values.Length)
        {
            var threshold = values[ordered[configuration.TopK - 1]];
            for (var i = 0; i < values.Length; i++) if (values[i] < threshold) values[i] = double.NegativeInfinity;
        }
        var maximum = values.Max();
        var probabilities = values.Select(v => Math.Exp(v - maximum)).ToArray();
        var total = probabilities.Sum();
        double cumulative = 0;
        foreach (var index in ordered)
        {
            var probability = probabilities[index] / total;
            if (cumulative > configuration.TopP) probabilities[index] = 0;
            cumulative += probability;
        }
        total = probabilities.Sum();
        for (var i = 0; i < probabilities.Length; i++) probabilities[i] /= total;
        return Array.AsReadOnly(probabilities);
    }

    internal static void Validate(SamplingOptions options)
    {
        if (!double.IsFinite(options.Temperature) || options.Temperature <= 0 ||
            !double.IsFinite(options.RepetitionPenalty) || options.RepetitionPenalty <= 0 ||
            options.TopK < 0 || !double.IsFinite(options.TopP) || options.TopP <= 0 || options.TopP > 1)
            throw new ArgumentOutOfRangeException(nameof(options));
    }

    internal static int Select(IReadOnlyList<double> probabilities, bool sample, ref ulong randomState)
    {
        if (!sample)
        {
            var best = 0;
            for (var i = 1; i < probabilities.Count; i++) if (probabilities[i] > probabilities[best]) best = i;
            return best;
        }
        randomState = unchecked(randomState + 0x9E3779B97F4A7C15UL);
        var value = randomState;
        value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
        value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
        value ^= value >> 31;
        var draw = (value >> 11) * (1.0 / (1UL << 53));
        double cumulative = 0;
        var lastPositive = 0;
        for (var i = 0; i < probabilities.Count; i++)
        {
            if (probabilities[i] > 0) lastPositive = i;
            cumulative += probabilities[i];
            if (draw < cumulative) return i;
        }
        return lastPositive;
    }
}
