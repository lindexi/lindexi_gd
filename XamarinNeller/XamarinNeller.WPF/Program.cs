using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;
using Application = System.Windows.Application;

namespace XamarinNeller.WPF
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var application = new Application();
            Forms.Init();
            var formsApplicationPage = new FormsApplicationPage();
            formsApplicationPage.LoadApplication(new XamarinNeller.App());

            Task.Run(() =>
            {
                Task.Delay(1000).Wait();

                application.Dispatcher.InvokeAsync(() =>
                {
                    var window = new Window()
                    {
                        Height = 600,
                        Width = 600
                    };

                    var viewPage = new ViewPage()
                    {
                        HeightRequest = 500,
                        WidthRequest = 500
                    };

                    var pageRenderer = new PageRenderer();

                    var mainPage = new MainPage();
                    pageRenderer.SetElement(viewPage);
                    
                    //var formsContentLoader = new FormsContentLoader();
                    //var content = formsContentLoader.LoadContentAsync(window,null, mainPage,new CancellationToken()).Result;

                    var frameworkElement = pageRenderer.GetNativeElement();
                    window.Content = frameworkElement;
                    window.Show();
                });
            });

            application.Run();
        }
    }
}
