using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AgentLib.Model;

internal static class CopilotChatMessageItemFormatter
{
    private static readonly JsonSerializerOptions IndentedJsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions HumanFriendlyJsonSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    internal static string? FormatArgumentsToHumans(FunctionCallContent functionCallContent)
    {
        return FormatArgumentsToHumans(functionCallContent.RawRepresentation);
    }

    internal static string? FormatArgumentsToHumans(FunctionResultContent functionResultContent)
    {
        return FormatArgumentsToHumans(functionResultContent.Result ?? functionResultContent.RawRepresentation);
    }

    internal static string? FormatArgumentsToHumans(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string text)
        {
            return text;
        }

        if (value is JsonElement jsonElement)
        {
            return jsonElement.ToString();
        }

        try
        {
            return JsonSerializer.Serialize(value, HumanFriendlyJsonSerializerOptions);
        }
        catch (NotSupportedException)
        {
            return value.ToString();
        }
    }

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