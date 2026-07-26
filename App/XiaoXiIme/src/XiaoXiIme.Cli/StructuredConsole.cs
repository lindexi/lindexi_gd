using System.Text.Encodings.Web;
using System.Text.Json;

namespace XiaoXiIme.Cli;

internal sealed class StructuredConsole(TextWriter output, TextWriter error)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public void Information(string stage, string message, object? data = null) => Write(output, "information", stage, message, data);

    public void Error(string stage, string message, object? data = null) => Write(error, "error", stage, message, data);

    private static void Write(TextWriter writer, string level, string stage, string message, object? data)
    {
        writer.WriteLine(JsonSerializer.Serialize(new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            level,
            stage,
            message,
            data,
        }, JsonOptions));
        writer.Flush();
    }
}
