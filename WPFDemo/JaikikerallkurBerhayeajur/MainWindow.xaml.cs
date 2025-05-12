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

using Rectangle = System.Drawing.Rectangle;

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
        Foo();
    }

    private unsafe void Foo()
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

            void*** pvtable = (void***) wicBitmapHandle;
            void** vtable = *pvtable;
            wicBitmap.lpVtbl = vtable;

            uint w = 0, h = 0;
            wicBitmap.GetSize(&w, &h);
            Debug.Assert(w == 100);

            var rectangle = new Rectangle()
            {
                X = 0,
                Y = 0,
                Width = 100,
                Height = 100
            };

            var buffer = new byte[w * h * 4];
            Random.Shared.NextBytes(buffer);
            fixed (byte* p = buffer)
            {
                wicBitmap.CopyPixels(&rectangle, w * 4, (uint) buffer.Length, p);

                writeableBitmap.Lock();
                writeableBitmap.WritePixels(new Int32Rect(0, 0, 1, 1), new IntPtr(p), 4, 4);
                writeableBitmap.Unlock();
            }


        }
        Debug.Assert(wicBitmapHandle != 0);
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        Foo();
    }
}
