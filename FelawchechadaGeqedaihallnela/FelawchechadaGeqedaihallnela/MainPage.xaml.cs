using Microsoft.UI.Xaml.Media.Imaging;

namespace FelawchechadaGeqedaihallnela;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        Loaded += MainPage_Loaded;
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
