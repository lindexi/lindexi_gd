using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.UI.Xaml;
using Window = System.Windows.Window;

namespace RairyeliluhuchiwallKuwhurdoji
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            XamlCheckProcessRequirements();

            var thread = new Thread(() =>
            {
                global::WinRT.ComWrappersSupport.InitializeComWrappers();
                global::Microsoft.UI.Xaml.Application.Start((p) =>
                {
                    var context =
                        new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI
                            .Dispatching.DispatcherQueue.GetForCurrentThread());
                    global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                    _ = new WinUIApp();
                    //Microsoft.UI.Xaml.
                });
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        [global::System.Runtime.InteropServices.DllImport("Microsoft.ui.xaml.dll")]
        private static extern void XamlCheckProcessRequirements();
    }

    partial class WinUIApp : global::Microsoft.UI.Xaml.Application
    {
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var window = new Microsoft.UI.Xaml.Window()
            {
                Title = "WinUI",
                Content = new Microsoft.UI.Xaml.Controls.TextBlock()
                {
                    Text = "WinUI",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                }
            };
            window.Activate();
            base.OnLaunched(args);
        }
    }
}