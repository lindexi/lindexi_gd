using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
        };
        application.Run();
    }
}
