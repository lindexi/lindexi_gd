using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace LechelaneHenayfucee;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var collectionViewSource = new CollectionViewSource()
        {
            Source = new List<Foo>(),
            IsLiveSortingRequested = true,
        };

        var collectionView = collectionViewSource.View;
        _collectionView = collectionView;

        collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Descending));

        Loaded += MainWindow_Loaded;
    }

    private readonly ICollectionView _collectionView;

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        GC.Collect();
        GC.WaitForFullGCComplete();
        GC.Collect();
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        _collectionView.SortDescriptions.Clear();
    }
}

public class Foo
{
    public string? Name { set; get; }
}
