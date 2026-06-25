using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FawduhachalereNawfurgaywaifar;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files is null)
            return;

        var now = DateTime.Now;
        var updateCreation = CreationTimeCheckBox.IsChecked == true;
        var updateLastWrite = LastWriteTimeCheckBox.IsChecked == true;
        var randomizeContent = RandomizeContentCheckBox.IsChecked == true;

        if (!updateCreation && !updateLastWrite && !randomizeContent)
        {
            MessageBox.Show(this, "请至少勾选一个选项。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ResultList.Items.Clear();

        foreach (var file in files)
        {
            string result;
            try
            {
                // 在修改前保存原始最后修改时间，用于内容随机化后还原
                var originalLastWriteTime = File.GetLastWriteTime(file);

                if (randomizeContent)
                    RandomizeFileContent(file);

                if (updateCreation)
                    File.SetCreationTime(file, now);

                if (updateLastWrite)
                    File.SetLastWriteTime(file, now);
                else if (randomizeContent)
                    File.SetLastWriteTime(file, originalLastWriteTime);

                result = "成功";
            }
            catch (Exception ex)
            {
                result = $"失败: {ex.Message}";
            }

            ResultList.Items.Add(new FileResult(System.IO.Path.GetFileName(file), result));
        }
    }

    private static void RandomizeFileContent(string filePath)
    {
        var info = new FileInfo(filePath);
        if (info.Length == 0)
            return;

        // 选择要随机修改的字节范围：文件中间区域的一部分
        var length = info.Length;
        var startOffset = length / 4;
        var regionLength = Math.Max(1, length / 8);

        var random = new Random();
        var buffer = new byte[regionLength];
        random.NextBytes(buffer);

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None);
        stream.Position = startOffset;
        stream.Write(buffer, 0, buffer.Length);
    }

    private sealed record FileResult(string FileName, string Result);
}