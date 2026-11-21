using FractalForge.Core;
using FractalForge.Training;
using MathGraphs.Neural;

namespace FractalForge.Research.Widening;

/// <summary>局部输出信号的相关性与出边加权信号 RMS；不是因果消融贡献。</summary>
public sealed record BranchSignal(string A, string B, double? Correlation, double AWeightedRms, double BWeightedRms);

/// <summary>零初始历史、同一固定 token 样本上的即时交叉熵扰动。</summary>
public sealed record ImmediateLoss(double Source, double Candidate, double Delta);

/// <summary>使用公共执行能力进行只读方向诊断，不训练、不读取最终测试集。</summary>
public static class WideningMetrics
{
    /// <summary>测量同一 token 序列的即时损失变化，调用方负责预登记样本身份。</summary>
    public static ImmediateLoss MeasureLoss(ForgeModel source, ForgeModel candidate, IReadOnlyList<int> tokens,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(candidate);
        var before = source.Network.EvaluateTokens(tokens, cancellationToken: cancellationToken);
        var after = candidate.Network.EvaluateTokens(tokens, cancellationToken: cancellationToken);
        return new(before, after, after - before);
    }

    /// <summary>在显式输入探针上测量一个已登记克隆单元；常量或少于两点时相关性未定义，返回 null。</summary>
    public static BranchSignal MeasureBranches(ForgeModel model, FourPointUnit unit,
        IReadOnlyList<IReadOnlyDictionary<string, double>> inputs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(inputs);
        if (inputs.Count == 0 || unit.Kind != "clone-widen" || !model.Topology.Units.Contains(unit)) throw WideningJson.Invalid();
        var rows = GraphInterpreter.Run(model.Network, inputs, cancellationToken);
        var a = rows.Select(r => r[unit.A]).ToArray();
        var b = rows.Select(r => r[unit.B]).ToArray();
        var ma = a.Average(); var mb = b.Average();
        double aa = 0, bb = 0, ab = 0;
        for (var i = 0; i < a.Length; i++)
        {
            aa += (a[i] - ma) * (a[i] - ma);
            bb += (b[i] - mb) * (b[i] - mb);
            ab += (a[i] - ma) * (b[i] - mb);
        }
        double Rms(string branch, double[] values)
        {
            var weights = model.Network.Graph.ElementList.Single(n => n.Id == branch).OutEdgeList.Select(e => e.EdgeInfo!.Weight.Value).ToArray();
            return Math.Sqrt(values.Average(v => v * v) * weights.Sum(w => w * w));
        }
        return new(unit.A, unit.B, aa > 0 && bb > 0 ? Math.Clamp(ab / Math.Sqrt(aa * bb), -1, 1) : null,
            Rms(unit.A, a), Rms(unit.B, b));
    }
}
