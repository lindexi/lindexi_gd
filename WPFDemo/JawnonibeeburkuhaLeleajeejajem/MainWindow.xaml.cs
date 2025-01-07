using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JawnonibeeburkuhaLeleajeejajem;

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

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        foreach (FontFamily? fontFamily in System.Windows.Media.Fonts.SystemFontFamilies)
        {
            if (!fontFamily.FamilyNames.TryGetValue(XmlLanguage.GetLanguage("zh-CN"),out var name))
            {
                name = fontFamily.Source;
            }
            foreach (var typeface in fontFamily.GetTypefaces())
            {
                var typefaceName = typeface.FaceNames.First().Value;
                if (typeface.TryGetGlyphTypeface(out GlyphTypeface? glyphTypeface))
                {
                    var text = "1";
                    var fontSize = 30;

                    var formattedText = new FormattedText(text, CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black, pixelsPerDip);
                    var sameGlyphTypefaceAndFormattedText = Math.Abs(formattedText.Baseline - glyphTypeface.Baseline * fontSize) < 0.01; 
                    var sameFontFamilyAndFormattedText = Math.Abs(formattedText.Baseline - fontFamily.Baseline * fontSize) < 0.01;

                    Debug.WriteLine($"""
                                     字体名： {name} - {typefaceName}
                                     斜体： {glyphTypeface.Style}
                                     加粗： {glyphTypeface.Weight}
                                     拉伸： {glyphTypeface.Stretch}
                                     基线 FontFamily： {fontFamily.Baseline}
                                     基线 GlyphTypeface： {glyphTypeface.Baseline}
                                     基线 FormattedText： {formattedText.Baseline / fontSize}
                                     基线相同 FontFamily == GlyphTypeface： {fontFamily.Baseline == glyphTypeface.Baseline}
                                     基线相近 GlyphTypeface ~ FormattedText： {sameGlyphTypefaceAndFormattedText}
                                     基线相近 FontFamily ~ FormattedText： {sameFontFamilyAndFormattedText}
                                     
                                     """);
                }
            }
        }
    }
}