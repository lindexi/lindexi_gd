namespace KekaneeginearNailulemnu;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        PointerMoved += MainPage_PointerMoved;
    }

    private void MainPage_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(null);
        MessageTextBlock.Text = $"Pointer moved to: {currentPoint.Position.X}, {currentPoint.Position.Y} {currentPoint.Properties.ContactRect}";
    }
}
