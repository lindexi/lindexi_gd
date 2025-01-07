using System.Globalization;
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

namespace LahallgucheHichawaki;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

class TextRenderTestFrameworkElement : FrameworkElement
{
    public TextRenderTestFrameworkElement()
    {
        Width = 200;
        Height = 200;

        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var dpiScale = VisualTreeHelper.GetDpi(this);
        
        // Create a formatted text string.
        FormattedText formattedText = new FormattedText(
            "Hello, world!",
            CultureInfo.GetCultureInfo("en-us"),
            FlowDirection.LeftToRight,
            new Typeface("Verdana"),
            32,
            Brushes.Black, dpiScale.PixelsPerDip);
        // Draw the formatted text string to the DrawingContext of the control.
        drawingContext.DrawText(formattedText, new Point(10, 0));
    }
}