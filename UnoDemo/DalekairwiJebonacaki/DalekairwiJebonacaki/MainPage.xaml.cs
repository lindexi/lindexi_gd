using System.Diagnostics;

namespace DalekairwiJebonacaki;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"Width={ActualWidth} Height={ActualHeight}");
    }
}
