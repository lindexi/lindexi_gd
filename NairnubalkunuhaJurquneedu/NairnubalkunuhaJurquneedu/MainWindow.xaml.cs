using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NairnubalkunuhaJurquneedu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var fakeToggleButton = new ToggleButton()
            {
                Margin = new Thickness(10),
                Content = "点击"
            }.Do(button => button.Click += ToggleButton_OnClick);

            Panel.Children.RemoveAt(0);
            Panel.Children.Insert(0, fakeToggleButton);
        }

        private void ToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            var bindingExpression = TextBlock.GetBindingExpression(TextBlock.TextProperty);
            var toggleButton = (ToggleButton) sender;
            if (ReferenceEquals(toggleButton, bindingExpression.DataItem))
            {
            }
        }
    }

    public static class UIElementExtension
    {
        public static T Do<T>(this T t, Action<T> action)
        {
            action(t);
            return t;
        }
    }

    public class FooConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is true)
            {
                return "林德熙是逗比";
            }

            return "林德熙不是逗比";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}