// 小鹤双拼键位映射参考 SeWZC_IME/src/Ime.Data/PhoneticProjection.cs，按 XiaoXiIme package 合同独立实现。
namespace XiaoXiIme.Dictionary;

internal static class PhoneticInputProjection
{
    private static readonly IReadOnlyDictionary<string, char> Initials = new Dictionary<string, char>(StringComparer.Ordinal)
    {
        ["b"] = 'b',
        ["p"] = 'p',
        ["m"] = 'm',
        ["f"] = 'f',
        ["d"] = 'd',
        ["t"] = 't',
        ["n"] = 'n',
        ["l"] = 'l',
        ["g"] = 'g',
        ["k"] = 'k',
        ["h"] = 'h',
        ["j"] = 'j',
        ["q"] = 'q',
        ["x"] = 'x',
        ["r"] = 'r',
        ["z"] = 'z',
        ["c"] = 'c',
        ["s"] = 's',
        ["y"] = 'y',
        ["w"] = 'w',
        ["zh"] = 'v',
        ["ch"] = 'i',
        ["sh"] = 'u',
    };

    private static readonly IReadOnlyDictionary<string, char> Finals = new Dictionary<string, char>(StringComparer.Ordinal)
    {
        ["iu"] = 'q',
        ["ei"] = 'w',
        ["uan"] = 'r',
        ["ue"] = 't',
        ["ve"] = 't',
        ["un"] = 'y',
        ["uo"] = 'o',
        ["ie"] = 'p',
        ["ong"] = 's',
        ["iong"] = 's',
        ["ai"] = 'd',
        ["en"] = 'f',
        ["eng"] = 'g',
        ["ang"] = 'h',
        ["an"] = 'j',
        ["uai"] = 'k',
        ["ing"] = 'k',
        ["uang"] = 'l',
        ["iang"] = 'l',
        ["ou"] = 'z',
        ["ua"] = 'x',
        ["ia"] = 'x',
        ["iao"] = 'n',
        ["ao"] = 'c',
        ["ui"] = 'v',
        ["in"] = 'b',
        ["ian"] = 'm',
    };

    private static readonly HashSet<string> SimpleFinals = new(StringComparer.Ordinal)
    {
        "a",
        "o",
        "e",
        "i",
        "u",
        "v",
    };

    private static readonly HashSet<string> ZeroInitialSyllables = new(StringComparer.Ordinal)
    {
        "a",
        "ai",
        "an",
        "ang",
        "ao",
        "e",
        "ei",
        "en",
        "eng",
        "er",
        "o",
        "ou",
    };

    internal static string CanonicalizeReading(string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var syllables = new List<string>();
        foreach (var token in reading.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var normalizedToken = token.ToLowerInvariant()
                .Replace("u:", "v", StringComparison.Ordinal)
                .Replace('ü', 'v');
            if (normalizedToken.Length == 0 || normalizedToken.Any(character => character is < 'a' or > 'z'))
            {
                continue;
            }

            if (TryEncodeXiaoheSyllable(normalizedToken, out _))
            {
                syllables.Add(normalizedToken);
                continue;
            }

            if (TrySplitConcatenatedSyllables(normalizedToken, out var splitSyllables))
            {
                syllables.AddRange(splitSyllables);
                continue;
            }

            syllables.Add(normalizedToken);
        }

        return string.Join(' ', syllables);
    }

    internal static bool TryProjectReading(string reading, string inputScheme, out string lookupKey)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentNullException.ThrowIfNull(inputScheme);

        if (string.Equals(inputScheme, DictionaryPackageFormat.FullPinyinInputScheme, StringComparison.Ordinal))
        {
            lookupKey = DictionaryPackageFormat.NormalizeLookupKey(reading);
            return lookupKey.Length > 0;
        }

        if (!string.Equals(inputScheme, DictionaryPackageFormat.XiaoheDoublePinyinInputScheme, StringComparison.Ordinal))
        {
            lookupKey = string.Empty;
            return false;
        }

        var syllables = reading.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (syllables.Length == 1 && !TryEncodeXiaoheSyllable(syllables[0], out _))
        {
            lookupKey = DictionaryPackageFormat.NormalizeLookupKey(syllables[0]);
            return lookupKey.Length > 0;
        }

        var encodedSyllables = new List<string>();
        foreach (var syllable in syllables)
        {
            if (!TryEncodeXiaoheSyllable(syllable, out var encodedSyllable))
            {
                lookupKey = string.Empty;
                return false;
            }

            encodedSyllables.Add(encodedSyllable);
        }

        lookupKey = string.Concat(encodedSyllables);
        return lookupKey.Length > 0;
    }

    private static bool TrySplitConcatenatedSyllables(string token, out IReadOnlyList<string> syllables)
    {
        var parts = new List<string>();
        var index = 0;
        while (index < token.Length)
        {
            var matchedLength = 0;
            var maxLength = Math.Min(6, token.Length - index);
            for (var length = maxLength; length >= 1; length--)
            {
                var candidate = token.Substring(index, length);
                if (TryEncodeXiaoheSyllable(candidate, out _))
                {
                    matchedLength = length;
                    parts.Add(candidate);
                    break;
                }
            }

            if (matchedLength == 0)
            {
                syllables = [];
                return false;
            }

            index += matchedLength;
        }

        syllables = parts;
        return parts.Count > 1;
    }

    private static bool TryEncodeXiaoheSyllable(string syllable, out string code)
    {
        var pinyin = syllable.ToLowerInvariant()
            .Replace("u:", "v", StringComparison.Ordinal)
            .Replace('ü', 'v');
        if (pinyin.Length == 0 || pinyin.Any(character => character is < 'a' or > 'z'))
        {
            code = string.Empty;
            return false;
        }

        if (TryEncodeZeroInitial(pinyin, out code))
        {
            return true;
        }

        var (initial, final) = SplitInitialFinal(pinyin);
        if (initial.Length == 0 || !TryEncodeFinal(final, out var finalCode))
        {
            code = string.Empty;
            return false;
        }

        code = string.Concat(Initials[initial], finalCode);
        return true;
    }

    private static bool TryEncodeZeroInitial(string pinyin, out string code)
    {
        if (!ZeroInitialSyllables.Contains(pinyin))
        {
            code = string.Empty;
            return false;
        }

        if (pinyin.Length == 1)
        {
            code = string.Concat(pinyin[0], pinyin[0]);
            return true;
        }

        if (pinyin.Length == 2)
        {
            code = pinyin;
            return true;
        }

        if (!TryEncodeFinal(pinyin, out var finalCode))
        {
            code = string.Empty;
            return false;
        }

        code = string.Concat(pinyin[0], finalCode);
        return true;
    }

    private static bool TryEncodeFinal(string final, out char code)
    {
        if (Finals.TryGetValue(final, out code))
        {
            return true;
        }

        if (SimpleFinals.Contains(final))
        {
            code = final[0];
            return true;
        }

        code = '\0';
        return false;
    }

    private static (string Initial, string Final) SplitInitialFinal(string pinyin)
    {
        foreach (var initial in new[] { "zh", "ch", "sh" })
        {
            if (pinyin.StartsWith(initial, StringComparison.Ordinal))
            {
                return (initial, pinyin[initial.Length..]);
            }
        }

        var singleInitial = pinyin[..1];
        return Initials.ContainsKey(singleInitial)
            ? (singleInitial, pinyin[1..])
            : (string.Empty, pinyin);
    }
}
