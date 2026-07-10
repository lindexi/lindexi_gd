using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Extensions.AI;

namespace CoursewarePptxGeneratorWpfDemo.Views.Converters;

/// <summary>
/// Converts a chat role to chat bubble alignment.
/// </summary>
public sealed class ChatBubbleAlignmentConverter : IValueConverter
{
    /// <summary>
    /// Gets the shared converter instance.
    /// </summary>
    public static readonly ChatBubbleAlignmentConverter Instance = new();

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ChatRole role && role == ChatRole.User ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>
/// Converts a chat role to chat bubble background.
/// </summary>
public sealed class ChatBubbleBackgroundConverter : IValueConverter
{
    /// <summary>
    /// Gets the shared converter instance.
    /// </summary>
    public static readonly ChatBubbleBackgroundConverter Instance = new();

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ChatRole role && role == ChatRole.User
            ? new SolidColorBrush(Color.FromRgb(219, 234, 254))
            : Brushes.White;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>
/// Converts a chat role to author text color.
/// </summary>
public sealed class ChatBubbleAuthorColorConverter : IValueConverter
{
    /// <summary>
    /// Gets the shared converter instance.
    /// </summary>
    public static readonly ChatBubbleAuthorColorConverter Instance = new();

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ChatRole role && role == ChatRole.User
            ? new SolidColorBrush(Color.FromRgb(37, 99, 235))
            : new SolidColorBrush(Color.FromRgb(15, 23, 42));
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
