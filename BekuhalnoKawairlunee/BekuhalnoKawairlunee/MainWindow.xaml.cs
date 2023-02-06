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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BekuhalnoKawairlunee;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        for (int i = 0; i < 3; i++)
        {
            var model = new Model()
            {
                Name = i.ToString()
            };

            List.Add(model);
        }

        List.CollectionChanged += List_CollectionChanged;

        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void List_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        List.CollectionChanged -= List_CollectionChanged;

        List.Add(new Model()
        {
            Name = "xx"
        });
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(100);

        List.RemoveAt(1);
    }

    public ObservableCollection<Model> List { get; } = new ObservableCollection<Model>();
}

public class Model
{
    public string? Name { get; set; }

    public override string? ToString() => Name;
}
