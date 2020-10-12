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
using dotnetCampus.Ipc.PipeCore;

namespace dotnetCampus.Ipc.WpfDemo
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

        private async void ServerPage_OnServerStarting(object? sender, string e)
        {
            var ipcProvider = new IpcProvider();

            await Task.Delay(TimeSpan.FromSeconds(1));
            ServerPage.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
        }
    }
}
