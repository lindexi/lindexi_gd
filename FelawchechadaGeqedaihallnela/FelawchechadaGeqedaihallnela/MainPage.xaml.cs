using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FelawchechadaGeqedaihallnela;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        Loaded += MainPage_Loaded;

        //RootPanel.PointerPressed += RootPanel_PointerPressed;
        RootPanel.PointerReleased += RootPanel_PointerReleased;
    }

    private void RootPanel_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(e.Pointer.PointerId);
    }

    private void RootPanel_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(e.Pointer.PointerId);
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        var imageFile = Path.GetFullPath("Image.jpg");
        var bitmapImage = new BitmapImage(new Uri(imageFile));
        bitmapImage.ImageOpened += (o, args) =>
        {

        };
        Image.Source = bitmapImage;
    }
}
