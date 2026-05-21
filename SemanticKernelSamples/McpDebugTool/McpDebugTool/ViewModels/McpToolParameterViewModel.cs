using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace McpDebugTool.ViewModels;

public sealed class McpToolParameterViewModel : ObservableObject
{
    private readonly JsonElement _schema;
    private string _rawValue;

    public McpToolParameterViewModel(string name, JsonElement schema, bool isRequired)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        _schema = schema;
        IsRequired = isRequired;
        DisplayType = GetDisplayType(schema);
        Description = TryGetString(schema, "description");
        Placeholder = BuildPlaceholder(schema, isRequired);
        _rawValue = string.Empty;
    }

    public string Name { get; }

    public string DisplayType { get; }

    public string? Description { get; }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public bool IsRequired { get; }

    public string Placeholder { get; }

    public string RawValue
    {
        get => _rawValue;
        set => SetProperty(ref _rawValue, value);
    }

    public object? GetValue()
    {
        string text = RawValue.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            if (IsRequired)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "参数 {0} 为必填项。", Name));
            }

            return null;
        }

        if (!TryGetType(_schema, out string? schemaType))
        {
            return ParseJson(text);
        }

        return schemaType switch
        {
            "string" => text,
            "integer" => ParseInteger(text),
            "number" => ParseNumber(text),
            "boolean" => ParseBoolean(text),
            "array" => ParseJson(text),
            "object" => ParseJson(text),
            _ => ParseJson(text),
        };
    }

    private static string GetDisplayType(JsonElement schema)
    {
        if (TryGetType(schema, out string? schemaType))
        {
            return schemaType ?? "json";
        }

        if (schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("enum", out _))
        {
            return "enum";
        }

        return "json";
    }

    private static string BuildPlaceholder(JsonElement schema, bool isRequired)
    {
        string type = GetDisplayType(schema);
        string requiredText = isRequired ? "必填" : "可选";
        string? enumValues = TryGetEnumValues(schema);

        return enumValues is null
            ? string.Format(CultureInfo.CurrentCulture, "{0}，类型：{1}", requiredText, type)
            : string.Format(CultureInfo.CurrentCulture, "{0}，可选值：{1}", requiredText, enumValues);
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
    }

    private static string? TryGetEnumValues(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object || !schema.TryGetProperty("enum", out JsonElement enumElement) || enumElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return string.Join(", ", enumElement.EnumerateArray().Select(item => item.ToString()));
    }

    private static bool TryGetType(JsonElement schema, out string? schemaType)
    {
        schemaType = null;

        if (schema.ValueKind != JsonValueKind.Object || !schema.TryGetProperty("type", out JsonElement typeElement))
        {
            return false;
        }

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            schemaType = typeElement.GetString();
            return !string.IsNullOrWhiteSpace(schemaType);
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            schemaType = typeElement.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .FirstOrDefault(item => !string.Equals(item, "null", StringComparison.OrdinalIgnoreCase));
            return !string.IsNullOrWhiteSpace(schemaType);
        }

        return false;
    }

    private static object ParseInteger(string text)
    {
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
        {
            return value;
        }

        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "参数值 {0} 不是有效的整数。", text));
    }

    private static object ParseNumber(string text)
    {
        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double value))
        {
            return value;
        }

        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "参数值 {0} 不是有效的数字。", text));
    }

    private static object ParseBoolean(string text)
    {
        if (bool.TryParse(text, out bool value))
        {
            return value;
        }

        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "参数值 {0} 不是有效的布尔值。", text));
    }

    private static JsonElement ParseJson(string text)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("请输入有效的 JSON 内容。", ex);
        }
    }
}
