using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace WadeaherkeaLihanececeeneri
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var ocrEngine = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("zh-CN"));

            FileOpenPicker picker = new FileOpenPicker()
            {
                FileTypeFilter = { ".jpg", ".png" },
                CommitButtonText = "识别图片",
            };
            var file = await picker.PickSingleFileAsync();

            var softwareBitmap = await StorageFileToSoftwareBitmapAsync(file);

            var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

            OcrTextBox.Text = ocrResult.Text;
        }

        private static async Task<SoftwareBitmap> StorageFileToSoftwareBitmapAsync(StorageFile inputFile)
        {
            using (IRandomAccessStream stream = await inputFile.OpenAsync(FileAccessMode.Read))
            {
                // Create the decoder from the stream
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // Get the SoftwareBitmap representation of the file
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                return softwareBitmap;
            }
        }
    }
}
