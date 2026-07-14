using System.Windows.Controls;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.ViewModels;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Displays the presentation-only whole-courseware analysis prototype.
/// </summary>
public partial class CoursewareAnalysisView : UserControl
{
    private readonly ICoursewareFolderPicker _coursewareFolderPicker = new OpenFolderDialogCoursewareFolderPicker();

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareAnalysisView" /> class.
    /// </summary>
    public CoursewareAnalysisView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Moves keyboard focus to the primary element for the current analysis state.
    /// </summary>
    public void FocusPrimaryElement()
    {
        if (OpenCoursewareButton.IsVisible)
        {
            OpenCoursewareButton.Focus();
            return;
        }

        PageTitle.Focus();
    }

    private void OpenCoursewareButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var folderPath = _coursewareFolderPicker.PickCoursewareFolder();
        if (!string.IsNullOrWhiteSpace(folderPath) && DataContext is DemoWorkspaceViewModel viewModel)
        {
            viewModel.OpenDemoCommand.Execute(folderPath);
        }
    }
}
