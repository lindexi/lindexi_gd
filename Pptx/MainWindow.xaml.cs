#nullable enable
using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

using dotnetCampus.OpenXmlUnitConverter;

using GeneratedCode;

using OpenMcdf;

using ColorMap = DocumentFormat.OpenXml.Presentation.ColorMap;
using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
using Path = DocumentFormat.OpenXml.Drawing.Path;
using Rectangle = System.Windows.Shapes.Rectangle;
using SchemeColorValues = DocumentFormat.OpenXml.Drawing.SchemeColorValues;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using ShapeProperties = DocumentFormat.OpenXml.Presentation.ShapeProperties;

namespace Pptx
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
            //foreach (var textAlignmentTypeValue in Enum.GetValues<TextAlignmentTypeValues>())
            //{
            //    var generatedClass = new GeneratedClass()
            //    {
            //        TextAlignment = textAlignmentTypeValue
            //    };

            //    var file = $"{textAlignmentTypeValue}.pptx";
            //    generatedClass.CreatePackage(file);

            //    Process.Start("explorer.exe", file);
            //}

            var file = new FileInfo("Test.pptx");

            using var presentationDocument = PresentationDocument.Open(file.FullName, false);
            var slide = presentationDocument.PresentationPart!.SlideParts.First().Slide;

            var shape = slide.CommonSlideData!.ShapeTree!.GetFirstChild<Shape>()!;
            /*
                  <p:sp>
                    <p:spPr>
                      <a:prstGeom prst="rect">
                      </a:prstGeom>
                      <a:noFill />
                    </p:spPr>
                    <p:txBody>
                      <a:bodyPr wrap="square" rtlCol="0">
                        <a:spAutoFit />
                      </a:bodyPr>
                      <a:lstStyle />
                      <a:p>
                        <a:r>
                          <a:rPr lang="zh-CN" altLang="en-US" sz="10000">
                            <a:ln w="9525">
                              <a:solidFill>
                                <a:srgbClr val="00FF00" />
                              </a:solidFill>
                            </a:ln>
                          </a:rPr>
                          <a:t>一行文本</a:t>
                        </a:r>
                        <a:endParaRPr lang="en-US" sz="10000" dirty="0" />
                      </a:p>
                    </p:txBody>
                  </p:sp>
             */
            var shapeProperties = shape.ShapeProperties!;
            var presetGeometry = shapeProperties.GetFirstChild<PresetGeometry>()!;
            // 这是一个文本框
            Debug.Assert(presetGeometry.Preset?.Value == ShapeTypeValues.Rectangle);
            Debug.Assert(shapeProperties.GetFirstChild<NoFill>() is not null);

            var textBody = shape.TextBody!;
            Debug.Assert(textBody != null);

            foreach (var paragraph in textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>())
            {
                // 一个文本里面有很多段落
                // 段落里面，文本有不同的样式，如一段可以有不同加粗的文本
                // 相同的样式的文本放在一个 TextRun 里面。不同的样式的文本放在不同的 TextRun 里面

                // 这个文本段落是没有属性的，为了方便样式，就不写代码
                //if (paragraph.ParagraphProperties != null)

                foreach (var run in paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>())
                {
                    var runProperties = run.RunProperties!;
                    var fontSize = new PoundHundredfold(runProperties.FontSize!.Value).ToPound();

                    var outline = runProperties.Outline!;
                    /*
                       <a:ln w="9525">
                         <a:solidFill>
                           <a:srgbClr val="00FF00" />
                         </a:solidFill>
                       </a:ln>
                     */
                    // 描边粗细
                    var outlineWidth = new Emu(outline.Width!.Value);
                    var solidFill = outline.GetFirstChild<SolidFill>()!;
                    var rgbColorModelHex = solidFill.GetFirstChild<RgbColorModelHex>()!;
                    // 描边颜色
                    var colorText = rgbColorModelHex.Val!;

                    // 默认字体前景色是黑色

                    // 文本
                    var text = run.Text!.Text;

                    // 在 WPF 里面绘制文本描边
                    // 请看 [WPF 文字描边](https://blog.lindexi.com/post/WPF-%E6%96%87%E5%AD%97%E6%8F%8F%E8%BE%B9.html )
                    var formattedText = new FormattedText(text, CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface
                        (
                            // 默认是宋体
                            new FontFamily("宋体"),
                            FontStyles.Normal,
                            FontWeights.Normal,
                            FontStretches.Normal
                        ),
                        // 在 WPF 里面，采用的是 EM 单位，约等于像素单位
                         fontSize.ToPixel().Value,
                        Brushes.Black, 96);

                    var geometry = formattedText.BuildGeometry(new ());

                    var path = new System.Windows.Shapes.Path
                    {
                        Data = geometry,
                        Fill = Brushes.Black,
                        // 描边颜色
                        Stroke = BrushCreator.CreateSolidColorBrush(colorText),
                        StrokeThickness = outlineWidth.ToPixel().Value,

                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    Root.Children.Add(path);
                }
            }
        }
    }
}
