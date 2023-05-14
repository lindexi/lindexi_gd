using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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

using Vortice.Mathematics;
using Vortice.WIC;

using BindingFlags = System.Reflection.BindingFlags;
using D2D = Vortice.Direct2D1;
using PixelFormat = Vortice.DCommon.PixelFormat;

namespace WpfVorticeWicTest;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        using IWICBitmap wicBitmap = OpenFileAsWICBitmap();

        var unmanagedBitmapWrapper = WICBitmapToBitmapSource(wicBitmap);

        Image.Source = unmanagedBitmapWrapper;
    }

    private static BitmapSource WICBitmapToBitmapSource(IWICBitmap wicBitmap)
    {
        var presentationCoreAssembly = typeof(BitmapSource).Assembly;
        var bitmapSourceSafeMILHandleType =
            presentationCoreAssembly.GetType("System.Windows.Media.Imaging.BitmapSourceSafeMILHandle", throwOnError: true)!;
        var bitmapSourceSafeMILHandleConstructor =
            bitmapSourceSafeMILHandleType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                new Type[] { typeof(IntPtr) })!;

        var bitmapSourceSafeMILHandle =
            bitmapSourceSafeMILHandleConstructor.Invoke(new object[] { wicBitmap.NativePointer });

        var unmanagedBitmapWrapperType =
            presentationCoreAssembly.GetType("System.Windows.Media.Imaging.UnmanagedBitmapWrapper")!;

        var unmanagedBitmapWrapperConstructor =
            unmanagedBitmapWrapperType.GetConstructor(BindingFlags.Public | BindingFlags.Instance,
                new Type[] { bitmapSourceSafeMILHandleType })!;

        var unmanagedBitmapWrapper = unmanagedBitmapWrapperConstructor.Invoke(new object[] { bitmapSourceSafeMILHandle });
        return (BitmapSource) unmanagedBitmapWrapper;
    }

    private static IWICBitmap OpenFileAsWICBitmap()
    {
        using var wicImagingFactory = new IWICImagingFactory();
        var imageFilePath = System.IO.Path.GetFullPath("Image.png");
        using var wicStream = wicImagingFactory.CreateStream(imageFilePath, FileAccess.Read);
        using var decoder = wicImagingFactory.CreateDecoderFromStream(wicStream, DecodeOptions.CacheOnLoad/*参数和 WPF 一样*/);
        // 解码器将可以解码出图片，对于动态图片可以解析出多张图片出来，对于静态图片只能解析出一张
        // 对于静态图片（区别于 gif 等动态图片）只须取首个
        using var imageFrame = decoder.GetFrame(0);

        IWICBitmap wicBitmap = wicImagingFactory.CreateBitmapFromSource(imageFrame, BitmapCreateCacheOption.CacheOnLoad);

        return wicBitmap;
    }
}
