using System;
using Xwt;

namespace LarwearceceyokuDokealuwo
{
<<<<<<< HEAD
    
=======
>>>>>>> 11125ca50dc91e50cf581c36476f03b853bc7ef8
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.Initialize(ToolkitType.Gtk);

            var mainWindow = new Window()
            {
                Title = "Xwt Demo Application",
                Width = 500,
                Height = 400
            };
            mainWindow.Show();
            Application.Run();
        }
    }
}
