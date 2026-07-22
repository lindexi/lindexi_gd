using System.Windows.Controls;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Displays the real single-slide beautification workspace.
/// </summary>
public partial class SlideWorkspaceView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlideWorkspaceView" /> class.
    /// </summary>
    public SlideWorkspaceView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Moves keyboard focus to the page-level return navigation.
    /// </summary>
    public void FocusPrimaryElement()
    {
        BackToAnalysisButton.Focus();
    }
}
