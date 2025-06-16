using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChekelnairlarleGedodarfebenay;

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
            FooCollection.Add(new object());
        }

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        while (true)
        {
            await Task.Delay(1000);
            SetCount(ListBox, 5);
            await Task.Delay(1000);
            SetCount(ListBox, 3);
        }
    }

    public ObservableCollection<object> FooCollection { get; } = [];

    public static readonly DependencyProperty CountProperty = DependencyProperty.RegisterAttached(
        "Count", typeof(int), typeof(MainWindow), new PropertyMetadata(default(int)));

    public static void SetCount(DependencyObject element, int value)
    {
        element.SetValue(CountProperty, value);
    }

    public static int GetCount(DependencyObject element)
    {
        return (int) element.GetValue(CountProperty);
    }
}