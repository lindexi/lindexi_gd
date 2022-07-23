using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace JagerekukibeHelcewhalay;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        var directory = FindSlnFolder();
        var file = System.IO.Path.Combine(directory.FullName, "JagerekukibeHelcewhalay", "Test.png");

        using IRandomAccessStream stream = await FileRandomAccessStream.OpenAsync(file, Windows.Storage.FileAccessMode.Read);

        // 解码图片
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(Windows.Graphics.Imaging.BitmapDecoder.PngDecoderId, stream);

        // 获取图像
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        // 准备识别
        Windows.Globalization.Language lang = new Windows.Globalization.Language("zh-CN");
        // 判断是否支持简体中文识别
        if (Windows.Media.Ocr.OcrEngine.IsLanguageSupported(lang))
        {
            var engine = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(lang);
            if (engine != null)
            {
                var result = await engine.RecognizeAsync(softwareBitmap);
                OcrText.Text = result.Text;
            }
        }
    }

    private DirectoryInfo FindSlnFolder()
    {
        var currentFolder = new DirectoryInfo(Environment.CurrentDirectory);

        while (currentFolder != null)
        {
            if (currentFolder.GetFiles("*.sln").Any())
            {
                return currentFolder;
            }

            currentFolder = currentFolder.Parent;
        }

        throw new DirectoryNotFoundException("找不到包含 sln 的文件夹");
    }
}
