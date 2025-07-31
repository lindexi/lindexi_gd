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

namespace RajereleawarnawaLarkeqairjo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        TheTextBlock.Text = new string([..Enumerable.Repeat('a', 2000)]);

        Loaded += MainWindow_Loaded;
    }

    protected override void OnManipulationBoundaryFeedback(ManipulationBoundaryFeedbackEventArgs e)
    {
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var fontFamily = new FontFamily(@"C:\lindexi\Font\StandardSymbolsPS.ttf");
        Typeface typeface = fontFamily.GetTypefaces().First();
        if (typeface.TryGetGlyphTypeface(out GlyphTypeface? glyphTypeface))
        {
            Rune rune = new Rune('p');
            var codePoint = rune.Value;
            glyphTypeface.CharacterToGlyphMap.TryGetValue(codePoint, out var glyphIndex);
        }
    }
}