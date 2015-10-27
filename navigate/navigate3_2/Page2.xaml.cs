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
using System.Diagnostics;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkID=390556 上有介绍

namespace navigate3_2
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Page2 : Page
    {
        public Page2 ()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo ( NavigationEventArgs e )
        {
            Debug.WriteLine("Page 2：OnNavigatedTo方法被调用。");
        }

        protected override void OnNavigatingFrom ( NavigatingCancelEventArgs e )
        {
            Debug.WriteLine("Page 2：OnNavigatingFrom方法被调用。");
        }

        protected override void OnNavigatedFrom ( NavigationEventArgs e )
        {
            Debug.WriteLine("Page 2：OnNavigatedFrom方法被调用。");
        }
    }
}
