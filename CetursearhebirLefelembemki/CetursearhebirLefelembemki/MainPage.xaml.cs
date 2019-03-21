using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.VisualBasic;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace CetursearhebirLefelembemki
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

        /// <summary>
        /// 将原来的图片转换图片质量和压缩质量
        /// </summary>
        /// <param name="sourceFile">原来的图片</param>
        /// <param name="outputFile">输出的文件</param>
        /// <param name="imageQuality">图片质量，取值范围是 0 到 1 其中 1 的质量最好，这个值设置只对 jpg 图片有效</param>
        /// <returns></returns>
        private async Task<StorageFile> ConvertImageToJpegAsync(StorageFile sourceFile, StorageFile outputFile,
            double imageQuality)
        {
            var sourceFileProperties = await sourceFile.GetBasicPropertiesAsync();
            var fileSize = sourceFileProperties.Size;
            var imageStream = await sourceFile.OpenReadAsync();
            using (imageStream)
            {
                var decoder = await BitmapDecoder.CreateAsync(imageStream);
                var pixelData = await decoder.GetPixelDataAsync();
                var detachedPixelData = pixelData.DetachPixelData();
                var imageWriteAbleStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite);
                using (imageWriteAbleStream)
                {
                    var propertySet = new BitmapPropertySet();
                    // 图片质量，值范围是 0到1 其中 1 的质量最好
                    var qualityValue = new BitmapTypedValue(imageQuality,
                        Windows.Foundation.PropertyType.Single);
                    propertySet.Add("ImageQuality", qualityValue);
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, imageWriteAbleStream,
                        propertySet);
                    //key thing here is to use decoder.OrientedPixelWidth and decoder.OrientedPixelHeight otherwise you will get garbled image on devices on some photos with orientation in metadata
                    encoder.SetPixelData(decoder.BitmapPixelFormat, decoder.BitmapAlphaMode, decoder.OrientedPixelWidth,
                        decoder.OrientedPixelHeight, decoder.DpiX, decoder.DpiY, detachedPixelData);
                    await encoder.FlushAsync();
                    await imageWriteAbleStream.FlushAsync();
                    var jpegImageSize = imageWriteAbleStream.Size;
                    // 欢迎访问我博客 https://blog.lindexi.com/ 里面有大量 UWP WPF 博客
                    Debug.WriteLine($"压缩之后比压缩前的文件小{fileSize - jpegImageSize}");
                }
            }

            return outputFile;
        }

        private async void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var pick = new FileOpenPicker();
            pick.FileTypeFilter.Add(".jpg");
            var file = await pick.PickSingleFileAsync();

            if (file != null)
            {
               await ConvertImageToJpegAsync(file, await ApplicationData.Current.TemporaryFolder.CreateFileAsync("lindexi"),
                    0);
            }
        }
    }
}