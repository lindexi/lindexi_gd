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
    private readonly CoursewareFolderLoader _coursewareFolderLoader;
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
    public CoursewareWorkspaceViewModel(
        CoursewareFolderLoader coursewareFolderLoader,
        IViewModelDispatcher? dispatcher = null,
        ICoursewareThemeAnalysisService? themeAnalysisService = null,
        ISlideChatManagerFactory? slideChatManagerFactory = null,
        CoursewareSlideSummaryService? slideSummaryService = null,
        ICoursewareSlidePromptBuilder? slidePromptBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(coursewareFolderLoader);

        _coursewareFolderLoader = coursewareFolderLoader;
        _themeAnalysisService = themeAnalysisService ?? new CoursewareThemeAnalysisService();
        _slideChatManagerFactory = slideChatManagerFactory ?? new SlideChatManagerFactory();
        _slideSummaryService = slideSummaryService ?? new CoursewareSlideSummaryService();
        _slidePromptBuilder = slidePromptBuilder ?? new CoursewareSlidePromptBuilder(
            _slideSummaryService,
            new CoursewareThemePageDesignAdapter());
        _dispatcher = dispatcher ?? WpfViewModelDispatcher.Instance;
        CoursewareThumbnails = new ObservableCollection<CoursewareThumbnailItemViewModel>();
        ThemeColors = new ObservableCollection<CoursewareThemeColorViewModel>();
        TypographyLevels = new ObservableCollection<CoursewareTypographyLevelViewModel>();
        LayoutPrinciples = new ObservableCollection<string>();
        PageTypeRecommendations = new ObservableCollection<CoursewarePageTypeRecommendationViewModel>();
        ContentRules = new ObservableCollection<CoursewareThemeRuleViewModel>();
        StyleKeywords = new ObservableCollection<string>();
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
    public string ThemeTitle => CoursewareSession?.ThemeAnalysisResult?.ThemeTitle ?? "正在形成课件主题";

    /// <summary>
    /// Gets the generated courseware theme summary.
    /// </summary>
    public string ThemeDescription => CoursewareSession?.ThemeAnalysisResult?.ThemeDescription ?? "分析完成后将在这里展示整份课件的统一视觉主题。";

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
                OnPropertyChanged(nameof(PageGenerationCapabilityText));
                _enterWorkspaceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the full-text analysis capability status.
    /// </summary>
    public string TextAnalysisCapabilityText => FormatCapabilityStatus(
        CoursewareSession?.ThemeAnalysisResult?.CapabilityStates.TextAnalysis ?? CoursewareCapabilityStatus.NotRequested);

    /// <summary>
    /// Gets the theme-suggestion capability status.
    /// </summary>
    public string ThemeSuggestionCapabilityText => FormatCapabilityStatus(
        CoursewareSession?.ThemeAnalysisResult?.CapabilityStates.ThemeSuggestion ?? CoursewareCapabilityStatus.NotRequested);

    /// <summary>
    /// Gets the executable design-system capability status.
    /// </summary>
    public string DesignSystemCapabilityText => FormatCapabilityStatus(
        CoursewareSession?.ThemeAnalysisResult?.CapabilityStates.DesignSystem ?? CoursewareCapabilityStatus.NotRequested);

    /// <summary>
    /// Gets the template-validation capability status.
    /// </summary>
    public string TemplateValidationCapabilityText => FormatCapabilityStatus(
        CoursewareSession?.ThemeAnalysisResult?.CapabilityStates.TemplateValidation ?? CoursewareCapabilityStatus.NotRequested);

    /// <summary>
    /// Gets the visual-analysis capability status.
    /// </summary>
    public string VisualAnalysisCapabilityText => FormatCapabilityStatus(
        CoursewareSession?.ThemeAnalysisResult?.CapabilityStates.VisualAnalysis ?? CoursewareCapabilityStatus.NotRequested);

    /// <summary>
    /// Gets the real page-generation capability status.
    /// </summary>
    public string PageGenerationCapabilityText
    {
        get
        {
            var summary = SlideWorkspace?.Summary;
            if (summary is null || summary.TotalCount == 0)
            {
                return "尚未生成";
            }

            if (summary.InProgressCount > 0)
            {
                return $"正在处理 {summary.InProgressCount} 页";
            }

            if (summary.CompletedCount == summary.TotalCount)
            {
                return "全部页面已完成";
            }

            if (summary.FailedCount > 0)
            {
                return $"存在 {summary.FailedCount} 页失败";
            }

            if (summary.CanceledCount > 0 && summary.CompletedCount == 0)
            {
                return $"已取消 {summary.CanceledCount} 页";
            }

            if (summary.CompletedCount + summary.FailedCount + summary.CanceledCount == 0)
            {
                return "尚未生成";
            }

            return $"已完成 {summary.CompletedCount} / {summary.TotalCount} 页";
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
                OnPropertyChanged(nameof(TextAnalysisCapabilityText));
                OnPropertyChanged(nameof(ThemeSuggestionCapabilityText));
                OnPropertyChanged(nameof(DesignSystemCapabilityText));
                OnPropertyChanged(nameof(TemplateValidationCapabilityText));
                OnPropertyChanged(nameof(VisualAnalysisCapabilityText));
                OnPropertyChanged(nameof(PageGenerationCapabilityText));
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
        var cancellationTokenSource = Interlocked.Exchange(ref _workflowCancellationTokenSource, null);
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
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
    /// Gets the recommendations for each slide type.
    /// </summary>
    public ObservableCollection<CoursewarePageTypeRecommendationViewModel> PageTypeRecommendations { get; }

    /// <summary>
    /// Gets the content presentation rules.
    /// </summary>
    public ObservableCollection<CoursewareThemeRuleViewModel> ContentRules { get; }

    /// <summary>
    /// Gets style keywords produced by the active theme analysis.
    /// </summary>
    public ObservableCollection<string> StyleKeywords { get; }

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
                : $"中文标题：{fonts.EastAsianHeading} · 中文正文：{fonts.EastAsianBody} · 西文：{fonts.LatinHeading} / {fonts.LatinBody}";
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
                : $"左 {safeArea.Left:0.#} · 上 {safeArea.Top:0.#} · 右 {safeArea.Right:0.#} · 下 {safeArea.Bottom:0.#}";
        }
    }

    /// <summary>
    /// Gets the color-scheme rationale.
    /// </summary>
    public string ColorRationale => CoursewareSession?.ThemeAnalysisResult?.Theme.Colors.Rationale ?? "分析完成后显示配色依据。";

    /// <summary>
    /// Gets combined input and analysis warnings.
    /// </summary>
    public IReadOnlyList<string> AnalysisWarnings => CoursewareSession?.ThemeAnalysisResult?.Warnings
        ?? CoursewareSession?.InputPackage.Warnings.Select(warning => warning.Message).ToArray()
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
        CoursewareWorkspaceState.LoadingCourseware => "正在解析清单、Markdown、资源和截图",
        CoursewareWorkspaceState.AnalyzingCourseware => $"已读取 {CoursewareThumbnails.Count} 页，正在归纳内容层级、配色、字体与版式",
        CoursewareWorkspaceState.AnalysisReady => string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            CoursewareUiStrings.AnalysisReadyCaptionFormat,
            CoursewareThumbnails.Count),
        CoursewareWorkspaceState.LoadFailed => "请选择有效的课件 Markdown 导出文件夹",
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
        previousCancellationTokenSource?.Cancel();
        previousCancellationTokenSource?.Dispose();

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
            var package = await _coursewareFolderLoader.LoadAsync(folderPath, cancellationTokenSource.Token).ConfigureAwait(true);
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
                WorkspaceState = CoursewareWorkspaceState.AnalyzingCourseware;
                OnPropertyChanged(nameof(SlideCountText));
                OnPropertyChanged(nameof(InputHealthText));
            });

            var progress = CreateAnalysisProgress(package, cancellationTokenSource);
            var analysisResult = await _themeAnalysisService.AnalyzeAsync(
                package,
                progress,
                CreateAnalysisMessageProgress(package, cancellationTokenSource),
                cancellationTokenSource.Token).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() => PublishAnalysisResult(package, cancellationTokenSource, analysisResult));
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                if (ReferenceEquals(_workflowCancellationTokenSource, cancellationTokenSource) && CoursewareSession is not null)
                {
                    WorkspaceState = CoursewareWorkspaceState.Canceled;
                }
            });
        }
        catch (Exception ex)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                if (!ReferenceEquals(_workflowCancellationTokenSource, cancellationTokenSource))
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
            if (ReferenceEquals(Interlocked.CompareExchange(ref _workflowCancellationTokenSource, null, cancellationTokenSource), cancellationTokenSource))
            {
                cancellationTokenSource.Dispose();
            }
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
        previousCancellationTokenSource?.Cancel();
        previousCancellationTokenSource?.Dispose();

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
            var progress = CreateAnalysisProgress(session.InputPackage, cancellationTokenSource);
            var analysisResult = await _themeAnalysisService.AnalyzeAsync(
                session.InputPackage,
                progress,
                CreateAnalysisMessageProgress(session.InputPackage, cancellationTokenSource),
                cancellationTokenSource.Token).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() => PublishAnalysisResult(session.InputPackage, cancellationTokenSource, analysisResult));
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                if (ReferenceEquals(_workflowCancellationTokenSource, cancellationTokenSource))
                {
                    WorkspaceState = CoursewareWorkspaceState.Canceled;
                }
            });
        }
        catch (Exception ex)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                if (!ReferenceEquals(_workflowCancellationTokenSource, cancellationTokenSource))
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
            if (ReferenceEquals(Interlocked.CompareExchange(ref _workflowCancellationTokenSource, null, cancellationTokenSource), cancellationTokenSource))
            {
                cancellationTokenSource.Dispose();
            }
        }
    }

    private void PublishAnalysisResult(
        CoursewareInputPackage inputPackage,
        CancellationTokenSource workflowCancellationTokenSource,
        CoursewareThemeAnalysisResult analysisResult)
    {
        if (CoursewareSession is null
            || !ReferenceEquals(CoursewareSession.InputPackage, inputPackage)
            || !ReferenceEquals(_workflowCancellationTokenSource, workflowCancellationTokenSource))
        {
            return;
        }

        CoursewareSession.ThemeAnalysisResult = analysisResult;
        ApplyThemePresentation(analysisResult.Theme);
        if (SlideWorkspace is null)
        {
            ReplaceSlideWorkspace(new CoursewareSlideWorkspaceViewModel(
                CoursewareSession,
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
        OnPropertyChanged(nameof(AnalysisWarnings));
        OnPropertyChanged(nameof(TextAnalysisCapabilityText));
        OnPropertyChanged(nameof(ThemeSuggestionCapabilityText));
        OnPropertyChanged(nameof(DesignSystemCapabilityText));
        OnPropertyChanged(nameof(TemplateValidationCapabilityText));
        OnPropertyChanged(nameof(VisualAnalysisCapabilityText));
        OnPropertyChanged(nameof(PageGenerationCapabilityText));
    }

    private IProgress<CoursewareAnalysisEvent> CreateAnalysisProgress(
        CoursewareInputPackage inputPackage,
        CancellationTokenSource workflowCancellationTokenSource)
    {
        return new AnalysisProgress<CoursewareAnalysisEvent>(analysisEvent =>
        {
            _ = _dispatcher.InvokeAsync(() =>
            {
                if (CoursewareSession is null
                    || !ReferenceEquals(CoursewareSession.InputPackage, inputPackage)
                    || !ReferenceEquals(_workflowCancellationTokenSource, workflowCancellationTokenSource))
                {
                    return;
                }

                UpdateAnalysisStage(analysisEvent);
            });
        });
    }

    private IProgress<CopilotChatMessage> CreateAnalysisMessageProgress(
        CoursewareInputPackage inputPackage,
        CancellationTokenSource workflowCancellationTokenSource)
    {
        return new AnalysisProgress<CopilotChatMessage>(message =>
        {
            _ = _dispatcher.InvokeAsync(() =>
            {
                if (CoursewareSession is null
                    || !ReferenceEquals(CoursewareSession.InputPackage, inputPackage)
                    || !ReferenceEquals(_workflowCancellationTokenSource, workflowCancellationTokenSource))
                {
                    return;
                }

                AnalysisChatMessages.Add(message);
            });
        });
    }

    private sealed class AnalysisProgress<T>(Action<T> report) : IProgress<T>
    {
        private readonly Action<T> _report = report;

        public void Report(T value)
        {
            _report(value);
        }
    }

    private static string FormatCapabilityStatus(CoursewareCapabilityStatus status)
    {
        return status switch
        {
            CoursewareCapabilityStatus.Passed => CoursewareUiStrings.CapabilityPassed,
            CoursewareCapabilityStatus.NotRequested => CoursewareUiStrings.CapabilityNotRequested,
            CoursewareCapabilityStatus.NotSupported => CoursewareUiStrings.CapabilityNotSupported,
            CoursewareCapabilityStatus.Failed => CoursewareUiStrings.CapabilityFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, CoursewareUiStrings.UnknownCapabilityStatus),
        };
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
        foreach (var color in theme.Colors.EnumerateColors())
        {
            ThemeColors.Add(new CoursewareThemeColorViewModel(color.Usage, color.Name, color.HexValue));
        }

        TypographyLevels.Clear();
        foreach (var level in theme.Typography.EnumerateLevels())
        {
            TypographyLevels.Add(new CoursewareTypographyLevelViewModel(
                level.Name,
                $"{level.FontSize:0.#} / {level.FontWeight}",
                level.Purpose));
        }

        ReplaceItems(StyleKeywords, theme.StyleKeywords);
        ReplaceItems(LayoutPrinciples, theme.LayoutPrinciples);

        PageTypeRecommendations.Clear();
        var recommendationNumber = 1;
        foreach (var recommendation in theme.PageTypes.EnumerateRecommendations())
        {
            PageTypeRecommendations.Add(new CoursewarePageTypeRecommendationViewModel(
                recommendation.Name,
                recommendationNumber.ToString("00"),
                recommendation.Description));
            recommendationNumber++;
        }

        ContentRules.Clear();
        foreach (var rule in theme.ContentPresentationRules)
        {
            ContentRules.Add(new CoursewareThemeRuleViewModel("内容呈现", rule));
        }
    }

    private void ClearAnalysisPresentation()
    {
        AnalysisEvents.Clear();
        AnalysisChatMessages.Clear();
        StyleKeywords.Clear();
        ThemeColors.Clear();
        TypographyLevels.Clear();
        LayoutPrinciples.Clear();
        PageTypeRecommendations.Clear();
        ContentRules.Clear();
        OnPropertyChanged(nameof(FontRecommendationText));
        OnPropertyChanged(nameof(SafeAreaText));
        OnPropertyChanged(nameof(ColorRationale));
        OnPropertyChanged(nameof(AnalysisWarnings));
        OnPropertyChanged(nameof(TextAnalysisCapabilityText));
        OnPropertyChanged(nameof(ThemeSuggestionCapabilityText));
        OnPropertyChanged(nameof(DesignSystemCapabilityText));
        OnPropertyChanged(nameof(TemplateValidationCapabilityText));
        OnPropertyChanged(nameof(VisualAnalysisCapabilityText));
        OnPropertyChanged(nameof(PageGenerationCapabilityText));
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

        CurrentPage = CoursewareApplicationPage.SlideWorkspace;
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
        workspace.PropertyChanged += OnSlideWorkspacePropertyChanged;
    }

    private void DisposeSlideWorkspace()
    {
        var workspace = SlideWorkspace;
        if (workspace is not null)
        {
            workspace.PropertyChanged -= OnSlideWorkspacePropertyChanged;
        }

        SlideWorkspace = null;
        workspace?.Dispose();
    }

    private void OnSlideWorkspacePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CoursewareSlideWorkspaceViewModel.Summary)
            or nameof(CoursewareSlideWorkspaceViewModel.SummaryText))
        {
            OnPropertyChanged(nameof(PageGenerationCapabilityText));
        }
    }

    private void HandleUnexpectedCommandException(Exception exception)
    {
        _ = _dispatcher.InvokeAsync(() =>
        {
            LoadErrorMessage = exception.Message;
            LoadErrorDetails = exception.ToString();
            WorkspaceState = CoursewareSession is null
                ? CoursewareWorkspaceState.LoadFailed
                : CoursewareWorkspaceState.AnalysisFailed;
        });
    }

}

/// <summary>
/// Represents a color in the analyzed theme.
/// </summary>
public sealed record CoursewareThemeColorViewModel(string Usage, string Name, string HexValue);

/// <summary>
/// Represents one level in the analyzed typography hierarchy.
/// </summary>
public sealed record CoursewareTypographyLevelViewModel(string Name, string Specification, string Purpose);

/// <summary>
/// Represents the recommendation for one slide type.
/// </summary>
public sealed record CoursewarePageTypeRecommendationViewModel(string Name, string Number, string Description);

/// <summary>
/// Represents one content presentation rule.
/// </summary>
public sealed record CoursewareThemeRuleViewModel(string Title, string Description);

