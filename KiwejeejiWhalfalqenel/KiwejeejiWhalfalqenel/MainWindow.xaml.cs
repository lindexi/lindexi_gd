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
using System.Windows.Shapes;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using Path = System.IO.Path;

namespace KiwejeejiWhalfalqenel
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

        private void FixWidthButton_OnClick(object sender, RoutedEventArgs e)
        {
            var file = GetFile("文本固定宽度下划线.pptx");
            ParseFile(file);
        }

        private void WrapButton_OnClick(object sender, RoutedEventArgs e)
        {
            var file = GetFile("文本自适应宽度下划线.pptx");
            ParseFile(file);
        }

        private void ParseFile(FileInfo file)
        {
            using (var presentationDocument =
                PresentationDocument.Open(file.FullName, false))
            {
                var slide = presentationDocument.PresentationPart.SlideParts.First().Slide;
                var textBody = slide.Descendants<DocumentFormat.OpenXml.Presentation.TextBody>().First();

                var textWrap = textBody.BodyProperties.Wrap?.Value??TextWrappingValues.Square;

                TextBlock.Text = textWrap == TextWrappingValues.None ? "自适应宽度" : "固定宽度";
            }
        }

        private FileInfo GetFile(string name)
        {
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return new FileInfo(Path.Combine(folder!, name));
        }
    }
}
