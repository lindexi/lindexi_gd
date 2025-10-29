using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RuhuyagayBemkaijearfear
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : PerformanceDesktopTransparentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SetTransparentHitThroughButton_OnClick(object sender, RoutedEventArgs e)
        {
            SetTransparentHitThrough();
        }
    }
}
