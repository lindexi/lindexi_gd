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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DirectShowLib;

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

                drawingContext.DrawRectangle(Brushes.DarkSalmon, null, new Rect(10, 10, 100, 100));

                var text = "林德熙abc123ATdVACC";

                var fontFamily = new FontFamily("微软雅黑");
                var typeface = fontFamily.GetTypefaces().First();
                typeface.TryGetGlyphTypeface(out var glyphTypeface);
                var location = new Point(10, 10);
                var fontSize = 25;

                List<ushort> glyphIndices = new List<ushort>();
                List<double> advanceWidths = new List<double>();
                List<Point> glyphOffsets = new List<Point>();
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
                    glyphOffsets: glyphOffsets,
                    characters: text.ToCharArray(), //new char[] {text[0]},
                    deviceFontName: null,
                    clusterMap: null,
                    caretStops: null,
                    language: defaultXmlLanguage
                );

                drawingContext.DrawGlyphRun(Brushes.White, glyphRun);
            }

            Background = new VisualBrush(drawingVisual);
        }

        public static DsDevice[] GetDevices(Guid filterCategory)
        {
            return (from d in DsDevice.GetDevicesOfCat(filterCategory) select d).ToArray();
        }
    }

    class F1 : UIElement
    {
        protected override Size MeasureCore(Size availableSize)
        {
            return new Size(availableSize.Width, 10);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawLine(new Pen(Brushes.Red, 2), new Point(10, 10), new Point(100, 10));
            base.OnRender(drawingContext);
        }
    }

    class F2 : UIElement
    {
        protected override Size MeasureCore(Size availableSize)
        {
            return new Size(availableSize.Width, 10);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            //base.ArrangeCore(finalRect);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawLine(new Pen(Brushes.Black, 3), new Point(10, 10), new Point(100, 10));
            base.OnRender(drawingContext);
        }
    }
}
