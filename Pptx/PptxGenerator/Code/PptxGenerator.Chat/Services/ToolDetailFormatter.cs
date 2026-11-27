using System.Text.Encodings.Web;
using System.Text.Json;

namespace PptxGenerator.Chat.Services;

internal static class ToolDetailFormatter
{
    private static readonly JsonSerializerOptions DisplayOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    internal static string Format(string? text)
    {
        if (text is null) return string.Empty;
        var candidate = text.AsSpan().TrimStart();
        if (candidate.IsEmpty || candidate[0] is not ('{' or '[')) return text;

        try
        {
            using var document = JsonDocument.Parse(text);
            return JsonSerializer.Serialize(document.RootElement, DisplayOptions);
        }
        catch (JsonException)
        {
            return text;
        }
    }
}
