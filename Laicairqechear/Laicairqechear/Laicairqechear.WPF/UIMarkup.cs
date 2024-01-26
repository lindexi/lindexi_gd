using System.Windows;
using System.Windows.Controls;

namespace Laicairqechear;

static class UIMarkup
{
    public static T GridRow<T>(this T element, int n) where T : FrameworkElement
    {
        Grid.SetRow(element, n);
        return element;
    }

    public static T GridColumn<T>(this T element, int n) where T : FrameworkElement
    {
        Grid.SetColumn(element, n);
        return element;
    }
}
