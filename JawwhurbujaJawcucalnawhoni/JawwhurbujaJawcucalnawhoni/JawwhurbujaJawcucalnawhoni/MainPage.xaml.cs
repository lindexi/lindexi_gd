namespace JawwhurbujaJawcucalnawhoni;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        Canvas.SetZIndex(Border, 1);
    }
}
