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

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace produproperty
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        viewModel view;
        public MainPage()
        {            
            this.InitializeComponent();
            text.Paste += Text_Paste;            
        }

      

        private void Text_Paste(object sender, TextControlPasteEventArgs e)
        {
            view.clipboard(e);
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "打开";
            e.Handled = true;
        }

        private void text_SelectionChanged(object sender, RoutedEventArgs e)
        {
            view.select = text.SelectionStart;
        }

        private void selectchange(int select, int selecti)
        {
            text.SelectionStart = select;
            text.SelectionLength = selecti;
        }

        private bool _ctrl;

        private void text_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            string str;
            if (e.Key.Equals(Windows.System.VirtualKey.Control))
            {
                _ctrl = true;
            }
            else if (e.Key == Windows.System.VirtualKey.V && _ctrl)
            {

            }

            if (_ctrl)
            {
                if (e.Key == Windows.System.VirtualKey.Z)
                {

                }
                else if (e.Key == Windows.System.VirtualKey.K)
                {
                    str = "\r\n```\r\n\r\n\r\n\r\n```\r\n";
                    view.tianjia(str);
                    text.SelectionStart -= 6;
                }
                else if (e.Key == Windows.System.VirtualKey.S)
                {
                    view.storage();
                }
            }

            //e.Handled = true;
        }

        private void text_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(Windows.System.VirtualKey.Control))
            {
                _ctrl = false;
            }
        }

        private void option(object sender, RoutedEventArgs e)
        {
            view.storage();
            Frame frame = Window.Current.Content as Frame;
            frame.Navigate(typeof(option), view);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is viewModel)
            {
                view = e.Parameter as viewModel;
                DataContext = view;
            }
            else
            {
                view = new viewModel();
                view.selectchange = selectchange;               
                this.DataContext = view;
            }

        }
    }
}
