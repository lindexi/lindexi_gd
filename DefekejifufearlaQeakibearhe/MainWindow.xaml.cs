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

namespace DefekejifufearlaQeakibearhe;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var file = @"C:\lindexi\Font\手书体.ttf";
        //var fontFamily = new FontFamily(new Uri(@"C:\lindexi\Font\"), "手书体");
        var fontFamily = new FontFamily(@"C:\lindexi\Font\#手书体");
        TextBlock.FontFamily = fontFamily;
    }
}