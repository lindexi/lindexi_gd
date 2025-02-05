using Microsoft.UI.Xaml.Data;
using WatchDog.Core.Context;

namespace WatchDog.Uno.ValueConverters;

public class CheckShouldWangResultToTextValueConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is CheckShouldWangResult result)
        {
            return CheckShouldWangResultToTextConverter.ToText(result);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value;
    }
}
