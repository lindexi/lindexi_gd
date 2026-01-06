using LecewaljemFeachojawhi.EdidReader;

using System.Linq;
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

namespace LecewaljemFeachojawhi;

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
        var stringBuilder = new StringBuilder();
        var edids = RawEdid.Get();
        for (var i = 0; i < edids.Length; i++)
        {
            var edid = edids[i];
            var detail = edid.GetDetail();
            stringBuilder
                .AppendLine($"Edid Manufacturer: {detail.Manufacturer}")
                .AppendLine($"{detail.HorizontalDisplaySize}x{detail.VerticalDisplaySize}")
                .AppendLine($"Raw Data:")
                .AppendLine($"{string.Join(' ', edid.GetRawData().Select(t => t.ToString("X2")))}")
                ;

            if (i < edids.Length - 1)
            {
                stringBuilder.AppendLine("==========");
            }
        }

        LogTextBlock.Text = stringBuilder.ToString();
    }
}

