using System;
using System.Collections.Generic;
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

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            using IWICBitmap wicBitmap = await OffScreenRenderingWICBitmapAsync();

            var unmanagedBitmapWrapper = WICBitmapToBitmapSource(wicBitmap);

            Image.Source = unmanagedBitmapWrapper;
        }
        catch (Exception)
        {
            // 这里是 async void 线程的顶层，如果有任何异常，那应用就炸了
            // 而采用离屏渲染的 OffScreenRenderingWICBitmapAsync 是预期会有很多奇怪的异常
        }
    }

    private static BitmapSource WICBitmapToBitmapSource(IWICBitmap wicBitmap)
    {
        return System.Windows.Interop.Imaging.CreateBitmapSourceFromWICBitmapSource(wicBitmap.NativePointer);

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

    private static Task<IWICBitmap> OffScreenRenderingWICBitmapAsync()
    {
        return Task.Run(OffScreenRenderingWICBitmap);
    }
    private static IWICBitmap OffScreenRenderingWICBitmap()
    {
        using var wicImagingFactory = new IWICImagingFactory();
        IWICBitmap wicBitmap =
            wicImagingFactory.CreateBitmap(1000, 1000, Win32.Graphics.Imaging.Apis.GUID_WICPixelFormat32bppPBGRA);

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
        // 以下是测试代码
        // 假装是耗时的渲染
        var color = new Color4((byte)Random.Shared.Next(255), (byte)Random.Shared.Next(255),
            (byte)Random.Shared.Next(255));
        renderTarget.Clear(color);

        color = new Color4((byte)Random.Shared.Next(255), (byte)Random.Shared.Next(255),
            (byte)Random.Shared.Next(255));
        using var brush = renderTarget.CreateSolidColorBrush(color);

        // 10万个圆，无论是啥都顶不住
        for (int i = 0; i < 100000; i++)
        {
            renderTarget.DrawEllipse(new D2D.Ellipse(new Vector2(Random.Shared.Next(900), Random.Shared.Next(900)),Random.Shared.Next(1,5), Random.Shared.Next(1, 5)),brush,Random.Shared.Next(1,2));
        }
    }
}
