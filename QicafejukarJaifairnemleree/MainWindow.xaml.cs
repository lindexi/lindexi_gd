using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QicafejukarJaifairnemleree
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

        private void ComboBox_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement frameworkElement)
            {
                if (frameworkElement.DataContext is Brush brush)
                {
                    TextBlock.Foreground = brush;
                }
            }
        }
    }
}
