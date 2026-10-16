using Lsj.Util.Win32;
using Lsj.Util.Win32.MediaFoundation;
using Lsj.Util.Win32.NativeUI;
using Lsj.Util.Win32.BaseTypes;
using static Lsj.Util.Win32.MediaFoundation.Constants;
using static Lsj.Util.Win32.MediaFoundation.Enums.MediaFoundationStartupFlags;
using Lsj.Util.Win32.NativeUI.DirectX;

namespace D3D11CaptureEngine
{
    internal class Program
    {
        static void Main(string[] args)
        {
            User32.SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            var uiThreadManager = new UIThreadManager();
            _ = uiThreadManager.InvokeAsync(() =>
            {
                Mfplat.MFStartup(MF_VERSION, MFSTARTUP_FULL);
                var win = new PlayerWindow(uiThreadManager);
                win.Show();
            });
            Thread.Sleep(-1);
        }
    }
}
