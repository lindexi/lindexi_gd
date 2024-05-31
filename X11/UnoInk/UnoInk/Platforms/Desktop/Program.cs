using Uno.UI.Runtime.Skia;

namespace UnoInk;
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("启动");
        FeatureConfiguration.Rendering.UseOpenGLOnX11 = false;
#if (!useDependencyInjection && useLoggingFallback)
        App.InitializeLogging();

#endif
        var host = SkiaHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();

        host.Run();
    }
}
