using System.Diagnostics;

using Uno.UI.Runtime.Skia;
using Uno.WinUI.Runtime.Skia.X11;

namespace UnoInk;
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        while (!Debugger.IsAttached)
        {
            Task.Delay(100).Wait();
        }
        Debugger.Break();

        App.InitializeLogging();

        FeatureConfiguration.Rendering.UseOpenGLOnX11 = false;
        //var x11ApplicationHost = new X11ApplicationHost(() => new App());
        
        //x11ApplicationHost.Run();

        //Console.WriteLine($"X11ApplicationHost 退出");

        var host = SkiaHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();
        host.Run();
        // 退出
    }
}
