using Uno.UI.Runtime.Skia;
using Uno.WinUI.Runtime.Skia.X11;

namespace UnoInk;
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
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
