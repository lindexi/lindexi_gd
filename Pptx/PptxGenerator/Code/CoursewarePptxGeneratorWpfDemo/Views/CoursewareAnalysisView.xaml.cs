using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.ViewModels;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Displays the presentation-only whole-courseware analysis prototype.
/// </summary>
public partial class CoursewareAnalysisView : UserControl
{
    private static readonly DependencyPropertyKey ThumbnailAspectRatioPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ThumbnailAspectRatio),
        typeof(ThumbnailAspectRatio),
        typeof(CoursewareAnalysisView),
        new PropertyMetadata(ThumbnailAspectRatio.Widescreen));

    /// <summary>
    /// Identifies the <see cref="ThumbnailAspectRatio" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty ThumbnailAspectRatioProperty = ThumbnailAspectRatioPropertyKey.DependencyProperty;

    private readonly ICoursewareFolderPicker _coursewareFolderPicker = new OpenFolderDialogCoursewareFolderPicker();
    private ObservableCollection<CoursewareThumbnailItemViewModel>? _coursewareThumbnails;
    private DispatcherOperation? _pendingThumbnailProbe;
    private long _thumbnailProbeGeneration;
    private bool _hasCompletedThumbnailProbe;
    private bool _isListeningForThumbnailChanges;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareAnalysisView" /> class.
    /// </summary>
    public CoursewareAnalysisView()
    {
        InitializeComponent();
        Loaded += CoursewareAnalysisView_OnLoaded;
        Unloaded += CoursewareAnalysisView_OnUnloaded;
        DataContextChanged += CoursewareAnalysisView_OnDataContextChanged;
    }

    /// <summary>
    /// Gets the shared visual aspect ratio used by every courseware thumbnail frame on this page.
    /// </summary>
    public ThumbnailAspectRatio ThumbnailAspectRatio => (ThumbnailAspectRatio) GetValue(ThumbnailAspectRatioProperty);

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
        if (!string.IsNullOrWhiteSpace(folderPath) && DataContext is CoursewareWorkspaceViewModel viewModel)
        {
            viewModel.OpenDemoCommand.Execute(folderPath);
        }
    }

    internal static ThumbnailAspectRatio GetThumbnailAspectRatio(int pixelWidth, int pixelHeight)
    {
        if (pixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth));
        }

        if (pixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight));
        }

        return (long) 9 * pixelWidth > (long) 14 * pixelHeight
            ? ThumbnailAspectRatio.Widescreen
            : ThumbnailAspectRatio.Standard;
    }

    private void CoursewareAnalysisView_OnLoaded(object sender, RoutedEventArgs e)
    {
        _isListeningForThumbnailChanges = true;
        AttachCoursewareThumbnails();
    }

    private void CoursewareAnalysisView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isListeningForThumbnailChanges = false;
        DetachCoursewareThumbnails();
    }

    private void CoursewareAnalysisView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_isListeningForThumbnailChanges)
        {
            AttachCoursewareThumbnails();
        }
    }

    private void AttachCoursewareThumbnails()
    {
        var coursewareThumbnails = (DataContext as CoursewareWorkspaceViewModel)?.CoursewareThumbnails;
        if (ReferenceEquals(_coursewareThumbnails, coursewareThumbnails))
        {
            return;
        }

        DetachCoursewareThumbnails();
        _coursewareThumbnails = coursewareThumbnails;
        if (_coursewareThumbnails is not null)
        {
            _coursewareThumbnails.CollectionChanged += CoursewareThumbnails_OnCollectionChanged;
        }

        BeginThumbnailProbeRound();
    }

    private void DetachCoursewareThumbnails()
    {
        if (_coursewareThumbnails is not null)
        {
            _coursewareThumbnails.CollectionChanged -= CoursewareThumbnails_OnCollectionChanged;
            _coursewareThumbnails = null;
        }

        _pendingThumbnailProbe?.Abort();
        _pendingThumbnailProbe = null;
        _thumbnailProbeGeneration++;
        _hasCompletedThumbnailProbe = false;
        SetValue(ThumbnailAspectRatioPropertyKey, ThumbnailAspectRatio.Widescreen);
    }

    private void CoursewareThumbnails_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset || _hasCompletedThumbnailProbe)
        {
            BeginThumbnailProbeRound();
        }
    }

    private void BeginThumbnailProbeRound()
    {
        _thumbnailProbeGeneration++;
        _hasCompletedThumbnailProbe = false;
        SetValue(ThumbnailAspectRatioPropertyKey, ThumbnailAspectRatio.Widescreen);
        ScheduleThumbnailProbe();
    }

    private void ScheduleThumbnailProbe()
    {
        if (_pendingThumbnailProbe is not null)
        {
            return;
        }

        _pendingThumbnailProbe = Dispatcher.InvokeAsync(
            () => ProbeThumbnailAspectRatio(_thumbnailProbeGeneration),
            DispatcherPriority.ContextIdle);
    }

    private void ProbeThumbnailAspectRatio(long generation)
    {
        _pendingThumbnailProbe = null;
        if (generation != _thumbnailProbeGeneration || _coursewareThumbnails is null)
        {
            return;
        }

        var collectionView = CollectionViewSource.GetDefaultView(_coursewareThumbnails);
        var representativePath = collectionView
            .Cast<object>()
            .OfType<CoursewareThumbnailItemViewModel>()
            .Select(thumbnail => thumbnail.ScreenshotFilePath)
            .FirstOrDefault(filePath => !string.IsNullOrWhiteSpace(filePath));

        var aspectRatio = TryGetThumbnailAspectRatio(representativePath, out var detectedAspectRatio)
            ? detectedAspectRatio
            : ThumbnailAspectRatio.Widescreen;

        if (generation != _thumbnailProbeGeneration)
        {
            return;
        }

        SetValue(ThumbnailAspectRatioPropertyKey, aspectRatio);
        _hasCompletedThumbnailProbe = true;
    }

    private static bool TryGetThumbnailAspectRatio(string? filePath, out ThumbnailAspectRatio aspectRatio)
    {
        aspectRatio = ThumbnailAspectRatio.Widescreen;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
            {
                return false;
            }

            aspectRatio = GetThumbnailAspectRatio(bitmap.PixelWidth, bitmap.PixelHeight);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
