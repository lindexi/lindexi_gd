using Microsoft.UI.Xaml.Media.Animation;

namespace WernujarlerhelkayFebairleewel;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        Loaded += MainPage_Loaded;

        PointerPressed += MainPage_PointerPressed;
    }

    private void MainPage_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(null).Properties;
        if (properties.IsLeftButtonPressed)
        {
            FooBorder.Width += 100;
            FooBorder.Height += 100;
        }
        else
        {
            FooBorder.Width -= 100;
            FooBorder.Height -= 100;
        }
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(200);
        var storyboard = (Storyboard) Resources["FooBorderStoryboard"];
        storyboard.Completed += Storyboard_Completed;
        storyboard.Begin();
    }

    private async void Storyboard_Completed(object? sender, object e)
    {
        await Task.Delay(100);
        var storyboard = (Storyboard) Resources["FooBorderStoryboard"];
        storyboard.Stop();
        storyboard.Resume();
        storyboard.Begin();
    }
}
