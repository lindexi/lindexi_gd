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
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;

namespace LaykearduchuNachairgurharhear
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

        private void InkCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            //InkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse;
        }

        private void WindowsXamlHost_ChildChanged(object? sender, EventArgs e)
        {
        }
    }
}
