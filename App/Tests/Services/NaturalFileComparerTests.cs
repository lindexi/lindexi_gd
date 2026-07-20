using ImageViewer.Services;

namespace ImageViewer.Tests.Services;

public sealed class NaturalFileComparerTests
{
    [Fact]
    public void CompareSortsNumberRunsByNumericValue()
    {
        var files = new[] { "img10.png", "img2.png", "img1.png" };

        Array.Sort(files, new NaturalFileComparer());

        Assert.Equal(["img1.png", "img2.png", "img10.png"], files);
    }

    [Fact]
    public void CompareIgnoresCharacterCaseForOrdering()
    {
        var comparer = new NaturalFileComparer();

        var result = comparer.Compare("IMG2.png", "img10.png");

        Assert.True(result < 0);
    }

    [Fact]
    public void CompareUsesLeadingZerosAsTieBreaker()
    {
        var comparer = new NaturalFileComparer();

        var result = comparer.Compare("img001.png", "img01.png");

        Assert.True(result < 0);
    }

    [Fact]
    public void CompareOrdersNullBeforeNonNull()
    {
        var comparer = new NaturalFileComparer();

        var result = comparer.Compare(null, "img1.png");

        Assert.True(result < 0);
    }

    [Fact]
    public void CompareReturnsZeroForSameReferenceIncludingNull()
    {
        var comparer = new NaturalFileComparer();
        string? fileName = null;

        var result = comparer.Compare(fileName, fileName);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CompareOrdersShorterPrefixBeforeLongerWhenEqualSoFar()
    {
        var comparer = new NaturalFileComparer();

        var result = comparer.Compare("img", "img1");

        Assert.True(result < 0);
    }
}
