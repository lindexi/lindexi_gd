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

namespace LiqahainiyurwearJayqucheno;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty Foo1Property
        = DependencyProperty.Register
        (
            nameof(Title), // WPF0001: 这里的 name 不匹配哦
            typeof(string),
            typeof(MainWindow),
            new PropertyMetadata(default(string))
        );

    public string Foo2 // WPF0003: 属性名应该和 DependencyProperty 关联
    {
        get => (string) GetValue(Foo1Property);
        set => SetValue(Foo1Property, value);
    }
}
