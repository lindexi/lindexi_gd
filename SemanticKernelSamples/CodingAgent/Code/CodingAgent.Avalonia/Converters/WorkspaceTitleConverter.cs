using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CodingChatRoom.AvaloniaShell.Converters;

/// <summary>
/// 将应用标题、当前任务名称、工作路径与会话标题组合为窗口标题。
/// </summary>
public sealed class WorkspaceTitleConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        string applicationTitle = parameter as string ?? string.Empty;
        string? workspacePath = values.Count > 0 ? values[0] as string : null;
        string? sessionTitle = values.Count > 1 ? values[1] as string : null;

        string? taskName = values.Count > 2 ? values[2] as string : null;

        var titleParts = new List<string>(4);
        AddTitlePart(titleParts, applicationTitle);
        AddTitlePart(titleParts, taskName);
        AddTitlePart(titleParts, workspacePath);
        AddTitlePart(titleParts, sessionTitle);
        return string.Join(" - ", titleParts);
    }

    private static void AddTitlePart(List<string> titleParts, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            titleParts.Add(value);
        }
    }
}