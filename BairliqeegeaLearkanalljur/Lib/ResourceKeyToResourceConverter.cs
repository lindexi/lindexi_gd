using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace Lib;

internal class ResourceKeyToResourceConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter,
        System.Globalization.CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return null;
        }

        FrameworkElement? element = values[0] as FrameworkElement;
        object? resourceKey = values[1];

        if (element == null)
        {
            return null;
        }

        if (ResourceKeyConverter != null)
        {
            resourceKey = ResourceKeyConverter.Convert(resourceKey, targetType, ConverterParameter, culture);
        }
        else if (StringFormat != null)
        {
            resourceKey = String.Format(StringFormat, resourceKey);
        }

        object? resource = FindResource == null ? element.TryFindResource(resourceKey) : FindResource(element, resourceKey);

        return resource;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
        System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    [DefaultValue(null)]
    public object? ConverterParameter { get; set; }

    [DefaultValue(null)]
    public IValueConverter? ResourceKeyConverter { get; set; }

    [DefaultValue(null)]
    public string? StringFormat { get; set; }

    public Func<FrameworkElement?, object?, object?>? FindResource { get; set; }
}