using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraph.PostExpansionLearning.Tests;

[TestClass]
public class FivePointTests
{
    private const double Step = 1.0 / 16;

    [DataTestMethod]
    [DataRow(true, 1.0, 1.0, 0.0)]
    [DataRow(false, 0.0, 1.0, 0.0)]
    [DataRow(false, 1.0, 1.0, 1.0)]
    [DataRow(false, 0.0, 1.0, 1.0)]
    public void PreservingInitializationsMatchTheParents(bool masked, double gate, double positive, double negative)
    {
        var p = new BranchParameters(gate, positive, negative);
        foreach (double x in new[] { -2.5, 0.0, 1.25 })
        {
            Assert.AreEqual(FivePoint.MergeParent(x, -0.75), FivePoint.Merge(x, -0.75, p, masked));
            Assert.AreEqual(FivePoint.SplitParent(x), FivePoint.Split(x, p, masked));
        }
    }

    [TestMethod]
    public void ActiveBranchesRespectOrderedPorts()
    {
        var p = new BranchParameters(0.5, 2, 1);
        Assert.AreEqual(5.5, FivePoint.Merge(2, -1, p));
        Assert.AreEqual((-0.5, 2.5), FivePoint.Split(-1, p));
    }

    [DataTestMethod]
    [DataRow(true, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0)]
    [DataRow(false, 0.0, 1.0, 0.0, -1.0, 0.0, 0.0)]
    [DataRow(false, 1.0, 1.0, 1.0, 0.0, -1.0, 1.0)]
    [DataRow(false, 0.0, 1.0, 1.0, 0.0, 0.0, 0.0)]
    public void FirstGradientsMatchThePredictedPattern(bool masked, double gate, double positive, double negative,
        double dg, double dp, double dn)
    {
        var p = new BranchParameters(gate, positive, negative);
        var expected = new BranchParameters(dg, dp, dn);
        Assert.AreEqual(expected, FivePoint.MergeLoss(1, 0, 2, p, masked).Gradient);
        Assert.AreEqual(expected, FivePoint.SplitLoss(1, 3, -3, p, masked).Gradient);
    }

    [DataTestMethod]
    [DataRow(true, 1.0, 1.0, 0.0)]
    [DataRow(false, 0.0, 1.0, 0.0)]
    [DataRow(false, 1.0, 1.0, 1.0)]
    [DataRow(false, 0.0, 1.0, 1.0)]
    [DataRow(false, 0.75, -0.25, 0.5)]
    public void CentralDifferencesMatchEveryParameter(bool masked, double gate, double positive, double negative)
    {
        var p = new BranchParameters(gate, positive, negative);
        CheckGradient(q => FivePoint.MergeLoss(-0.75, 0.5, -1.125, q, masked), p);
        CheckGradient(q => FivePoint.SplitLoss(-1.25, 0.125, -0.5, q, masked), p);
    }

    [TestMethod]
    public void OppositeSplitResidualsAreOrthogonalToBothActiveBranches()
    {
        var p = new BranchParameters(1, 1, 1);
        var result = FivePoint.SplitLoss(1, 2, -3, p);
        Assert.AreEqual(new LossGradient(1, default), result);
    }

    [TestMethod]
    public void EqualMergeInputsHideTheBranchesEvenWhenTheResidualIsNonzero()
    {
        var p = new BranchParameters(1, 1, 1);
        Assert.AreEqual(new LossGradient(0.5, default), FivePoint.MergeLoss(1, 1, 3, p));
    }

    [DataTestMethod]
    [DataRow(false, true, 1.0, 0.0, 0.0, 1.0, 0.5)]
    [DataRow(false, false, 0.0, 0.0, 0.0, 1.0 / 16, 0.01)]
    [DataRow(false, false, 1.0, 1.0, 1.0 / 8, 1.0, 0.01)]
    [DataRow(false, false, 0.0, 1.0, 0.0, 0.0, 0.5)]
    [DataRow(true, true, 1.0, 0.0, 0.0, 1.0, 1.0)]
    [DataRow(true, false, 0.0, 0.0, 0.0, 1.0 / 8, 0.01)]
    [DataRow(true, false, 1.0, 1.0, 1.0 / 4, 1.0, 0.01)]
    [DataRow(true, false, 0.0, 1.0, 0.0, 0.0, 1.0)]
    public void GradientDescentSeparatesTheFourInitializations(bool split, bool masked, double gate, double negative,
        double firstDifferenceChange, double firstGate, double finalLossBound)
    {
        var p = new BranchParameters(gate, 1, negative);
        var initial = p;
        LossGradient Evaluate(BranchParameters q) => split
            ? FivePoint.SplitLoss(1, 2, -1, q, masked)
            : FivePoint.MergeLoss(1, 0, 2, q, masked);

        var first = Descend(p, Evaluate(p).Gradient);
        Assert.AreEqual(firstDifferenceChange, (first.Positive - first.Negative) - (p.Positive - p.Negative));
        Assert.AreEqual(firstGate, first.Gate);

        double initialLoss = Evaluate(p).Loss;
        double previousLoss = initialLoss;
        for (int i = 0; i < 24; i++)
        {
            var next = Descend(p, Evaluate(p).Gradient);
            double nextLoss = Evaluate(next).Loss;
            Assert.IsTrue(nextLoss <= previousLoss, $"Loss increased at step {i + 1}.");
            p = next;
            previousLoss = nextLoss;
        }

        if (finalLossBound == initialLoss)
        {
            Assert.AreEqual(initial, p);
            Assert.AreEqual(initialLoss, previousLoss);
        }
        else
        {
            Assert.IsTrue(previousLoss < finalLossBound, $"Step 24 loss: {previousLoss:R}.");
            Assert.AreNotEqual(first.Positive - first.Negative, p.Positive - p.Negative);
        }
    }

    [DataTestMethod]
    [DataRow(false, -15.0 / 256)]
    [DataRow(true, -7.0 / 32)]
    public void ZeroGateEnablesBranchWeightGradientsAfterItsFirstUpdate(bool split, double expected)
    {
        var p = new BranchParameters(0, 1, 0);
        LossGradient Evaluate(BranchParameters q) => split
            ? FivePoint.SplitLoss(1, 2, -1, q)
            : FivePoint.MergeLoss(1, 0, 2, q);
        var next = Descend(p, Evaluate(p).Gradient);
        Assert.AreEqual(expected, Evaluate(next).Gradient.Positive);
    }

    [DataTestMethod]
    [DataRow(false, 0.25)]
    [DataRow(true, 0.5)]
    public void OneTrainingStepCanLeaveTheAffineParentFamily(bool split, double expectedSecondDifference)
    {
        var p = new BranchParameters(1, 1, 1);
        var gradient = split ? FivePoint.SplitLoss(1, 2, -1, p).Gradient : FivePoint.MergeLoss(1, 0, 2, p).Gradient;
        var trained = Descend(p, gradient);
        double Output(double x) => split ? FivePoint.Split(x, trained).First : FivePoint.Merge(x, 0, trained);
        Assert.AreEqual(expectedSecondDifference, Output(-1) - 2 * Output(0) + Output(1));
    }

    private static BranchParameters Descend(BranchParameters p, BranchParameters gradient) =>
        new(p.Gate - Step * gradient.Gate, p.Positive - Step * gradient.Positive, p.Negative - Step * gradient.Negative);

    private static void CheckGradient(Func<BranchParameters, LossGradient> evaluate, BranchParameters p)
    {
        const double h = 1e-6;
        const double tolerance = 1e-8;
        double Difference(BranchParameters plus, BranchParameters minus) => (evaluate(plus).Loss - evaluate(minus).Loss) / (2 * h);
        var gradient = evaluate(p).Gradient;
        Assert.AreEqual(gradient.Gate, Difference(p with { Gate = p.Gate + h }, p with { Gate = p.Gate - h }), tolerance);
        Assert.AreEqual(gradient.Positive, Difference(p with { Positive = p.Positive + h }, p with { Positive = p.Positive - h }), tolerance);
        Assert.AreEqual(gradient.Negative, Difference(p with { Negative = p.Negative + h }, p with { Negative = p.Negative - h }), tolerance);
    }
}
