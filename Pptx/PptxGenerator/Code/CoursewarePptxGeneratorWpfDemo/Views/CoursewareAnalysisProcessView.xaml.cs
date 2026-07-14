using System.Windows;
using System.Windows.Controls;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Displays the steps performed during courseware analysis.
/// </summary>
public partial class CoursewareAnalysisProcessView : UserControl
{
    /// <summary>
    /// Identifies the <see cref="ShowActions" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty ShowActionsProperty = DependencyProperty.Register(
        nameof(ShowActions),
        typeof(bool),
        typeof(CoursewareAnalysisProcessView),
        new PropertyMetadata(true));

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareAnalysisProcessView" /> class.
    /// </summary>
    public CoursewareAnalysisProcessView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets a value indicating whether analysis action buttons are displayed.
    /// </summary>
    public bool ShowActions
    {
        get => (bool)GetValue(ShowActionsProperty);
        set => SetValue(ShowActionsProperty, value);
    }
}
