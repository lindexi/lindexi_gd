namespace MathGraphs.Neural;

/// <summary>最近一次成功训练窗口的统计，避免累计统计对旧数据长期滞留。</summary>
public sealed record WindowStatistics(long Samples, double Mean, double Variance, double MeanAbsoluteGradient);

public sealed partial class RecursiveNetwork
{
    private const int StructureFeatureCount = 8;

    private static double[] NodeFeatures(MathGraphElement<Neuron, NeuralConnection> node, int depth)
    {
        var statistics = node.Value.Statistics;
        var recent = statistics.LastWindow;
        var mean = recent?.Mean ?? statistics.ActivationMean;
        var variance = recent?.Variance ?? statistics.ActivationVariance;
        var gradient = recent?.MeanAbsoluteGradient ?? statistics.MeanAbsoluteGradient;
        var spread = Math.Sqrt(variance);
        var g = Compress(gradient);
        var s = Compress(spread);
        return [Compress(Math.Abs(mean)), s, g, g * s, Compress(depth),
            Compress(node.InEdgeList.Count), Compress(node.OutEdgeList.Count), Compress(Math.Log(1.0 + statistics.Samples))];
    }

    private static double Compress(double value) => value / (1 + value);

    private static double[] UpdateFeatureWeights(ControllerData controller, PendingStructure pending, double advantage)
    {
        var weights = (double[])(controller.FeatureWeights ?? new double[StructureFeatureCount]).Clone();
        if (!controller.Options.UseNodeFeatures) return weights;
        var features = pending.Features ?? throw new InvalidOperationException();
        for (var f = 0; f < weights.Length; f++)
        {
            double expected = 0;
            for (var i = 0; i < features.Length; i++) expected += pending.Probabilities[i] * features[i][f];
            weights[f] = Numeric.RequireFinite(weights[f] + controller.Options.LearningRate * advantage *
                (features[pending.Selected][f] - expected));
        }
        return weights;
    }

    private static void ValidateFeatureState(ControllerData controller)
    {
        if (controller.FeatureWeights is { } weights &&
            (weights.Length != StructureFeatureCount || weights.Any(v => !double.IsFinite(v)))) throw new InvalidDataException();
        if (!controller.Options.UseNodeFeatures) return;
        if (controller.FeatureWeights is null) throw new InvalidDataException();
        if (controller.Pending is not { } pending) return;
        var rows = pending.Features;
        if (rows is null || pending.Actions is null || rows.Length != pending.Actions.Length || rows.Length == 0 ||
            rows.Any(row => row is null || row.Length != StructureFeatureCount || row.Any(v => !double.IsFinite(v) || v < 0 || v > 1)) ||
            rows[0].Any(v => v != 0)) throw new InvalidDataException();
    }
}
