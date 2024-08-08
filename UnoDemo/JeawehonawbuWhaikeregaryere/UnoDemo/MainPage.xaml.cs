namespace UnoDemo;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        var grid1ColumnDefinition = Grid1.ColumnDefinitions[0];
        grid1ColumnDefinition.Width.IsAbsolute
    }
}

public static class ColumnSharedSizeHelper 
{
    public static readonly DependencyProperty IsSharedSizeScopeProperty =
        DependencyProperty.RegisterAttached(nameof(ColumnSharedSizeHelper), typeof(bool), typeof(UIElement), new PropertyMetadata(false));


    private static readonly DependencyProperty SharedSizeGroupProperty =
        DependencyProperty.RegisterAttached("SharedSizeGroup", typeof(string), typeof(UIElement), new PropertyMetadata(null));


}
