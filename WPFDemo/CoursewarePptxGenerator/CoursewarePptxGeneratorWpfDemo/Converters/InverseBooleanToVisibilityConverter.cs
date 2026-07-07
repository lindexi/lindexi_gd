using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CoursewarePptxGeneratorWpfDemo.Converters;

/// <summary>
/// Converts a boolean value to inverse visibility.
/// </summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Gets the shared converter instance.
    /// </summary>
    public static readonly InverseBooleanToVisibilityConverter Instance = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
