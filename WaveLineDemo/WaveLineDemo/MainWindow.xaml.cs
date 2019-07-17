using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HandyControl.Data;
using TextBox = HandyControl.Controls.TextBox;

namespace WaveLineDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = (ViewModel) DataContext;
        }

        public ViewModel ViewModel { get; set; }

        private void DrawWaveLine_OnClick(object sender, RoutedEventArgs e)
        {
            var waveLine = ViewModel.Draw();
            WaveLinePanel.Children.Add(waveLine);
        }

        private void CleanGrid_OnClick(object sender, RoutedEventArgs e)
        {
            WaveLinePanel.Children.Clear();
        }
    }
}