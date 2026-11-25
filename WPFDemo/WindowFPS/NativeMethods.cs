using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace WindowFPS;

internal static class NativeMethods
{
    private const uint D3D11CreateDeviceBgraSupport = 0x20;
    private const uint D3D11SdkVersion = 7;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmSystemBackdropMainWindow = 2;

    internal static bool TryEnableLightMica(nint window)
    {
        var darkMode = 0;
        var backdrop = DwmSystemBackdropMainWindow;
        if (DwmSetWindowAttribute(window, DwmwaUseImmersiveDarkMode, in darkMode, sizeof(int)) < 0 ||
            DwmSetWindowAttribute(window, DwmwaSystemBackdropType, in backdrop, sizeof(int)) < 0)
        {
            return false;
        }

        var margins = new Margins(-1, -1, -1, -1);
        return DwmExtendFrameIntoClientArea(window, in margins) >= 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Margins(int Left, int Right, int Top, int Bottom);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmExtendFrameIntoClientArea(nint hwnd, in Margins margins);

    internal static IDirect3DDevice CreateCaptureDevice()
    {
        var dxgiDeviceId = new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
        Marshal.ThrowExceptionForHR(D3D11CreateDevice(0, 1, 0, D3D11CreateDeviceBgraSupport,
            0, 0, D3D11SdkVersion, out var nativeDevice, 0, 0));

        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(nativeDevice, ref dxgiDeviceId, out var dxgiDevice));
            try
            {
                Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out var inspectable));
                try
                {
                    return MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
                }
                finally
                {
                    Marshal.Release(inspectable);
                }
            }
            finally
            {
                Marshal.Release(dxgiDevice);
            }
        }
        finally
        {
            Marshal.Release(nativeDevice);
        }
    }

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, in int value, int size);

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int D3D11CreateDevice(nint adapter, int driverType, nint software, uint flags,
        nint featureLevels, uint featureLevelCount, uint sdkVersion, out nint device,
        nint selectedFeatureLevel, nint immediateContext);

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(nint dxgiDevice, out nint graphicsDevice);
}
