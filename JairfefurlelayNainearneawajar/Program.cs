using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using AvaloniaWukeaqalbairceLarheherebi;
using Application = System.Windows.Application;

namespace JairfefurlelayNainearneawajar;
internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var application = new Application();
        application.Startup += (sender, eventArgs) =>
        {
            var window = new Window();
            window.Show();

            var appBuilder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
            appBuilder.StartWithClassicDesktopLifetime(args);
        };
        application.Run();
    }
}
