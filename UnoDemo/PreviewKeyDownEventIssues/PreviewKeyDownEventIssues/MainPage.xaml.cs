namespace PreviewKeyDownEventIssues;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        PreviewKeyDown += MainPage_PreviewKeyDown;
        KeyDown += MainPage_KeyDown;
        
        Focus(FocusState.Keyboard);
    }
    
    private void MainPage_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        LogTextBlock.Text += "\r\n PreviewKeyDown";
    }
    
    private void MainPage_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        LogTextBlock.Text += "\r\n KeyDown";
    }
}
