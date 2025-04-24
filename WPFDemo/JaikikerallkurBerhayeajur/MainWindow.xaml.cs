using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
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

using Microsoft.Win32.SafeHandles;

using Win32.Graphics.Imaging;

namespace JaikikerallkurBerhayeajur;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var width = (int) 100;
        var height = (int) 100;

        var writeableBitmap =
            new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        Image.Source = writeableBitmap;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var writeableBitmap = (WriteableBitmap) Image.Source;
        var type = writeableBitmap.GetType();
        var propertyInfo = type.GetProperty("WicSourceHandle", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = propertyInfo?.GetValue(writeableBitmap);
        nint wicBitmapHandle = 0;
        if (value is SafeHandleZeroOrMinusOneIsInvalid safeHandle)
        {
            var handle = safeHandle.DangerousGetHandle();
            wicBitmapHandle = handle;
            var wicBitmap = new IWICBitmap();
            unsafe
            {
                void*** pvtable = (void***) wicBitmapHandle;
                void** vtable = *pvtable;
                wicBitmap.lpVtbl = vtable;

                uint w = 0, h = 0;
                wicBitmap.GetSize(&w, &h);
                Debug.Assert(w == 100);
            }
        }
        Debug.Assert(wicBitmapHandle != 0);
    }
}
