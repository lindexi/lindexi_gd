namespace LeefayjehekijawlalWhichayfawcelhega.Services;

internal sealed class SlideOutlineParser
{
    public IReadOnlyList<string> Parse(string outlineText)
    {
        if (string.IsNullOrWhiteSpace(outlineText))
        {
            return [];
        }

        string normalizedText = outlineText.Replace("\r\n", "\n");
        string[] blocks = normalizedText
            .Split("\n\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (blocks.Length > 1)
        {
            return blocks;
        }

        return normalizedText
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
