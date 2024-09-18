using System.Runtime.CompilerServices;
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
using dotnetCampus.Logging;

namespace LurlalaychawbuWhalljaljemwo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(100,2);
        defaultInterpolatedStringHandler.AppendLiteral("123123123123123123");
        defaultInterpolatedStringHandler.AppendFormatted(123);
        
        Log.Debug($"Foo={typeof(MainWindow)}");
    }
}