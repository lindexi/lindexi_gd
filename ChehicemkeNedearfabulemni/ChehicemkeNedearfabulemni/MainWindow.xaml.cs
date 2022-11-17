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

namespace ChehicemkeNedearfabulemni;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        for (int i = 0; i < 100; i++)
        {
            ModelList.Add(new Model());
        }

        InitializeComponent();
    }

    public ObservableCollection<Model> ModelList { get; } = new ObservableCollection<Model>();
}

public class Model
{
    public Model()
    {
        Name = "Name_" + _count;
        Description = "Description_" + _count;
        Number = _count;

        _count++;
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public int Number { get; set; }

    private static int _count;
}