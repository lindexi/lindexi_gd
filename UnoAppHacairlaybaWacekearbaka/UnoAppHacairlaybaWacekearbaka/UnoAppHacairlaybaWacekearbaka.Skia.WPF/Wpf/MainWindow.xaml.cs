using Window = System.Windows.Window;

namespace UnoAppHacairlaybaWacekearbaka.WPF;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        root.Content = new global::Uno.UI.Skia.Platform.WpfHost(Dispatcher, () => new AppHead());
    }
}
