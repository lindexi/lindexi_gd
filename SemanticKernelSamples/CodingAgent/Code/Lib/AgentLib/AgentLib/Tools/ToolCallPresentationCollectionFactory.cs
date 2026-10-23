using System.Collections;
using System.Text.Json;

using AgentLib.Model;

namespace AgentLib.Tools;

public static class ToolCallPresentationCollectionFactory
{
    public static ToolCallPresentation ForCodeSearch(IDictionary<string, object?> arguments)
    {
        IReadOnlyList<string> queries = GetStrings(arguments, "searchQueries");
        string? primaryText = queries.Count switch
        {
            0 => null,
            1 => queries[0],
            _ => $"{queries.Count} 个查询",
        };
        return new ToolCallPresentation(primaryText, null);
    }

    public static ToolCallPresentation ForMultipleReplacements(IDictionary<string, object?> arguments)
    {
        IReadOnlyList<string> filePaths = GetPropertyStrings(arguments, "replacements", "filePath", "FilePath");
        int operationCount = GetCollectionCount(arguments, "replacements");
        string[] distinctFiles = filePaths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        string? primaryText = distinctFiles.Length switch
        {
            0 => null,
            1 => GetLastPathSegment(distinctFiles[0]),
            _ => $"{distinctFiles.Length} 个文件",
        };
        string? secondaryText = operationCount > 0 ? $"{operationCount} 项修改" : null;
        return new ToolCallPresentation(primaryText, secondaryText);
    }

    private static IReadOnlyList<string> GetStrings(IDictionary<string, object?> arguments, string name)
    {
        if (!arguments.TryGetValue(name, out object? value) || value is null)
        {
            return [];
        }

        if (value is JsonElement { ValueKind: JsonValueKind.Array } element)
        {
            return element.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Cast<string>()
                .ToArray();
        }

        return value is IEnumerable<string> strings
            ? strings.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray()
            : [];
    }

    private static IReadOnlyList<string> GetPropertyStrings(
        IDictionary<string, object?> arguments,
        string collectionName,
        params string[] propertyNames)
    {
        if (!arguments.TryGetValue(collectionName, out object? value) || value is null)
        {
            return [];
        }

        if (value is JsonElement { ValueKind: JsonValueKind.Array } element)
        {
            return element.EnumerateArray().Select(item =>
            {
                foreach (string propertyName in propertyNames)
                {
                    if (item.TryGetProperty(propertyName, out JsonElement property)
                        && property.ValueKind == JsonValueKind.String)
                    {
                        return property.GetString();
                    }
                }

                return null;
            }).Where(item => !string.IsNullOrWhiteSpace(item)).Cast<string>().ToArray();
        }

        if (value is IEnumerable<ReplaceOperation> replacements)
        {
            return replacements
                .Select(replacement => replacement.FilePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
        }

        return [];
    }

    private static int GetCollectionCount(IDictionary<string, object?> arguments, string name)
    {
        if (!arguments.TryGetValue(name, out object? value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            JsonElement { ValueKind: JsonValueKind.Array } element => element.GetArrayLength(),
            ICollection collection => collection.Count,
            IEnumerable enumerable => enumerable.Cast<object?>().Count(),
            _ => 0,
        };
    }

    private static string GetLastPathSegment(string path)
    {
        string normalizedPath = path.Replace('/', '\\').TrimEnd('\\');
        int separatorIndex = normalizedPath.LastIndexOf('\\');
        return separatorIndex < 0 ? normalizedPath : normalizedPath[(separatorIndex + 1)..];
    }
}
