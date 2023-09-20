using Windows.UI.Xaml;

namespace LefernochihairWhemfawqarkemche.Helpers
{
    public class ButtonHelper
    {
        public static readonly DependencyProperty PathDataProperty = DependencyProperty.RegisterAttached(
            "PathData", typeof(string), typeof(ButtonHelper), new PropertyMetadata(default(string)));

        public static void SetPathData(DependencyObject element, string value)
        {
            element.SetValue(PathDataProperty, value);
        }

        public static string GetPathData(DependencyObject element)
        {
            return (string)element.GetValue(PathDataProperty);
        }
    }
}