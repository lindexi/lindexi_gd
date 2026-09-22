using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace MoveAppFolder;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BrowseSourceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            SourceFolderTextBox.Text = dialog.FolderName;
        }
    }

    private void BrowseTargetButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            TargetFolderTextBox.Text = dialog.FolderName;
        }
    }

    private async void StartMoveButton_Click(object sender, RoutedEventArgs e)
    {
        string sourcePath = SourceFolderTextBox.Text.Trim();
        string targetPath = TargetFolderTextBox.Text.Trim();

        if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
        {
            MessageBox.Show("请选择有效的源文件夹", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (string.IsNullOrEmpty(targetPath))
        {
            MessageBox.Show("请选择目标文件夹", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (sourcePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("源文件夹和目标文件夹不能相同", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        StartMoveButton.IsEnabled = false;
        LogTextBox.Clear();

        try
        {
            await Task.Run(() => MoveDirectoryRecursive(sourcePath, targetPath));
            Log("移动完成！");
            MessageBox.Show("文件夹移动完成", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log($"移动失败: {ex.Message}");
            MessageBox.Show($"移动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            StartMoveButton.IsEnabled = true;
        }
    }

    private void MoveDirectoryRecursive(string sourceDir, string targetDir)
    {
        // 创建目标目录如果不存在
        if (!Directory.Exists(targetDir))
        {
            Log($"创建目录: {targetDir}");
            Directory.CreateDirectory(targetDir);
        }

        // 移动所有文件
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string targetFile = Path.Combine(targetDir, fileName);

            try
            {
                Log($"移动文件: {file} -> {targetFile}");
                File.Move(file, targetFile, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new IOException($"移动文件 {file} 到 {targetFile} 失败: {ex.Message}", ex);
            }
        }

        // 递归移动子目录
        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string subDirName = Path.GetFileName(subDir);
            string targetSubDir = Path.Combine(targetDir, subDirName);
            MoveDirectoryRecursive(subDir, targetSubDir);
        }

        // 移动完成后删除源目录
        Log($"删除空目录: {sourceDir}");
        Directory.Delete(sourceDir);
    }

    private void Log(string message)
    {
        Dispatcher.Invoke(() =>
        {
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            LogTextBox.ScrollToEnd();
        });
    }
}