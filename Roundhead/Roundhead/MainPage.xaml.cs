using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
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

namespace Roundhead
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

        private void Grid_DragOver(object sender , DragEventArgs e)
        {
            //需要using Windows.ApplicationModel.DataTransfer;
            e.AcceptedOperation = DataPackageOperation.Copy;

            // 设置拖放时显示的文字。
            //e.DragUIOverride.Caption = "拖放打开";

            // 是否显示拖放时的文字。默认为 true。
            //e.DragUIOverride.IsCaptionVisible = false;

            // 是否显示文件预览内容，一般为文件图标。默认为 true。
            // e.DragUIOverride.IsContentVisible = false;

            // Caption 前面的图标是否显示。默认为 true。
            //e.DragUIOverride.IsGlyphVisible = false;

            //需要using Windows.UI.Xaml.Media.Imaging;
            //设置拖动图形，覆盖文件预览
            //e.DragUIOverride.SetContentFromBitmapImage(new BitmapImage(new Uri("ms-appx:///Assets/1.jpg")));

            e.Handled = true;
        }

        private async void Grid_Drop(object sender , DragEventArgs e)
        {
            var defer = e.GetDeferral();

            try
            {
                DataPackageView dataView = e.DataView;
                // 拖放类型为文件存储。
                if (dataView.Contains(StandardDataFormats.StorageItems))
                {
                    var files = await dataView.GetStorageItemsAsync();
                    StorageFile file = files.OfType<StorageFile>().First();
                    if (file.FileType == ".png" || file.FileType == ".jpg")
                    {
                        // 拖放的是图片文件。
                        BitmapImage bitmap = new BitmapImage();
                        await bitmap.SetSourceAsync(await file.OpenAsync(FileAccessMode.Read));
                        ximg.ImageSource = bitmap;
                    }                    
                }
            }
            finally
            {
                defer.Complete();
            }
        }
    }
}
