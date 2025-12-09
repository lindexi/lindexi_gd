using System.Drawing.Imaging;
using Microsoft.Office.Interop.Word;

using System.IO;
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

using Application = Microsoft.Office.Interop.Word.Application;
using Image = System.Windows.Controls.Image;
using Page = Microsoft.Office.Interop.Word.Page;
using Path = System.IO.Path;
using Window = System.Windows.Window;

namespace WowahafallbuNairchearyalai;

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
        ApplicationClass app = new ApplicationClass();
        _app = app;
        app.ApplicationEvents2_Event_WindowActivate += App_ApplicationEvents2_Event_WindowActivate;

        foreach (Microsoft.Office.Interop.Word.Window appWindow in app.Windows)
        {
            foreach (Pane appWindowPane in appWindow.Panes)
            {
                var documentTitle = appWindowPane.Document.OriginalDocumentTitle;
            }
        }
    }

    private ApplicationClass? _app;

    private void App_ApplicationEvents2_Event_WindowActivate(Document doc, Microsoft.Office.Interop.Word.Window wn)
    {
        Panes? documentWindowPanes = wn.Panes;
        if (documentWindowPanes is null)
        {
            return;
        }

        var workFolder = Path.Join(AppContext.BaseDirectory, $"Image_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}");
        Directory.CreateDirectory(workFolder);

        var list = new List<BitmapImage>();

        var documentWindowPanesCount = documentWindowPanes.Count;
        for (var index = 0; index < documentWindowPanesCount; index++)
        {
            Pane documentWindowPane = documentWindowPanes[index + 1];
            var pagesCount = documentWindowPane.Pages.Count;
            for (int i = 0; i < pagesCount; i++)
            {
                Page? page = documentWindowPane.Pages[i + 1];

                var bits = page.EnhMetaFileBits;

                var ms = new MemoryStream((byte[])(bits));

                var imageFile = Path.Join(workFolder, $"{i}.png");
                var image = System.Drawing.Image.FromStream(ms);
                image.Save(imageFile, ImageFormat.Png);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                //bitmapImage.StreamSource = ms;
                bitmapImage.UriSource = new Uri(imageFile);
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                list.Add(bitmapImage);
            }
        }

        Dispatcher.InvokeAsync(() =>
        {
            ListView.Items.Clear();

            var width = ListView.ActualWidth;

            foreach (var bitmapImage in list)
            {
                var height = bitmapImage.PixelHeight * (width / bitmapImage.PixelWidth);

                ListView.Items.Add(new Image()
                {
                    Source = bitmapImage,
                    Width = width,
                    Height = height,
                    Stretch = Stretch.Fill
                });
            }
        });
    }
}