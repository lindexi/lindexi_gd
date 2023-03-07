using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BeredallqerjearJekarbearwai
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(Window_OnLoaded));

            EventManager.RegisterClassHandler(typeof(Window), Window.SizeChangedEvent, new RoutedEventHandler(Window_SizeChanged));
        }

        private void Window_OnLoaded(object sender, RoutedEventArgs e)
        {
            // 如果窗口没有 XAML 或者没有监听 Loaded 事件，将不会被触发
        }

        private void Window_SizeChanged(object sender, RoutedEventArgs e)
        {
            // 所有窗口都会触发
        }
    }
}
