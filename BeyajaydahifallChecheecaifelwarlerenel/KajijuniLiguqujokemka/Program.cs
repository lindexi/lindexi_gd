using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows;

namespace KajijuniLiguqujokemka
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application app = new Application();
            app.Startup += App_Startup;

            app.Run();
        }
        
        [HandleProcessCorruptedStateExceptions]
        private static void App_Startup(object sender, StartupEventArgs args)
        {
            Window mainWindow = new Window();
            mainWindow.Show();

            try
            {
                Console.WriteLine(HeederajiYeafalludall());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [DllImport("BeyajaydahifallChecheecaifelwarlerenel.dll")]
        static extern Int16 HeederajiYeafalludall();
    }
}
