using Microsoft.UI.Xaml.Media.Animation;

namespace LairhalawcarchemKacallligekay;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(200);
        var storyboard = (Storyboard) Resources["FooBorderStoryboard"];
        storyboard.Completed += Storyboard_Completed;
        storyboard.Begin();
    }

    private void Storyboard_Completed(object? sender, object e)
    {
        Console.WriteLine("Storyboard_Completed");
    }
}
