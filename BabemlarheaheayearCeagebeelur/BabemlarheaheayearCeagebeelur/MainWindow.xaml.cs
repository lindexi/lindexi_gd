using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace BabemlarheaheayearCeagebeelur
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        [Fody.ConfigureAwait(false)]
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"ThreadId={Thread.CurrentThread.ManagedThreadId}");
            await Task.Delay(100);
            Debug.WriteLine($"ThreadId={Thread.CurrentThread.ManagedThreadId}");
        }
    }
}
