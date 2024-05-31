using Uno.UI.Runtime.Skia;
<<<<<<< HEAD
using Uno.WinUI.Runtime.Skia.X11;
=======
>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0

namespace UnoInk;
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
<<<<<<< HEAD
        App.InitializeLogging();

        FeatureConfiguration.Rendering.UseOpenGLOnX11 = false;
        //var x11ApplicationHost = new X11ApplicationHost(() => new App());
        
        //x11ApplicationHost.Run();

        //Console.WriteLine($"X11ApplicationHost 退出");

=======
#if (!useDependencyInjection && useLoggingFallback)
        App.InitializeLogging();

#endif
>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
        var host = SkiaHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();
<<<<<<< HEAD
=======

>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
        host.Run();
    }
}
