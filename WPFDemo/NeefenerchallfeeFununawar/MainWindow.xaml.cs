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
using SystemFonts = System.Windows.SystemFonts;

namespace NeefenerchallfeeFununawar;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var messageFontFamily = SystemFonts.MessageFontFamily;
        var menuFontName = System.Drawing.SystemFonts.MenuFont?.Name;
        var defaultFontName = System.Windows.Forms.Control.DefaultFont.Name;
    }
}