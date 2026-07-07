using System.Windows.Controls;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.Win32;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Interaction logic for MainContentPanel.xaml.
/// </summary>
public partial class MainContentPanel : UserControl
{
    public MainContentPanel()
    {
        InitializeComponent();
    }

    private void OpenCoursewareFolderButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择课件 Markdown 导出文件夹",
            Multiselect = false,
        };

        if (dialog.ShowDialog() == true && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.OpenCoursewareFolder(dialog.FolderName);
        }
    }
}
