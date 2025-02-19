using System.Collections.Specialized;
using System.Diagnostics;
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

namespace BawayhairhallkerrerHarbalfuleafal;

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

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var longPath = new StringBuilder();
        longPath.Append(@"C:\");
        for (int i = 0; i < 1000; i++)
        {
            longPath.Append(@"abc\");
        }
        longPath.Append("File.cs");

        Clipboard.SetFileDropList(new StringCollection()
        {
            longPath.ToString(),
        });

        OpenClipboard(IntPtr.Zero);
        var dataPointer =
            GetClipboardData(15); //15 is for CF_HDROP datatype - this does in fact return a pointer, so it's working fine
        var fileCount = DragQueryFile(dataPointer, unchecked((int) 0xFFFFFFFF), null, 0);

        for (int i = 0; i < fileCount; i++)
        {
            var charCount = DragQueryFile(dataPointer, i, null, 0);

            var stringBuilder = new StringBuilder(charCount);
            var result = DragQueryFile(dataPointer, i, stringBuilder, charCount);
            var success = result != 0;
            if (success)
            {
                Debug.WriteLine($"Get the file from Clipboard. Length of File Path is {stringBuilder.Length}. FilePath={stringBuilder.ToString()}");
            }
        }

        CloseClipboard();
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern int DragQueryFile(IntPtr hDrop, int iFile, StringBuilder lpszFile, int cch);

    [DllImport("user32.dll")]
    static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool CloseClipboard();
}