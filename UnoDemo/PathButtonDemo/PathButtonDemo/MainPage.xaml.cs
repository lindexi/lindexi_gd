namespace PathButtonDemo;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }
}

public class ButtonHelper
{
    public static readonly DependencyProperty ButtonPathProperty = DependencyProperty.RegisterAttached(
        "ButtonPath", typeof(string), typeof(ButtonHelper), new PropertyMetadata(default(string)));
    
    public static void SetButtonPath(DependencyObject element, string value)
    {
        element.SetValue(ButtonPathProperty, value);
    }
    
    public static string GetButtonPath(DependencyObject element)
    {
        return (string)element.GetValue(ButtonPathProperty);
    }
}
