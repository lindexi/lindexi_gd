using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using MS.Internal;


namespace dotnetCampus.WPF
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var application = new Application();
            var window = new Window()
            {
                Title = "林德熙是逗比"

            };
            window.Loaded += (sender, eventArgs) =>
            {
                // 这里的 GetAppWindow 是 internal 的方法，但是在这个程序集可以访问
                var navigationWindow = application.GetAppWindow();
            };
            application.Run(window);
        }
    }
}
