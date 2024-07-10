using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace HekodicairciFibaykucoyo
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            for (int i = 0; i < 50; i++)
            {
                var n = i;
                var thread = new Thread(() =>
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Title = n.ToString();
                    var windowInteropHelper = new WindowInteropHelper(mainWindow);
                    windowInteropHelper.EnsureHandle();
                    mainWindow.Show();
                    Dispatcher.Run();
                });
                thread.Name = i.ToString();
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }

            base.OnStartup(e);
        }
    }
}
