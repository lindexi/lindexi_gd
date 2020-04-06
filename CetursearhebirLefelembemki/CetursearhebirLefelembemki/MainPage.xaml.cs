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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CetursearhebirLefelembemki
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            // 选择文件
            var imgFile = await picker.PickSingleFileAsync();

            if(imgFile != null)
            {
                using (var inStream = await imgFile.OpenReadAsync())
                {
                    // 解码图片
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(Windows.Graphics.Imaging.BitmapDecoder.PngDecoderId, inStream);

                    // 获取图像
                    var swbmp = await decoder.GetSoftwareBitmapAsync();
                    // 准备识别
                    Windows.Globalization.Language lang = new Windows.Globalization.Language("zh-CN");
                    // 判断是否支持简体中文识别
                    if (Windows.Media.Ocr.OcrEngine.IsLanguageSupported(lang))
                    {
                        var engine = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(lang);
                        if (engine != null)
                        {
                            var result = await engine.RecognizeAsync(swbmp);
                            if (result != null)
                            {
                                var dialog = new Windows.UI.Popups.MessageDialog($"识别内容 {result.Text}");
                                await dialog.ShowAsync();
                            }
                        }
                    }
                    else
                    {
                        var dialog = new Windows.UI.Popups.MessageDialog("不支持简体中文的识别。");
                        await dialog.ShowAsync();
                    }
                }
            }
        }
    }
}
