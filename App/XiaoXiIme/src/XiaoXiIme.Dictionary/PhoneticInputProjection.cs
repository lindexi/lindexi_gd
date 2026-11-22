// 小鹤双拼键位映射参考 SeWZC_IME/src/Ime.Data/PhoneticProjection.cs，按 XiaoXiIme package 合同独立实现。

namespace XiaoXiIme.Dictionary;

internal static class PhoneticInputProjection
{
    private static readonly PhoneticLayout Xiaohe = PhoneticSchemeCatalog.All.Single
        (scheme => scheme.SchemeId == "xiaoheDoublePinyin").Layout!;

    private static readonly IReadOnlyDictionary<string, char> Initials = Xiaohe.Initials;

    private static readonly IReadOnlyDictionary<string, char> Finals = Xiaohe.Finals;

    private static readonly HashSet<string> SimpleFinals = new(StringComparer.Ordinal)
    {
        "a",
        "o",
        "e",
        "i",
        "u",
        "v",
    };


    internal static IReadOnlyList<ImeProjectionEntry> GetComponentEntries(bool doublePinyin)
    {
        var entries = new List<ImeProjectionEntry>();
        foreach (var initial in Initials.Keys.Prepend(string.Empty))
        foreach (var final in Finals.Keys.Concat(SimpleFinals).Append("er").Distinct())
        {
            var syllable = initial + final;
            if (!doublePinyin)
            {
                entries.Add(new(initial, final, syllable));
            }
            else if (TryEncodeXiaoheSyllable(syllable, out var code))
            {
                entries.Add(new(initial, final, code));
            }
        }

        return entries;
    }

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

        if (!string.Equals
                (inputScheme, DictionaryPackageFormat.XiaoheDoublePinyinInputScheme, StringComparison.Ordinal))
        {
            var layout = PhoneticSchemeCatalog.All.SingleOrDefault(item => item.SchemeId == inputScheme)?.Layout;
            if (layout?.ZeroInitialCodes is not null)
            {
                var codes = new List<string>();
                foreach (var syllable in reading.Split
                             (' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var normalized = syllable.ToLowerInvariant().Replace("u:", "v", StringComparison.Ordinal).Replace
                        ('ü', 'v');
                    var (initial, final) = SplitInitialFinal(normalized);
                    if (initial is "n" or "l" && final == "ue")
                    {
                        final = "ve";
                    }

                    if (initial is "j" or "q" or "x" or "y")
                    {
                        final = final.Replace('v', 'u');
                    }

                    if (final == "ve" && !layout.Finals.ContainsKey("ve"))
                    {
                        final = "ue";
                    }

                    if (!layout.TryProjectComponents(initial, final, out var code))
                    {
                        lookupKey = string.Empty;
                        return false;
                    }

                    codes.Add(code);
                }

                lookupKey = string.Concat(codes);
                return lookupKey.Length > 0;
            }
        }

        if (!string.Equals
                (inputScheme, DictionaryPackageFormat.XiaoheDoublePinyinInputScheme, StringComparison.Ordinal))
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
        return Xiaohe.TryProjectComponents(string.Empty, pinyin, out code);
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