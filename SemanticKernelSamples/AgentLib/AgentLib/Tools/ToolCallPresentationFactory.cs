using System.Globalization;
using System.Text.Json;

using AgentLib.Model;

namespace AgentLib.Tools;

/// <summary>
/// 提供不依赖文件系统的通用工具调用摘要生成方法。
/// </summary>
public static class ToolCallPresentationFactory
{
    /// <summary>
    /// 为路径参数创建摘要。
    /// </summary>
    public static ToolCallPresentation ForPath(
        IDictionary<string, object?> arguments,
        string pathArgumentName,
        string? emptyTargetText = null)
    {
        string? path = GetString(arguments, pathArgumentName);
        return new ToolCallPresentation(GetPathDisplayText(path) ?? emptyTargetText, null, path);
    }

    /// <summary>
    /// 为文件及行范围创建摘要。
    /// </summary>
    public static ToolCallPresentation ForFileLineRange(
        IDictionary<string, object?> arguments,
        string filePathArgumentName,
        string startLineArgumentName,
        string endLineArgumentName)
    {
        string? path = GetString(arguments, filePathArgumentName);
        return new ToolCallPresentation(
            GetPathDisplayText(path),
            FormatLineRange(GetInt32(arguments, startLineArgumentName), GetInt32(arguments, endLineArgumentName)),
            path);
    }

    /// <summary>
    /// 为查询和可选目录创建摘要。
    /// </summary>
    public static ToolCallPresentation ForQuery(
        IDictionary<string, object?> arguments,
        string queryArgumentName,
        string? directoryArgumentName = null,
        string? suffix = null)
    {
        string? query = GetString(arguments, queryArgumentName);
        string? directory = directoryArgumentName is null ? null : GetString(arguments, directoryArgumentName);
        string? secondary = GetPathDisplayText(directory);
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            secondary = string.IsNullOrWhiteSpace(secondary) ? suffix : $"{secondary} · {suffix}";
        }

        return new ToolCallPresentation(Quote(SingleLine(query)), secondary, directory);
    }

    /// <summary>
    /// 为测试运行创建摘要。
    /// </summary>
    public static ToolCallPresentation ForTestRun(
        IDictionary<string, object?> arguments,
        string targetPathArgumentName,
        string filterArgumentName)
    {
        string? targetPath = GetString(arguments, targetPathArgumentName);
        return new ToolCallPresentation(
            GetPathDisplayText(targetPath) ?? "整个工作区",
            SingleLine(GetString(arguments, filterArgumentName)),
            targetPath);
    }

    /// <summary>
    /// 为构建调用创建摘要。
    /// </summary>
    public static ToolCallPresentation ForBuild(IDictionary<string, object?> arguments)
    {
        string? targetPath = GetString(arguments, "targetPath");
        string? secondary = JoinNonEmpty(
            GetString(arguments, "configuration"),
            GetString(arguments, "targetFramework"),
            GetString(arguments, "runtimeIdentifier"));
        return new ToolCallPresentation(GetPathDisplayText(targetPath) ?? "整个工作区", secondary, targetPath);
    }

    /// <summary>
    /// 从参数中安全读取字符串。
    /// </summary>
    public static string? GetString(IDictionary<string, object?> arguments, string name)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!arguments.TryGetValue(name, out object? value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null,
        };
    }

    /// <summary>
    /// 从参数中安全读取整数。
    /// </summary>
    public static int? GetInt32(IDictionary<string, object?> arguments, string name)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!arguments.TryGetValue(name, out object? value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int number => number,
            long number when number is >= int.MinValue and <= int.MaxValue => (int)number,
            JsonElement { ValueKind: JsonValueKind.Number } element when element.TryGetInt32(out int number) => number,
            _ => null,
        };
    }

    /// <summary>
    /// 从参数中安全读取布尔值。
    /// </summary>
    public static bool? GetBoolean(IDictionary<string, object?> arguments, string name)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!arguments.TryGetValue(name, out object? value) || value is null)
        {
            return null;
        }

        return value switch
        {
            bool flag => flag,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            _ => null,
        };
    }

    private static string? GetPathDisplayText(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string normalizedPath = path.Trim().Replace('/', '\\').TrimEnd('\\');
        string[] segments = normalizedPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length switch
        {
            0 => normalizedPath,
            1 => segments[0],
            _ => $"{segments[^2]}\\{segments[^1]}",
        };
    }

    private static string? FormatLineRange(int? startLine, int? endLine)
    {
        if (startLine is null or <= 0)
        {
            return null;
        }

        if (endLine is null or <= 0)
        {
            return $"从第 {startLine.Value.ToString(CultureInfo.InvariantCulture)} 行开始";
        }

        return startLine == endLine
            ? $"第 {startLine.Value.ToString(CultureInfo.InvariantCulture)} 行"
            : $"第 {startLine.Value.ToString(CultureInfo.InvariantCulture)}–{endLine.Value.ToString(CultureInfo.InvariantCulture)} 行";
    }

    private static string? SingleLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? Quote(string? value) => string.IsNullOrWhiteSpace(value) ? null : $"“{value}”";

    private static string? JoinNonEmpty(params string?[] values)
    {
        string[] normalizedValues = values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()).ToArray();
        return normalizedValues.Length == 0 ? null : string.Join(" · ", normalizedValues);
    }
}
