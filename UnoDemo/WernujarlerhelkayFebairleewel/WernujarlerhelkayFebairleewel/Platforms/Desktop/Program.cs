using Uno.UI.Runtime.Skia;

namespace WernujarlerhelkayFebairleewel;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        FeatureConfiguration.Rendering.UseOpenGLOnX11 = true;

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
