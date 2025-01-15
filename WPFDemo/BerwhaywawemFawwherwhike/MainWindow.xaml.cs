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
using Windows.Win32;

namespace BerwhaywawemFawwherwhike;

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
        // https://github.com/mootoh/mozc/blob/b85bb8e851c0724ca9dcf21ae817eda06ad2cd03/src/dictionary/user_dictionary_importer.cc
        Span<string> libList =
        [
            "imjp14k.dll", // Office 14 / 2010
            "imjp12k.dll", // Office 12 / 2007
            "imjp10k.dll", // Windows NT 6.0, 6.1
            "imjp9k.dll", // Office 11 / 2003
            // The bottom-of-the-line of our targets is Windows XP
            // so we should stop looking up IMEs at "imjp81k.dll"
            // http://b/2440318
            "imjp81k.dll", // Windows NT 5.1, 5.2
            "imjp8k.dll",   // Office 10 / XP / 2002]
        ];

        FreeLibrarySafeHandle libHandle = new FreeLibrarySafeHandle(IntPtr.Zero);
        foreach (var libName in libList)
        {
            libHandle = PInvoke.LoadLibrary(libName);
            if (!libHandle.IsInvalid)
            {
                break;
            }
        }

        if (!libHandle.IsInvalid)
        {
            libHandle = PInvoke.LoadLibrary("kernel32.dll");
        }

        var procAddress = PInvoke.GetProcAddress(libHandle, "CreateIFEDictionaryInstance");
        if (procAddress.IsNull)
        {
            libHandle = PInvoke.LoadLibrary("user32.dll");
            procAddress = PInvoke.GetProcAddress(libHandle, "CreateIFEDictionaryInstance");
        }
    }
}