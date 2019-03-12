using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace popupMenu菜单
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

        private async void Button_Click(object sender , RoutedEventArgs e)
        {
            PopupMenu menu = new PopupMenu();

            UICommandInvokedHandler invokedHandler = (cmd) =>
            {
                SolidColorBrush brush = cmd.Id as SolidColorBrush;
                tb.Foreground = brush;
            };
            UICommand cmdred = new UICommand("红" , invokedHandler , new SolidColorBrush(Colors.Red));
            UICommand cmdYellow = new UICommand("黄" , invokedHandler , new SolidColorBrush(Colors.Yellow));
            menu.Commands.Add(cmdred);
            menu.Commands.Add(cmdYellow);
            GeneralTransform gt = tb.TransformToVisual(null);
            Point popupPoint = gt.TransformPoint(new Point(0d , tb.ActualHeight));   
                     
            tb.Text = popupPoint.X.ToString() + " " + popupPoint.Y.ToString();
            await menu.ShowAsync(popupPoint);
        }
    }
}
