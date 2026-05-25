using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AgentLib.Model;

internal static class CopilotChatMessageItemFormatter
{
    private static readonly JsonSerializerOptions IndentedJsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    internal static string? FormatArguments(FunctionCallContent functionCallContent)
    {
        ArgumentNullException.ThrowIfNull(functionCallContent);
        return FormatValue(functionCallContent.Arguments);
    }

    internal static string? FormatResult(FunctionResultContent functionResultContent)
    {
        ArgumentNullException.ThrowIfNull(functionResultContent);
        return FormatValue(functionResultContent.Result);
    }

    internal static string? FormatValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string text)
        {
            return text;
        }

        try
        {
            return JsonSerializer.Serialize(value, IndentedJsonSerializerOptions);
        }
        catch (NotSupportedException)
        {
            return value.ToString();
        }
    }
}