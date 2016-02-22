using rss.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace rss
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        viewModel view;
        public MainPage()
        {
            view = new ViewModel.viewModel();
            this.InitializeComponent();
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += BackRequested;
        }

        private void BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            view.rssVisibility=Visibility.Collapsed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;

        }

        private void select(object sender, SelectionChangedEventArgs e)
        {
            //Frame frame = Window.Current.Content as Frame;
            //frame?.Navigate(typeof(rss_page), (ViewModel.rssstr)(sender as ListView)?.SelectedItem);
            rss_frame.Navigate(typeof(rss_page), (ViewModel.rssstr)(sender as ListView)?.SelectedItem);
            view.rssVisibility = Visibility.Visible;
        }


        private void sizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid=sender as Grid;
            
        }
    }
}
