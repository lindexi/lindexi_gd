using Uno.UI.Runtime.Skia;

namespace ManipulationDemo;
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
#if (!useDependencyInjection && useLoggingFallback)
        App.InitializeLogging();

#endif
        FeatureConfiguration.Rendering.UseOpenGLOnX11 = false;
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
