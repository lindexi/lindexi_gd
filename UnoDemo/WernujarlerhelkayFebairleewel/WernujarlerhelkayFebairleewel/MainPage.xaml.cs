using Microsoft.UI.Xaml.Media.Animation;

namespace WernujarlerhelkayFebairleewel;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(2000);
        var storyboard = (Storyboard) Resources["FooBorderStoryboard"];
        storyboard.Begin();
    }
}
