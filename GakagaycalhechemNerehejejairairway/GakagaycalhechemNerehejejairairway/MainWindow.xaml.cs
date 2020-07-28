using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using DocumentFormat.OpenXml.Packaging;
using dotnetCampus.OpenXMLUnitConverter;

namespace GakagaycalhechemNerehejejairairway
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var file = GetFile("文本框边距.pptx");
            ParseFile(file);
        }

        private void ParseFile(FileInfo file)
        {
            using (var presentationDocument =
                PresentationDocument.Open(file.FullName, false))
            {
                var slide = presentationDocument.PresentationPart.SlideParts.First().Slide;
                var textBody = slide.Descendants<DocumentFormat.OpenXml.Presentation.TextBody>().First();

                var textBodyProperties = textBody.BodyProperties;

                var marginLeft = textBodyProperties.LeftInset?.ToEmu().ToPixel()
                                 
                                 ?? DefaultMargin.Left;

                var marginTop = textBodyProperties.TopInset?.ToEmu().ToPixel()
                                ?? DefaultMargin.Top;

                var marginRight = textBodyProperties.RightInset?.ToEmu().ToPixel()
                                  ?? DefaultMargin.Right;

                var marginBottom = textBodyProperties.BottomInset
                                       ?.ToEmu().ToPixel()
                                   ?? DefaultMargin.Bottom;

                TextBlock.Text = $@"左边距 {marginLeft.Value:0.00} 像素
上边距 {marginTop.Value:0.00} 像素
右边距 {marginRight.Value:0.00} 像素
下边距 {marginBottom.Value:0.00} 像素";
            }
        }

        private FileInfo GetFile(string name)
        {
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return new FileInfo(Path.Combine(folder!, name));
        }

        private static MarginThickness DefaultMargin { get; } = new MarginThickness
        (
            new Inch(0.1).ToPixel(),
            new Inch(0.05).ToPixel(),
            new Inch(0.1).ToPixel(),
            new Inch(0.05).ToPixel()
        );
    }
}
