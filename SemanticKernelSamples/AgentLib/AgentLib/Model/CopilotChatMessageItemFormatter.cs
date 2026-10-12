using System.Collections.Generic;
using System.Text;
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
        if (functionCallContent.RawRepresentation is not null)
        {
            return FormatArgumentsToHumans(functionCallContent.RawRepresentation);
        }

        // 手动构造的 FunctionCallContent 没有 RawRepresentation，回退到 Arguments 字典做友好展示
        return FormatArgumentsForApproval(functionCallContent.Arguments);
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

        if (value is DataContent dataContent)
        {
            return FormatDataContentToPlaceholder(dataContent);
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

    /// <summary>
    /// 将工具参数字典格式化为审批面板中用户可读的键值对文本。
    /// 与 <see cref="FormatValue"/> 不同，此方法不做 JSON 序列化，而是逐项展开为"参数名: 值"格式。
    /// </summary>
    internal static string? FormatArgumentsForApproval(IEnumerable<KeyValuePair<string, object?>>? arguments)
    {
        if (arguments is null)
        {
            return null;
        }

        var pairs = arguments as IReadOnlyCollection<KeyValuePair<string, object?>> ?? arguments.ToList();
        if (pairs.Count == 0)
        {
            return null;
        }

        if (pairs.Count == 1)
        {
            // 单参数时直接展示值，不显示 key
            return FormatSingleValue(pairs.First().Value);
        }

        var builder = new StringBuilder();
        foreach (KeyValuePair<string, object?> pair in pairs)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(pair.Key).Append(": ").Append(FormatSingleValue(pair.Value));
        }

        return builder.ToString();
    }

    private static string FormatSingleValue(object? value)
    {
        if (value is null)
        {
            return "(空)";
        }

        if (value is string text)
        {
            return text;
        }

        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind == JsonValueKind.String
                ? jsonElement.GetString() ?? string.Empty
                : jsonElement.ToString();
        }

        if (value is DataContent dataContent)
        {
            return FormatDataContentToPlaceholder(dataContent);
        }

        return value.ToString() ?? string.Empty;
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

        if (value is DataContent dataContent)
        {
            return FormatDataContentToPlaceholder(dataContent);
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

    private static string FormatDataContentToPlaceholder(DataContent dataContent)
    {
        string mediaType = dataContent.MediaType ?? string.Empty;
        if (mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "[图片]";
        }

        if (mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return "[音频]";
        }

        return $"[二进制数据: {(string.IsNullOrWhiteSpace(mediaType) ? "application/octet-stream" : mediaType)}]";
    }
}