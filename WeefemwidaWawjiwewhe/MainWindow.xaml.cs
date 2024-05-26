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

namespace WeefemwidaWawjiwewhe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        dataGrid.Items.Add(new Something()
        {
            A = "Hello",
            B = "World",
        });
    }

    public class Something
    {
        public string A { get; set; }
        public string B { get; set; }
    }
}