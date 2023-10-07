using System;
using System.Collections.Generic;
using System.Drawing.Text;
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

namespace CalkejeljereChalreloyajur;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var file = "f:\\temp\\汉仪文黑-85W.ttf";

        PrivateFontCollection collection = new PrivateFontCollection();
        collection.AddFontFile(file);
        foreach (var collectionFamily in collection.Families)
        {
            
        }

        var uri = new Uri(file);
        FontFamily fontFamily = new FontFamily(uri, "汉仪文黑-85W");
        //TextBlock.FontFamily = fontFamily;
        TextBlock.FontWeight = FontWeight.FromOpenTypeWeight(485);
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var font = TextBlock.FontFamily;
    }
}
