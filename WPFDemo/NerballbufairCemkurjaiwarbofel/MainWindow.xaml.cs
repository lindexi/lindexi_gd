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
using TerraFX.Interop.Windows;

namespace NerballbufairCemkurjaiwarbofel;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        var inkDesktopHost = new InkDesktopHost();
        var desktopHost = new IInkDesktopHost();
        desktopHost.CreateInkPresenter();
        var inkPresenterDesktop = new IInkPresenterDesktop();
    }
}