using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JawgileaweChaquweejer
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

        private void TestButton_OnClick(object sender, RoutedEventArgs e)
        {
            TestButton.Unloaded += (sender, e) =>
            {
                Debug.WriteLine("TestButton_Unloaded");
            };
            ButtonGrid.Children.Remove(TestButton);

            Debug.WriteLine("Before Maximized");
            WindowState = WindowState.Maximized;
            Debug.WriteLine("After Maximized");
        }
    }
}
