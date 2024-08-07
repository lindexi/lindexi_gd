using Uno.UI.Runtime.Skia;

namespace TextBoxX11;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();
        //FeatureConfiguration.Rendering.UseOpenGLOnX11 = false;
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
