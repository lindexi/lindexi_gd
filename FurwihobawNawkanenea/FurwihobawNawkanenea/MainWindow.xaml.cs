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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FurwihobawNawkanenea
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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwndSource1 = (HwndSource) PresentationSource.FromVisual(TextBox1); // not null
            var hwndSource2 = (HwndSource) PresentationSource.FromVisual(TextBox2); // null

            if (hwndSource1 is null)
            {
                throw new ArgumentNullException(nameof(hwndSource1));
            }

            if (hwndSource2 is null)
            {
                throw new ArgumentNullException(nameof(hwndSource2));
            }
        }
    }
}
