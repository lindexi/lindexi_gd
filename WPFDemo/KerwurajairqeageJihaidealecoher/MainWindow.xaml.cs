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

using SkiaSharp;

namespace KerwurajairqeageJihaidealecoher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var fontFamily = new FontFamily("微软雅黑");
        var lineSpacing = fontFamily.LineSpacing;

        SKTypeface? skTypeface = SKFontManager.Default.MatchFamily("微软雅黑");
        var skFont = new SKFont(skTypeface, 100);
        var leading = skFont.Metrics.Leading / 100;
        var h = (-skFont.Metrics.Ascent + skFont.Metrics.Descent) / 100;
    }
}