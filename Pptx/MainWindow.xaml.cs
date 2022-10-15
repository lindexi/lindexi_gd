using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using PptxCore;

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

            var modelReader = new ModelReader();
            var areaChartRender = modelReader.BuildAreaChartRender(file);

            var tempFile = Path.GetTempFileName();
            var outputFile = new FileInfo(tempFile);

            var skiaPngImageRenderCanvas = new SkiaPngImageRenderCanvas((int) Math.Ceiling(areaChartRender.Context.Width.Value), (int) Math.Ceiling(areaChartRender.Context.Height.Value), outputFile);

            skiaPngImageRenderCanvas.Render(areaChartRender.Render);

            var image = new Image()
            {
                Width = areaChartRender.Context.Width.Value,
                Height = areaChartRender.Context.Height.Value,
                Margin = new Thickness(10, 10, 10, 10),
                Source = new BitmapImage(new Uri(outputFile.FullName))
            };

            Root.Children.Add(image);
        }
    }
}