using System;
using System.Linq;

namespace LightTextEditorPlus.Core.Utils;

static class StringExtension
{
    public static string LimitTrim(this string str, int totalLength, string? replaceText = null, bool saveStartAndEnd = true)
    {
        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }

        if (str.Length <= totalLength)
        {
            return str;
        }

        replaceText ??= "...";

        var subLength = totalLength - replaceText.Length;

        if (saveStartAndEnd)
        {
            var halfSubLength = subLength / 2;
            ReadOnlySpan<char> readOnlySpan = str.AsSpan();
            return string.Concat(readOnlySpan[..halfSubLength], replaceText, readOnlySpan[^halfSubLength..]);
        }
        else
        {
            return str.Substring(0, subLength) + replaceText;
        }
    }
}