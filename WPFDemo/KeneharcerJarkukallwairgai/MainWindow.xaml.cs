using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        Foo foo = JsonSerializer.Deserialize<Foo>("{}", FooContext.Default.Foo);

        InitializeComponent();
    }
}

[JsonSerializable(typeof(Foo))]
partial class FooContext : JsonSerializerContext
{

}

class Foo
{
    public string F1 { get; init; } = "asd";
}