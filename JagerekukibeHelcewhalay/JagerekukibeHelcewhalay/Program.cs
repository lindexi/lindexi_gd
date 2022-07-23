using System;
using System.Windows;

namespace JagerekukibeHelcewhalay;

public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application application = new Application();
        application.Startup += (s, e) =>
        {
            MainWindow window = new MainWindow();
            window.Show();
        };
        application.Run();
    }
}

