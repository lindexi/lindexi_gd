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
            var file = new FileInfo("Test.pptx");

            using var presentationDocument = PresentationDocument.Open(file.FullName, false);
            var slide = presentationDocument.PresentationPart!.SlideParts.First().Slide;

            var shape = slide.CommonSlideData!.ShapeTree!.GetFirstChild<Shape>()!;
            /*
                <p:sp>
                  <p:nvSpPr>
                    <p:cNvPr id="4" name="文本框 3" />
                    <p:cNvSpPr txBox="1" />
                    <p:nvPr />
                  </p:nvSpPr>
                  <p:spPr>
                    <a:xfrm>
                      <a:off x="4168346" y="914401" />
                      <a:ext cx="6096000" cy="3170090" />
                    </a:xfrm>
                    <a:prstGeom prst="rect">
                      <a:avLst />
                    </a:prstGeom>
                    <a:noFill />
                  </p:spPr>
                  <p:txBody>
                    <a:bodyPr wrap="square" rtlCol="0">
                      <a:normAutofit fontScale="60000"/>
                    </a:bodyPr>
                    <a:lstStyle />
                    <a:p>
                      <a:r>
                        <a:rPr lang="zh-CN" altLang="en-US" sz="10000">
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
            var textBodyProperties = textBody.BodyProperties!;
            Debug.Assert(textBodyProperties != null);
            var normalAutoFit = textBodyProperties.GetFirstChild<NormalAutoFit>()!;
            Debug.Assert(normalAutoFit != null);
            Percentage fontScale = normalAutoFit.FontScale is null
                ? Percentage.FromDouble(1)
                : new Percentage(normalAutoFit.FontScale);

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

                    // 默认字体前景色是黑色
                    var text = run.Text!.Text;

                    var textBlock = new TextBlock()
                    {
                        Text = text,
                        FontSize = fontSize.ToPixel().Value * fontScale.DoubleValue,
                        FontFamily = new FontFamily("宋体"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    Root.Children.Add(textBlock);
                }
            }
        }
    }
}
