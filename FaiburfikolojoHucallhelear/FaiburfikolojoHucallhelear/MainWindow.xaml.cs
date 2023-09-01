using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FaiburfikolojoHucallhelear;
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
            List.Add(i);
        }
    }

    public ObservableCollection<int> List { get; }= new ObservableCollection<int>();

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        windowInteropHelper.EnsureHandle();

        this.UpdateLayout();
    }
}
