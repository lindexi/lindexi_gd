using System.Windows;
using System.Windows.Controls;

namespace LojafeajahaykaWiweyarcerhelralya
{
    public class F1 : UserControl
    {
        public F1()
        {
            Loaded += F1_Loaded;
        }

        private void F1_Loaded(object sender, RoutedEventArgs e)
        {
            var foo = Resources["Foo"];
        }
    }
}