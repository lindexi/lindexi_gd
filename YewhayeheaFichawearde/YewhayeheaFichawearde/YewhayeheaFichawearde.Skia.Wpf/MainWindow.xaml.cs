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

namespace YewhayeheaFichawearde.WPF.Host
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Root1.Content = new global::Uno.UI.Skia.Platform.WpfHost(Dispatcher, () => new YewhayeheaFichawearde.App()
            {
                Text = "WpfHost 1"
            });

            Root2.Content = new global::Uno.UI.Skia.Platform.WpfHost(Dispatcher, () => new YewhayeheaFichawearde.App());
        }
    }
}
