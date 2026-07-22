using System.Windows.Controls;
using System.IO;
using System.Windows;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.Win32;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Interaction logic for CopilotPanel.xaml.
/// </summary>
public partial class CopilotPanel : UserControl
{
    public CopilotPanel()
    {
        InitializeComponent();
    }

    private CoursewareSlideWorkspaceViewModel ViewModel => DataContext as CoursewareSlideWorkspaceViewModel
        ?? throw new InvalidOperationException("DataContext must be CoursewareSlideWorkspaceViewModel.");

    private void AttachImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "选择图片文件",
            Multiselect = true,
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp",
        };

        if (openFileDialog.ShowDialog() == true)
        {
            ViewModel.AddAttachedImageFiles(openFileDialog.FileNames);
        }
    }

    private void CloseAttachedImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: FileInfo fileInfo })
        {
            ViewModel.SelectedSlide?.AttachedImageFiles.Remove(fileInfo);
        }
    }

}
