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

namespace KeneharcerJarkukallwairgai;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        var args = new List<string>();
        Foo foo = new Foo();
        var f1 = foo.F1;
        if (args[0] == "")
        {
            f1 = args[0];
        }

        foo = new Foo()
        {
            F1 = f1
        };

        InitializeComponent();
    }
}

class Foo
{
    public object F1 { get; init; } = "asd";
}