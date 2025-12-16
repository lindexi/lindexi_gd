using System.Diagnostics;
using Silk.NET.Core.Native;
using Silk.NET.DirectWrite;

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

namespace JallwirekebalaChelchelkonuya;

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

    // 宋体字体
    private const string FontFile = @"C:\windows\fonts\simsun.ttc";

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        DWrite dWrite = DWrite.GetApi();
        ComPtr<IDWriteFactory6> factory = dWrite.DWriteCreateFactory<IDWriteFactory6>(FactoryType.Shared);

        // 宋体字体
        var fontFile = FontFile;

        unsafe
        {
            HResult hr = 0;

            IDWriteFontFaceReference* fontFaceReference;

            fixed (char* pFontFile = fontFile)
            {
                hr = factory.Handle->CreateFontFaceReference(pFontFile, null, (uint) 0, FontSimulations.None,
                    &fontFaceReference);
                hr.Throw();
            }

            IDWriteFontFace3* fontFace3;
            fontFaceReference->CreateFontFace(&fontFace3);

            uint rangeCount = 0;
            fontFace3->GetUnicodeRanges(0, null, ref rangeCount);
            var unicodeRanges = new UnicodeRange[rangeCount];

            fixed (UnicodeRange* p = unicodeRanges)
            {
                fontFace3->GetUnicodeRanges(rangeCount, p, ref rangeCount);
            }

            for (var i = 0; i < unicodeRanges.Length; i++)
            {
                var unicodeRange = unicodeRanges[i];
                var start = new Rune(unicodeRange.First);
                var end = new Rune(unicodeRange.Last);

                Console.WriteLine($"Range {i}: '{start.ToString()}'({start.Value}) - '{end.ToString()}'({end.Value}) Length={end.Value - start.Value + 1}");
            }

            TestWpf(unicodeRanges);
        }
    }

    private void TestWpf(IReadOnlyList<UnicodeRange> unicodeRanges)
    {
        var wpfFontFamily = new FontFamily("宋体");
        Typeface typeface = wpfFontFamily.GetTypefaces().First();
        if (typeface.TryGetGlyphTypeface(out var glyphTypeface))
        {
            for (uint i = 0; i < 6000; i++)
            {
                if (IsInUnicodeRange(i))
                {
                    if (glyphTypeface.CharacterToGlyphMap.TryGetValue((int) i, out var glyphIndex))
                    {
                        // 在范围内的字符，可以找到对应的字形索引
                        _ = glyphIndex;
                    }
                    else
                    {
                        // 在范围内的字符，找不到对应的字形索引
                        Console.WriteLine($"Character {i} is not in the glyph map.");
                        Debugger.Break();
                    }
                }
                else
                {
                    // 不在范围内的字符，预期找不到对应的字形索引
                    if (glyphTypeface.CharacterToGlyphMap.TryGetValue((int) i, out var glyphIndex))
                    {
                        // 不在范围内的字符，居然可以找到对应的字形索引
                        _ = glyphIndex;
                        Debugger.Break();
                    }
                    else
                    {
                        // 不在范围内的字符，预期找不到对应的字形索引
                    }
                }
            }

            bool IsInUnicodeRange(uint codepoint)
            {
                foreach (var unicodeRange in unicodeRanges)
                {
                    if (codepoint >= unicodeRange.First && codepoint <= unicodeRange.Last)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}