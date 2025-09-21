using Lsj.Util.Win32.MediaFoundation;
using Lsj.Util.Win32.NativeUI;
using static Lsj.Util.Win32.MediaFoundation.Constants;
using static Lsj.Util.Win32.MediaFoundation.Enums.MediaFoundationStartupFlags;

namespace D3D11MediaFoundation
{
    internal class Program
    {
        public static string VideoPath = @"D:\Samples\lg-uhd-arctic.mkv";
        //public static string VideoPath = @"D:\Samples\Big_Buck_Bunny_1080_10s_30MB.mp4";

        static void Main(string[] args)
        {
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
