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

namespace JoluherfaBiqadearjo;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        for (int i = 0; i < 10; i++)
        {
            var foo = new Foo()
            {
                Name = i.ToString()
            };

            foreach (var _ in Enumerable.Repeat(1,10))
            {
                foo.F1.Add(new Foo()
                {
                    Name = i.ToString()
                });
            }

            Foo.Add(foo);
        }
    }

    public List<Foo> Foo { get; } = new List<Foo>();
}


public class Foo
{
    public string? Name { get; set; }

    public List<Foo> F1 { set; get; } = new List<Foo>();
}