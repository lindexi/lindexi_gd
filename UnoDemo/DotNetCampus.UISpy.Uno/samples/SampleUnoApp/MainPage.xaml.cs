namespace SampleUnoApp;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    public void Button_OnClick(object sender, RoutedEventArgs args)
    {
        this.ShowUnoSpyWindow();

        StackPanel.Children.Add(new TextBlock()
        {
            Text = "Test"
        });
    }
}
