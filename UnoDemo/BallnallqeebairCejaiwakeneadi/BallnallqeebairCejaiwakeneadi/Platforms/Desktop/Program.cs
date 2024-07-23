using Uno.UI.Runtime.Skia;

namespace BallnallqeebairCejaiwakeneadi;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        if (args.Length > 0)
        {
            FeatureConfiguration.Rendering.UseOpenGLOnX11 = false;
        }

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
