using System;
using System.Collections.Generic;
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
using System.Windows.Threading;

namespace NerehebunaywarRoheeyeekularu
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

        private async void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new Window()
            {
                Background = Brushes.Gray,
                Height = 200,
                Width = 200,
                Top = 100000
            };

            window.Show();

            await Dispatcher.Yield();
            window.Top = 200;
        }
    }
}
