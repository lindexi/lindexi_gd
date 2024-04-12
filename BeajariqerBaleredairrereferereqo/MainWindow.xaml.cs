using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace BeajariqerBaleredairrereferereqo;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        WindowStyle = System.Windows.WindowStyle.None;
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            // this removes the thin white bar at the top, but this causes the window to grow a little.
            // No work around was found for this yet.
            CaptionHeight = 0
        });

        //this.ClearValue(WindowChrome.WindowChromeProperty);
        //WindowChrome.SetWindowChrome(this, null);

        //// for some reason touchpad physical presses work without this, but not "taps"
        //WindowChrome.SetIsHitTestVisibleInChrome((IInputElement) Content, true);
    }
}