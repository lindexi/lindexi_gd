using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DocumentFormat.OpenXml.Drawing;
using dotnetCampus.OpenXmlUnitConverter;
using Paragraph = DocumentFormat.OpenXml.Drawing.Paragraph;
using Run = DocumentFormat.OpenXml.Drawing.Run;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

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

            using var presentationDocument =
                DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(file.FullName, false);
            var slide = presentationDocument.PresentationPart.SlideParts.First().Slide;

            var shape = slide.CommonSlideData.ShapeTree.GetFirstChild<Shape>();
            // 获取坐标
            var offset = shape.ShapeProperties.Transform2D.Offset;
            var x = new Emu(offset.X);
            var y = new Emu(offset.Y);

            // 读取文本内容
            var textBody = shape.TextBody;

            // 读取段落
            var paragraph = textBody.GetFirstChild<Paragraph>();

            // 读取段落的文本
            var run = paragraph.GetFirstChild<Run>();

            // 读取删除线
            var strike = run.RunProperties.Strike;

            // 创建元素
            var textBlock = new TextBlock()
            {
                TextDecorations = strike.Value == TextStrikeValues.NoStrike? new TextDecorationCollection():TextDecorations.Strikethrough,
                Text = run.Text.Text,
                Margin = new Thickness()
                {
                    Left = x.ToPixel().Value,
                    Top = y.ToPixel().Value,
                }
            };

            Canvas.Children.Add(textBlock);
        }
    }
}
