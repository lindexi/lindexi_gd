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

namespace LucakideJaiweejemnalwo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        var menuItem = (MenuItem) sender;

        DependencyObject t = menuItem;
        while (t != null || t is ContextMenu)
        {
            t = LogicalTreeHelper.GetParent(t);
        }

        var c = t as ContextMenu;
        var placementTarget = c?.PlacementTarget as Grid;
    }
}