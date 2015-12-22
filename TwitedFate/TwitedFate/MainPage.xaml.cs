using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace TwitedFate
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ViewModel.viewModel view;
        public MainPage()
        {
            view = new ViewModel.viewModel();
            this.InitializeComponent();
            c();          
        }

        private async void c()
        {
           
            StorageFile f = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/桌面.jpg"));
            StorageFolder folder =await f.GetParentAsync();

            var file = await folder.GetFilesAsync();
            Image image;

            foreach (var temp in file)
            {
                image = new Image();
                image.Source = new BitmapImage(new Uri(temp.Path));
                image.Tag = temp;               
                xf.Items.Add(image);
            }

            view.xf = xf;
        }
    }
}
