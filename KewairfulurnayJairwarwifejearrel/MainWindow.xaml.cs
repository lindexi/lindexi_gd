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

namespace KewairfulurnayJairwarwifejearrel;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InkCanvas.PreviewMouseDown += InkCanvas_PreviewMouseDown;
        InkCanvas.PreviewMouseMove += InkCanvas_PreviewMouseMove;
        InkCanvas.PreviewMouseUp += InkCanvas_PreviewMouseUp;
    }

    private void InkCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void InkCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        e.Handled = true;
    }

    private void InkCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}