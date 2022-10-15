using System;
using System.Collections.Generic;
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

namespace JagerekukibeHelcewhalay;
/// <summary>
/// ImageSelectionCanvas.xaml 的交互逻辑
/// </summary>
public partial class ImageSelectionCanvas : UserControl
{
    public ImageSelectionCanvas()
    {
        InitializeComponent();

        Loaded += ImageSelectionCanvas_Loaded;
    }

    private void ImageSelectionCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        Image.Width = this.ActualWidth;
        Image.Height = this.ActualHeight;
    }
}
