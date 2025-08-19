using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RalhekajelRiyemlini;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var name = GetLogFolderName();
        var match = GetLogFolderProcessRegex().Match(name);
    }

    public static string GetLogFolderName()
    {
        // 命名规则： 年月日_小时分钟秒,进程ID
        // 20250724_171139,63912
        var now = DateTime.Now;
        var processId = Environment.ProcessId;
        return $"{now:yyyyMMdd_HHmmss},{processId}";
    }


    [GeneratedRegex(@"\d+_\d+,(\d+)")]
    private static partial Regex GetLogFolderProcessRegex();
}