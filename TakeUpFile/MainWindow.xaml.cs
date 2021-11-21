using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace TakeUpFile
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private FileStream? CurrentFileStream { set; get; }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            Release();

            var fileList = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (fileList is not null)
            {
                var file = fileList.FirstOrDefault();
                if (file != null)
                {
                    if (File.Exists(file))
                    {
                        try
                        {
                            CurrentFileStream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        }
                        catch (IOException ioException)
                        {
                            if (ioException.HResult == unchecked((int)0x80070020))
                            {
                                var processList = FileUtil.WhoIsLocking(file);
                                if (processList != null)
                                {
                                    var message = $"文件{file}被程序占用中：";
                                    foreach (var item in processList)
                                    {
                                        message += $"{item.ProcessName}({item.Id});";
                                    }

                                    TracerTextBlock.Text = message;
                                    return;
                                }
                            }
                        }

                        TracerTextBlock.Text = $"锁定 {file}";
                    }
                }
            }
        }

        private void Release()
        {
            try
            {
                CurrentFileStream?.Dispose();
            }
            catch
            {
                // 忽略
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Release();
        }
    }
}
