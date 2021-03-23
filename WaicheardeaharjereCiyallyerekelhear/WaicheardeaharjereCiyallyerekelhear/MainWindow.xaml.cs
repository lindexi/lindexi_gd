using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WaicheardeaharjereCiyallyerekelhear
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < 10; i++)
            {
                FileResourceList.Add(new FileResource()
                {
                    FileName = "逗比.txt",
                    DownloadTime = DateTime.Now,
                    DownloadUrl = "http://blog.lindexi.com",
                    FileSize = i,
                    FilePath = "C:\\lindexi"
                });
            }

            DataContext = this;

            for (int i = 0; i < 10; i++)
            {
                Debug.WriteLine(FileSizeFormatter.FormatSize((long)Math.Pow(10, i)));
            }
        }

        public ObservableCollection<FileResource> FileResourceList { get; } = new ObservableCollection<FileResource>();
    }

    static class FileSizeFormatter
    {
        public static string FormatSize(long bytes, string formatString = "{0:0.00}")
        {
            int counter = 0;
            double number = bytes;

            // 最大单位就是 PB 了，而 PB 是第 5 级，从 0 开始数
            // "Bytes", "KB", "MB", "GB", "TB", "PB"
            const int maxCount = 5;
            // 后续也有 "EB", "ZB", "YB" 等，但是 long 也没表示这么有趣的单位

            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;

                if (counter >= maxCount)
                {
                    break;
                }
            }

            var suffix = counter switch
            {
                0 => "B",
                1 => "KB",
                2 => "MB",
                3 => "GB",
                4 => "TB",
                5 => "PB",
                // 通过 maxCount 限制了最大的值就是 5 了
                _ => throw new ArgumentException("骚年，你是不是忘了更新 maxCount 等级了")
            };

            return $"{string.Format(formatString, number)}{suffix}";
        }
    }

    public class FileResource
    {
        public string FileName { get; set; }

        public DateTime DownloadTime { get; set; }

        public string FilePath { get; set; }

        public string DownloadUrl { get; set; }

        public long FileSize { get; set; }
    }
}
