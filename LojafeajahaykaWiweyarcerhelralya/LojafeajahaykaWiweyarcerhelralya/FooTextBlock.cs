using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace LojafeajahaykaWiweyarcerhelralya
{
    public class FooTextBlock : TextBlock
    {
        public FooTextBlock()
        {
            Loaded += FooTextBlock_Loaded;
            Unloaded += FooTextBlock_Unloaded;
        }

        private void FooTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"{Text} FooTextBlock_Loaded");
        }

        private void FooTextBlock_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"{Text} FooTextBlock_Unloaded");
        }
    }
}