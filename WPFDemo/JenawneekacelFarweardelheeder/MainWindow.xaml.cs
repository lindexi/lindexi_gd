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

namespace JenawneekacelFarweardelheeder;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void LimitMemoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        JobProvider.StartWithMemoryLimit(100_000);
    }

    private void TakeMemoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        LinkedList.AddLast(new byte[1024000_00]);
        VisitLast();
    }

    private LinkedList<byte[]> LinkedList { get; } = new LinkedList<byte[]>();
    private void VisitLast()
    {
        if (LinkedList.Last is null)
        {
            return;
        }

        var value = LinkedList.Last.Value;
        var random = new Random();
        for (int i = 0; i < value.Length; i += random.Next(1, 100))
        {
            value[i] = (byte) random.Next();
        }
    }
}