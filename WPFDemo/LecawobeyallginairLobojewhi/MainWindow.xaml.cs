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

namespace LecawobeyallginairLobojewhi;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button1_OnClick(object sender, RoutedEventArgs e)
    {
        _asyncLocal.Value = new Foo()
        {
            Name = "Hello, World!"
        };
    }

    private void Button2_OnClick(object sender, RoutedEventArgs e)
    {
        var foo = _asyncLocal.Value;
        //MessageBox.Show(foo?.Name);

        Task.Run(() =>
        {
            F1();
        });

        _asyncLocal.Value = new Foo()
        {
            Name = "lindexi"
        };
    }

    private void F1()
    {
        var foo = _asyncLocal.Value;
        MessageBox.Show($"F1 {foo?.Name}");
        _asyncLocal.Value = null;
    }

    private readonly AsyncLocal<Foo> _asyncLocal = new AsyncLocal<Foo>();
}

public record Foo
{
    public string? Name { get; set; }
}