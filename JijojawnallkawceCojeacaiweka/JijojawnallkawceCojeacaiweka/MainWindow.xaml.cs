using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JijojawnallkawceCojeacaiweka;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        TextFormatter f = TextFormatter.Create(TextFormattingMode.Display);

    }

    class F1 : TextFormatter
    {
        public override TextLine FormatLine(TextSource textSource, int firstCharIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak previousLineBreak)
        {
            throw new NotImplementedException();
        }

        public override TextLine FormatLine(TextSource textSource, int firstCharIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak previousLineBreak, TextRunCache textRunCache)
        {
            throw new NotImplementedException();
        }

        public override MinMaxParagraphWidth FormatMinMaxParagraphWidth(TextSource textSource, int firstCharIndex,
            TextParagraphProperties paragraphProperties)
        {
            throw new NotImplementedException();
        }

        public override MinMaxParagraphWidth FormatMinMaxParagraphWidth(TextSource textSource, int firstCharIndex,
            TextParagraphProperties paragraphProperties, TextRunCache textRunCache)
        {
            throw new NotImplementedException();
        }
    }

    class S : TextSource
    {
        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
        {
            throw new NotImplementedException();
        }

        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
        {
            throw new NotImplementedException();
        }

        public override TextRun GetTextRun(int textSourceCharacterIndex)
        {
            return new TextCharacters("xx", 0, 2, new F());
        }

        class F : TextRunProperties
        {
            public override Brush BackgroundBrush { get; }
            public override CultureInfo CultureInfo { get; }
            public override double FontHintingEmSize { get; }
            public override double FontRenderingEmSize { get; }
            public override Brush ForegroundBrush { get; }
            public override TextDecorationCollection TextDecorations { get; }
            public override TextEffectCollection TextEffects { get; }
            public override Typeface Typeface { get; }
        }
    }
}
