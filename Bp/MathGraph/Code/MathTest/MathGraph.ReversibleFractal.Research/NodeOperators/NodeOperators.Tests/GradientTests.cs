using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraph.ReversibleFractal.Research.NodeOperators.Tests;

[TestClass]
public class GradientTests
{
    [TestMethod]
    public void BaseIdentityMatchesHandCalculatedForwardAndGradients()
    {
        var graph = FullCross.Create([], _ => Activation.Identity);
        double[] parameters = [2, 1, -1, 2, 3, -1, 1, 4, 5, -2, 1, -3, 4, -2, 2, -1, 3];
        var result = graph.Differentiate([0.5, -1], parameters, [1]);

        CollectionAssert.AreEqual(new[] { 35.5, 17, -16, 5, 10, 1.5, 3, 4, -4, 4, -4,
            4, -8, 2, -1.5, -3, -1, 19, 5.5, 1 },
            result.Outputs.Concat(result.Inputs).Concat(result.Parameters).ToArray());
    }

    [TestMethod]
    public void UnsortedGraphSumsRepeatedBindingsFanOutAndAliasedSeeds()
    {
        var join = new AffineOperator(2, 1, Activation.Identity);
        var split = new AffineOperator(1, 2, Activation.Identity);
        var graph = new OperatorGraph(1,
            [new(join, [new(2, 0), new(2, 0)]), new(join, [new(2, 0), new(2, 1)]), new(split, [new(-1, 0)])],
            [new(0, 0), new(1, 0), new(2, 0), new(2, 1), new(2, 0)]);
        var result = graph.Differentiate([2], [3, -1, 0.5, -2, 4, 1, 2, 1, -3, 4], [2, -1, 3, 5, -2]);

        CollectionAssert.AreEqual(new[] { 10.5, -17, 5, -2, 5, 11, 10, 10, 2, -5, 2, -1, 14, 7, 2, 1 },
            result.Outputs.Concat(result.Inputs).Concat(result.Parameters).ToArray());
    }

    [TestMethod]
    public void TanhUsesSechSquaredForBothOutputRows()
    {
        var graph = new OperatorGraph(1, [new(new(1, 2, Activation.Tanh), [new(-1, 0)])], [new(0, 0), new(0, 1)]);
        var result = graph.Differentiate([0.5], [2, -0.25, -3, 1], [2, -1]);
        double d0 = 2 / Math.Pow(Math.Cosh(0.75), 2);
        double d1 = -1 / Math.Pow(Math.Cosh(-0.5), 2);
        double[] expected = [2 * d0 - 3 * d1, 0.5 * d0, d0, 0.5 * d1, d1];
        AssertCoordinates(expected, result.Inputs.Concat(result.Parameters).ToArray(), 1e-14);
    }

    [DataTestMethod]
    [DataRow("base", "identity", 5)]
    [DataRow("base", "tanh", 5)]
    [DataRow("base", "mixed", 5)]
    [DataRow("reverse", "mixed", 9)]
    [DataRow("forward", "mixed", 9)]
    [DataRow("both", "identity", 13)]
    [DataRow("both", "tanh", 13)]
    [DataRow("both", "mixed", 13)]
    [DataRow("nested", "mixed", 17)]
    public void FullCrossVjpMatchesCentralDifferences(string layout, string activation, int nodeCount)
    {
        string[] expanded = layout switch
        {
            "base" => [], "reverse" => ["A"], "forward" => ["C"], "both" => ["A", "C"],
            "nested" => ["A/A", "C", "A"], _ => throw new ArgumentException(nameof(layout))
        };
        var graph = FullCross.Create(expanded, path => activation switch
        {
            "identity" => Activation.Identity,
            "tanh" => Activation.Tanh,
            _ => Mixed(path)
        });
        Assert.AreEqual(nodeCount, graph.NodeCount);
        double[] inputs = [0.37, -0.68];
        var parameters = CreateParameters(graph.ParameterCount);
        double[] seed = [-1.7];
        var result = graph.Differentiate(inputs, parameters, seed);

        AssertCoordinates(CentralDifferences(inputs, x => seed[0] * graph.Forward(x, parameters)[0]), result.Inputs, 2e-8);
        AssertCoordinates(CentralDifferences(parameters, p => seed[0] * graph.Forward(inputs, p)[0]), result.Parameters, 2e-8);
    }

    [TestMethod]
    public void MultipleGraphOutputsMatchCentralDifferencesOfSeededSum()
    {
        var graph = new OperatorGraph(2,
            [new(new(2, 1, Activation.Tanh), [new(1, 1), new(1, 0)]),
                new(new(2, 2, Activation.Identity), [new(-1, 0), new(-1, 1)])],
            [new(1, 1), new(0, 0), new(1, 0), new(-1, 1)]);
        double[] inputs = [0.37, -0.68];
        var parameters = CreateParameters(graph.ParameterCount);
        double[] seed = [0.9, -1.7, 2.3, -0.4];
        var result = graph.Differentiate(inputs, parameters, seed);

        AssertCoordinates(CentralDifferences(inputs, x => Objective(x, parameters)), result.Inputs, 2e-8);
        AssertCoordinates(CentralDifferences(parameters, p => Objective(inputs, p)), result.Parameters, 2e-8);

        double Objective(double[] x, double[] p) => graph.Forward(x, p).Zip(seed, (y, s) => y * s).Sum();
    }

    [TestMethod]
    public void BothTemplatesMatchExplicitPortComposition()
    {
        var graph = FullCross.Create(["C", "A"], Mixed);
        var p = CreateParameters(graph.ParameterCount);
        // In A..E leaf order, a split has four parameters and a join has three.
        int[] offsets = [0, 3, 6, 10, 14, 18, 22, 26, 30, 33, 36, 39, 42];
        var (a, b) = Split(4, "A/E", 0.37);
        var (c, d) = Split(2, "A/C", a);
        var (e, f) = Split(3, "A/D", b);
        double a0 = Join(0, "A/A", c, e);
        double a1 = Join(1, "A/B", d, f);
        var (b0, b1) = Split(5, "B", -0.68);
        var (g, h) = Split(6, "C/A", a0);
        var (i, j) = Split(7, "C/B", b0);
        double c0 = Join(10, "C/E", Join(8, "C/C", g, i), Join(9, "C/D", h, j));
        double d0 = Join(11, "D", a1, b1);
        double expected = Join(12, "E", c0, d0);

        Assert.AreEqual(expected, graph.Forward([0.37, -0.68], p)[0], 1e-14);

        double Join(int node, string path, double x, double y)
        {
            int k = offsets[node];
            return Apply(path, p[k] * x + p[k + 1] * y + p[k + 2]);
        }
        (double, double) Split(int node, string path, double x)
        {
            int k = offsets[node];
            return (Apply(path, p[k] * x + p[k + 1]), Apply(path, p[k + 2] * x + p[k + 3]));
        }
    }

    [TestMethod]
    public void ConstructorRejectsCycle()
    {
        var op = new AffineOperator(1, 1, Activation.Identity);
        Assert.ThrowsException<ArgumentException>(() =>
            new OperatorGraph(1, [new(op, [new(1, 0)]), new(op, [new(0, 0)])], [new(0, 0)]));
    }

    [TestMethod]
    public void ConstructorRejectsUnknownPort()
    {
        var op = new AffineOperator(1, 1, Activation.Identity);
        Assert.ThrowsException<ArgumentException>(() =>
            new OperatorGraph(1, [new(op, [new(-1, 1)])], [new(0, 0)]));
    }

    [TestMethod]
    public void ExpansionRejectsMissingAncestor() =>
        Assert.ThrowsException<ArgumentException>(() => FullCross.Create(["A/A"], Mixed));

    private static Activation Mixed(string path) => path[^1] is 'B' or 'D' ? Activation.Tanh : Activation.Identity;
    private static double Apply(string path, double z) => Mixed(path) == Activation.Tanh ? Math.Tanh(z) : z;

    private static double[] CreateParameters(int count) => Enumerable.Range(0, count)
        .Select(i => (((i * 17 + 5) % 23) - 11) * 0.09 + 0.03).ToArray();

    private static double[] CentralDifferences(double[] coordinates, Func<double[], double> objective)
    {
        const double h = 1e-5;
        var result = new double[coordinates.Length];
        var sample = (double[])coordinates.Clone();
        for (int i = 0; i < sample.Length; i++)
        {
            sample[i] = coordinates[i] + h;
            double plus = objective(sample);
            sample[i] = coordinates[i] - h;
            double minus = objective(sample);
            sample[i] = coordinates[i];
            result[i] = (plus - minus) / (2 * h);
        }
        return result;
    }

    private static void AssertCoordinates(double[] expected, double[] actual, double tolerance)
    {
        Assert.AreEqual(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
            Assert.IsTrue(Math.Abs(expected[i] - actual[i]) <= tolerance * (1 + Math.Abs(expected[i])),
                $"Coordinate {i}: expected {expected[i]:G17}, actual {actual[i]:G17}.");
    }
}
