using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;
using Application = System.Windows.Application;

namespace SkiaSharpFormsDemos.WPF
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var application = new Application();
            Forms.Init();
            var formsApplicationPage = new FormsApplicationPage();
            formsApplicationPage.LoadApplication(new App());
            application.Run(formsApplicationPage);
        }
    }
}
