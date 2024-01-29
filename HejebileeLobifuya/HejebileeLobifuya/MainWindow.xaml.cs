using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HejebileeLobifuya;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var slide = new Slide();
        var textBlock = new TextBlock { Text = "Hello World" };
        slide.Children.Add(textBlock);

        var currentSlide = Slide.GetSlide(textBlock);
        if (ReferenceEquals(slide, currentSlide))
        {
        }

        var parent = LogicalTreeHelper.GetParent(textBlock);
    }
}

public class Slide : Canvas
{
    public Slide()
    {
        SetSlide(this, this);
    }

    public static readonly DependencyProperty SlideProperty = DependencyProperty.RegisterAttached(
        "Slide", typeof(Slide), typeof(Slide), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    public static void SetSlide(DependencyObject element, Slide value)
    {
        element.SetValue(SlideProperty, value);
    }

    public static Slide GetSlide(DependencyObject element)
    {
        return (Slide) element.GetValue(SlideProperty);
    }
}