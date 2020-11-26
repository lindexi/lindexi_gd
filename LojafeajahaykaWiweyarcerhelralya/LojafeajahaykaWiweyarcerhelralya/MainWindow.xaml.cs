using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LojafeajahaykaWiweyarcerhelralya
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
    }

    public class ViewModel
    {
        public ViewModel()
        {
            for (int i = 0; i < 100; i++)
            {
                Collection.Add(i.ToString());
            }
        }

        public ObservableCollection<string> Collection { get; }  = new ObservableCollection<string>();
    }

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
