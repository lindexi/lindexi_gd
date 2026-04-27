using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LightTextEditorPlus.Document.UnitTests;


/// <summary>
/// Unit tests for the <see cref="SkiaImmutableRun"/> class.
/// </summary>
[TestClass]
public class SkiaImmutableRunTests
{
    /// <summary>
    /// Tests that Count property returns the correct number of elements for various array sizes.
    /// </summary>
    /// <param name="elementCount">The number of elements in the array.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(10)]
    [DataRow(100)]
    [DataRow(1000)]
    public void Count_WithVariousArraySizes_ReturnsCorrectLength(int elementCount)
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var charObjects = Enumerable.Range(0, elementCount)
            .Select(_ => Mock.Of<ICharObject>())
            .ToImmutableArray();
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjects);

        // Act
        var actualCount = run.Count;

        // Assert
        Assert.AreEqual(elementCount, actualCount);
    }

    /// <summary>
    /// Tests that Count property returns 0 when initialized with an empty IEnumerable.
    /// </summary>
    [TestMethod]
    public void Count_WithEmptyEnumerable_ReturnsZero()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var emptyEnumerable = Enumerable.Empty<ICharObject>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, emptyEnumerable);

        // Act
        var actualCount = run.Count;

        // Assert
        Assert.AreEqual(0, actualCount);
    }

    /// <summary>
    /// Tests that Count property returns correct count when initialized with IEnumerable constructor.
    /// </summary>
    [TestMethod]
    public void Count_WithIEnumerableConstructor_ReturnsCorrectLength()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var charObjects = new List<ICharObject>
        {
            Mock.Of<ICharObject>(),
            Mock.Of<ICharObject>(),
            Mock.Of<ICharObject>()
        };
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjects);

        // Act
        var actualCount = run.Count;

        // Assert
        Assert.AreEqual(3, actualCount);
    }

    /// <summary>
    /// Tests that Count property returns correct count when initialized with ImmutableArray constructor.
    /// </summary>
    [TestMethod]
    public void Count_WithImmutableArrayConstructor_ReturnsCorrectLength()
    {
        // Arrange
        var charObjects = ImmutableArray.Create(
            Mock.Of<ICharObject>(),
            Mock.Of<ICharObject>(),
            Mock.Of<ICharObject>(),
            Mock.Of<ICharObject>(),
            Mock.Of<ICharObject>()
        );
        var run = new SkiaImmutableRun(null!, charObjects);

        // Act
        var actualCount = run.Count;

        // Assert
        Assert.AreEqual(5, actualCount);
    }

    /// <summary>
    /// Tests that Count property returns 0 when initialized with empty ImmutableArray.
    /// </summary>
    [TestMethod]
    public void Count_WithEmptyImmutableArray_ReturnsZero()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var emptyArray = ImmutableArray<ICharObject>.Empty;
        var run = new SkiaImmutableRun(mockRunProperty.Object, emptyArray);

        // Act
        var actualCount = run.Count;

        // Assert
        Assert.AreEqual(0, actualCount);
    }

    /// <summary>
    /// Tests that the constructor successfully creates an instance with valid non-empty immutable array.
    /// The constructor should properly assign the runProperty and charObjectArray parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidParametersAndNonEmptyArray_CreatesInstanceSuccessfully()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var mockCharObject1 = new Mock<ICharObject>();
        var mockCharObject2 = new Mock<ICharObject>();
        var charObjectArray = ImmutableArray.Create(mockCharObject1.Object, mockCharObject2.Object);

        // Act
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjectArray);

        // Assert
        Assert.IsNotNull(run);
        Assert.AreEqual(mockRunProperty.Object, run.RunProperty);
        Assert.AreEqual(2, run.Count);
    }

    /// <summary>
    /// Tests that the constructor successfully creates an instance with an empty immutable array.
    /// The Count property should return 0 for an empty array.
    /// </summary>
    [TestMethod]
    public void Constructor_WithEmptyImmutableArray_CreatesInstanceWithZeroCount()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var charObjectArray = ImmutableArray<ICharObject>.Empty;

        // Act
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjectArray);

        // Assert
        Assert.IsNotNull(run);
        Assert.AreEqual(mockRunProperty.Object, run.RunProperty);
        Assert.AreEqual(0, run.Count);
    }

    /// <summary>
    /// Tests that the constructor successfully creates an instance with a single element immutable array.
    /// The Count property should return 1.
    /// </summary>
    [TestMethod]
    public void Constructor_WithSingleElementArray_CreatesInstanceWithCountOne()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var mockCharObject = new Mock<ICharObject>();
        var charObjectArray = ImmutableArray.Create(mockCharObject.Object);

        // Act
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjectArray);

        // Assert
        Assert.IsNotNull(run);
        Assert.AreEqual(mockRunProperty.Object, run.RunProperty);
        Assert.AreEqual(1, run.Count);
    }

    /// <summary>
    /// Tests that the constructor handles a default (uninitialized) ImmutableArray.
    /// A default ImmutableArray has IsDefault = true and accessing Length may throw.
    /// </summary>
    [TestMethod]
    public void Constructor_WithDefaultImmutableArray_CreatesInstanceButCountThrows()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var charObjectArray = default(ImmutableArray<ICharObject>);

        // Act
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjectArray);

        // Assert
        Assert.IsNotNull(run);
        Assert.AreEqual(mockRunProperty.Object, run.RunProperty);
        Assert.ThrowsException<InvalidOperationException>(() => { var count = run.Count; });
    }

    /// <summary>
    /// Tests that the constructor accepts a null runProperty parameter.
    /// While not ideal, the constructor doesn't validate this and will accept null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullRunProperty_CreatesInstanceWithNullProperty()
    {
        // Arrange
        var mockCharObject = new Mock<ICharObject>();
        var charObjectArray = ImmutableArray.Create(mockCharObject.Object);

        // Act
        var run = new SkiaImmutableRun(null!, charObjectArray);

        // Assert
        Assert.IsNotNull(run);
        Assert.IsNull(run.RunProperty);
        Assert.AreEqual(1, run.Count);
    }

    /// <summary>
    /// Tests that the constructor handles a large immutable array correctly.
    /// Verifies boundary behavior with many elements.
    /// </summary>
    [TestMethod]
    public void Constructor_WithLargeImmutableArray_CreatesInstanceSuccessfully()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var charObjects = new ICharObject[1000];
        for (int i = 0; i < charObjects.Length; i++)
        {
            charObjects[i] = new Mock<ICharObject>().Object;
        }
        var charObjectArray = ImmutableArray.Create(charObjects);

        // Act
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjectArray);

        // Assert
        Assert.IsNotNull(run);
        Assert.AreEqual(mockRunProperty.Object, run.RunProperty);
        Assert.AreEqual(1000, run.Count);
    }

    /// <summary>
    /// Tests that GetChar returns the correct character at a valid index.
    /// Verifies that the charObjectArray was properly assigned in the constructor.
    /// </summary>
    [TestMethod]
    public void Constructor_VerifyCharObjectArrayAssignment_GetCharReturnsCorrectObject()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var mockCharObject1 = new Mock<ICharObject>();
        var mockCharObject2 = new Mock<ICharObject>();
        var charObjectArray = ImmutableArray.Create(mockCharObject1.Object, mockCharObject2.Object);

        // Act
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjectArray);

        // Assert
        Assert.AreEqual(mockCharObject1.Object, run.GetChar(0));
        Assert.AreEqual(mockCharObject2.Object, run.GetChar(1));
    }

    /// <summary>
    /// Tests that the constructor with multiple elements of various types works correctly.
    /// Uses parameterized test data to validate different array sizes.
    /// </summary>
    /// <param name="arraySize">The size of the immutable array to test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(10)]
    [DataRow(100)]
    public void Constructor_WithVariousArraySizes_CreatesInstanceWithCorrectCount(int arraySize)
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var charObjects = new ICharObject[arraySize];
        for (int i = 0; i < charObjects.Length; i++)
        {
            charObjects[i] = new Mock<ICharObject>().Object;
        }
        var charObjectArray = arraySize == 0 ? ImmutableArray<ICharObject>.Empty : ImmutableArray.Create(charObjects);

        // Act
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjectArray);

        // Assert
        Assert.IsNotNull(run);
        Assert.AreEqual(arraySize, run.Count);
    }

    /// <summary>
    /// Tests that SplitAt correctly splits a run at a middle index.
    /// Input: A run with 5 characters, split at index 2.
    /// Expected: First run contains 2 characters, second run contains 3 characters.
    /// </summary>
    [TestMethod]
    [DataRow(2)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    public void SplitAt_WithMiddleIndex_ReturnsTwoRunsWithCorrectCounts(int splitIndex)
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(5);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(splitIndex);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(splitIndex, firstRun.Count);
        Assert.AreEqual(5 - splitIndex, secondRun.Count);
        Assert.AreEqual(5, firstRun.Count + secondRun.Count);
    }

    /// <summary>
    /// Tests that SplitAt at index 0 creates an empty first run and a second run with all characters.
    /// Input: A run with 3 characters, split at index 0.
    /// Expected: First run is empty, second run contains all 3 characters.
    /// </summary>
    [TestMethod]
    public void SplitAt_WithIndexZero_ReturnsEmptyFirstRunAndFullSecondRun()
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(3);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(0);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(0, firstRun.Count);
        Assert.AreEqual(3, secondRun.Count);
    }

    /// <summary>
    /// Tests that SplitAt at index equal to Count creates a first run with all characters and an empty second run.
    /// Input: A run with 3 characters, split at index 3.
    /// Expected: First run contains all 3 characters, second run is empty.
    /// </summary>
    [TestMethod]
    public void SplitAt_WithIndexEqualToCount_ReturnsFullFirstRunAndEmptySecondRun()
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(3);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(3);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(3, firstRun.Count);
        Assert.AreEqual(0, secondRun.Count);
    }

    /// <summary>
    /// Tests that SplitAt with an empty run returns two empty runs.
    /// Input: An empty run, split at index 0.
    /// Expected: Both runs are empty.
    /// </summary>
    [TestMethod]
    public void SplitAt_WithEmptyRun_ReturnsTwoEmptyRuns()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, ImmutableArray<ICharObject>.Empty);

        // Act
        var (firstRun, secondRun) = run.SplitAt(0);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(0, firstRun.Count);
        Assert.AreEqual(0, secondRun.Count);
    }

    /// <summary>
    /// Tests that SplitAt with a single character run at index 0 returns empty first run and full second run.
    /// Input: A run with 1 character, split at index 0.
    /// Expected: First run is empty, second run contains 1 character.
    /// </summary>
    [TestMethod]
    public void SplitAt_WithSingleCharacterRunAtIndexZero_ReturnsEmptyFirstAndFullSecond()
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(1);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(0);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(0, firstRun.Count);
        Assert.AreEqual(1, secondRun.Count);
    }

    /// <summary>
    /// Tests that SplitAt with a single character run at index 1 returns full first run and empty second run.
    /// Input: A run with 1 character, split at index 1.
    /// Expected: First run contains 1 character, second run is empty.
    /// </summary>
    [TestMethod]
    public void SplitAt_WithSingleCharacterRunAtIndexOne_ReturnsFullFirstAndEmptySecond()
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(1);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(1);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(1, firstRun.Count);
        Assert.AreEqual(0, secondRun.Count);
    }

    /// <summary>
    /// Tests that SplitAt with a negative index returns empty first run and full second run (LINQ Take behavior).
    /// Input: A run with 3 characters, split at index -1.
    /// Expected: First run is empty, second run contains all 3 characters.
    /// </summary>
    [TestMethod]
    public void SplitAt_WithNegativeIndex_ReturnsEmptyFirstAndFullSecond()
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(3);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(-1);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(0, firstRun.Count);
        Assert.AreEqual(3, secondRun.Count);
    }

    /// <summary>
    /// Tests that SplitAt with an index greater than Count returns full first run and empty second run.
    /// Input: A run with 3 characters, split at index 10.
    /// Expected: First run contains all 3 characters, second run is empty.
    /// </summary>
    [TestMethod]
    public void SplitAt_WithIndexGreaterThanCount_ReturnsFullFirstAndEmptySecond()
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(3);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(10);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(3, firstRun.Count);
        Assert.AreEqual(0, secondRun.Count);
    }

    /// <summary>
    /// Tests that SplitAt with int.MaxValue returns full first run and empty second run.
    /// Input: A run with 3 characters, split at int.MaxValue.
    /// Expected: First run contains all 3 characters, second run is empty.
    /// </summary>
    [TestMethod]
    public void SplitAt_WithMaxValue_ReturnsFullFirstAndEmptySecond()
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(3);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(int.MaxValue);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(3, firstRun.Count);
        Assert.AreEqual(0, secondRun.Count);
    }

    /// <summary>
    /// Tests that SplitAt with int.MinValue returns empty first run and full second run.
    /// Input: A run with 3 characters, split at int.MinValue.
    /// Expected: First run is empty, second run contains all 3 characters.
    /// </summary>
    [TestMethod]
    public void SplitAt_WithMinValue_ReturnsEmptyFirstAndFullSecond()
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(3);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(int.MinValue);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(0, firstRun.Count);
        Assert.AreEqual(3, secondRun.Count);
    }

    /// <summary>
    /// Tests that SplitAt preserves the RunProperty in both resulting runs.
    /// Input: A run with a specific RunProperty, split at index 2.
    /// Expected: Both resulting runs reference the same RunProperty.
    /// </summary>
    [TestMethod]
    public void SplitAt_PreservesRunPropertyInBothRuns()
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(4);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(2);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.IsTrue(firstRun is SkiaImmutableRun);
        Assert.IsTrue(secondRun is SkiaImmutableRun);
        Assert.AreSame(mockRunProperty.Object, ((SkiaImmutableRun)firstRun).RunProperty);
        Assert.AreSame(mockRunProperty.Object, ((SkiaImmutableRun)secondRun).RunProperty);
    }

    /// <summary>
    /// Tests that SplitAt correctly distributes characters between first and second runs.
    /// Input: A run with 5 distinct characters, split at index 3.
    /// Expected: First run contains first 3 characters, second run contains last 2 characters.
    /// </summary>
    [TestMethod]
    public void SplitAt_CorrectlyDistributesCharactersBetweenRuns()
    {
        // Arrange
        var mockCharObjects = CreateMockCharObjects(5);
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var run = new SkiaImmutableRun(mockRunProperty.Object, mockCharObjects.Select(m => m.Object).ToImmutableArray());

        // Act
        var (firstRun, secondRun) = run.SplitAt(3);

        // Assert
        Assert.IsNotNull(firstRun);
        Assert.IsNotNull(secondRun);
        Assert.AreEqual(3, firstRun.Count);
        Assert.AreEqual(2, secondRun.Count);

        // Verify first run contains first 3 characters
        for (int i = 0; i < 3; i++)
        {
            Assert.AreSame(mockCharObjects[i].Object, firstRun.GetChar(i));
        }

        // Verify second run contains last 2 characters
        for (int i = 0; i < 2; i++)
        {
            Assert.AreSame(mockCharObjects[i + 3].Object, secondRun.GetChar(i));
        }
    }

    /// <summary>
    /// Helper method to create mock ICharObject instances.
    /// </summary>
    /// <param name="count">Number of mock objects to create.</param>
    /// <returns>List of mock ICharObject instances.</returns>
    private List<Mock<ICharObject>> CreateMockCharObjects(int count)
    {
        var mocks = new List<Mock<ICharObject>>();
        for (int i = 0; i < count; i++)
        {
            mocks.Add(new Mock<ICharObject>());
        }
        return mocks;
    }

    /// <summary>
    /// Tests that the explicit interface implementation of RunProperty returns the same instance as the public RunProperty.
    /// </summary>
    [TestMethod]
    public void RunProperty_WhenAccessedThroughInterface_ReturnsSameInstanceAsPublicProperty()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var charObjects = ImmutableArray<ICharObject>.Empty;
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjects);
        var interfaceRun = (IImmutableRun)run;

        // Act
        var publicProperty = run.RunProperty;
        var interfaceProperty = interfaceRun.RunProperty;

        // Assert
        Assert.IsNotNull(interfaceProperty, "Interface property should not be null.");
        Assert.AreSame(publicProperty, interfaceProperty, "Interface property should return the same instance as public property.");
    }

    /// <summary>
    /// Tests that the explicit interface implementation of RunProperty returns the same instance that was passed to the constructor.
    /// </summary>
    [TestMethod]
    public void RunProperty_WhenAccessedThroughInterface_ReturnsSameInstancePassedToConstructor()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var expectedRunProperty = mockRunProperty.Object;
        var charObjects = ImmutableArray<ICharObject>.Empty;
        var run = new SkiaImmutableRun(expectedRunProperty, charObjects);
        var interfaceRun = (IImmutableRun)run;

        // Act
        var actualRunProperty = interfaceRun.RunProperty;

        // Assert
        Assert.IsNotNull(actualRunProperty, "Interface property should not be null.");
        Assert.AreSame(expectedRunProperty, actualRunProperty, "Interface property should return the same instance passed to constructor.");
    }

    /// <summary>
    /// Tests that the explicit interface implementation of RunProperty is of type IReadOnlyRunProperty.
    /// </summary>
    [TestMethod]
    public void RunProperty_WhenAccessedThroughInterface_ReturnsIReadOnlyRunPropertyType()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var charObjects = ImmutableArray<ICharObject>.Empty;
        var run = new SkiaImmutableRun(mockRunProperty.Object, charObjects);
        var interfaceRun = (IImmutableRun)run;

        // Act
        var runProperty = interfaceRun.RunProperty;

        // Assert
        Assert.IsNotNull(runProperty, "RunProperty should not be null.");
        Assert.IsInstanceOfType(runProperty, typeof(IReadOnlyRunProperty), "RunProperty should be of type IReadOnlyRunProperty.");
    }

    /// <summary>
    /// Tests that the public RunProperty returns the instance passed to the constructor.
    /// </summary>
    [TestMethod]
    public void RunProperty_Public_ReturnsSameInstancePassedToConstructor()
    {
        // Arrange
        var mockRunProperty = new Mock<SkiaTextRunProperty>();
        var expectedRunProperty = mockRunProperty.Object;
        var charObjects = ImmutableArray<ICharObject>.Empty;
        var run = new SkiaImmutableRun(expectedRunProperty, charObjects);

        // Act
        var actualRunProperty = run.RunProperty;

        // Assert
        Assert.AreSame(expectedRunProperty, actualRunProperty, "Public RunProperty should return the same instance passed to constructor.");
    }

    /// <summary>
    /// Tests that the constructor successfully creates an instance with valid runProperty and valid charObjects collection.
    /// Input: Valid SkiaTextRunProperty mock and a list of ICharObject mocks.
    /// Expected: Object is created successfully, RunProperty is set, and Count reflects the number of characters.
    /// </summary>
    [TestMethod]
    [DataRow(0, DisplayName = "Empty collection")]
    [DataRow(1, DisplayName = "Single item collection")]
    [DataRow(3, DisplayName = "Multiple items collection")]
    [DataRow(100, DisplayName = "Large collection")]
    public void SkiaImmutableRun_ValidInputsWithVaryingCollectionSizes_CreatesInstanceSuccessfully(int charCount)
    {
        // Arrange
        var mockRunProperty = Mock.Of<SkiaTextRunProperty>();
        var charObjects = Enumerable.Range(0, charCount)
            .Select(_ => Mock.Of<ICharObject>())
            .ToList();

        // Act
        var result = new SkiaImmutableRun(mockRunProperty, charObjects);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(mockRunProperty, result.RunProperty);
        Assert.AreEqual(charCount, result.Count);
    }

    /// <summary>
    /// Tests that the constructor properly stores charObjects and GetChar retrieves them correctly.
    /// Input: Valid runProperty and a collection of ICharObject mocks.
    /// Expected: GetChar returns the correct ICharObject at each index.
    /// </summary>
    [TestMethod]
    public void SkiaImmutableRun_ValidInputs_GetCharReturnsCorrectCharObjects()
    {
        // Arrange
        var mockRunProperty = Mock.Of<SkiaTextRunProperty>();
        var char1 = Mock.Of<ICharObject>();
        var char2 = Mock.Of<ICharObject>();
        var char3 = Mock.Of<ICharObject>();
        var charObjects = new List<ICharObject> { char1, char2, char3 };

        // Act
        var result = new SkiaImmutableRun(mockRunProperty, charObjects);

        // Assert
        Assert.AreSame(char1, result.GetChar(0));
        Assert.AreSame(char2, result.GetChar(1));
        Assert.AreSame(char3, result.GetChar(2));
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when charObjects is null.
    /// Input: Valid runProperty and null charObjects.
    /// Expected: ArgumentNullException is thrown.
    /// </summary>
    [TestMethod]
    public void SkiaImmutableRun_NullCharObjects_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRunProperty = Mock.Of<SkiaTextRunProperty>();
        IEnumerable<ICharObject>? charObjects = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new SkiaImmutableRun(mockRunProperty, charObjects!));
    }

    /// <summary>
    /// Tests that the constructor accepts null runProperty (runtime behavior test).
    /// Input: Null runProperty and valid charObjects.
    /// Expected: Object is created with null RunProperty (no validation in constructor).
    /// </summary>
    [TestMethod]
    public void SkiaImmutableRun_NullRunProperty_CreatesInstanceWithNullProperty()
    {
        // Arrange
        SkiaTextRunProperty? runProperty = null;
        var charObjects = new List<ICharObject> { Mock.Of<ICharObject>() };

        // Act
        var result = new SkiaImmutableRun(runProperty!, charObjects);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNull(result.RunProperty);
        Assert.AreEqual(1, result.Count);
    }

    /// <summary>
    /// Tests that the constructor works with various IEnumerable implementations.
    /// Input: Valid runProperty and different IEnumerable implementations (Array, List, HashSet, custom enumerable).
    /// Expected: Object is created successfully for all enumerable types.
    /// </summary>
    [TestMethod]
    public void SkiaImmutableRun_DifferentEnumerableTypes_CreatesInstanceSuccessfully()
    {
        // Arrange
        var mockRunProperty = Mock.Of<SkiaTextRunProperty>();
        var char1 = Mock.Of<ICharObject>();
        var char2 = Mock.Of<ICharObject>();

        IEnumerable<ICharObject>[] enumerableTypes = new[]
        {
            new[] { char1, char2 },
            new List<ICharObject> { char1, char2 },
            new HashSet<ICharObject> { char1, char2 },
            EnumerableCharObjects(char1, char2)
        };

        // Act & Assert
        foreach (var charObjects in enumerableTypes)
        {
            var result = new SkiaImmutableRun(mockRunProperty, charObjects);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
        }
    }

    /// <summary>
    /// Tests that the constructor properly converts a lazy enumerable (with deferred execution) to an immutable array.
    /// Input: Valid runProperty and a LINQ query (deferred execution).
    /// Expected: Object is created successfully and the enumerable is materialized.
    /// </summary>
    [TestMethod]
    public void SkiaImmutableRun_LazyEnumerable_MaterializesCorrectly()
    {
        // Arrange
        var mockRunProperty = Mock.Of<SkiaTextRunProperty>();
        var sourceList = new List<ICharObject>
        {
            Mock.Of<ICharObject>(),
            Mock.Of<ICharObject>(),
            Mock.Of<ICharObject>()
        };
        var lazyEnumerable = sourceList.Where(c => c != null);

        // Act
        var result = new SkiaImmutableRun(mockRunProperty, lazyEnumerable);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count);
    }

    /// <summary>
    /// Helper method to create an enumerable using yield return.
    /// </summary>
    private static IEnumerable<ICharObject> EnumerableCharObjects(params ICharObject[] charObjects)
    {
        foreach (var charObject in charObjects)
        {
            yield return charObject;
        }
    }
}