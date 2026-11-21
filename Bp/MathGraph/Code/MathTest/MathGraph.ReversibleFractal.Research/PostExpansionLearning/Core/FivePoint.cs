namespace MathGraph.PostExpansionLearning;

/// <summary>Parameters of two independent branches and their shared gate.</summary>
public readonly record struct BranchParameters(double Gate, double Positive, double Negative);

/// <summary>The squared-error loss and its derivatives in parameter order.</summary>
public readonly record struct LossGradient(double Loss, BranchParameters Gradient);

/// <summary>Fixed affine parents and their trainable, ordered five-point expansions.</summary>
public static class FivePoint
{
    /// <summary>Evaluates the original 2-to-1 parent.</summary>
    public static double MergeParent(double x, double y) => x + y;

    /// <summary>Evaluates the original 1-to-2 parent in output port order.</summary>
    public static (double First, double Second) SplitParent(double t) => (t, -2 * t);

    /// <summary>Evaluates A, B, C, D, E in the forward wiring.</summary>
    public static double Merge(double x, double y, BranchParameters p, bool masked = false)
    {
        var (a0, a1) = Copy(x);
        var (b0, b1) = Copy(y);
        double c = a0 + Branch(Gate(p, masked), p.Positive, Square(a0 - b0));
        double d = b1 - Branch(Gate(p, masked), p.Negative, Square(a1 - b1));
        return MergeParent(c, d);
    }

    /// <summary>Evaluates E, C, D, A, B with every forward wire reversed.</summary>
    public static (double First, double Second) Split(double t, BranchParameters p, bool masked = false)
    {
        var (e0, e1) = SplitParent(t);
        var (c0, c1) = Copy(e0);
        var (d0, d1) = Copy(e1);
        return (Mix(c0, c0, d0 / 2, p, masked), Mix(d1, d1 / 2, c1, p, masked));
    }

    /// <summary>Evaluates the merge loss and its analytical branch gradient for one sample.</summary>
    public static LossGradient MergeLoss(double x, double y, double target, BranchParameters p, bool masked = false)
    {
        double error = Merge(x, y, p, masked) - target;
        return Differentiate(0.5 * error * error, error * Square(x - y), p, masked);
    }

    /// <summary>Evaluates the split loss and its analytical branch gradient for one sample.</summary>
    public static LossGradient SplitLoss(double t, double firstTarget, double secondTarget, BranchParameters p, bool masked = false)
    {
        var (first, second) = Split(t, p, masked);
        double error0 = first - firstTarget;
        double error1 = second - secondTarget;
        return Differentiate(0.5 * (error0 * error0 + error1 * error1), (error0 + error1) * Square(t), p, masked);
    }

    private static double Mix(double basis, double positiveInput, double negativeInput, BranchParameters p, bool masked)
    {
        double gate = Gate(p, masked);
        return basis + (Branch(gate, p.Positive, Square(positiveInput)) - Branch(gate, p.Negative, Square(negativeInput)));
    }

    private static (double First, double Second) Copy(double x) => (x, x);

    private static double Square(double x) => x * x;

    private static double Gate(BranchParameters p, bool masked) => masked ? 0 : p.Gate;

    private static double Branch(double gate, double weight, double feature) => gate == 0 ? 0 : gate * weight * feature;

    private static LossGradient Differentiate(double loss, double signal, BranchParameters p, bool masked) =>
        new(loss, masked ? default : new((p.Positive - p.Negative) * signal, p.Gate * signal, -p.Gate * signal));
}
