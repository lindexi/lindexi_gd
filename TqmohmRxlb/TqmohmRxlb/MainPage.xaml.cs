using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace TqmohmRxlb
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Grid1_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine("Grid1 key down");
        }

        private void Grid2_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine("Grid2 key down");
        }

        private async void Grid2_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { Foo.Focus(FocusState.Keyboard); });
        }

        private void Grid2_OnGotFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Grid2 focus");
        }

        private void Foo_OnLostFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"lost focus {FocusManager.GetFocusedElement()}");
        }
    }

    class Foo : Control
    {
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            Debug.WriteLine("Foo key down");
        }
    }
}
