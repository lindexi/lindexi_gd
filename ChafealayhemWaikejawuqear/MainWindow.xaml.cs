using System.Collections.ObjectModel;
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

namespace ChafealayhemWaikejawuqear;
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
            var model = new Model();
            for (int j = 0; j < 3; j++)
            {
                model.Collection.Add(new Model());
            }
            Collection.Add(model);
        }
    }

    public ObservableCollection<Model> Collection { get; } = new ObservableCollection<Model>();
}

public class Model
{
    public Model()
    {
        Name = _count.ToString();
        _count++;
    }

    public string Name { get; }

    public ObservableCollection<Model> Collection { get; } = new ObservableCollection<Model>();

    private static int _count;
}