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

namespace RokelnejallwhuNeaferkairce;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_OnStylusDown(object sender, StylusDownEventArgs e)
    {
        StylusPointCollection stylusPointCollection = e.GetStylusPoints(this);

        var stylusPoint = stylusPointCollection[0];
        stylusPoint.X = 1;
        stylusPoint.Y = 2;

        stylusPointCollection.Add(stylusPoint);
    }
}
