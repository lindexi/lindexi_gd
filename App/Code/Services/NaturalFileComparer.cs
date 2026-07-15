using System;
using System.Collections.Generic;
using System.Globalization;

namespace ImageViewer.Services;

internal sealed class NaturalFileComparer : IComparer<string?>
{
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var xIndex = 0;
        var yIndex = 0;

        while (xIndex < x.Length && yIndex < y.Length)
        {
            var xChar = x[xIndex];
            var yChar = y[yIndex];

            if (char.IsDigit(xChar) && char.IsDigit(yChar))
            {
                var numberComparison = CompareNumberRuns(x, ref xIndex, y, ref yIndex);
                if (numberComparison != 0)
                {
                    return numberComparison;
                }

                continue;
            }

            var charComparison = char.ToUpperInvariant(xChar).CompareTo(char.ToUpperInvariant(yChar));
            if (charComparison != 0)
            {
                return charComparison;
            }

            xIndex++;
            yIndex++;
        }

        return x.Length.CompareTo(y.Length);
    }

    private static int CompareNumberRuns(string x, ref int xIndex, string y, ref int yIndex)
    {
        var xStart = xIndex;
        var yStart = yIndex;

        while (xIndex < x.Length && char.IsDigit(x[xIndex]))
        {
            xIndex++;
        }

        while (yIndex < y.Length && char.IsDigit(y[yIndex]))
        {
            yIndex++;
        }

        var xNumber = x[xStart..xIndex].TrimStart('0');
        var yNumber = y[yStart..yIndex].TrimStart('0');

        if (xNumber.Length != yNumber.Length)
        {
            return xNumber.Length.CompareTo(yNumber.Length);
        }

        var valueComparison = string.Compare(xNumber, yNumber, StringComparison.Ordinal);
        if (valueComparison != 0)
        {
            return valueComparison;
        }

        return string.Compare(x[xStart..xIndex], y[yStart..yIndex], CultureInfo.InvariantCulture, CompareOptions.Ordinal);
    }
}
