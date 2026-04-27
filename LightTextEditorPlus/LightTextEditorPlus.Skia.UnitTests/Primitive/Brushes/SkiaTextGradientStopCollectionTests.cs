using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using LightTextEditorPlus.Primitive;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;

namespace LightTextEditorPlus.Primitive.UnitTests;


/// <summary>
/// Unit tests for <see cref="SkiaTextGradientStopCollection"/>.
/// </summary>
[TestClass]
public class SkiaTextGradientStopCollectionTests
{
    /// <summary>
    /// Tests that GetCacheList correctly applies opacity to gradient stops and returns arrays with expected values.
    /// </summary>
    /// <param name="opacity">The opacity value to apply.</param>
    /// <param name="inputAlpha">The input alpha channel value.</param>
    /// <param name="expectedAlpha">The expected alpha channel value after applying opacity.</param>
    [TestMethod]
    [DataRow(1.0, (byte)255, (byte)255)]
    [DataRow(0.5, (byte)255, (byte)127)]
    [DataRow(0.0, (byte)255, (byte)0)]
    [DataRow(1.0, (byte)128, (byte)128)]
    [DataRow(0.5, (byte)128, (byte)64)]
    [DataRow(0.25, (byte)200, (byte)50)]
    [DataRow(0.75, (byte)100, (byte)75)]
    public void GetCacheList_WithVariousOpacityValues_AppliesOpacityCorrectly(double opacity, byte inputAlpha, byte expectedAlpha)
    {
        // Arrange
        var color = new SKColor(255, 100, 50, inputAlpha);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(opacity);

        // Assert
        Assert.AreEqual(1, result.ColorList.Length);
        Assert.AreEqual(1, result.OffsetList.Length);
        Assert.AreEqual(expectedAlpha, result.ColorList[0].Alpha);
        Assert.AreEqual((byte)255, result.ColorList[0].Red);
        Assert.AreEqual((byte)100, result.ColorList[0].Green);
        Assert.AreEqual((byte)50, result.ColorList[0].Blue);
        Assert.AreEqual(0.5f, result.OffsetList[0]);
    }

    /// <summary>
    /// Tests that GetCacheList handles opacity values greater than 1.0.
    /// When opacity exceeds 1.0, the result may overflow and wrap around due to byte casting.
    /// </summary>
    [TestMethod]
    public void GetCacheList_WithOpacityGreaterThanOne_HandlesOverflow()
    {
        // Arrange
        var color = new SKColor(255, 100, 50, 200);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(2.0);

        // Assert
        // 200 * 2.0 = 400, cast to byte = 144 (400 % 256)
        Assert.AreEqual((byte)144, result.ColorList[0].Alpha);
    }

    /// <summary>
    /// Tests that GetCacheList handles negative opacity values.
    /// Negative opacity may result in unexpected alpha values due to casting behavior.
    /// </summary>
    [TestMethod]
    public void GetCacheList_WithNegativeOpacity_HandlesCastingBehavior()
    {
        // Arrange
        var color = new SKColor(255, 100, 50, 100);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(-0.5);

        // Assert
        // 100 * -0.5 = -50, cast to byte results in 206 (wrapped around: 256 - 50 = 206)
        Assert.AreEqual((byte)206, result.ColorList[0].Alpha);
    }

    /// <summary>
    /// Tests that GetCacheList handles double.NaN opacity value.
    /// NaN multiplication results in NaN, which casts to 0 as a byte.
    /// </summary>
    [TestMethod]
    public void GetCacheList_WithNaNOpacity_ResultsInZeroAlpha()
    {
        // Arrange
        var color = new SKColor(255, 100, 50, 128);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(double.NaN);

        // Assert
        Assert.AreEqual((byte)0, result.ColorList[0].Alpha);
    }

    /// <summary>
    /// Tests that GetCacheList handles double.PositiveInfinity opacity value.
    /// Infinity multiplication with a finite value results in infinity, which casts to 0 as a byte.
    /// </summary>
    [TestMethod]
    public void GetCacheList_WithPositiveInfinityOpacity_ResultsInZeroAlpha()
    {
        // Arrange
        var color = new SKColor(255, 100, 50, 128);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(double.PositiveInfinity);

        // Assert
        Assert.AreEqual((byte)0, result.ColorList[0].Alpha);
    }

    /// <summary>
    /// Tests that GetCacheList handles double.NegativeInfinity opacity value.
    /// Negative infinity multiplication with a finite value results in negative infinity, which casts to 0 as a byte.
    /// </summary>
    [TestMethod]
    public void GetCacheList_WithNegativeInfinityOpacity_ResultsInZeroAlpha()
    {
        // Arrange
        var color = new SKColor(255, 100, 50, 128);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(double.NegativeInfinity);

        // Assert
        Assert.AreEqual((byte)0, result.ColorList[0].Alpha);
    }

    /// <summary>
    /// Tests that GetCacheList works correctly with an empty collection.
    /// Should return empty arrays without throwing exceptions.
    /// </summary>
    [TestMethod]
    public void GetCacheList_WithEmptyCollection_ReturnsEmptyArrays()
    {
        // Arrange
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(0);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(1.0);

        // Assert
        Assert.AreEqual(0, result.ColorList.Length);
        Assert.AreEqual(0, result.OffsetList.Length);
    }

    /// <summary>
    /// Tests that GetCacheList works correctly with a single gradient stop.
    /// Should return arrays with one element each.
    /// </summary>
    [TestMethod]
    public void GetCacheList_WithSingleGradientStop_ReturnsCorrectArrays()
    {
        // Arrange
        var color = new SKColor(200, 150, 100, 180);
        var gradientStop = new SkiaTextGradientStop(color, 0.75f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(0.5);

        // Assert
        Assert.AreEqual(1, result.ColorList.Length);
        Assert.AreEqual(1, result.OffsetList.Length);
        Assert.AreEqual((byte)90, result.ColorList[0].Alpha); // 180 * 0.5 = 90
        Assert.AreEqual((byte)200, result.ColorList[0].Red);
        Assert.AreEqual((byte)150, result.ColorList[0].Green);
        Assert.AreEqual((byte)100, result.ColorList[0].Blue);
        Assert.AreEqual(0.75f, result.OffsetList[0]);
    }

    /// <summary>
    /// Tests that GetCacheList works correctly with multiple gradient stops.
    /// Should return arrays with all gradient stops processed.
    /// </summary>
    [TestMethod]
    public void GetCacheList_WithMultipleGradientStops_ReturnsCorrectArrays()
    {
        // Arrange
        var color1 = new SKColor(255, 0, 0, 255);
        var color2 = new SKColor(0, 255, 0, 200);
        var color3 = new SKColor(0, 0, 255, 100);
        var gradientStop1 = new SkiaTextGradientStop(color1, 0.0f);
        var gradientStop2 = new SkiaTextGradientStop(color2, 0.5f);
        var gradientStop3 = new SkiaTextGradientStop(color3, 1.0f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(3);
        mockList.Setup(x => x[0]).Returns(gradientStop1);
        mockList.Setup(x => x[1]).Returns(gradientStop2);
        mockList.Setup(x => x[2]).Returns(gradientStop3);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(0.6);

        // Assert
        Assert.AreEqual(3, result.ColorList.Length);
        Assert.AreEqual(3, result.OffsetList.Length);

        // First gradient stop
        Assert.AreEqual((byte)153, result.ColorList[0].Alpha); // 255 * 0.6 = 153
        Assert.AreEqual((byte)255, result.ColorList[0].Red);
        Assert.AreEqual(0.0f, result.OffsetList[0]);

        // Second gradient stop
        Assert.AreEqual((byte)120, result.ColorList[1].Alpha); // 200 * 0.6 = 120
        Assert.AreEqual((byte)255, result.ColorList[1].Green);
        Assert.AreEqual(0.5f, result.OffsetList[1]);

        // Third gradient stop
        Assert.AreEqual((byte)60, result.ColorList[2].Alpha); // 100 * 0.6 = 60
        Assert.AreEqual((byte)255, result.ColorList[2].Blue);
        Assert.AreEqual(1.0f, result.OffsetList[2]);
    }

    /// <summary>
    /// Tests that GetCacheList reuses the same cache arrays across multiple calls.
    /// The returned arrays should be the same instance (reference equality).
    /// </summary>
    [TestMethod]
    public void GetCacheList_CalledMultipleTimes_ReusesCacheArrays()
    {
        // Arrange
        var color = new SKColor(255, 100, 50, 200);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result1 = collection.GetCacheList(0.5);
        var result2 = collection.GetCacheList(0.8);

        // Assert
        Assert.AreSame(result1.ColorList, result2.ColorList);
        Assert.AreSame(result1.OffsetList, result2.OffsetList);
    }

    /// <summary>
    /// Tests that GetCacheList updates cached arrays with new opacity values on subsequent calls.
    /// The arrays are reused but their contents should be updated.
    /// </summary>
    [TestMethod]
    public void GetCacheList_CalledWithDifferentOpacity_UpdatesCachedValues()
    {
        // Arrange
        var color = new SKColor(255, 100, 50, 200);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result1 = collection.GetCacheList(0.5);
        var firstAlpha = result1.ColorList[0].Alpha;

        var result2 = collection.GetCacheList(0.8);
        var secondAlpha = result2.ColorList[0].Alpha;

        // Assert
        Assert.AreEqual((byte)100, firstAlpha); // 200 * 0.5 = 100
        Assert.AreEqual((byte)160, secondAlpha); // 200 * 0.8 = 160
    }

    /// <summary>
    /// Tests that GetCacheList correctly handles gradient stops with zero alpha.
    /// Zero alpha should remain zero regardless of opacity value.
    /// </summary>
    [TestMethod]
    public void GetCacheList_WithZeroAlpha_RemainsZero()
    {
        // Arrange
        var color = new SKColor(255, 100, 50, 0);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(0.5);

        // Assert
        Assert.AreEqual((byte)0, result.ColorList[0].Alpha);
    }

    /// <summary>
    /// Tests that GetCacheList correctly preserves offset values.
    /// Offset values should be copied unchanged regardless of opacity.
    /// </summary>
    [TestMethod]
    [DataRow(0.0f)]
    [DataRow(0.25f)]
    [DataRow(0.5f)]
    [DataRow(0.75f)]
    [DataRow(1.0f)]
    [DataRow(-0.5f)]
    [DataRow(1.5f)]
    public void GetCacheList_WithVariousOffsets_PreservesOffsetValues(float offset)
    {
        // Arrange
        var color = new SKColor(255, 100, 50, 200);
        var gradientStop = new SkiaTextGradientStop(color, offset);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(0.5);

        // Assert
        Assert.AreEqual(offset, result.OffsetList[0]);
    }

    /// <summary>
    /// Tests that GetCacheList preserves RGB color channels unchanged.
    /// Only the alpha channel should be modified by opacity.
    /// </summary>
    [TestMethod]
    public void GetCacheList_AlwaysPreservesRGBChannels()
    {
        // Arrange
        var color = new SKColor(123, 45, 67, 200);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(0.3);

        // Assert
        Assert.AreEqual((byte)123, result.ColorList[0].Red);
        Assert.AreEqual((byte)45, result.ColorList[0].Green);
        Assert.AreEqual((byte)67, result.ColorList[0].Blue);
    }

    /// <summary>
    /// Tests that GetCacheList handles extreme alpha values correctly.
    /// Tests boundary conditions with alpha at minimum and maximum byte values.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0, 0.5, (byte)0)]
    [DataRow((byte)1, 0.5, (byte)0)]
    [DataRow((byte)255, 0.5, (byte)127)]
    [DataRow((byte)255, 1.0, (byte)255)]
    [DataRow((byte)1, 1.0, (byte)1)]
    public void GetCacheList_WithExtremeAlphaValues_HandlesCorrectly(byte inputAlpha, double opacity, byte expectedAlpha)
    {
        // Arrange
        var color = new SKColor(255, 100, 50, inputAlpha);
        var gradientStop = new SkiaTextGradientStop(color, 0.5f);
        var mockList = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockList.Setup(x => x.Count).Returns(1);
        mockList.Setup(x => x[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockList.Object);

        // Act
        var result = collection.GetCacheList(opacity);

        // Assert
        Assert.AreEqual(expectedAlpha, result.ColorList[0].Alpha);
    }

    /// <summary>
    /// Tests that GetEnumerator returns an enumerator that correctly iterates over an empty collection.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_EmptyCollection_ReturnsEmptyEnumerator()
    {
        // Arrange
        var emptyList = new List<SkiaTextGradientStop>();
        var collection = new SkiaTextGradientStopCollection(emptyList);

        // Act
        var enumerator = ((IEnumerable<SkiaTextGradientStop>)collection).GetEnumerator();
        var items = new List<SkiaTextGradientStop>();
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        // Assert
        Assert.AreEqual(0, items.Count);
    }

    /// <summary>
    /// Tests that GetEnumerator returns an enumerator that correctly iterates over a collection with a single item.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_SingleItem_ReturnsSingleItem()
    {
        // Arrange
        var expectedStop = new SkiaTextGradientStop(new SKColor(255, 0, 0), 0.5f);
        var list = new List<SkiaTextGradientStop> { expectedStop };
        var collection = new SkiaTextGradientStopCollection(list);

        // Act
        var enumerator = ((IEnumerable<SkiaTextGradientStop>)collection).GetEnumerator();
        var items = new List<SkiaTextGradientStop>();
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        // Assert
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual(expectedStop, items[0]);
    }

    /// <summary>
    /// Tests that GetEnumerator returns an enumerator that correctly iterates over a collection with multiple items in the correct order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_MultipleItems_ReturnsItemsInCorrectOrder()
    {
        // Arrange
        var stop1 = new SkiaTextGradientStop(new SKColor(255, 0, 0), 0.0f);
        var stop2 = new SkiaTextGradientStop(new SKColor(0, 255, 0), 0.5f);
        var stop3 = new SkiaTextGradientStop(new SKColor(0, 0, 255), 1.0f);
        var list = new List<SkiaTextGradientStop> { stop1, stop2, stop3 };
        var collection = new SkiaTextGradientStopCollection(list);

        // Act
        var enumerator = ((IEnumerable<SkiaTextGradientStop>)collection).GetEnumerator();
        var items = new List<SkiaTextGradientStop>();
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        // Assert
        Assert.AreEqual(3, items.Count);
        Assert.AreEqual(stop1, items[0]);
        Assert.AreEqual(stop2, items[1]);
        Assert.AreEqual(stop3, items[2]);
    }

    /// <summary>
    /// Tests that GetEnumerator works correctly with foreach statement on an empty collection.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_EmptyCollectionWithForeach_DoesNotIterate()
    {
        // Arrange
        var emptyList = new List<SkiaTextGradientStop>();
        var collection = new SkiaTextGradientStopCollection(emptyList);
        var iterationCount = 0;

        // Act
        foreach (var item in (IEnumerable<SkiaTextGradientStop>)collection)
        {
            iterationCount++;
        }

        // Assert
        Assert.AreEqual(0, iterationCount);
    }

    /// <summary>
    /// Tests that GetEnumerator works correctly with foreach statement on a collection with multiple items.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_MultipleItemsWithForeach_IteratesAllItems()
    {
        // Arrange
        var stop1 = new SkiaTextGradientStop(new SKColor(255, 0, 0), 0.0f);
        var stop2 = new SkiaTextGradientStop(new SKColor(0, 255, 0), 0.5f);
        var stop3 = new SkiaTextGradientStop(new SKColor(0, 0, 255), 1.0f);
        var list = new List<SkiaTextGradientStop> { stop1, stop2, stop3 };
        var collection = new SkiaTextGradientStopCollection(list);
        var items = new List<SkiaTextGradientStop>();

        // Act
        foreach (var item in (IEnumerable<SkiaTextGradientStop>)collection)
        {
            items.Add(item);
        }

        // Assert
        Assert.AreEqual(3, items.Count);
        Assert.AreEqual(stop1, items[0]);
        Assert.AreEqual(stop2, items[1]);
        Assert.AreEqual(stop3, items[2]);
    }

    /// <summary>
    /// Tests that GetEnumerator returns an enumerator that can be used with LINQ methods.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WithLinq_WorksCorrectly()
    {
        // Arrange
        var stop1 = new SkiaTextGradientStop(new SKColor(255, 0, 0), 0.0f);
        var stop2 = new SkiaTextGradientStop(new SKColor(0, 255, 0), 0.5f);
        var stop3 = new SkiaTextGradientStop(new SKColor(0, 0, 255), 1.0f);
        var list = new List<SkiaTextGradientStop> { stop1, stop2, stop3 };
        var collection = new SkiaTextGradientStopCollection(list);

        // Act
        var count = ((IEnumerable<SkiaTextGradientStop>)collection).Count();
        var firstItem = ((IEnumerable<SkiaTextGradientStop>)collection).First();
        var lastItem = ((IEnumerable<SkiaTextGradientStop>)collection).Last();

        // Assert
        Assert.AreEqual(3, count);
        Assert.AreEqual(stop1, firstItem);
        Assert.AreEqual(stop3, lastItem);
    }

    /// <summary>
    /// Tests that GetEnumerator can be called multiple times and each enumerator works independently.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_CalledMultipleTimes_ReturnsIndependentEnumerators()
    {
        // Arrange
        var stop1 = new SkiaTextGradientStop(new SKColor(255, 0, 0), 0.0f);
        var stop2 = new SkiaTextGradientStop(new SKColor(0, 255, 0), 1.0f);
        var list = new List<SkiaTextGradientStop> { stop1, stop2 };
        var collection = new SkiaTextGradientStopCollection(list);

        // Act
        var enumerator1 = ((IEnumerable<SkiaTextGradientStop>)collection).GetEnumerator();
        var enumerator2 = ((IEnumerable<SkiaTextGradientStop>)collection).GetEnumerator();

        enumerator1.MoveNext();
        var firstFromEnum1 = enumerator1.Current;

        enumerator2.MoveNext();
        var firstFromEnum2 = enumerator2.Current;

        // Assert
        Assert.AreEqual(stop1, firstFromEnum1);
        Assert.AreEqual(stop1, firstFromEnum2);
        Assert.AreNotSame(enumerator1, enumerator2);
    }

    /// <summary>
    /// Tests that the Count property returns the correct count from the underlying collection
    /// for various collection sizes including empty, single item, and multiple items.
    /// </summary>
    /// <param name="expectedCount">The expected count value.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(100)]
    [DataRow(int.MaxValue)]
    public void Count_VariousCollectionSizes_ReturnsCorrectCount(int expectedCount)
    {
        // Arrange
        var mockCollection = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        mockCollection.Setup(c => c.Count).Returns(expectedCount);
        var gradientStopCollection = new SkiaTextGradientStopCollection(mockCollection.Object);

        // Act
        var actualCount = gradientStopCollection.Count;

        // Assert
        Assert.AreEqual(expectedCount, actualCount);
    }

    /// <summary>
    /// Tests that GetEnumerator returns an enumerator that successfully enumerates an empty collection.
    /// Input: Empty collection.
    /// Expected: Enumerator returns false on first MoveNext call.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_EmptyCollection_ReturnsFalseOnFirstMoveNext()
    {
        // Arrange
        var mockCollection = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        var emptyList = new List<SkiaTextGradientStop>();
        mockCollection.Setup(c => c.GetEnumerator()).Returns(emptyList.GetEnumerator());
        mockCollection.Setup(c => ((IEnumerable)c).GetEnumerator()).Returns(((IEnumerable)emptyList).GetEnumerator());
        mockCollection.Setup(c => c.Count).Returns(0);
        var collection = new SkiaTextGradientStopCollection(mockCollection.Object);

        // Act
        var enumerator = ((IEnumerable)collection).GetEnumerator();
        var result = enumerator.MoveNext();

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that GetEnumerator returns an enumerator that successfully enumerates a single item collection.
    /// Input: Collection with one gradient stop.
    /// Expected: Enumerator returns true on first MoveNext, then false on second.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_SingleItem_EnumeratesOneItem()
    {
        // Arrange
        var gradientStop = new SkiaTextGradientStop(SKColors.Red, 0.5f);
        var mockCollection = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        var singleItemList = new List<SkiaTextGradientStop> { gradientStop };
        mockCollection.Setup(c => c.GetEnumerator()).Returns(singleItemList.GetEnumerator());
        mockCollection.Setup(c => ((IEnumerable)c).GetEnumerator()).Returns(((IEnumerable)singleItemList).GetEnumerator());
        mockCollection.Setup(c => c.Count).Returns(1);
        mockCollection.Setup(c => c[0]).Returns(gradientStop);
        var collection = new SkiaTextGradientStopCollection(mockCollection.Object);

        // Act
        var enumerator = ((IEnumerable)collection).GetEnumerator();
        var firstMoveNext = enumerator.MoveNext();
        var current = enumerator.Current;
        var secondMoveNext = enumerator.MoveNext();

        // Assert
        Assert.IsTrue(firstMoveNext);
        Assert.IsNotNull(current);
        Assert.IsFalse(secondMoveNext);
    }

    /// <summary>
    /// Tests that GetEnumerator returns an enumerator that successfully enumerates multiple items in order.
    /// Input: Collection with three gradient stops.
    /// Expected: Enumerator returns true for each item, then false after all items are enumerated.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_MultipleItems_EnumeratesAllItemsInOrder()
    {
        // Arrange
        var gradientStop1 = new SkiaTextGradientStop(SKColors.Red, 0.0f);
        var gradientStop2 = new SkiaTextGradientStop(SKColors.Green, 0.5f);
        var gradientStop3 = new SkiaTextGradientStop(SKColors.Blue, 1.0f);
        var mockCollection = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        var multiItemList = new List<SkiaTextGradientStop> { gradientStop1, gradientStop2, gradientStop3 };
        mockCollection.Setup(c => c.GetEnumerator()).Returns(multiItemList.GetEnumerator());
        mockCollection.Setup(c => ((IEnumerable)c).GetEnumerator()).Returns(((IEnumerable)multiItemList).GetEnumerator());
        mockCollection.Setup(c => c.Count).Returns(3);
        mockCollection.Setup(c => c[0]).Returns(gradientStop1);
        mockCollection.Setup(c => c[1]).Returns(gradientStop2);
        mockCollection.Setup(c => c[2]).Returns(gradientStop3);
        var collection = new SkiaTextGradientStopCollection(mockCollection.Object);

        // Act
        var enumerator = ((IEnumerable)collection).GetEnumerator();
        var items = new List<object>();
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        // Assert
        Assert.AreEqual(3, items.Count);
        Assert.AreEqual(gradientStop1, items[0]);
        Assert.AreEqual(gradientStop2, items[1]);
        Assert.AreEqual(gradientStop3, items[2]);
    }

    /// <summary>
    /// Tests that GetEnumerator returns an enumerator where MoveNext returns false after enumeration completes.
    /// Input: Collection with one gradient stop, call MoveNext multiple times after completion.
    /// Expected: MoveNext returns false consistently after enumeration is complete.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_MoveNextAfterCompletion_ConsistentlyReturnsFalse()
    {
        // Arrange
        var gradientStop = new SkiaTextGradientStop(SKColors.Red, 0.5f);
        var mockCollection = new Mock<IReadOnlyList<SkiaTextGradientStop>>();
        var singleItemList = new List<SkiaTextGradientStop> { gradientStop };
        mockCollection.Setup(c => c.GetEnumerator()).Returns(singleItemList.GetEnumerator());
        mockCollection.Setup(c => ((IEnumerable)c).GetEnumerator()).Returns(((IEnumerable)singleItemList).GetEnumerator());
        mockCollection.Setup(c => c.Count).Returns(1);
        var collection = new SkiaTextGradientStopCollection(mockCollection.Object);

        // Act
        var enumerator = ((IEnumerable)collection).GetEnumerator();
        enumerator.MoveNext(); // Move to first item
        var firstFalse = enumerator.MoveNext(); // Should return false
        var secondFalse = enumerator.MoveNext(); // Should still return false
        var thirdFalse = enumerator.MoveNext(); // Should still return false

        // Assert
        Assert.IsFalse(firstFalse);
        Assert.IsFalse(secondFalse);
        Assert.IsFalse(thirdFalse);
    }

    /// <summary>
    /// Tests that the constructor correctly stores an empty collection and Count returns 0.
    /// </summary>
    [TestMethod]
    public void Constructor_WithEmptyCollection_StoresCollectionAndCountIsZero()
    {
        // Arrange
        var emptyCollection = new List<SkiaTextGradientStop>();

        // Act
        var gradientStopCollection = new SkiaTextGradientStopCollection(emptyCollection);

        // Assert
        Assert.AreEqual(0, gradientStopCollection.Count);
    }

    /// <summary>
    /// Tests that the constructor correctly stores a single-item collection and the item is accessible.
    /// </summary>
    [TestMethod]
    public void Constructor_WithSingleItem_StoresCollectionAndItemIsAccessible()
    {
        // Arrange
        var gradientStop = new SkiaTextGradientStop(SKColors.Red, 0.5f);
        var collection = new List<SkiaTextGradientStop> { gradientStop };

        // Act
        var gradientStopCollection = new SkiaTextGradientStopCollection(collection);

        // Assert
        Assert.AreEqual(1, gradientStopCollection.Count);
        Assert.AreEqual(gradientStop, gradientStopCollection[0]);
    }

    /// <summary>
    /// Tests that the constructor correctly stores multiple items and all items are accessible via indexer.
    /// </summary>
    [TestMethod]
    public void Constructor_WithMultipleItems_StoresCollectionAndItemsAreAccessible()
    {
        // Arrange
        var gradientStop1 = new SkiaTextGradientStop(SKColors.Red, 0.0f);
        var gradientStop2 = new SkiaTextGradientStop(SKColors.Green, 0.5f);
        var gradientStop3 = new SkiaTextGradientStop(SKColors.Blue, 1.0f);
        var collection = new List<SkiaTextGradientStop> { gradientStop1, gradientStop2, gradientStop3 };

        // Act
        var gradientStopCollection = new SkiaTextGradientStopCollection(collection);

        // Assert
        Assert.AreEqual(3, gradientStopCollection.Count);
        Assert.AreEqual(gradientStop1, gradientStopCollection[0]);
        Assert.AreEqual(gradientStop2, gradientStopCollection[1]);
        Assert.AreEqual(gradientStop3, gradientStopCollection[2]);
    }

    /// <summary>
    /// Tests that the constructor preserves the order of items in the collection.
    /// </summary>
    [TestMethod]
    public void Constructor_WithMultipleItems_PreservesOrder()
    {
        // Arrange
        var gradientStop1 = new SkiaTextGradientStop(SKColors.Yellow, 0.25f);
        var gradientStop2 = new SkiaTextGradientStop(SKColors.Cyan, 0.75f);
        var collection = new List<SkiaTextGradientStop> { gradientStop1, gradientStop2 };

        // Act
        var gradientStopCollection = new SkiaTextGradientStopCollection(collection);

        // Assert
        var items = gradientStopCollection.ToList();
        Assert.AreEqual(2, items.Count);
        Assert.AreEqual(gradientStop1, items[0]);
        Assert.AreEqual(gradientStop2, items[1]);
    }

    /// <summary>
    /// Tests that the generic GetEnumerator returns all items in the collection.
    /// </summary>
    [TestMethod]
    public void Constructor_WithMultipleItems_GenericGetEnumeratorReturnsAllItems()
    {
        // Arrange
        var gradientStop1 = new SkiaTextGradientStop(SKColors.Red, 0.0f);
        var gradientStop2 = new SkiaTextGradientStop(SKColors.Blue, 1.0f);
        var collection = new List<SkiaTextGradientStop> { gradientStop1, gradientStop2 };
        var gradientStopCollection = new SkiaTextGradientStopCollection(collection);

        // Act
        var enumeratedItems = new List<SkiaTextGradientStop>();
        foreach (var item in gradientStopCollection)
        {
            enumeratedItems.Add(item);
        }

        // Assert
        Assert.AreEqual(2, enumeratedItems.Count);
        Assert.AreEqual(gradientStop1, enumeratedItems[0]);
        Assert.AreEqual(gradientStop2, enumeratedItems[1]);
    }

    /// <summary>
    /// Tests that the non-generic GetEnumerator returns all items in the collection.
    /// </summary>
    [TestMethod]
    public void Constructor_WithMultipleItems_NonGenericGetEnumeratorReturnsAllItems()
    {
        // Arrange
        var gradientStop1 = new SkiaTextGradientStop(SKColors.White, 0.0f);
        var gradientStop2 = new SkiaTextGradientStop(SKColors.Black, 1.0f);
        var collection = new List<SkiaTextGradientStop> { gradientStop1, gradientStop2 };
        var gradientStopCollection = new SkiaTextGradientStopCollection(collection);

        // Act
        var enumeratedItems = new List<SkiaTextGradientStop>();
        var enumerator = ((IEnumerable)gradientStopCollection).GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumeratedItems.Add((SkiaTextGradientStop)enumerator.Current!);
        }

        // Assert
        Assert.AreEqual(2, enumeratedItems.Count);
        Assert.AreEqual(gradientStop1, enumeratedItems[0]);
        Assert.AreEqual(gradientStop2, enumeratedItems[1]);
    }

    /// <summary>
    /// Tests that the constructor works correctly with edge case offset values (0.0f and 1.0f).
    /// </summary>
    [TestMethod]
    public void Constructor_WithEdgeCaseOffsetValues_StoresCollectionCorrectly()
    {
        // Arrange
        var gradientStop1 = new SkiaTextGradientStop(SKColors.Red, 0.0f);
        var gradientStop2 = new SkiaTextGradientStop(SKColors.Blue, 1.0f);
        var collection = new List<SkiaTextGradientStop> { gradientStop1, gradientStop2 };

        // Act
        var gradientStopCollection = new SkiaTextGradientStopCollection(collection);

        // Assert
        Assert.AreEqual(2, gradientStopCollection.Count);
        Assert.AreEqual(0.0f, gradientStopCollection[0].Offset);
        Assert.AreEqual(1.0f, gradientStopCollection[1].Offset);
    }

    /// <summary>
    /// Tests that the constructor works correctly with special float offset values like NaN and Infinity.
    /// </summary>
    [TestMethod]
    [DataRow(float.NaN)]
    [DataRow(float.PositiveInfinity)]
    [DataRow(float.NegativeInfinity)]
    [DataRow(-1.0f)]
    [DataRow(2.0f)]
    public void Constructor_WithSpecialFloatOffsetValues_StoresCollectionCorrectly(float offset)
    {
        // Arrange
        var gradientStop = new SkiaTextGradientStop(SKColors.Red, offset);
        var collection = new List<SkiaTextGradientStop> { gradientStop };

        // Act
        var gradientStopCollection = new SkiaTextGradientStopCollection(collection);

        // Assert
        Assert.AreEqual(1, gradientStopCollection.Count);
        if (float.IsNaN(offset))
        {
            Assert.IsTrue(float.IsNaN(gradientStopCollection[0].Offset));
        }
        else
        {
            Assert.AreEqual(offset, gradientStopCollection[0].Offset);
        }
    }
}