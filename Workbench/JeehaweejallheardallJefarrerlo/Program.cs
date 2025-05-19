using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JeehaweejallheardallJefarrerlo;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var application = new Application();
        application.Startup += (_, _) =>
        {
            var window = new Window();
            window.Show();
        };
        application.Run();
    }
}
