namespace LightTextEditorPlus.Core.Utils;

static class StringExtension
{
    public static string LimitTrim(this string str, int totalLength, string? replaceText = null)
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
        return str.Substring(0, subLength) + replaceText;
    }
}