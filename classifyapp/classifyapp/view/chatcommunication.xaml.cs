using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using classifyapp.ViewModel;


namespace classifyapp.view
{
    public sealed partial class chatcommunication:Page
    {
        public chatcommunication()
        {
            InitializeComponent();
        }

        private async void QQ_Click(object sender , RoutedEventArgs e)
        {
            //https://www.microsoft.com/zh-cn/store/apps/qq/9wzdncrfj1ps

            string uri = "ms-windows-store://pdp/?ProductId=9wzdncrfj1ps";
            await Windows.System.Launcher.LaunchUriAsync(new Uri(uri));
        }
    }
}
