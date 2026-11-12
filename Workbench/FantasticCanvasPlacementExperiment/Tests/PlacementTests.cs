using FantasticCanvasPlacementExperiment.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace FantasticCanvasPlacementExperiment.Tests;

[TestClass]
public sealed class PlacementTests
{
    [TestMethod]
    public void EmptyViewportStartsTopLeftAtCenter()
    {
        Assert.AreEqual(new PointD(60, 70), Placement.Find(new(10, 20, 100, 100), new(10, 10), []));
    }

    [TestMethod]
    public void RightSideIsSearchedBeforeCloserLeftSide()
    {
        Assert.AreEqual(new PointD(90, 50), Placement.Find(new(0, 0, 100, 100), new(10, 10), [new(50, 50, 30, 10)]));
    }

    [TestMethod]
    public void LeftSideIsSearchedBeforeNextRow()
    {
        Assert.AreEqual(new PointD(30, 50), Placement.Find(new(0, 0, 100, 100), new(10, 10), [new(50, 50, 50, 10)]));
    }

    [TestMethod]
    [DataRow(0, 120.0)]
    [DataRow(1, 80.0)]
    [DataRow(2, 140.0)]
    [DataRow(3, 60.0)]
    public void RowsAlternateBelowAndAbove(int blockedRows, double expectedY)
    {
        List<RectD> obstacles = [new(0, 100, 200, 10)];
        RectD[] more = [new(0, 120, 200, 10), new(0, 80, 200, 10), new(0, 140, 200, 10)];
        obstacles.AddRange(more.Take(blockedRows));
        PointD result = Placement.Find(new(0, 0, 200, 200), new(10, 10), obstacles);
        Assert.AreEqual(expectedY, result.Y);
    }

    [TestMethod]
    public void EventSearchMatchesExhaustiveSmallGridFeasibility()
    {
        RectD viewport = new(0, 0, 23, 23);
        RectD[] cells = Enumerable.Range(0, 9).Select(i => new RectD(i % 3 * 11, i / 3 * 11, 1, 1)).ToArray();
        RectD[] candidates = Enumerable.Range(0, 23 * 23).Select(i => new RectD(i % 23, i / 23, 1, 1)).ToArray();
        bool[] matches = Enumerable.Range(0, 512).Select(mask =>
        {
            RectD[] obstacles = cells.Where((_, index) => (mask & (1 << index)) != 0).ToArray();
            bool gridHasSpace = candidates.Any(candidate => obstacles.All(obstacle => HasMinimumGap(candidate, obstacle)));
            PointD result = Placement.Find(viewport, new(1, 1), obstacles);
            RectD rectangle = new(result.X, result.Y, 1, 1);
            return gridHasSpace == viewport.Contains(rectangle) &&
                obstacles.All(obstacle => HasMinimumGap(rectangle, obstacle));
        }).ToArray();
        Assert.IsTrue(matches.All(match => match));
    }

    [TestMethod]
    public void FractionalNarrowSlotIsNotSkipped()
    {
        RectD[] obstacles = [new(0, 0, 39.125, 100), new(59.375, 0, 40.625, 100)];
        Assert.AreEqual(new PointD(49.125, 50), Placement.Find(new(0, 0, 100, 100), new(0.25, 10), obstacles));
    }

    [TestMethod]
    public void VerticalNarrowSlotIsNotSkipped()
    {
        RectD[] obstacles = [new(0, 0, 100, 39.125), new(0, 59.375, 100, 40.625)];
        Assert.AreEqual(new PointD(50, 49.125), Placement.Find(new(0, 0, 100, 100), new(10, 0.25), obstacles));
    }

    [TestMethod]
    public void EntireRectangleMustFitNotOnlyItsTopLeft()
    {
        PointD result = Placement.Find(new(0, 0, 100, 100), new(80, 80), []);
        Assert.IsTrue(new RectD(0, 0, 100, 100).Contains(new(result.X, result.Y, 80, 80)));
    }

    [TestMethod]
    public void TouchingEdgesDoNotOverlap()
    {
        Assert.IsFalse(new RectD(0, 0, 10, 10).Overlaps(new(10, 0, 10, 10)));
    }

    [TestMethod]
    public void ConsecutivePlacementsNeverOverlap()
    {
        List<RectD> occupied = [];
        for (int i = 0; i < 80; i++)
        {
            PointD result = Placement.Find(new(-100, -100, 200, 200), new(31, 23), occupied);
            occupied.Add(new(result.X, result.Y, 31, 23));
        }
        Assert.IsTrue(occupied.SelectMany((rectangle, i) => occupied.Skip(i + 1).Select(other => HasMinimumGap(rectangle, other))).All(gap => gap));
    }

    [TestMethod]
    public void FullRangeExpandsByExactlyOnePointTwo()
    {
        // 扩张障碍占满 [0,100]，首轮扩大后右边界恰好允许 x=100。
        Assert.AreEqual(new PointD(100, 50), Placement.Find(new(0, 0, 100, 100), new(10, 10), [new(10, 10, 80, 80)]));
    }

    [TestMethod]
    public void FirstExpansionAllowsPlacementAtItsLeftBoundary()
    {
        Assert.AreEqual(new PointD(-10, 50), Placement.Find(new(0, 0, 100, 100), new(10, 10), [new(10, 10, 100, 80)]));
    }

    [TestMethod]
    public void RepeatedExpansionPreservesCenter()
    {
        double width = 100;
        double height = 80;
        while (width < 10000 || height < 3000)
        {
            width *= 1.2;
            height *= 1.2;
        }
        Assert.AreEqual(new PointD((60 - width / 2) + width - 10000, 60),
            Placement.Find(new(10, 20, 100, 80), new(10000, 3000), []));
    }

    [TestMethod]
    public void OversizedContainerFitsFinalLogicalRange()
    {
        double width = 100;
        double height = 100;
        while (width < 1e100 || height < 2e100)
        {
            width *= 1.2;
            height *= 1.2;
        }
        Assert.AreEqual(new PointD(50, (50 - height / 2) + height - 2e100),
            Placement.Find(new(0, 0, 100, 100), new(1e100, 2e100), []));
    }

    [TestMethod]
    public void ExpansionDimensionsFollowRepeatedMultiplication()
    {
        double width = 100;
        double height = 80;
        for (int i = 0; i < 12; i++)
        {
            width *= 1.2;
            height *= 1.2;
        }
        Assert.AreEqual(new PointD((50 - width / 2) + width - 700, (40 - height / 2) + height - 600),
            Placement.Find(new(0, 0, 100, 80), new(700, 600), []));
    }

    [TestMethod]
    public void InputCollectionIsNotModified()
    {
        RectD[] occupied = [new(50, 50, 20, 20)];
        RectD[] original = occupied.ToArray();
        Placement.Find(new(0, 0, 100, 100), new(10, 10), Array.AsReadOnly(occupied));
        CollectionAssert.AreEqual(original, occupied);
    }

    [TestMethod]
    public void ViewportAndSizeAreNotModified()
    {
        RectD viewport = new(0, 0, 100, 100);
        SizeD size = new(200, 200);
        Placement.Find(viewport, size, []);
        Assert.AreEqual((new RectD(0, 0, 100, 100), new SizeD(200, 200)), (viewport, size));
    }

    [TestMethod]
    [DataRow(0.0)]
    [DataRow(-1.0)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    public void InvalidSizeIsRejected(double width)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Placement.Find(new(0, 0, 100, 100), new(width, 10), []));
    }

    [TestMethod]
    public void InvalidHeightIsRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Placement.Find(new(0, 0, 100, 100), new(10, 0), []));
    }

    [TestMethod]
    public void NullCollectionIsRejected()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Placement.Find(new(0, 0, 100, 100), new(10, 10), null!));
    }

    [TestMethod]
    public void InvalidObstacleIsRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Placement.Find(new(0, 0, 100, 100), new(10, 10), [new(0, 0, 0, 10)]));
    }

    [TestMethod]
    public void NonFiniteViewportPositionIsRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Placement.Find(new(double.NaN, 0, 100, 100), new(10, 10), []));
    }

    [TestMethod]
    public void UnrepresentableRectangleIsRejected()
    {
        Assert.ThrowsExactly<OverflowException>(() => Placement.Find(new(1e100, 0, 1, 100), new(10, 10), []));
    }

    [TestMethod]
    [DataRow(30.0, 50.0)]
    [DataRow(70.0, 50.0)]
    [DataRow(50.0, 30.0)]
    [DataRow(50.0, 70.0)]
    public void ExactlyTenUnitsOnEverySideAllowsCenter(double x, double y)
    {
        Assert.AreEqual(new PointD(50, 50), Placement.Find(new(0, 0, 100, 100), new(10, 10), [new(x, y, 10, 10)]));
    }

    [TestMethod]
    [DataRow(31.0, 50.0, 51.0, 50.0)]
    [DataRow(69.0, 50.0, 89.0, 50.0)]
    [DataRow(50.0, 31.0, 70.0, 50.0)]
    [DataRow(50.0, 69.0, 70.0, 50.0)]
    public void NineUnitsOnEverySideRequiresAvoidance(double x, double y, double expectedX, double expectedY)
    {
        Assert.AreEqual(new PointD(expectedX, expectedY), Placement.Find(new(0, 0, 100, 100), new(10, 10), [new(x, y, 10, 10)]));
    }

    [TestMethod]
    public void ManuallyOverlappingObstaclesStillRequireAutomaticClearance()
    {
        RectD[] occupied = [new(40, 40, 30, 30), new(50, 50, 30, 30), new(45, 45, 10, 10)];
        PointD result = Placement.Find(new(0, 0, 100, 100), new(10, 10), occupied);
        Assert.IsTrue(occupied.All(obstacle => HasMinimumGap(new(result.X, result.Y, 10, 10), obstacle)));
    }

    [TestMethod]
    public void MixedSizeConsecutivePlacementsPreserveMinimumGap()
    {
        SizeD[] sizes = [new(210, 297), new(480, 270), new(1440, 270), new(0.25, 10), new(31, 23)];
        List<RectD> occupied = [];
        for (int i = 0; i < 40; i++)
        {
            SizeD size = sizes[i % sizes.Length];
            PointD result = Placement.Find(new(-500, -400, 1000, 800), size, occupied);
            occupied.Add(new(result.X, result.Y, size.Width, size.Height));
        }
        Assert.IsTrue(occupied.SelectMany((rectangle, i) => occupied.Skip(i + 1)
            .Select(other => HasMinimumGap(rectangle, other))).All(gap => gap));
    }

    [TestMethod]
    public void ViewportEdgesDoNotRequireMargin()
    {
        Assert.AreEqual(new PointD(0, 0), Placement.Find(new(0, 0, 10, 10), new(10, 10), []));
    }

    [TestMethod]
    [DataRow(1e100, 0.0, 1e90, 10.0)]
    [DataRow(0.0, 1e100, 10.0, 1e90)]
    [DataRow(0.0, 0.0, 1e100, 10.0)]
    [DataRow(0.0, 0.0, 10.0, 1e100)]
    [DataRow(-8e307, 0.0, 1.6e308, 10.0)]
    public void UnrepresentableObstacleMarginIsRejected(double x, double y, double width, double height)
    {
        Assert.ThrowsExactly<OverflowException>(() => Placement.Find(new(0, 0, 100, 100), new(10, 10),
            [new(x, y, width, height)]));
    }

    private static bool HasMinimumGap(RectD first, RectD second) =>
        second.X - first.Right >= 10 || first.X - second.Right >= 10 ||
        second.Y - first.Bottom >= 10 || first.Y - second.Bottom >= 10;

    [TestMethod]
    public void NumericExpansionOverflowIsReported()
    {
        Assert.ThrowsExactly<OverflowException>(() => Placement.Find(new(-8e307, -8e307, 1.6e308, 1.6e308), new(1.7e308, 1.7e308), []));
    }
}
