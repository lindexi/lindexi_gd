using System.IO;
using System.Windows;
using VirtualFileExplorer.Core.PhysicalFileManagers;

namespace YerrolemwaLearcaynarho;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var demoRootPath = Path.Combine(Path.GetTempPath(), "VirtualFileExplorerDemo");
        PrepareDemoDirectory(demoRootPath);
        FileExplorerUserControl.FileManager = new PhysicalFileManager(demoRootPath, "演示目录");
    }

    private static void PrepareDemoDirectory(string rootPath)
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, true);
        }

        Directory.CreateDirectory(rootPath);

        var projectsFolder = Directory.CreateDirectory(Path.Combine(rootPath, "项目资料"));
        var imagesFolder = Directory.CreateDirectory(Path.Combine(rootPath, "图片素材"));
        var archiveFolder = Directory.CreateDirectory(Path.Combine(projectsFolder.FullName, "归档"));

        File.WriteAllText(Path.Combine(rootPath, "欢迎使用.txt"), "VirtualFileExplorer 示例目录。\r\n可直接测试重命名、复制、移动、删除。", System.Text.Encoding.UTF8);
        File.WriteAllText(Path.Combine(projectsFolder.FullName, "迭代计划.md"), "# 迭代计划\r\n- 浏览\r\n- 重命名\r\n- 移动", System.Text.Encoding.UTF8);
        File.WriteAllText(Path.Combine(imagesFolder.FullName, "说明.txt"), "这里可以放置图片素材说明。", System.Text.Encoding.UTF8);
        File.WriteAllText(Path.Combine(archiveFolder.FullName, "历史记录.log"), "2025-01-01 初始化", System.Text.Encoding.UTF8);
    }
}