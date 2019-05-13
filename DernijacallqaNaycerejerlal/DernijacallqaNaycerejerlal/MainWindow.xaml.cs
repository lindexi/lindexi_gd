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

namespace DernijacallqaNaycerejerlal
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TouchDown += MainWindow_TouchDown;

            MouseDown += MainWindow_MouseDown;

            SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
             MessageTouchDevice.UseMessageTouch(this);
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e)
        {
            Console.WriteLine("按下");
        }
    }
}