using System.Windows.Controls;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Displays the presentation-only whole-courseware analysis prototype.
/// </summary>
public partial class CoursewareAnalysisView : UserControl
{
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
}
