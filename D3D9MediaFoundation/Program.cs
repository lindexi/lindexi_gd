using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.MediaFoundation;
using Lsj.Util.Win32.NativeUI;
using System.Threading;
using static Lsj.Util.Win32.MediaFoundation.Constants;
using static Lsj.Util.Win32.MediaFoundation.Enums.MediaFoundationStartupFlags;

namespace D3D9MediaFoundation
{
    internal class Program
    {
        public static string VideoPath = @"D:\Samples\lg-uhd-arctic.mkv";

        static void Main(string[] args)
        {
            User32.SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            var uiThreadManager = new UIThreadManager();
            _ = uiThreadManager.InvokeAsync(() =>
            {
                Mfplat.MFStartup(MF_VERSION, MFSTARTUP_FULL);
                var win = new PlayerWindow();
                win.Show();
            });
            Thread.Sleep(-1);
        }
    }
}
