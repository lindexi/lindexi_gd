using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace NiwejabainelFehargaye
{
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
            var pixelsPerDip = (float)VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, ActualWidth, ActualHeight));

                var text = "林德熙abc123ATdVACC";

                var fontFamily = new FontFamily("微软雅黑");
                var typeface = fontFamily.GetTypefaces().First();
                typeface.TryGetGlyphTypeface(out var glyphTypeface);
                var location = new Point(10, 100);
                var fontSize = 30;

                List<ushort> glyphIndices = new List<ushort>();
                List<double> advanceWidths = new List<double>();
                //List<Point> glyphOffsets = new List<Point>();
                for (var i = 0; i < text.Length; i++)
                {
                    var c = text[i];
                    var glyphIndex = glyphTypeface.CharacterToGlyphMap[c];
                    glyphIndices.Add(glyphIndex);

                    var width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;
                    advanceWidths.Add(width);

                    // 只是决定每个字的偏移量，记得加上 i 乘以哦。字符最好是叠加上 fontSize 的值，使用 fontSize 的倍数
                    //glyphOffsets.Add(new Point(fontSize * i, 0));
                }

                XmlLanguage defaultXmlLanguage =
                    XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);

                var glyphRun = new GlyphRun
                (
                    glyphTypeface,
                    bidiLevel: 0,
                    isSideways: false,
                    renderingEmSize: fontSize,
                    pixelsPerDip: pixelsPerDip,
                    glyphIndices: glyphIndices,
                    baselineOrigin: location,
                    advanceWidths: advanceWidths,
                    glyphOffsets: null,
                    characters: text.ToCharArray(), //new char[] {text[0]},
                    deviceFontName: null,
                    clusterMap: null,
                    caretStops: null,
                    language: defaultXmlLanguage
                );

                var computeInkBoundingBox = glyphRun.ComputeInkBoundingBox();
                var matrix = new Matrix();
                matrix.Translate(location.X, location.Y);
                computeInkBoundingBox.Transform(matrix);
                //相对于run.BuildGeometry().Bounds方法，run.ComputeInkBoundingBox()会多出一个厚度为1的框框，所以要减去
                if (computeInkBoundingBox.Width >= 2 && computeInkBoundingBox.Height >= 2)
                {
                    computeInkBoundingBox.Inflate(-1, -1);
                }

                drawingContext.DrawRectangle(Brushes.Blue, null, computeInkBoundingBox);
                drawingContext.DrawGlyphRun(Brushes.White, glyphRun);
            }

            Background = new VisualBrush(drawingVisual);
        }
    }
}
