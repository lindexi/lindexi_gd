using System.Numerics;
using System.Runtime.CompilerServices;

using SharpGen.Runtime;

using Vortice;
using Vortice.D3DCompiler;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.DXGI;
using Vortice.DXGI.Debug;
using Vortice.Mathematics;

using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;
using AlphaMode = Vortice.DXGI.AlphaMode;
using FeatureLevel = Vortice.Direct3D.FeatureLevel;
using D2D = Vortice.Direct2D1;

namespace QacejoceaHayawhocall;

class GraphicsDevice : IGraphicsDevice
{

    public GraphicsDevice(Application application)
    {
        Application = application;

        Factory = CreateDXGIFactory1<IDXGIFactory2>();

        var hardwareAdapter = GetHardwareAdapter().ToList().FirstOrDefault();
        if (hardwareAdapter == null)
        {
            throw new InvalidOperationException("Cannot detect D3D11 adapter");
        }

        using (IDXGIAdapter1 adapter = hardwareAdapter)
        {
            DeviceCreationFlags creationFlags = DeviceCreationFlags.BgraSupport;

            if (D3D11CreateDevice(
                    adapter,
                    DriverType.Unknown,
                    creationFlags,
                    s_featureLevels,
                    out ID3D11Device tempDevice, out _featureLevel, out ID3D11DeviceContext tempContext).Success)
            {

            }
            else
            {
                // 失败了
                // If the initialization fails, fall back to the WARP device.
                // For more information on WARP, see:
                // http://go.microsoft.com/fwlink/?LinkId=286690
                D3D11CreateDevice(
                    IntPtr.Zero,
                    DriverType.Warp,
                    creationFlags,
                    s_featureLevels,
                    out tempDevice, out _featureLevel, out tempContext).CheckError();
            }

            Device = tempDevice.QueryInterface<ID3D11Device1>();
            DeviceContext = tempContext.QueryInterface<ID3D11DeviceContext1>();
            tempContext.Dispose();
            tempDevice.Dispose();
        }

        var window = application.MainWindow;
        var hwnd = window.Handle;
        Format colorFormat = Format.B8G8R8A8_UNorm;

        const int FrameCount = 2;

        SwapChainDescription1 swapChainDescription = new()
        {
            Width = window.ClientSize.Width,
            Height = window.ClientSize.Height,
            Format = colorFormat,
            BufferCount = FrameCount,
            BufferUsage = Usage.RenderTargetOutput,
            SampleDescription = SampleDescription.Default,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore
        };

        SwapChainFullscreenDescription fullscreenDescription = new SwapChainFullscreenDescription
        {
            Windowed = true
        };

        SwapChain = Factory.CreateSwapChainForHwnd(Device, hwnd, swapChainDescription, fullscreenDescription);

        Factory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAltEnter);

        BackBufferTexture = SwapChain.GetBuffer<ID3D11Texture2D>(0);
        RenderTargetView = Device.CreateRenderTargetView(BackBufferTexture);

        var dxgiSurface = BackBufferTexture.QueryInterface<IDXGISurface>();

        var d2DFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();

        var renderTargetProperties = new D2D.RenderTargetProperties(PixelFormat.Premultiplied);
        D2D1RenderTarget = d2DFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, renderTargetProperties);

        //for (int i = 0; i < 10; i++)
        //{
        Task.Run(() =>
        {
            while (true)
            {
                D2D1RenderTarget.BeginDraw();

                D2D1RenderTarget.Clear(new Color4((byte) Random.Shared.Next(255), (byte) Random.Shared.Next(255), (byte) Random.Shared.Next(255)));

                D2D1RenderTarget.EndDraw();

                SwapChain.Present(1, PresentFlags.None);
                Device.ImmediateContext1.Flush();
            }
        });
        //}
    }

    public ID2D1RenderTarget D2D1RenderTarget { get; }

    public ID3D11RenderTargetView RenderTargetView { get; }

    public ID3D11Texture2D BackBufferTexture { get; }

    public ID3D11Device1 Device { get; }
    public ID3D11DeviceContext1 DeviceContext { get; }

    public IDXGISwapChain1 SwapChain { get; }

    private readonly FeatureLevel _featureLevel;

    private static readonly FeatureLevel[] s_featureLevels = new[]
    {
        FeatureLevel.Level_11_1,
        FeatureLevel.Level_11_0,
        FeatureLevel.Level_10_1,
        FeatureLevel.Level_10_0,
        FeatureLevel.Level_9_3,
        FeatureLevel.Level_9_2,
        FeatureLevel.Level_9_1,
    };

    private IEnumerable<IDXGIAdapter1> GetHardwareAdapter()
    {
        IDXGIFactory6? factory6 = Factory.QueryInterfaceOrNull<IDXGIFactory6>();
        if (factory6 != null)
        {
            for (int adapterIndex = 0;
                 factory6.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out IDXGIAdapter1? adapter).Success;
                 adapterIndex++)
            {
                if (adapter == null)
                {
                    continue;
                }

                AdapterDescription1 desc = adapter.Description1;

                if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
                {
                    // Don't select the Basic Render Driver adapter.
                    adapter.Dispose();
                    continue;
                }

                //factory6.Dispose();

                Console.WriteLine($"枚举到 {adapter.Description1.Description} 显卡");
                yield return adapter;
            }

            factory6.Dispose();
        }

        for (int adapterIndex = 0;
             Factory.EnumAdapters1(adapterIndex, out IDXGIAdapter1? adapter).Success;
             adapterIndex++)
        {
            AdapterDescription1 desc = adapter.Description1;

            if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
            {
                // Don't select the Basic Render Driver adapter.
                adapter.Dispose();

                continue;
            }

            Console.WriteLine($"枚举到 {adapter.Description1.Description} 显卡");
            yield return adapter;
        }
    }

    public IDXGIFactory2 Factory { get; }

    public Application Application { get; }

    public void DrawFrame()
    {
        //D2D1RenderTarget.BeginDraw();

        //D2D1RenderTarget.Clear(new Color4((byte) Random.Shared.Next(255), (byte) Random.Shared.Next(255), (byte) Random.Shared.Next(255)));

        //D2D1RenderTarget.EndDraw();

        //SwapChain.Present(1, PresentFlags.None);
        //Device.ImmediateContext1.Flush();
    }

    public void Dispose()
    {

    }
}