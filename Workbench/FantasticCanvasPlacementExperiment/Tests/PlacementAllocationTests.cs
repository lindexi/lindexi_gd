using FantasticCanvasPlacementExperiment.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FantasticCanvasPlacementExperiment.Tests;

[TestClass]
public sealed class PlacementAllocationTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    public void DeterministicLayoutsMatchOriginalResult(int scenario)
    {
        var (viewport, size, occupied) = Layout(scenario);
        RectD expected = ReferencePlacement.Find(viewport, size, occupied).Rectangle;
        Assert.AreEqual(new PointD(expected.X, expected.Y), Placement.Find(viewport, size, occupied));
    }

    [TestMethod]
    public void MixedSizeSequenceMatchesOriginalResults()
    {
        SizeD[] sizes = [new(210, 297), new(480, 270), new(1440, 270), new(0.25, 10), new(31, 23)];
        List<RectD> originalOccupied = [];
        List<RectD> optimizedOccupied = [];
        List<PointD> expected = [];
        List<PointD> actual = [];
        for (int i = 0; i < 30; i++)
        {
            RectD viewport = new(-731.5, -419.25, 1000, 800);
            SizeD size = sizes[i % sizes.Length];
            ReferenceResult original = ReferencePlacement.Find(viewport, size, originalOccupied);
            PointD optimized = Placement.Find(viewport, size, optimizedOccupied);
            originalOccupied.Add(original.Rectangle);
            optimizedOccupied.Add(new(optimized.X, optimized.Y, size.Width, size.Height));
            expected.Add(new(original.Rectangle.X, original.Rectangle.Y));
            actual.Add(optimized);
        }
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void AllSmallGridLayoutsMatchOriginalResults()
    {
        RectD[] cells = Enumerable.Range(0, 9).Select(i => new RectD(i % 3 * 11 - 17, i / 3 * 11 - 29, 1, 1)).ToArray();
        List<PointD> expected = [];
        List<PointD> actual = [];
        for (int mask = 0; mask < 512; mask++)
        {
            RectD[] occupied = cells.Where((_, index) => (mask & (1 << index)) != 0).ToArray();
            RectD rectangle = ReferencePlacement.Find(new(-17, -29, 23, 23), new(1, 1), occupied).Rectangle;
            expected.Add(new(rectangle.X, rectangle.Y));
            actual.Add(Placement.Find(new(-17, -29, 23, 23), new(1, 1), occupied));
        }
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void NumericExceptionsMatchOriginal(int scenario)
    {
        (RectD viewport, SizeD size, RectD[] occupied) layout = scenario switch
        {
            0 => (new(0, 0, 100, 100), new(double.NaN, 10), []),
            1 => (new(1e100, 0, 1, 100), new(10, 10), []),
            2 => (new(-8e307, -8e307, 1.6e308, 1.6e308), new(1.7e308, 1.7e308), []),
            3 => (new(0, 0, 100, 100), new(10, 10), [new(0, 1e100, 10, 1e90)]),
            _ => (new(1e100, 1e100, 1e90, 1e90), new(1, 1), [])
        };
        var (viewport, size, occupied) = layout;
        var expected = CaptureException(() => ReferencePlacement.Find(viewport, size, occupied));
        var actual = CaptureException(() => Placement.Find(viewport, size, occupied));
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(8, false)]
    [DataRow(64, false)]
    [DataRow(0, true)]
    [DataRow(8, true)]
    [DataRow(64, true)]
    public void AllocationStaysWithinLinearBudget(int count, bool expand)
    {
        RectD viewport = new(-73, -41, 200, 160);
        SizeD size = expand ? new(300, 240) : new(17, 13);
        // 覆盖多个事件行和扩展；重复测量时不构造输入或委托。
        RectD[] occupied = Enumerable.Range(0, count)
            .Select(i => new RectD(-83 + i % 8 * 25, -51 + i / 8 * 20, 17 + i % 3, 13 + i % 5)).ToArray();
        Func<ReferenceResult> original = () => ReferencePlacement.Find(viewport, size, occupied);
        Func<PointD> optimized = () => Placement.Find(viewport, size, occupied);
        for (int i = 0; i < 32; i++)
        {
            original();
            optimized();
        }
        const int iterations = 100;
        long originalBytes = Measure(original, iterations);
        long optimizedBytes = Measure(optimized, iterations);
        TestContext.WriteLine($"n={count}, expand={expand}, calls={iterations}, original={originalBytes} B ({originalBytes / (double)iterations:F2} B/call), optimized={optimizedBytes} B ({optimizedBytes / (double)iterations:F2} B/call)");
        Assert.IsLessThanOrEqualTo(iterations * (1024L + 96L * count), optimizedBytes,
            $"Allocated {optimizedBytes} bytes for {iterations} calls with {count} obstacles.");
    }

    private static long Measure<T>(Func<T> find, int iterations)
    {
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
        {
            find();
        }
        GC.KeepAlive(find);
        return GC.GetAllocatedBytesForCurrentThread() - start;
    }

    private static (Type Type, string? Parameter) CaptureException<T>(Func<T> find)
    {
        try
        {
            find();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return (exception.GetType(), exception.ParamName);
        }
        catch (OverflowException exception)
        {
            return (exception.GetType(), null);
        }
        Assert.Fail("Expected a numeric validation exception.");
        return default;
    }

    private static (RectD Viewport, SizeD Size, RectD[] Occupied) Layout(int scenario) => scenario switch
    {
        0 => (new(-173.5, -291.25, 100, 80), new(17, 13), []),
        1 => (new(-173.5, -291.25, 100, 80), new(17, 13),
            [new(-130, -260, 30, 20), new(-125, -255, 11, 9), new(-140, -270, 40, 35)]),
        2 => (new(-73, -41, 100, 80), new(700, 600), [new(-63, -31, 80, 60)]),
        3 => (new(0, 0, 100, 100), new(0.25, 10), [new(0, 0, 39.125, 100), new(59.375, 0, 40.625, 100)]),
        4 => (new(0, 0, 200, 200), new(10, 10),
            [new(0, 100, 200, 10), new(0, 120, 200, 10), new(0, 80, 200, 10), new(0, 140, 200, 10)]),
        5 => (new(-100, -80, 100, 100), new(10, 10), [new(-70, -30, 10, 10), new(-30, -30, 10, 10)]),
        _ => (new(-10, -20, 10, 10), new(10, 10), [new(100, 100, 10, 10), new(-200, -200, 10, 10)])
    };
}
