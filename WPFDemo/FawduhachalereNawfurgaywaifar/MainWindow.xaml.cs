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

        if (!updateCreation && !updateLastWrite)
        {
            MessageBox.Show(this, "请至少勾选一个时间选项。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ResultList.Items.Clear();

        foreach (var file in files)
        {
            string result;
            try
            {
                if (updateCreation)
                    File.SetCreationTime(file, now);
                if (updateLastWrite)
                    File.SetLastWriteTime(file, now);
                result = "成功";
            }
            catch (Exception ex)
            {
                result = $"失败: {ex.Message}";
            }

            ResultList.Items.Add(new FileResult(System.IO.Path.GetFileName(file), result));
        }
    }

    private sealed record FileResult(string FileName, string Result);
}