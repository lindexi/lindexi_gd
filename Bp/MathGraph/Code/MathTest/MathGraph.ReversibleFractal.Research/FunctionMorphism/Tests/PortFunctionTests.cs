using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraph.FunctionMorphism.Tests;

[TestClass]
public sealed class PortFunctionTests
{
    [TestMethod]
    public void ForwardWiresPreserveEveryPortIndex()
    {
        var module = PortFunction<int>.FivePoint(
            PortFunction<int>.Split(x => (x + 10, x + 20)),
            PortFunction<int>.Split(y => (10 * y - 3, 20 * y - 3)),
            PortFunction<int>.Merge((u, v) => u + 2 * v),
            PortFunction<int>.Merge((u, v) => 3 * u - v),
            PortFunction<int>.Merge((u, v) => 5 * u + v));

        CollectionAssert.AreEqual(new[] { -141 }, module.Evaluate(2, -3));
    }

    [TestMethod]
    public void ReverseWiresPreserveEveryPortIndex()
    {
        var module = PortFunction<int>.FivePoint(
            PortFunction<int>.Merge((u, v) => 5 * u - v),
            PortFunction<int>.Merge((u, v) => u + 3 * v),
            PortFunction<int>.Split(x => (x + 10, x + 20)),
            PortFunction<int>.Split(x => (x - 3, 2 * x)),
            PortFunction<int>.Split(x => (x + 2, 3 * x - 1)));

        CollectionAssert.AreEqual(new[] { 72, 92 }, module.Evaluate(4));
    }

    [TestMethod]
    public void ForwardExpansionReducesToTheOriginalOrderedFormalTerm()
    {
        var original = PortFunction<string>.Merge((x, y) => $"f({x},{y})");

        CollectionAssert.AreEqual(new[] { "f(x,y)" }, original.Expand().Evaluate("x", "y"));
    }

    [TestMethod]
    public void ReverseExpansionReducesToTheOriginalOrderedFormalTerms()
    {
        var original = PortFunction<string>.Split(x => ($"g0({x})", $"g1({x})"));

        CollectionAssert.AreEqual(new[] { "g0(x)", "g1(x)" }, original.Expand().Evaluate("x"));
    }

    [TestMethod]
    public void NestedExpansionsOnBothEndsOfWiresRetainNonlinearDagFunction()
    {
        var original = PortFunction<BigInteger>.FivePoint(
            PortFunction<BigInteger>.Split(x => (x * x, x - 1)),
            PortFunction<BigInteger>.Split(y => (y + 2, y * y * y)),
            PortFunction<BigInteger>.Merge((a, b) => a - 2 * b),
            PortFunction<BigInteger>.Merge((a, b) => a * b),
            PortFunction<BigInteger>.Merge((a, b) => a * a + 3 * b));
        var nested = original.Expand('A').Expand('A', 'A').Expand('A', 'A', 'B')
            .Expand('C').Expand('C', 'B').Expand('C', 'B', 'A').Expand('E').Expand('E', 'E');

        CollectionAssert.AreEqual(new BigInteger[] { -45, -45 },
            original.Evaluate(2, -3).Concat(nested.Evaluate(2, -3)).ToArray());
    }

    [TestMethod]
    public void NestedReverseExpansionPreservesBothNonlinearOutputs()
    {
        var original = PortFunction<BigInteger>.Split(x => (x * x + 1, x * x * x - x));
        var nested = original.Expand().Expand('A').Expand('A', 'B').Expand('E').Expand('E', 'E');

        CollectionAssert.AreEqual(new BigInteger[] { 17, -60 }, nested.Evaluate(-4));
    }

    [TestMethod]
    public void KeepingForwardOperatorWithoutRoutingDoesNotPreserveFunction()
    {
        var sum = PortFunction<int>.Merge((x, y) => x + y);
        var copy = PortFunction<int>.Split(x => (x, x));
        var original = PortFunction<int>.Merge((x, y) => x * x - y);
        var module = PortFunction<int>.FivePoint(copy, copy, sum, sum, original);

        CollectionAssert.AreEqual(new[] { 7, 2 }, new[] { original.Evaluate(2, -3)[0], module.Evaluate(2, -3)[0] });
    }

    [TestMethod]
    public void KeepingReverseOperatorWithoutRoutingDoesNotPreservePorts()
    {
        var sum = PortFunction<int>.Merge((x, y) => x + y);
        var copy = PortFunction<int>.Split(x => (x, x));
        var original = PortFunction<int>.Split(x => (x * x, x - 1));
        var module = PortFunction<int>.FivePoint(sum, sum, copy, copy, original);

        CollectionAssert.AreEqual(new[] { 9, -4, 5, 5 },
            original.Evaluate(-3).Concat(module.Evaluate(-3)).ToArray());
    }

    [TestMethod]
    public void DroppingForwardOperatorEvenWithCorrectRoutingChangesFunction()
    {
        var original = PortFunction<int>.Merge((x, y) => x * x - y);
        var replacement = PortFunction<int>.Merge((x, y) => x + y).Expand();

        CollectionAssert.AreEqual(new[] { 7, -1 },
            original.Evaluate(2, -3).Concat(replacement.Evaluate(2, -3)).ToArray());
    }

    [TestMethod]
    public void DroppingReverseOperatorEvenWithCorrectRoutingChangesPorts()
    {
        var original = PortFunction<int>.Split(x => (x * x, x - 1));
        var replacement = PortFunction<int>.Split(x => (x, x)).Expand();

        CollectionAssert.AreEqual(new[] { 9, -4, -3, -3 },
            original.Evaluate(-3).Concat(replacement.Evaluate(-3)).ToArray());
    }

    [TestMethod]
    public void SharedTanhCanBeOutOfReachOfAllFivePointOutputsWithUnitFinalWeights()
    {
        var merge = PortFunction<double>.Merge((x, y) => Math.Tanh(x + y));
        var split = PortFunction<double>.Split(x => (Math.Tanh(x), Math.Tanh(x)));
        var module = PortFunction<double>.FivePoint(split, split, merge, merge, merge);

        Assert.IsTrue(module.Evaluate(3, 0)[0] <= Math.Tanh(2) && merge.Evaluate(3, 0)[0] > Math.Tanh(2));
    }

    [TestMethod]
    public void SharedTanhWithUnitFinalWeightsAlsoRestrictsBothReverseOutputs()
    {
        var merge = PortFunction<double>.Merge((x, y) => Math.Tanh(x + y));
        var split = PortFunction<double>.Split(x => (Math.Tanh(x), Math.Tanh(-x)));
        var module = PortFunction<double>.FivePoint(merge, merge, split, split, split);
        var limit = Math.Tanh(2);

        Assert.IsTrue(module.Evaluate(3).Zip(split.Evaluate(3),
            (actual, expected) => Math.Abs(actual) <= limit && Math.Abs(expected) > limit).All(value => value));
    }

    [TestMethod]
    public void ScaledTanhRoutingObeysTheCompactDomainErrorBound()
    {
        const double alpha = 0.125;
        const double radius = 2;
        double Route(double x) => Math.Tanh(alpha * x) / alpha;
        var copy = PortFunction<double>.Split(x => (Route(x), Route(x)));
        var first = PortFunction<double>.Merge((x, _) => Route(x));
        var second = PortFunction<double>.Merge((_, y) => Route(y));
        var original = PortFunction<double>.Merge((x, y) => Math.Tanh(x - y));
        var module = PortFunction<double>.FivePoint(copy, copy, first, second, original);
        var error = Math.Abs(module.Evaluate(1.5, -2)[0] - original.Evaluate(1.5, -2)[0]);
        var bound = 2 * 2 * alpha * alpha * radius * radius * radius / 3;

        Assert.IsTrue(error > 0 && error <= bound);
    }

    [TestMethod]
    public void RoutingIntroducesNoRoundingBeyondTheOriginalDoubleOperator()
    {
        var original = PortFunction<double>.Merge((x, y) => x + y);
        var expanded = original.Expand().Expand('B').Expand('C').Expand('E');
        var expected = original.Evaluate(0.1, 0.2)[0];
        var actual = expanded.Evaluate(0.1, 0.2)[0];

        CollectionAssert.AreEqual(new[] { 0x3fd3333333333334L, 0x3fd3333333333334L, 0x3fd3333333333333L },
            new[] { BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual), BitConverter.DoubleToInt64Bits(0.3) });
    }

    [TestMethod]
    public void ForwardPerturbationsCanAttainTheLipschitzBound()
    {
        var copy = PortFunction<int>.Split(x => (x + 1, x + 1));
        var first = PortFunction<int>.Merge((x, _) => x + 1);
        var second = PortFunction<int>.Merge((_, y) => y + 1);
        var original = PortFunction<int>.Merge((x, y) => 2 * x + 3 * y);
        var e = PortFunction<int>.Merge((x, y) => 2 * x + 3 * y + 1);
        var perturbed = PortFunction<int>.FivePoint(copy, copy, first, second, e);

        Assert.AreEqual(1 + 5 * (1 + 1), perturbed.Evaluate(2, -3)[0] - original.Evaluate(2, -3)[0]);
    }

    [TestMethod]
    public void ReversePerturbationsCanAttainTheBoundOnBothPorts()
    {
        var first = PortFunction<int>.Merge((x, _) => x + 1);
        var second = PortFunction<int>.Merge((_, y) => y + 1);
        var copy = PortFunction<int>.Split(x => (x + 1, x + 1));
        var original = PortFunction<int>.Split(x => (x * x, x - 1));
        var e = PortFunction<int>.Split(x => (x * x + 1, x));
        var perturbed = PortFunction<int>.FivePoint(first, second, copy, copy, e);
        var actual = perturbed.Evaluate(-3);
        var expected = original.Evaluate(-3);

        CollectionAssert.AreEqual(new[] { 3, 3 }, actual.Zip(expected, (a, b) => a - b).ToArray());
    }
}
