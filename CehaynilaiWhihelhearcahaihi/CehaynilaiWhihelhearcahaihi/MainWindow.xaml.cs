using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace CehaynilaiWhihelhearcahaihi;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        foreach (var file in Directory.GetFiles(Environment.CurrentDirectory,"*.png"))
        {
            var bitmapImage = new BitmapImage(new Uri(file));
            ModelCollection.Add(new Model(bitmapImage));
        }

        InitializeComponent();
    }

    public ObservableCollection<Model> ModelCollection { get; } = new ObservableCollection<Model>();

    private void Bd_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not Border border)
        {
            return;
        }

        if (e.WidthChanged)
        {
            border.Height = e.NewSize.Width * 9 / 16;
        }
    }
}

public class Model
{
    public Model(BitmapSource image)
    {
        Image = image;
    }

    public BitmapSource Image { set; get; }
}