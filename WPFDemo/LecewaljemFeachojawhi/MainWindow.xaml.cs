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

using X11ApplicationFramework.Utils.Edid;

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
        var result = EdidInfo.ReadFromWindows();
        var edids = result.EdidInfoList;
        for (var i = 0; i < edids.Count; i++)
        {
            var edid = edids[i];
            stringBuilder
                .AppendLine($"Edid Manufacturer: {edid.ManufacturerName}")
                .AppendLine($"{edid.BasicDisplayParameters.MonitorPhysicalWidth} x {edid.BasicDisplayParameters.MonitorPhysicalHeight}");

            if (i < edids.Count - 1)
            {
                stringBuilder.AppendLine("==========");
            }
        }

        LogTextBlock.Text = stringBuilder.ToString();
    }
}

