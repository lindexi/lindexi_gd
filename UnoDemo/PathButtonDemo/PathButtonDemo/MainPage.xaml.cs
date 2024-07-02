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
    
    public static readonly DependencyProperty PointerOverStrokeBrushProperty = DependencyProperty.RegisterAttached(
        "PointerOverStrokeBrush", typeof(Brush), typeof(ButtonHelper), new PropertyMetadata(default(Brush)));
    
    public static void SetPointerOverStrokeBrush(DependencyObject element, Brush value)
    {
        element.SetValue(PointerOverStrokeBrushProperty, value);
    }
    
    public static Brush GetPointerOverStrokeBrush(DependencyObject element)
    {
        return (Brush)element.GetValue(PointerOverStrokeBrushProperty);
    }
}
