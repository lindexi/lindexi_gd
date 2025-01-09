using System.Diagnostics;
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

        foreach (var fontFamilyName in SKFontManager.Default.GetFontFamilies())
        {
            var fontFamily = new FontFamily(fontFamilyName);
            var lineSpacing = fontFamily.LineSpacing;

            SKTypeface? skTypeface = SKFontManager.Default.MatchFamily(fontFamilyName);
            var skFont = new SKFont(skTypeface, 100);
            var h = (-skFont.Metrics.Ascent + skFont.Metrics.Descent) / 100;

            Debug.WriteLine($"{fontFamilyName} 是否相近 {Math.Abs(lineSpacing - h) < 0.01} {Math.Abs(lineSpacing - h):0.00000}");
        }

    }
}