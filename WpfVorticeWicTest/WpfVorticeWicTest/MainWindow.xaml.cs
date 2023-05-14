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

        using IWICBitmap wicBitmap = OffScreenRenderingWICBitmap();

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

    private static IWICBitmap OffScreenRenderingWICBitmap()
    {
        using var wicImagingFactory = new IWICImagingFactory();
        IWICBitmap wicBitmap =
            wicImagingFactory.CreateBitmap(1000, 1000, Vortice.WIC.PixelFormat.Format32bppPBGRA);

        using D2D.ID2D1Factory1 d2DFactory = D2D.D2D1.D2D1CreateFactory<D2D.ID2D1Factory1>();
        var renderTargetProperties = new D2D.RenderTargetProperties(PixelFormat.Premultiplied);
        D2D.ID2D1RenderTarget d2D1RenderTarget =
            d2DFactory.CreateWicBitmapRenderTarget(wicBitmap, renderTargetProperties);

        using var renderTarget = d2D1RenderTarget;
        // 开始绘制逻辑
        renderTarget.BeginDraw();

        Render(renderTarget);

        renderTarget.EndDraw();

        return wicBitmap;
    }

    private static void Render(D2D.ID2D1RenderTarget renderTarget)
    {
        using var wicImagingFactory = new IWICImagingFactory();
        var imageFilePath = System.IO.Path.GetFullPath("Image.png");
        using var wicStream = wicImagingFactory.CreateStream(imageFilePath, FileAccess.Read);
        using var decoder = wicImagingFactory.CreateDecoderFromStream(wicStream, DecodeOptions.CacheOnLoad/*参数和 WPF 一样*/);
        // 解码器将可以解码出图片，对于动态图片可以解析出多张图片出来，对于静态图片只能解析出一张
        // 对于静态图片（区别于 gif 等动态图片）只须取首个
        using var imageFrame = decoder.GetFrame(0);

        using IWICBitmap bitmap = wicImagingFactory.CreateBitmapFromSource(imageFrame, BitmapCreateCacheOption.CacheOnLoad);

        // 图片的格式不一定是能符合 D2D 预期的，转换一下格式
        // 否则 CreateBitmapFromWicBitmap 失败 0x88982F80 WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT
        using IWICFormatConverter converter = wicImagingFactory.CreateFormatConverter();
        // 这里不是真实的立刻进行转换哦，实际转换执行是隐藏起来的
        converter.Initialize(imageFrame, Vortice.WIC.PixelFormat.Format32bppPBGRA, BitmapDitherType.None, null, 0, BitmapPaletteType.MedianCut);
        // 这个 IWICFormatConverter 也继承是 IWICBitmapSource 类型

        var d2DBitmap = renderTarget.CreateBitmapFromWicBitmap(converter);

        renderTarget.DrawBitmap(d2DBitmap);
    }
}
