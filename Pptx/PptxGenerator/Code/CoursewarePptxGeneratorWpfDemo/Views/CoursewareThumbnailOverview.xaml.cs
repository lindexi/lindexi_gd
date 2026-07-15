using System.Windows;
using System.Windows.Controls;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Displays the presentation-wide demonstration thumbnail overview.
/// </summary>
public partial class CoursewareThumbnailOverview : UserControl
{
    /// <summary>
    /// Identifies the <see cref="ThumbnailAspectRatio" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty ThumbnailAspectRatioProperty = DependencyProperty.Register(
        nameof(ThumbnailAspectRatio),
        typeof(ThumbnailAspectRatio),
        typeof(CoursewareThumbnailOverview),
        new PropertyMetadata(ThumbnailAspectRatio.Widescreen));

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareThumbnailOverview" /> class.
    /// </summary>
    public CoursewareThumbnailOverview()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the shared visual aspect ratio used by every thumbnail frame.
    /// </summary>
    public ThumbnailAspectRatio ThumbnailAspectRatio
    {
        get => (ThumbnailAspectRatio) GetValue(ThumbnailAspectRatioProperty);
        set => SetValue(ThumbnailAspectRatioProperty, value);
    }
}
