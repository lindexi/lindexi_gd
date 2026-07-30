using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using AgentLib.Model;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Resources;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Threading;

namespace CoursewarePptxGeneratorWpfDemo.ViewModels;

/// <summary>
/// Identifies the page displayed by the courseware application shell.
/// </summary>
public enum CoursewareApplicationPage
{
    /// <summary>
    /// The whole-courseware analysis page.
    /// </summary>
    CoursewareAnalysis,

    /// <summary>
    /// The single-slide workspace page.
    /// </summary>
    SlideWorkspace,
}

/// <summary>
/// Identifies the content displayed in the courseware analysis workspace.
/// </summary>
public enum CoursewareAnalysisTab
{
    /// <summary>
    /// The Copilot conversation produced during theme analysis.
    /// </summary>
    Conversation,

    /// <summary>
    /// The completed courseware theme result.
    /// </summary>
    ThemeResult,
}

/// <summary>
/// Provides data and navigation for the courseware workspace.
/// </summary>
public sealed class CoursewareWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly CoursewareWorkspaceFolderLoader _workspaceFolderLoader;
    private readonly ICoursewareThemeAnalysisSnapshotStore _themeAnalysisSnapshotStore;
    private readonly ICoursewareThemeAnalysisService _themeAnalysisService;
    private readonly ISlideChatManagerFactory _slideChatManagerFactory;
    private readonly CoursewareSlideSummaryService _slideSummaryService;
    private readonly ICoursewareSlidePromptBuilder _slidePromptBuilder;
    private readonly IViewModelDispatcher _dispatcher;
    private readonly AsyncRelayCommand _enterWorkspaceCommand;
    private readonly AsyncRelayCommand _reanalyzeCommand;
    private readonly RelayCommand _cancelAnalysisCommand;
    private CoursewareApplicationPage _currentPage = CoursewareApplicationPage.CoursewareAnalysis;
    private CoursewareAnalysisTab _selectedAnalysisTab = CoursewareAnalysisTab.Conversation;
    private CoursewareWorkspaceState _workspaceState = CoursewareWorkspaceState.Welcome;
    private CoursewareWorkspaceSession? _coursewareSession;
    private CoursewareSlideWorkspaceViewModel? _slideWorkspace;
    private string? _loadErrorMessage;
    private string? _loadErrorDetails;
    private CancellationTokenSource? _workflowCancellationTokenSource;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareWorkspaceViewModel" /> class.
    /// </summary>
    public CoursewareWorkspaceViewModel()
        : this(new CoursewareFolderLoader())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareWorkspaceViewModel" /> class.
    /// </summary>
    /// <param name="coursewareFolderLoader">The courseware export folder loader.</param>
    /// <param name="dispatcher">The dispatcher used for ViewModel state updates.</param>
    /// <param name="themeAnalysisService">The service used to analyze the loaded courseware theme.</param>
    /// <param name="slideChatManagerFactory">The factory used to create independent page runtimes.</param>
    /// <param name="slideSummaryService">The deterministic page summary service.</param>
    /// <param name="slidePromptBuilder">The structured page prompt builder.</param>
    /// <param name="themeAnalysisSnapshotStore">The store used to save and restore theme-analysis snapshots.</param>
    /// <param name="workspaceFolderLoader">The unified ordinary-courseware and snapshot folder loader.</param>
    public CoursewareWorkspaceViewModel(
        CoursewareFolderLoader coursewareFolderLoader,
        IViewModelDispatcher? dispatcher = null,
        ICoursewareThemeAnalysisService? themeAnalysisService = null,
        ISlideChatManagerFactory? slideChatManagerFactory = null,
        CoursewareSlideSummaryService? slideSummaryService = null,
        ICoursewareSlidePromptBuilder? slidePromptBuilder = null,
        ICoursewareThemeAnalysisSnapshotStore? themeAnalysisSnapshotStore = null,
        CoursewareWorkspaceFolderLoader? workspaceFolderLoader = null)
    {
        ArgumentNullException.ThrowIfNull(coursewareFolderLoader);

        _themeAnalysisSnapshotStore = themeAnalysisSnapshotStore ?? new CoursewareThemeAnalysisSnapshotStore();
        _workspaceFolderLoader = workspaceFolderLoader
            ?? new CoursewareWorkspaceFolderLoader(coursewareFolderLoader, _themeAnalysisSnapshotStore);
        _themeAnalysisService = themeAnalysisService ?? new CoursewareThemeAnalysisService();
        _slideChatManagerFactory = slideChatManagerFactory ?? new SlideChatManagerFactory();
        _slideSummaryService = slideSummaryService ?? new CoursewareSlideSummaryService();
        _slidePromptBuilder = slidePromptBuilder ?? new CoursewareSlidePromptBuilder();
        _dispatcher = dispatcher ?? WpfViewModelDispatcher.Instance;
        CoursewareThumbnails = new ObservableCollection<CoursewareThumbnailItemViewModel>();
        ThemeColors = new ObservableCollection<CoursewareThemeColorViewModel>();
        TypographyLevels = new ObservableCollection<CoursewareTypographyLevelViewModel>();
        LayoutPrinciples = new ObservableCollection<string>();
        AnalysisEvents = new ObservableCollection<CoursewareAnalysisEvent>();
        AnalysisChatMessages = new ObservableCollection<CopilotChatMessage>();
        _enterWorkspaceCommand = new AsyncRelayCommand(
            _ => EnterWorkspaceAsync(),
            _ => CanEnterWorkspace(),
            HandleUnexpectedCommandException);
        _reanalyzeCommand = new AsyncRelayCommand(
            _ => ReanalyzeAsync(),
            _ => CoursewareSession is not null
                && WorkspaceState is not CoursewareWorkspaceState.LoadingCourseware
                && WorkspaceState is not CoursewareWorkspaceState.AnalyzingCourseware,
            HandleUnexpectedCommandException,
            () => _workflowCancellationTokenSource?.Cancel());
        _cancelAnalysisCommand = new RelayCommand(
            _ => _workflowCancellationTokenSource?.Cancel(),
            _ => WorkspaceState == CoursewareWorkspaceState.AnalyzingCourseware);
        BackToAnalysisCommand = new RelayCommand(_ => ReturnToAnalysis());
    }

    /// <summary>
    /// Gets the courseware title shown by the application.
    /// </summary>
    public string CoursewareTitle => CoursewareSession?.InputPackage.CoursewareName ?? "尚未打开课件";

    /// <summary>
    /// Gets the product title.
    /// </summary>
    public string ProductTitle => "课件智绘";

    /// <summary>
    /// Gets the generated courseware theme name.
    /// </summary>
    public string ThemeTitle => CoursewareSession?.ThemeAnalysisResult?.Theme.Style ?? "正在形成课件主题";

    /// <summary>
    /// Gets the generated courseware theme summary.
    /// </summary>
    public string ThemeDescription => CoursewareSession?.ThemeAnalysisResult?.Theme.SpacingAndVisualEffects ?? "分析完成后将在这里展示整份课件的统一视觉主题。";

    /// <summary>
    /// Gets the slide count summary.
    /// </summary>
    public string SlideCountText => CoursewareSession is null ? "尚未加载页面" : $"共 {CoursewareThumbnails.Count} 页";

    /// <summary>
    /// Gets the demonstration input health summary.
    /// </summary>
    public string InputHealthText
    {
        get
        {
            var warningCount = CoursewareSession?.InputPackage.Warnings.Count ?? 0;
            return warningCount == 0 ? "输入完整，无缺失截图和资源警告" : $"发现 {warningCount} 项输入警告";
        }
    }

    /// <summary>
    /// Gets the real single-slide workspace created from the latest successful theme analysis.
    /// </summary>
    public CoursewareSlideWorkspaceViewModel? SlideWorkspace
    {
        get => _slideWorkspace;
        private set
        {
            if (SetProperty(ref _slideWorkspace, value))
            {
                _enterWorkspaceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the thumbnails loaded from the selected courseware folder.
    /// </summary>
    public ObservableCollection<CoursewareThumbnailItemViewModel> CoursewareThumbnails { get; }

    /// <summary>
    /// Gets the current lightweight courseware session.
    /// </summary>
    public CoursewareWorkspaceSession? CoursewareSession
    {
        get => _coursewareSession;
        private set
        {
            if (SetProperty(ref _coursewareSession, value))
            {
                OnPropertyChanged(nameof(CoursewareTitle));
                OnPropertyChanged(nameof(SlideCountText));
                OnPropertyChanged(nameof(InputHealthText));
                OnPropertyChanged(nameof(ThemeTitle));
                OnPropertyChanged(nameof(ThemeDescription));
                OnPropertyChanged(nameof(ShowsCoursewareContext));
                OnPropertyChanged(nameof(ShowsThemeResult));
            }
        }
    }

    /// <summary>
    /// Cancels active work and releases the current slide workspace.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        CancelIfActive(Volatile.Read(ref _workflowCancellationTokenSource));
        DisposeSlideWorkspace();
    }

    /// <summary>
    /// Gets the state of the real courseware loading workflow.
    /// </summary>
    public CoursewareWorkspaceState WorkspaceState
    {
        get => _workspaceState;
        private set
        {
            if (SetProperty(ref _workspaceState, value))
            {
                OnPropertyChanged(nameof(IsCoursewareWelcome));
                OnPropertyChanged(nameof(IsCoursewareLoading));
                OnPropertyChanged(nameof(IsAnalyzingTheme));
                OnPropertyChanged(nameof(IsAnalysisReady));
                OnPropertyChanged(nameof(IsCoursewareLoadFailed));
                OnPropertyChanged(nameof(IsAnalysisFailed));
                OnPropertyChanged(nameof(IsCanceled));
                OnPropertyChanged(nameof(ShowsCoursewareContext));
                OnPropertyChanged(nameof(ShowsThemeResult));
                OnPropertyChanged(nameof(AnalysisStatus));
                OnPropertyChanged(nameof(AnalysisCaption));
                _enterWorkspaceCommand.RaiseCanExecuteChanged();
                _reanalyzeCommand.RaiseCanExecuteChanged();
                _cancelAnalysisCommand.RaiseCanExecuteChanged();

                if (value == CoursewareWorkspaceState.AnalyzingCourseware)
                {
                    SelectedAnalysisTab = CoursewareAnalysisTab.Conversation;
                }
                else if (value == CoursewareWorkspaceState.AnalysisReady)
                {
                    SelectedAnalysisTab = CoursewareAnalysisTab.ThemeResult;
                }
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether no real courseware has been selected.
    /// </summary>
    public bool IsCoursewareWelcome => WorkspaceState == CoursewareWorkspaceState.Welcome;

    /// <summary>
    /// Gets a value indicating whether a real courseware folder is being loaded.
    /// </summary>
    public bool IsCoursewareLoading => WorkspaceState == CoursewareWorkspaceState.LoadingCourseware;

    /// <summary>
    /// Gets a value indicating whether the loaded courseware is being analyzed.
    /// </summary>
    public bool IsAnalyzingTheme => WorkspaceState == CoursewareWorkspaceState.AnalyzingCourseware;

    /// <summary>
    /// Gets a value indicating whether the theme result is ready for review.
    /// </summary>
    public bool IsAnalysisReady => WorkspaceState == CoursewareWorkspaceState.AnalysisReady;

    /// <summary>
    /// Gets a value indicating whether the last real courseware load failed.
    /// </summary>
    public bool IsCoursewareLoadFailed => WorkspaceState == CoursewareWorkspaceState.LoadFailed;

    /// <summary>
    /// Gets a value indicating whether the last theme analysis failed.
    /// </summary>
    public bool IsAnalysisFailed => WorkspaceState == CoursewareWorkspaceState.AnalysisFailed;

    /// <summary>
    /// Gets a value indicating whether the current analysis was canceled.
    /// </summary>
    public bool IsCanceled => WorkspaceState == CoursewareWorkspaceState.Canceled;

    /// <summary>
    /// Gets a value indicating whether loaded courseware context is available.
    /// </summary>
    public bool ShowsCoursewareContext => CoursewareSession is not null;

    /// <summary>
    /// Gets a value indicating whether a complete theme result is available.
    /// </summary>
    public bool ShowsThemeResult => CoursewareSession?.ThemeAnalysisResult is not null;

    /// <summary>
    /// Gets or sets the content selected in the courseware analysis workspace.
    /// </summary>
    public CoursewareAnalysisTab SelectedAnalysisTab
    {
        get => _selectedAnalysisTab;
        set
        {
            if (value == CoursewareAnalysisTab.ThemeResult && !IsAnalysisReady)
            {
                return;
            }

            if (SetProperty(ref _selectedAnalysisTab, value))
            {
                OnPropertyChanged(nameof(SelectedAnalysisTabIndex));
            }
        }
    }

    /// <summary>
    /// Gets or sets the zero-based index selected in the courseware analysis workspace.
    /// </summary>
    public int SelectedAnalysisTabIndex
    {
        get => (int)SelectedAnalysisTab;
        set
        {
            if (Enum.IsDefined(typeof(CoursewareAnalysisTab), value))
            {
                SelectedAnalysisTab = (CoursewareAnalysisTab)value;
            }
        }
    }

    /// <summary>
    /// Gets the user-facing courseware load error.
    /// </summary>
    public string? LoadErrorMessage
    {
        get => _loadErrorMessage;
        private set => SetProperty(ref _loadErrorMessage, value);
    }

    /// <summary>
    /// Gets the technical courseware load error details.
    /// </summary>
    public string? LoadErrorDetails
    {
        get => _loadErrorDetails;
        private set => SetProperty(ref _loadErrorDetails, value);
    }

    /// <summary>
    /// Gets the theme color swatches.
    /// </summary>
    public ObservableCollection<CoursewareThemeColorViewModel> ThemeColors { get; }

    /// <summary>
    /// Gets the analyzed typography hierarchy.
    /// </summary>
    public ObservableCollection<CoursewareTypographyLevelViewModel> TypographyLevels { get; }

    /// <summary>
    /// Gets the recommended layout principles.
    /// </summary>
    public ObservableCollection<string> LayoutPrinciples { get; }

    /// <summary>
    /// <summary>
    /// Gets user-facing events produced by the active theme analysis.
    /// </summary>
    public ObservableCollection<CoursewareAnalysisEvent> AnalysisEvents { get; }

    /// <summary>
    /// Gets the user-facing Copilot messages produced during theme analysis.
    /// </summary>
    public ObservableCollection<CopilotChatMessage> AnalysisChatMessages { get; }

    /// <summary>
    /// Gets the theme font recommendation summary.
    /// </summary>
    public string FontRecommendationText
    {
        get
        {
            var fonts = CoursewareSession?.ThemeAnalysisResult?.Theme.Fonts;
            return fonts is null
                ? "分析完成后显示字体建议"
                : $"中文：{fonts.Chinese} · 西文：{fonts.Western}";
        }
    }

    /// <summary>
    /// Gets the theme safe-area summary.
    /// </summary>
    public string SafeAreaText
    {
        get
        {
            var safeArea = CoursewareSession?.ThemeAnalysisResult?.Theme.SafeArea;
            return safeArea is null
                ? "分析完成后显示安全区"
                : $"左 {safeArea.LeftRatio:P0} · 上 {safeArea.TopRatio:P0} · 右 {safeArea.RightRatio:P0} · 下 {safeArea.BottomRatio:P0}";
        }
    }

    /// <summary>
    /// Gets the color-scheme rationale.
    /// </summary>
    public string ColorRationale => CoursewareSession?.ThemeAnalysisResult?.Theme.SpacingAndVisualEffects ?? "分析完成后显示间距与视觉效果建议。";

    /// <summary>
    /// Gets the Theme 2.1 style description.
    /// </summary>
    public string ThemeStyle => CoursewareSession?.ThemeAnalysisResult?.Theme.Style ?? string.Empty;

    /// <summary>
    /// Gets the Theme 2.1 spacing and visual-effects guidance.
    /// </summary>
    public string SpacingAndVisualEffects => CoursewareSession?.ThemeAnalysisResult?.Theme.SpacingAndVisualEffects ?? string.Empty;

    /// <summary>
    /// Gets the Theme 2.1 layout principles.
    /// </summary>
    public string ThemeLayoutPrinciples => CoursewareSession?.ThemeAnalysisResult?.Theme.LayoutPrinciples ?? string.Empty;

    /// <summary>
    /// Gets the complete Theme 2.1 cover-page SlideML.
    /// </summary>
    public string CoverPageSlideMl => CoursewareSession?.ThemeAnalysisResult?.Theme.CoverPageSlideMl ?? string.Empty;

    /// <summary>
    /// Gets the complete Theme 2.1 content-page SlideML.
    /// </summary>
    public string ContentPageSlideMl => CoursewareSession?.ThemeAnalysisResult?.Theme.ContentPageSlideMl ?? string.Empty;

    /// <summary>
    /// Gets courseware input warnings.
    /// </summary>
    public IReadOnlyList<string> AnalysisWarnings => CoursewareSession?.InputPackage.Warnings
        .Select(warning => warning.Message)
        .ToArray()
        ?? [];

    /// <summary>
    /// Gets or sets the page displayed by the application shell.
    /// </summary>
    public CoursewareApplicationPage CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsAnalysisPage));
                OnPropertyChanged(nameof(IsWorkspacePage));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the analysis page is visible.
    /// </summary>
    public bool IsAnalysisPage => CurrentPage == CoursewareApplicationPage.CoursewareAnalysis;

    /// <summary>
    /// Gets a value indicating whether the slide workspace is visible.
    /// </summary>
    public bool IsWorkspacePage => CurrentPage == CoursewareApplicationPage.SlideWorkspace;

    /// <summary>
    /// Gets the current analysis status.
    /// </summary>
    public string AnalysisStatus => WorkspaceState switch
    {
        CoursewareWorkspaceState.LoadingCourseware => "正在读取课件",
        CoursewareWorkspaceState.AnalyzingCourseware => "正在分析全课件主题",
        CoursewareWorkspaceState.AnalysisReady => CoursewareUiStrings.AnalysisReadyStatus,
        CoursewareWorkspaceState.LoadFailed => "课件读取失败",
        CoursewareWorkspaceState.AnalysisFailed => "主题分析失败",
        CoursewareWorkspaceState.Canceled => "主题分析已取消",
        _ => "等待打开课件",
    };

    /// <summary>
    /// Gets the secondary analysis status text.
    /// </summary>
    public string AnalysisCaption => WorkspaceState switch
    {
        CoursewareWorkspaceState.LoadingCourseware => "正在读取页面内容、资源和预览图",
        CoursewareWorkspaceState.AnalyzingCourseware => $"已读取 {CoursewareThumbnails.Count} 页，正在归纳内容层级、配色、字体与版式",
        CoursewareWorkspaceState.AnalysisReady => string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            CoursewareUiStrings.AnalysisReadyCaptionFormat,
            CoursewareThumbnails.Count),
        CoursewareWorkspaceState.LoadFailed => "请选择包含完整课件内容的文件夹",
        CoursewareWorkspaceState.AnalysisFailed => "课件和缩略图已保留，可以修复配置后重试",
        CoursewareWorkspaceState.Canceled => "已保留课件概览，可随时重新分析",
        _ => "打开课件后将自动形成统一视觉主题",
    };

    /// <summary>
    /// Gets the command that enters the slide workspace.
    /// </summary>
    public AsyncRelayCommand EnterWorkspaceCommand => _enterWorkspaceCommand;

    /// <summary>
    /// Gets the command that returns to the courseware analysis page.
    /// </summary>
    public ICommand BackToAnalysisCommand { get; }

    /// <summary>
    /// Gets the command that cancels the active theme analysis.
    /// </summary>
    public ICommand CancelAnalysisCommand => _cancelAnalysisCommand;

    /// <summary>
    /// Gets the command that restarts the demonstration analysis.
    /// </summary>
    public AsyncRelayCommand ReanalyzeCommand => _reanalyzeCommand;

    /// <summary>
    /// Gets the command that retries the failed theme analysis.
    /// </summary>
    public AsyncRelayCommand RetryAnalysisCommand => _reanalyzeCommand;

    /// <summary>
    /// Loads the selected courseware export folder without starting theme analysis or page generation.
    /// </summary>
    /// <param name="folderPath">The selected courseware export folder.</param>
    /// <returns>A task that represents the loading operation.</returns>
    public async Task OpenCoursewareFolderAsync(string? folderPath)
    {
        if (_isDisposed || string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        var previousCancellationTokenSource = Interlocked.Exchange(ref _workflowCancellationTokenSource, cancellationTokenSource);
        CancelIfActive(previousCancellationTokenSource);

        await _dispatcher.InvokeAsync(() =>
        {
            DisposeSlideWorkspace();
            CurrentPage = CoursewareApplicationPage.CoursewareAnalysis;
            CoursewareSession = null;
            CoursewareThumbnails.Clear();
            ClearAnalysisPresentation();
            WorkspaceState = CoursewareWorkspaceState.LoadingCourseware;
            LoadErrorMessage = null;
            LoadErrorDetails = null;
        });

        try
        {
            var folderLoadResult = await _workspaceFolderLoader.LoadAsync(folderPath, cancellationTokenSource.Token)
                .ConfigureAwait(false);
            var package = folderLoadResult.InputPackage;
            var thumbnails = new List<CoursewareThumbnailItemViewModel>(package.Slides.Count);
            foreach (var slide in package.Slides)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                thumbnails.Add(CoursewareThumbnailItemViewModel.Create(slide));
            }

            await _dispatcher.InvokeAsync(() =>
            {
                CoursewareThumbnails.Clear();
                CoursewareSession = null;
                foreach (var thumbnail in thumbnails)
                {
                    CoursewareThumbnails.Add(thumbnail);
                }

                CoursewareSession = new CoursewareWorkspaceSession(package);
                ClearAnalysisPresentation();
                WorkspaceState = folderLoadResult.IsThemeAnalysisSnapshot
                    ? CoursewareWorkspaceState.LoadingCourseware
                    : CoursewareWorkspaceState.AnalyzingCourseware;
                OnPropertyChanged(nameof(SlideCountText));
                OnPropertyChanged(nameof(InputHealthText));
            });

            if (folderLoadResult.AnalysisResult is not null)
            {
                await _dispatcher.InvokeAsync(() => PublishRestoredAnalysisResult(
                    package,
                    cancellationTokenSource,
                    folderLoadResult.AnalysisResult));
                await EnterWorkspaceAsync().ConfigureAwait(false);
                return;
            }

            var analysisResult = await AnalyzeThemeAsync(
                package,
                cancellationTokenSource).ConfigureAwait(false);
            await SaveThemeAnalysisSnapshotAsync(
                package,
                analysisResult,
                cancellationTokenSource.Token).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() => PublishAnalysisResult(package, cancellationTokenSource, analysisResult));
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                if (!_isDisposed
                    && ReferenceEquals(_workflowCancellationTokenSource, cancellationTokenSource)
                    && CoursewareSession is not null)
                {
                    WorkspaceState = CoursewareWorkspaceState.Canceled;
                }
            });
        }
        catch (Exception ex)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                if (_isDisposed
                    || !ReferenceEquals(_workflowCancellationTokenSource, cancellationTokenSource))
                {
                    return;
                }

                LoadErrorMessage = ex.Message;
                LoadErrorDetails = ex.ToString();
                if (CoursewareSession is null)
                {
                    CoursewareThumbnails.Clear();
                    WorkspaceState = CoursewareWorkspaceState.LoadFailed;
                }
                else
                {
                    WorkspaceState = CoursewareWorkspaceState.AnalysisFailed;
                }
            });
        }
        finally
        {
            Interlocked.CompareExchange(ref _workflowCancellationTokenSource, null, cancellationTokenSource);
            cancellationTokenSource.Dispose();
        }
    }

    private async Task ReanalyzeAsync()
    {
        var session = CoursewareSession;
        if (_isDisposed || session is null)
        {
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        var previousCancellationTokenSource = Interlocked.Exchange(ref _workflowCancellationTokenSource, cancellationTokenSource);
        CancelIfActive(previousCancellationTokenSource);

        await _dispatcher.InvokeAsync(() =>
        {
            ClearAnalysisPresentation();
            LoadErrorMessage = null;
            LoadErrorDetails = null;
            WorkspaceState = CoursewareWorkspaceState.AnalyzingCourseware;
            OnPropertyChanged(nameof(ShowsThemeResult));
        });

        try
        {
            var analysisResult = await AnalyzeThemeAsync(
                session.InputPackage,
                cancellationTokenSource).ConfigureAwait(false);
            await SaveThemeAnalysisSnapshotAsync(
                session.InputPackage,
                analysisResult,
                cancellationTokenSource.Token).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() => PublishAnalysisResult(session.InputPackage, cancellationTokenSource, analysisResult));
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                if (!_isDisposed
                    && ReferenceEquals(_workflowCancellationTokenSource, cancellationTokenSource))
                {
                    WorkspaceState = CoursewareWorkspaceState.Canceled;
                }
            });
        }
        catch (Exception ex)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                if (_isDisposed
                    || !ReferenceEquals(_workflowCancellationTokenSource, cancellationTokenSource))
                {
                    return;
                }

                LoadErrorMessage = ex.Message;
                LoadErrorDetails = ex.ToString();
                WorkspaceState = CoursewareWorkspaceState.AnalysisFailed;
            });
        }
        finally
        {
            Interlocked.CompareExchange(ref _workflowCancellationTokenSource, null, cancellationTokenSource);
            cancellationTokenSource.Dispose();
        }
    }

    private void PublishAnalysisResult(
        CoursewareInputPackage inputPackage,
        CancellationTokenSource workflowCancellationTokenSource,
        CoursewareThemeAnalysisResult analysisResult)
    {
        if (_isDisposed
            || CoursewareSession is null
            || !ReferenceEquals(CoursewareSession.InputPackage, inputPackage)
            || !ReferenceEquals(_workflowCancellationTokenSource, workflowCancellationTokenSource))
        {
            return;
        }

        CommitThemeAnalysisResult(CoursewareSession, analysisResult);
    }

    private async Task SaveThemeAnalysisSnapshotAsync(
        CoursewareInputPackage inputPackage,
        CoursewareThemeAnalysisResult analysisResult,
        CancellationToken cancellationToken)
    {
        _ = await _themeAnalysisSnapshotStore.SaveAsync(
            inputPackage,
            analysisResult,
            cancellationToken).ConfigureAwait(false);
    }

    private void PublishRestoredAnalysisResult(
        CoursewareInputPackage inputPackage,
        CancellationTokenSource workflowCancellationTokenSource,
        CoursewareThemeAnalysisResult analysisResult)
    {
        if (_isDisposed
            || CoursewareSession is null
            || !ReferenceEquals(CoursewareSession.InputPackage, inputPackage)
            || !ReferenceEquals(_workflowCancellationTokenSource, workflowCancellationTokenSource))
        {
            return;
        }

        CommitThemeAnalysisResult(CoursewareSession, analysisResult);
    }

    private void CommitThemeAnalysisResult(
        CoursewareWorkspaceSession session,
        CoursewareThemeAnalysisResult analysisResult)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(analysisResult);

        session.ThemeAnalysisResult = analysisResult;
        ApplyThemePresentation(analysisResult.Theme);
        if (SlideWorkspace is null)
        {
            ReplaceSlideWorkspace(new CoursewareSlideWorkspaceViewModel(
                session,
                _slideChatManagerFactory,
                _slidePromptBuilder,
                _slideSummaryService,
                _dispatcher));
        }
        else
        {
            SlideWorkspace.UpdateThemeAnalysisResult(analysisResult);
        }
        WorkspaceState = CoursewareWorkspaceState.AnalysisReady;
        OnPropertyChanged(nameof(ThemeTitle));
        OnPropertyChanged(nameof(ThemeDescription));
        OnPropertyChanged(nameof(ShowsThemeResult));
        OnPropertyChanged(nameof(FontRecommendationText));
        OnPropertyChanged(nameof(SafeAreaText));
        OnPropertyChanged(nameof(ColorRationale));
        OnPropertyChanged(nameof(ThemeStyle));
        OnPropertyChanged(nameof(SpacingAndVisualEffects));
        OnPropertyChanged(nameof(ThemeLayoutPrinciples));
        OnPropertyChanged(nameof(CoverPageSlideMl));
        OnPropertyChanged(nameof(ContentPageSlideMl));
        OnPropertyChanged(nameof(AnalysisWarnings));
    }

    private async Task<CoursewareThemeAnalysisResult> AnalyzeThemeAsync(
        CoursewareInputPackage inputPackage,
        CancellationTokenSource workflowCancellationTokenSource)
    {
        var progress = CreateAnalysisProgress(inputPackage, workflowCancellationTokenSource);
        var messageProgress = CreateAnalysisMessageProgress(inputPackage, workflowCancellationTokenSource);
        try
        {
            return await _themeAnalysisService.AnalyzeAsync(
                inputPackage,
                progress,
                messageProgress,
                workflowCancellationTokenSource.Token).ConfigureAwait(false);
        }
        finally
        {
            await Task.WhenAll(
                progress.WaitForCompletionAsync(),
                messageProgress.WaitForCompletionAsync()).ConfigureAwait(false);
        }
    }

    private OrderedDispatcherProgress<CoursewareAnalysisEvent> CreateAnalysisProgress(
        CoursewareInputPackage inputPackage,
        CancellationTokenSource workflowCancellationTokenSource)
    {
        return new OrderedDispatcherProgress<CoursewareAnalysisEvent>(_dispatcher, analysisEvent =>
        {
            if (_isDisposed
                || CoursewareSession is null
                || !ReferenceEquals(CoursewareSession.InputPackage, inputPackage)
                || !ReferenceEquals(_workflowCancellationTokenSource, workflowCancellationTokenSource))
            {
                return;
            }

            UpdateAnalysisStage(analysisEvent);
        });
    }

    private OrderedDispatcherProgress<CopilotChatMessage> CreateAnalysisMessageProgress(
        CoursewareInputPackage inputPackage,
        CancellationTokenSource workflowCancellationTokenSource)
    {
        return new OrderedDispatcherProgress<CopilotChatMessage>(_dispatcher, message =>
        {
            if (_isDisposed
                || CoursewareSession is null
                || !ReferenceEquals(CoursewareSession.InputPackage, inputPackage)
                || !ReferenceEquals(_workflowCancellationTokenSource, workflowCancellationTokenSource))
            {
                return;
            }

            AnalysisChatMessages.Add(message);
        });
    }

    private sealed class OrderedDispatcherProgress<T>(
        IViewModelDispatcher dispatcher,
        Action<T> report) : IProgress<T>
    {
        private readonly IViewModelDispatcher _dispatcher = dispatcher;
        private readonly Action<T> _report = report;
        private readonly object _syncRoot = new();
        private Task _pendingTask = Task.CompletedTask;

        public void Report(T value)
        {
            lock (_syncRoot)
            {
                _pendingTask = DispatchAfterAsync(_pendingTask, value);
            }
        }

        public Task WaitForCompletionAsync()
        {
            lock (_syncRoot)
            {
                return _pendingTask;
            }
        }

        private async Task DispatchAfterAsync(Task previousTask, T value)
        {
            await previousTask.ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() => _report(value)).ConfigureAwait(false);
        }
    }

    private void UpdateAnalysisStage(CoursewareAnalysisEvent analysisEvent)
    {
        var existingIndex = -1;
        for (var index = 0; index < AnalysisEvents.Count; index++)
        {
            if (AnalysisEvents[index].Stage == analysisEvent.Stage)
            {
                existingIndex = index;
                break;
            }
        }

        if (existingIndex >= 0)
        {
            AnalysisEvents[existingIndex] = analysisEvent;
            return;
        }

        var insertIndex = 0;
        while (insertIndex < AnalysisEvents.Count && AnalysisEvents[insertIndex].Stage < analysisEvent.Stage)
        {
            insertIndex++;
        }

        AnalysisEvents.Insert(insertIndex, analysisEvent);
    }

    private void ApplyThemePresentation(CoursewareTheme theme)
    {
        ThemeColors.Clear();
        foreach (var color in theme.ColorSuggestions)
        {
            ThemeColors.Add(new CoursewareThemeColorViewModel(color.Name, color.Usage, color.Hex));
        }

        TypographyLevels.Clear();
        TypographyLevels.Add(new CoursewareTypographyLevelViewModel(CoursewareUiStrings.TypographyChineseFontLabel, theme.Fonts.Chinese));
        TypographyLevels.Add(new CoursewareTypographyLevelViewModel(CoursewareUiStrings.TypographyWesternFontLabel, theme.Fonts.Western));
        TypographyLevels.Add(new CoursewareTypographyLevelViewModel(CoursewareUiStrings.TypographyFontSizeRulesLabel, theme.FontSizeRules));
        ReplaceItems(LayoutPrinciples, [theme.LayoutPrinciples]);
    }

    private void ClearAnalysisPresentation()
    {
        AnalysisEvents.Clear();
        AnalysisChatMessages.Clear();
        ThemeColors.Clear();
        TypographyLevels.Clear();
        LayoutPrinciples.Clear();
        OnPropertyChanged(nameof(FontRecommendationText));
        OnPropertyChanged(nameof(SafeAreaText));
        OnPropertyChanged(nameof(ColorRationale));
        OnPropertyChanged(nameof(ThemeStyle));
        OnPropertyChanged(nameof(SpacingAndVisualEffects));
        OnPropertyChanged(nameof(ThemeLayoutPrinciples));
        OnPropertyChanged(nameof(CoverPageSlideMl));
        OnPropertyChanged(nameof(ContentPageSlideMl));
        OnPropertyChanged(nameof(AnalysisWarnings));
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void ResetCourseware()
    {
        _workflowCancellationTokenSource?.Cancel();
        DisposeSlideWorkspace();
        CurrentPage = CoursewareApplicationPage.CoursewareAnalysis;
        CoursewareSession = null;
        CoursewareThumbnails.Clear();
        ClearAnalysisPresentation();
        LoadErrorMessage = null;
        LoadErrorDetails = null;
        WorkspaceState = CoursewareWorkspaceState.Welcome;
    }

    private async Task EnterWorkspaceAsync()
    {
        var workspace = SlideWorkspace;
        if (workspace is null || !CanEnterWorkspace())
        {
            return;
        }

        await _dispatcher.InvokeAsync(() => CurrentPage = CoursewareApplicationPage.SlideWorkspace);
        await workspace.ActivateAsync().ConfigureAwait(false);
    }

    private bool CanEnterWorkspace()
    {
        return SlideWorkspace is not null
            && WorkspaceState is CoursewareWorkspaceState.AnalysisReady
                or CoursewareWorkspaceState.AnalyzingCourseware
                or CoursewareWorkspaceState.AnalysisFailed
                or CoursewareWorkspaceState.Canceled;
    }

    private void ReturnToAnalysis()
    {
        SlideWorkspace?.Deactivate();
        CurrentPage = CoursewareApplicationPage.CoursewareAnalysis;
    }

    private void ReplaceSlideWorkspace(CoursewareSlideWorkspaceViewModel workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        DisposeSlideWorkspace();
        SlideWorkspace = workspace;
    }

    private void DisposeSlideWorkspace()
    {
        var workspace = SlideWorkspace;
        SlideWorkspace = null;
        workspace?.Dispose();
    }

    private void HandleUnexpectedCommandException(Exception exception)
    {
        LoadErrorMessage = exception.Message;
        LoadErrorDetails = exception.ToString();
        WorkspaceState = CoursewareSession is null
            ? CoursewareWorkspaceState.LoadFailed
            : CoursewareWorkspaceState.AnalysisFailed;
    }

    private static void CancelIfActive(CancellationTokenSource? cancellationTokenSource)
    {
        if (cancellationTokenSource is null)
        {
            return;
        }

        try
        {
            cancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

}

/// <summary>
/// Represents a color in the analyzed theme.
/// </summary>
public sealed record CoursewareThemeColorViewModel(string Name, string Usage, string Hex);

/// <summary>
/// Represents one level in the analyzed typography hierarchy.
/// </summary>
public sealed record CoursewareTypographyLevelViewModel(string Name, string Specification);


