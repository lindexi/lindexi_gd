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

using stakx.WIC;


namespace CabawgakaicurrecalLalkiniyajagear;

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
            var wicBitmap = (IWICBitmap) Marshal.GetObjectForIUnknown(handle);

            var size = wicBitmap.GetSize();
            var buffer = new byte[size.Width * size.Height * 4];
            Random.Shared.NextBytes(buffer);

            // 这里的绘制是无效的，因为在 WPF 底层会重新被 m_pFrontBuffer 覆盖
            wicBitmap.CopyPixels(4 * size.Width, buffer);
        }
        Debug.Assert(wicBitmapHandle != 0);
        var fieldInfo = type.GetField("_pDoubleBufferedBitmap", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo?.GetValue(writeableBitmap) is SafeHandleZeroOrMinusOneIsInvalid
            doubleBufferedBitmapHandle)
        {
            var handle = doubleBufferedBitmapHandle.DangerousGetHandle();
            var doubleBufferedBitmap = Marshal.PtrToStructure<CSwDoubleBufferedBitmap>(handle);
            Debug.Assert(doubleBufferedBitmap.WicBitmap == wicBitmapHandle);
            var wicBitmap = (IWICBitmap) Marshal.GetObjectForIUnknown(doubleBufferedBitmap.WicBitmap);
            var size = wicBitmap.GetSize();
            Debug.Assert(size.Width == 100);
        }
    }
}

[StructLayout(LayoutKind.Explicit)]
struct CSwDoubleBufferedBitmap
{
    // IWICBitmap *                        m_pBackBuffer;
    [FieldOffset(32)]
    public nint WicBitmap; // 刚好 m_pBackBuffer 就是第一个指针字段，无论是 x86 还是 x64 都刚好是第 32 个字节
}