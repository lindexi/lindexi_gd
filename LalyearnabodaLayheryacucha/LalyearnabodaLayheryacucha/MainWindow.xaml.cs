using System.Collections.Generic;
using System.Linq;
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
using DocumentFormat.OpenXml.Presentation;
using dotnetCampus.Threading;

namespace LalyearnabodaLayheryacucha
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Parse();
        }

        private readonly AsyncAutoResetEvent _asyncAutoResetEvent = new AsyncAutoResetEvent(false);

        private async void Parse()
        {
            using (var presentationDocument =
                DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("自定义形状.pptx", false))
            {
                var presentationPart = presentationDocument.PresentationPart;
                var presentation = presentationPart.Presentation;

                // 先获取页面
                var slideIdList = presentation.SlideIdList;

                foreach (var slideId in slideIdList.ChildElements.OfType<SlideId>())
                {
                    // 获取页面内容
                    SlidePart slidePart = (SlidePart) presentationPart.GetPartById(slideId.RelationshipId);

                    var slide = slidePart.Slide;

                    foreach (var customGeometry in slide.Descendants<DocumentFormat.OpenXml.Drawing.CustomGeometry>())
                    {
                        var pathList = customGeometry.Descendants<PathList>().FirstOrDefault();
                        var (stringPath, isLine) = PathListToGeometryStringConverter.BuildPathString(pathList);
                        var geometry = Geometry.Parse(stringPath);
                        Path.Data = geometry;
                        if (isLine)
                        {
                            Path.Fill = Brushes.Transparent;
                        }
                        else
                        {
                            Path.Fill = Brushes.Red;
                        }

                        await _asyncAutoResetEvent.WaitOneAsync();
                    }
                }
            }
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            _asyncAutoResetEvent.Set();
        }
    }
}