using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace JallbacelarlearyaLereyilagawhelna
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    [DefaultEvent("Foo")]
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();

            MouseDown += Window1_MouseDown;
            Deactivated += Window1_Deactivated;
            LostFocus += Window1_LostFocus;

        }

        public event EventHandler Foo;

        private void Window1_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        private void Window1_Deactivated(object sender, EventArgs e)
        {
            Mouse.Capture(this);
        }

        private void Window1_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void OpenPopupButton_OnClick(object sender, RoutedEventArgs e)
        {
            Popup.IsOpen = true;
        }
    }
}
