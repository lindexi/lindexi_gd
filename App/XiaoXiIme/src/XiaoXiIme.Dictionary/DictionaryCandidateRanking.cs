using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Applies the dictionary ranking contract before results are mapped to <see cref="ImeCandidate"/>.
/// </summary>
public static class DictionaryCandidateRanking
{
    /// <summary>
    /// Deduplicates by text and orders candidates as user exact, system exact, system prefix, then fallback.
    /// </summary>
    public static IReadOnlyList<ImeCandidate> Rank(IEnumerable<DictionaryCandidate> candidates, int maxCount)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (maxCount <= 0)
        {
            return [];
        }

        return RankCore(candidates, maxCount, groupByReading: false, reverse: false);
    }

    /// <summary>
    /// Deduplicates by reading and prefers longer canonical encodings over abbreviations.
    /// </summary>
    public static IReadOnlyList<ImeCandidate> RankReadings(IEnumerable<DictionaryCandidate> candidates, int maxCount)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (maxCount <= 0)
        {
            return [];
        }

        return RankCore(candidates, maxCount, groupByReading: true, reverse: true);
    }

    private static IReadOnlyList<ImeCandidate> RankCore(
        IEnumerable<DictionaryCandidate> candidates,
        int maxCount,
        bool groupByReading,
        bool reverse)
    {
        IComparer<DictionaryCandidate> comparer = reverse
            ? ReverseComparer.Instance
            : Comparer.Instance;
        return candidates
            .GroupBy(
                candidate => groupByReading ? candidate.Reading : candidate.Text,
                StringComparer.Ordinal)
            .Select(group => group.Order(comparer).First())
            .Order(comparer)
            .Take(maxCount)
            .Select(candidate => candidate.ToImeCandidate())
            .ToArray();
    }

    private sealed class Comparer : IComparer<DictionaryCandidate>
    {
        internal static readonly Comparer Instance = new();

        public int Compare(DictionaryCandidate? left, DictionaryCandidate? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            var layer = left.Layer.CompareTo(right.Layer);
            if (layer != 0)
            {
                return layer;
            }

            var frequency = right.SortFrequency.CompareTo(left.SortFrequency);
            if (frequency != 0)
            {
                return frequency;
            }

            var encodingLength = EncodingLength(left.Reading).CompareTo(EncodingLength(right.Reading));
            if (encodingLength != 0)
            {
                return encodingLength;
            }

            var textLength = left.Text.Length.CompareTo(right.Text.Length);
            if (textLength != 0)
            {
                return textLength;
            }

            return string.CompareOrdinal(left.Text, right.Text);
        }

        private static int EncodingLength(string reading)
        {
            // Canonical readings keep syllable spaces; ranking must use the lookup key length.
            return DictionaryPackageFormat.NormalizeLookupKey(reading).Length;
        }
    }

    private sealed class ReverseComparer : IComparer<DictionaryCandidate>
    {
        internal static readonly ReverseComparer Instance = new();

        public int Compare(DictionaryCandidate? left, DictionaryCandidate? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            var layer = left.Layer.CompareTo(right.Layer);
            if (layer != 0)
            {
                return layer;
            }

            var frequency = right.SortFrequency.CompareTo(left.SortFrequency);
            if (frequency != 0)
            {
                return frequency;
            }

            var encodingLength = EncodingLength(right.Reading).CompareTo(EncodingLength(left.Reading));
            if (encodingLength != 0)
            {
                return encodingLength;
            }

            var readingCompare = string.CompareOrdinal(left.Reading, right.Reading);
            if (readingCompare != 0)
            {
                return readingCompare;
            }

            return string.CompareOrdinal(left.Text, right.Text);
        }

        private static int EncodingLength(string reading)
        {
            return DictionaryPackageFormat.NormalizeLookupKey(reading).Length;
        }
    }
}
