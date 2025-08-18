using System.Runtime.InteropServices;
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

namespace App1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    [DllImport("Lib1.dll")]
    private static extern int Start();

    [DllImport("Lib1.dll")]
    private static extern void SetGreetText(IntPtr greetText, int charCount);

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var thread = new Thread(() =>
        {
            var result = Start();

        });
        thread.Start();
    }

    private unsafe void UpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        var greetText = GreetTextBox.Text;
        fixed (char* c = greetText)
        {
            SetGreetText(new IntPtr(c), greetText.Length);
        }
    }
}