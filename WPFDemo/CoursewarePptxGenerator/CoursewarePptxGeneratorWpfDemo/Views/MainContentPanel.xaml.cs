using System.Windows.Controls;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.ViewModels;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Interaction logic for MainContentPanel.xaml.
/// </summary>
public partial class MainContentPanel : UserControl
{
    private ICoursewareFolderPicker _coursewareFolderPicker = new OpenFolderDialogCoursewareFolderPicker();

    public MainContentPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the courseware folder picker used by the open button.
    /// </summary>
    /// <param name="coursewareFolderPicker">The folder picker to use.</param>
    public void SetCoursewareFolderPicker(ICoursewareFolderPicker coursewareFolderPicker)
    {
        ArgumentNullException.ThrowIfNull(coursewareFolderPicker);
        _coursewareFolderPicker = coursewareFolderPicker;
    }

    private void OpenCoursewareFolderButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        OpenSelectedCoursewareFolder();
    }

    /// <summary>
    /// Opens the courseware folder selected by the configured folder picker.
    /// </summary>
    public void OpenSelectedCoursewareFolder()
    {
        var folderPath = _coursewareFolderPicker.PickCoursewareFolder();
        if (!string.IsNullOrWhiteSpace(folderPath) && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.OpenCoursewareFolder(folderPath);
        }
    }
}
