using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LabujeeneferejurFeqawjewur
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (Environment.GetCommandLineArgs().Contains("重启信息"))
            {
                MessageBox.Show("重启");
            }

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hr = ApplicationCrashRecovery.RegisterApplicationRestart("重启信息", CrashRecoveryApi.RestartFlags.NONE);

            ApplicationCrashRecovery.RegisterApplicationRecoveryCallback();
            ApplicationCrashRecovery.OnApplicationCrashed += ApplicationCrashRecovery_OnApplicationCrashed;

            await Task.Delay(TimeSpan.FromSeconds(3));

            Thread.Sleep(1000000000);
        }

        private void ApplicationCrashRecovery_OnApplicationCrashed()
        {
            // [RegisterApplicationRecoveryCallback function (winbase.h) - Win32 apps | Microsoft Docs](https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-registerapplicationrecoverycallback )
            //  By default, the interval is 5 seconds (RECOVERY_DEFAULT_PING_INTERVAL). The maximum interval is 5 minutes. 
            MessageBox.Show("应用程序炸掉");
        }
    }
}
