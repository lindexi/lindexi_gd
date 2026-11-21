using MathGraph.GdIfs.Wpf.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraph.GdIfs.Wpf.Tests;

/// <summary>Pure geometry tests; no window, dispatcher, or UI thread is required.</summary>
[TestClass]
public class CycleModelTests
{
    /// <summary>Every edge connects to the next vertex, including the closing edge.</summary>
    [TestMethod]
    public void EdgesFormClosedC5()
    {
        CollectionAssert.AreEqual(
            new[] { new CycleEdge(0, 1), new CycleEdge(1, 2), new CycleEdge(2, 3), new CycleEdge(3, 4), new CycleEdge(4, 0) },
            CycleModel.Edges.ToArray());
    }

    /// <summary>Depth zero is exactly the base ring.</summary>
    [TestMethod]
    public void DepthZeroHasOneRing() => Assert.AreEqual(1, CycleModel.Create(0, 0, 0.2).Rings.Count);

    /// <summary>Each depth adds exactly one ring.</summary>
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(6)]
    [DataRow(8)]
    public void RingCountGrowsLinearly(int depth) => Assert.AreEqual(depth + 1, CycleModel.Create(2, depth, 0.3).Rings.Count);

    /// <summary>Containers do not count as vertices.</summary>
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(8)]
    public void NodeCountExcludesContainers(int depth) => Assert.AreEqual(5 * (depth + 1), CycleModel.Create(1, depth, 0.12).NodeCount);

    /// <summary>Containment does not create inter-level edges.</summary>
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(8)]
    public void EdgeCountExcludesContainment(int depth) => Assert.AreEqual(5 * (depth + 1), CycleModel.Create(4, depth, 0.36).EdgeCount);

    /// <summary>Every selected index locates its child at the corresponding parent vertex.</summary>
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void SelectionDeterminesChildPosition(int selected)
    {
        var model = CycleModel.Create(selected, 2, 0.25);
        Assert.AreEqual(model.Rings[0].Nodes[selected], model.Rings[1].Center);
    }

    /// <summary>Changing the supernode changes the child position.</summary>
    [TestMethod]
    public void ChangingSelectionMovesChild() => Assert.AreNotEqual(
        CycleModel.Create(0, 1, 0.25).Rings[1].Center,
        CycleModel.Create(1, 1, 0.25).Rings[1].Center);

    /// <summary>All child vertices follow the same similarity map at every level.</summary>
    [DataTestMethod]
    [DataRow(0.12)]
    [DataRow(0.28)]
    [DataRow(0.36)]
    public void EveryLevelUsesTheContraction(double ratio)
    {
        var model = CycleModel.Create(3, 8, ratio);
        double maxError = model.Rings.Skip(1).Max(ring =>
        {
            var parent = model.Rings[ring.Level - 1];
            return ring.Nodes.Select((point, index) =>
            {
                var expected = new GraphPoint(
                    ring.Center.X + ratio * (parent.Nodes[index].X - parent.Center.X),
                    ring.Center.Y + ratio * (parent.Nodes[index].Y - parent.Center.Y));
                return Math.Max(Math.Abs(expected.X - point.X), Math.Abs(expected.Y - point.Y));
            }).Max();
        });
        Assert.AreEqual(0, maxError, 1e-12);
    }

    /// <summary>The innermost radius is the corresponding power of the ratio.</summary>
    [TestMethod]
    public void RadiusContractsAtEveryLevel() => Assert.AreEqual(Math.Pow(0.28, 8), CycleModel.Create(0, 8, 0.28).Rings[8].Radius, 1e-15);

    /// <summary>Invalid node indices are rejected.</summary>
    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(5)]
    public void InvalidSelectionThrows(int selection) => Assert.ThrowsException<ArgumentOutOfRangeException>(() => CycleModel.Create(selection, 1, 0.2));

    /// <summary>Invalid recursion depths are rejected.</summary>
    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(9)]
    public void InvalidDepthThrows(int depth) => Assert.ThrowsException<ArgumentOutOfRangeException>(() => CycleModel.Create(0, depth, 0.2));

    /// <summary>Invalid and nonfinite contraction ratios are rejected.</summary>
    [DataTestMethod]
    [DataRow(0.119)]
    [DataRow(0.361)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    public void InvalidContractionThrows(double ratio) => Assert.ThrowsException<ArgumentOutOfRangeException>(() => CycleModel.Create(0, 1, ratio));
}
