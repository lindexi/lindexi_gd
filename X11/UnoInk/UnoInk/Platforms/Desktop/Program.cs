using Uno.UI.Runtime.Skia;
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
using Uno.WinUI.Runtime.Skia.X11;
=======
>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
=======
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b
=======
>>>>>>> bb3ca7ae7043b68590131278b336efd0445ad5c3

namespace UnoInk;
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        App.InitializeLogging();

        FeatureConfiguration.Rendering.UseOpenGLOnX11 = false;
        //var x11ApplicationHost = new X11ApplicationHost(() => new App());
        
        //x11ApplicationHost.Run();

        //Console.WriteLine($"X11ApplicationHost 退出");

=======
=======
        Console.WriteLine("启动");
        FeatureConfiguration.Rendering.UseOpenGLOnX11 = false;
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b
=======
>>>>>>> bb3ca7ae7043b68590131278b336efd0445ad5c3
#if (!useDependencyInjection && useLoggingFallback)
        App.InitializeLogging();

#endif
<<<<<<< HEAD
<<<<<<< HEAD
>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
=======
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b
=======
>>>>>>> bb3ca7ae7043b68590131278b336efd0445ad5c3
        var host = SkiaHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
=======

>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
=======

>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b
=======

>>>>>>> bb3ca7ae7043b68590131278b336efd0445ad5c3
        host.Run();
    }
}
