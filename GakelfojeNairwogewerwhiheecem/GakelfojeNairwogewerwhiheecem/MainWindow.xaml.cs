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

namespace GakelfojeNairwogewerwhiheecem;
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
        string xaml = "<Section xmlns=\"[http://schemas.microsoft.com/winfx/2006/xaml/presentation\"](http://schemas.microsoft.com/winfx/2006/xaml/presentation/%22) >" +
                      "<Paragraph><Run>" +
                      "<Run.TextDecorations><TextDecorationCollection xmlns=\"[http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextDecoration](http://schemas.microsoft.com/winfx/2006/xaml/presentation/%22%3E%3CTextDecoration) PenThicknessUnit=\"FontRecommended\" Location=\"Underline\"><TextDecoration.Pen><Pen Brush=\"#FF00FFFF\" Thickness=\"2\" /></TextDecoration.Pen></TextDecoration></TextDecorationCollection></Run.TextDecorations>" +
                      "xxx</Run></Paragraph></Section>";



        MemoryStream mems = new MemoryStream();
        mems.Write(Encoding.UTF8.GetBytes(xaml));
        mems.Position = 0;

        FlowDocument doc = new FlowDocument();



        TextRange all = new TextRange(doc.ContentStart, doc.ContentEnd);
        all.Load(mems, DataFormats.Xaml);



        MemoryStream saveStream = new MemoryStream();
        all = new TextRange(doc.ContentStart, doc.ContentEnd);
        all.Save(saveStream, DataFormats.XamlPackage);



        saveStream.Position = 0;
        all = new TextRange(doc.ContentStart, doc.ContentEnd);

        // THIS LINE THROWS
        all.Load(saveStream, DataFormats.XamlPackage);
    }
}
