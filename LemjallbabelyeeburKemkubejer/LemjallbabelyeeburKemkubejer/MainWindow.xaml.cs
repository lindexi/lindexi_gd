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
using System.Runtime;
using System.Diagnostics;

namespace LemjallbabelyeeburKemkubejer;
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
        var foo1 = new Foo1();

        HandleList.Add(new DependentHandle(foo1, new Foo2()));

        _foo1 = foo1;
    }

    private Foo1? _foo1;

    private void FreeObjectButton_Click(object sender, RoutedEventArgs e)
    {
        _foo1 = null;
    }

    private void GCButton_Click(object sender, RoutedEventArgs e)
    {
        GC.Collect();
        GC.WaitForFullGCComplete();
    }

    private List<DependentHandle> HandleList { get; } = new List<DependentHandle>();

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Debugger.Break();
    }
}

public class Foo1
{

}

public class Foo2
{

}
