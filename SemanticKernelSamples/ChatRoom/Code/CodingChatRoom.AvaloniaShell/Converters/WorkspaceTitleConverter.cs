using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace CodingChatRoom.AvaloniaShell.Converters;

/// <summary>
/// 将应用标题与当前工作路径组合为窗口标题。
/// </summary>
public sealed class WorkspaceTitleConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string applicationTitle = parameter as string ?? string.Empty;
        if (value is not string workspacePath || string.IsNullOrWhiteSpace(workspacePath))
        {
            return applicationTitle;
        }

        return string.IsNullOrWhiteSpace(applicationTitle)
            ? workspacePath
            : $"{applicationTitle} - {workspacePath}";
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}