namespace MathGraphs.Neural;

internal struct NodeStatisticsAccumulator
{
    internal long Samples;
    internal double ActivationMean;
    internal double ActivationM2;
    internal double MeanAbsoluteGradient;

    internal NodeStatisticsAccumulator(NodeStatistics statistics)
    {
        Samples = statistics.Samples;
        ActivationMean = statistics.ActivationMean;
        ActivationM2 = statistics.ActivationM2;
        MeanAbsoluteGradient = statistics.MeanAbsoluteGradient;
    }

    internal void Observe(double activation, double gradient)
    {
        var count = checked(Samples + 1);
        var delta = Numeric.RequireFinite(activation - ActivationMean);
        var mean = Numeric.RequireFinite(ActivationMean + delta / count);
        var m2 = Numeric.RequireFinite(ActivationM2 + delta * (activation - mean));
        var magnitude = Numeric.RequireFinite(MeanAbsoluteGradient + (Math.Abs(gradient) - MeanAbsoluteGradient) / count);
        Samples = count;
        ActivationMean = mean;
        ActivationM2 = m2;
        MeanAbsoluteGradient = magnitude;
    }
}
