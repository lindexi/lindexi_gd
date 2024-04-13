using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace HayceewiballfergeBawbalhega;

class Program
{
    [STAThread]
    static void Main()
    {
        // Enable SoftwareOnly RenderMode to avoid hardware effect.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        var application = new Application();
        application.Startup += (s, e) =>
        {
            new Window()
            {
                Width = 200,
                Height = 100,
            }.Show();
        };
        application.Run();
    }
}
