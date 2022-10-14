using System.Diagnostics;
using System.Numerics;
using System.Runtime.Versioning;
using Vortice.Mathematics;
using Vortice.WIC;

using D3D = Vortice.Direct3D;
using D3D11 = Vortice.Direct3D11;
using DXGI = Vortice.DXGI;
using D2D = Vortice.Direct2D1;
using PixelFormat = Vortice.DCommon.PixelFormat;
using Win32.Graphics.Direct3D11;
using System.Drawing;

namespace WakolerwhaKanicabirem;

class Program
{
    // 设置可以支持 Win7 和以上版本。如果用到 WinRT 可以设置为支持 win10 和以上。这个特性只是给 VS 看的，没有实际影响运行的逻辑
    [SupportedOSPlatform("Windows")]
    static unsafe void Main(string[] args)
    {
        D3D.FeatureLevel[] featureLevels = new[]
        {
            D3D.FeatureLevel.Level_11_1,
            D3D.FeatureLevel.Level_11_0,
            D3D.FeatureLevel.Level_10_1,
            D3D.FeatureLevel.Level_10_0,
            D3D.FeatureLevel.Level_9_3,
            D3D.FeatureLevel.Level_9_2,
            D3D.FeatureLevel.Level_9_1,
        };
        D3D11.DeviceCreationFlags creationFlags = D3D11.DeviceCreationFlags.BgraSupport;
        var result = D3D11.D3D11.D3D11CreateDevice
        (
            IntPtr.Zero,
            D3D.DriverType.Warp,
            creationFlags,
            featureLevels,
            out D3D11.ID3D11Device d3D11Device, out D3D.FeatureLevel featureLevel,
            out D3D11.ID3D11DeviceContext d3D11DeviceContext
        );

        result.CheckError();

        var width = 1000;
        var height = 1000;

        var dxgiDevice = d3D11Device.QueryInterface<DXGI.IDXGIDevice>();

        // 对接 D2D 需要创建工厂
        using D2D.ID2D1Factory1 d2DFactory = D2D.D2D1.D2D1CreateFactory<D2D.ID2D1Factory1>();
        using D2D.ID2D1Device d2D1Device = d2DFactory.CreateDevice(dxgiDevice);
        using D2D.ID2D1DeviceContext d2D1DeviceContext = d2D1Device.CreateDeviceContext(D2D.DeviceContextOptions.EnableMultithreadedOptimizations);
        DXGI.Format colorFormat = DXGI.Format.B8G8R8A8_UNorm;

        var texture2DDescription = new D3D11.Texture2DDescription
        {
            CPUAccessFlags = D3D11.CpuAccessFlags.Read,
            BindFlags = D3D11.BindFlags.RenderTarget | D3D11.BindFlags.ShaderResource,
            Usage = D3D11.ResourceUsage.Default,
            Width = width,
            Height = height,
            Format = colorFormat,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = DXGI.SampleDescription.Default,
            MiscFlags = D3D11.ResourceOptionFlags.Shared,
            //{
            //    Count = 1,
            //    Quality = 0
            //},
        };

        using D3D11.ID3D11Texture2D d3D11Texture2D = d3D11Device.CreateTexture2D(texture2DDescription);
        using var dxgiSurface = d3D11Texture2D.QueryInterface<DXGI.IDXGISurface>();

        using var d3D11RenderTargetView = d3D11Device.CreateRenderTargetView(d3D11Texture2D,
            new D3D11.RenderTargetViewDescription(d3D11Texture2D, D3D11.RenderTargetViewDimension.Texture2D,
                texture2DDescription.Format));

        using var d3D11ShaderResourceView = d3D11Device.CreateShaderResourceView(d3D11Texture2D,
            new D3D11.ShaderResourceViewDescription(d3D11Texture2D, D3D.ShaderResourceViewDimension.Texture2D,
                texture2DDescription.Format, mostDetailedMip: 0, mipLevels: 1));

        d3D11DeviceContext.OMSetRenderTargets(d3D11RenderTargetView);


        //var dxgiSurface = dxgiDevice.CreateSurface(new DXGI.SurfaceDescription()
        //{
        //    Format = colorFormat,
        //    Width = width,
        //    Height = height,
        //    SampleDescription = DXGI.SampleDescription.Default,
        //}, 1, DXGI.Usage.Shared | DXGI.Usage.RenderTargetOutput | DXGI.Usage.UnorderedAccess);
        var renderTargetProperties = new D2D.RenderTargetProperties(PixelFormat.Premultiplied);

        using D2D.ID2D1RenderTarget d2D1RenderTarget =
            d2DFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, renderTargetProperties);

        using var renderTarget = d2D1RenderTarget;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            // 开始绘制逻辑
            renderTarget.BeginDraw();

            // 随意创建颜色
            var color = new Color4((byte) Random.Shared.Next(255), (byte) Random.Shared.Next(255),
                (byte) Random.Shared.Next(255));
            renderTarget.Clear(color);
            color = new Color4(GetRandom(), GetRandom(), GetRandom());
            using D2D.ID2D1SolidColorBrush brush = renderTarget.CreateSolidColorBrush(color);

            for (int i = 0; i < 10000; i++)
            {
                var radiusX = 5;
                var radiusY = 5;
                renderTarget.DrawEllipse(new D2D.Ellipse(new Vector2(Random.Shared.Next(width - radiusX), Random.Shared.Next(height - radiusY)), radiusX, radiusY), brush, 2);
            }

            stopwatch.Stop();
            Console.WriteLine($"Draw: {stopwatch.ElapsedMilliseconds}");
            stopwatch.Restart();

            renderTarget.EndDraw();

            stopwatch.Stop();
            Console.WriteLine($"EndDraw: {stopwatch.ElapsedMilliseconds}");
            stopwatch.Restart();


            byte GetRandom() => (byte) Random.Shared.Next(255);

            d3D11DeviceContext.Flush();

            CopyResourceAndToFile(d3D11Device, d3D11DeviceContext, d3D11Texture2D);

            //ToFile(d3D11Device, d3D11DeviceContext, dxgiSurface);

            var dataRectangle = dxgiSurface.Map(DXGI.MapFlags.Read);
            var dataRectangleDataPointer = (byte*) dataRectangle.DataPointer;
            for (int i = 0; i < 10000; i++)
            {
                var t = *(dataRectangleDataPointer + i);
            }
        }


        //

    }

    [SupportedOSPlatform("Windows")]
    private static void CopyResourceAndToFile(D3D11.ID3D11Device d3D11Device,
        D3D11.ID3D11DeviceContext d3D11DeviceContext, D3D11.ID3D11Texture2D originalTexture)
    {
        var originalDesc = originalTexture.Description;
        var texture2DDescription = new D3D11.Texture2DDescription
        {
            CPUAccessFlags = D3D11.CpuAccessFlags.Read,
            BindFlags = D3D11.BindFlags.None,
            Usage = D3D11.ResourceUsage.Staging,
            Width = originalDesc.Width,
            Height = originalDesc.Height,
            Format = originalDesc.Format,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription =
            {
                Count = 1,
                Quality = 0
            },
        };

        var texture2D = d3D11Device.CreateTexture2D(texture2DDescription);
        d3D11DeviceContext.CopyResource(originalTexture, texture2D);
        d3D11DeviceContext.Flush();

        ToFile(d3D11Device, d3D11DeviceContext, texture2D);
    }

    [SupportedOSPlatform("Windows")]
    private static void ToFile(D3D11.ID3D11Device d3D11Device, D3D11.ID3D11DeviceContext d3D11DeviceContext, DXGI.IDXGISurface dxgiSurface)
    {
        var d3D11Texture2D = dxgiSurface.QueryInterface<D3D11.ID3D11Texture2D>();

        var originalTexture = d3D11Texture2D;

        var originalDesc = originalTexture.Description;
        var texture2DDescription = new D3D11.Texture2DDescription
        {
            CPUAccessFlags = D3D11.CpuAccessFlags.Read,
            BindFlags = D3D11.BindFlags.None,
            Usage = D3D11.ResourceUsage.Staging,
            Width = originalDesc.Width,
            Height = originalDesc.Height,
            Format = originalDesc.Format,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription =
            {
                Count = 1,
                Quality = 0
            },
        };

        var texture2D = d3D11Device.CreateTexture2D(texture2DDescription);
        d3D11DeviceContext.CopyResource(originalTexture, texture2D);

        ToFile(d3D11Device, d3D11DeviceContext, texture2D);
    }

    [SupportedOSPlatform("Windows")]
    private static unsafe void ToFile(D3D11.ID3D11Device d3D11Device, D3D11.ID3D11DeviceContext d3D11DeviceContext, D3D11.ID3D11Texture2D texture2D)
    {
        var bitmap = new System.Drawing.Bitmap(texture2D.Description.Width, texture2D.Description.Height);

        using var dxgiSurface = texture2D.QueryInterface<DXGI.IDXGISurface>();
        var dataRectangle = dxgiSurface.Map(DXGI.MapFlags.Read);
        //using var dataStream = dxgiSurface.MapDataStream(DXGI.MapFlags.Read);
        //var lines = (int) (dataStream.Length / dataRectangle.Pitch);
        var sizeOfColor = 4; // 一个颜色 = 4 * sizeof(byte)
        //var actualWidth = dxgiSurface.Description.Width * sizeOfColor;
        for (var y = 0; y < texture2D.Description.Height; y++)
        {
            var ptr = ((byte*) dataRectangle.DataPointer) + y * dataRectangle.Pitch;

            for (var x = 0; x < texture2D.Description.Width; x++)
            {
                var b = *(ptr + sizeOfColor * x);
                var g = *(ptr + sizeOfColor * x + 1);
                var r = *(ptr + sizeOfColor * x + 2);
                var a = *(ptr + sizeOfColor * x + 3);
                bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(a, r, g, b));
            }
        }

        dxgiSurface.Unmap();

        bitmap.Save("Image.png");
    }
}