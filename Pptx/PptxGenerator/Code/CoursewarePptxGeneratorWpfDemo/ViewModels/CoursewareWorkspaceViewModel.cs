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
/// Identifies the page displayed by the prototype shell.
/// </summary>
public enum PrototypePage
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
public sealed class CoursewareWorkspaceViewModel : ObservableObject
{
    private const int DemoSlideCount = 60;
    private readonly CoursewareFolderLoader _coursewareFolderLoader;
    private readonly ICoursewareThemeAnalysisService _themeAnalysisService;
    private readonly IViewModelDispatcher _dispatcher;
    private readonly RelayCommand _enterWorkspaceCommand;
    private readonly RelayCommand _reanalyzeCommand;
    private readonly RelayCommand _cancelAnalysisCommand;
    private DemoSlideViewModel? _selectedSlide;
    private string _demoInputText = string.Empty;
    private PrototypePage _currentPage = PrototypePage.CoursewareAnalysis;
    private CoursewareAnalysisTab _selectedAnalysisTab = CoursewareAnalysisTab.Conversation;
    private CoursewareWorkspaceState _workspaceState = CoursewareWorkspaceState.Welcome;
    private CoursewareWorkspaceSession? _coursewareSession;
    private string? _loadErrorMessage;
    private string? _loadErrorDetails;
    private CancellationTokenSource? _workflowCancellationTokenSource;

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
    public CoursewareWorkspaceViewModel(
        CoursewareFolderLoader coursewareFolderLoader,
        IViewModelDispatcher? dispatcher = null,
        ICoursewareThemeAnalysisService? themeAnalysisService = null)
    {
        ArgumentNullException.ThrowIfNull(coursewareFolderLoader);

        _coursewareFolderLoader = coursewareFolderLoader;
        _themeAnalysisService = themeAnalysisService ?? new CoursewareThemeAnalysisService();
        _dispatcher = dispatcher ?? WpfViewModelDispatcher.Instance;
        Slides = new ObservableCollection<DemoSlideViewModel>(CreateSlides());
        CoursewareThumbnails = new ObservableCollection<CoursewareThumbnailItemViewModel>();
        ThemeColors = new ObservableCollection<DemoThemeColorViewModel>();
        TypographyLevels = new ObservableCollection<DemoTypographyLevelViewModel>();
        LayoutPrinciples = new ObservableCollection<string>();
        PageTypeRecommendations = new ObservableCollection<DemoPageTypeRecommendationViewModel>();
        ContentRules = new ObservableCollection<DemoThemeRuleViewModel>();
        StyleKeywords = new ObservableCollection<string>();
        AnalysisEvents = new ObservableCollection<CoursewareAnalysisEvent>();
        AnalysisChatMessages = new ObservableCollection<CopilotChatMessage>();
        ChatMessages = new ObservableCollection<DemoChatMessageViewModel>(
        [
            new("Copilot", "我已读取当前页面内容和全局主题。可以继续调整例题讲解结构、视觉层级或生成新的 SlideML。", false),
            new("你", "请让公式推导更清晰，并突出本页的核心结论。", true),
            new("Copilot", "已按全局主题强化标题层级，将推导拆分为三个步骤，并使用强调色突出核心结论。", false),
        ]);

        _selectedSlide = Slides[4];
        _enterWorkspaceCommand = new RelayCommand(
            _ => CurrentPage = PrototypePage.SlideWorkspace,
            _ => WorkspaceState == CoursewareWorkspaceState.AnalysisReady);
        _reanalyzeCommand = new RelayCommand(
            _ => _ = ReanalyzeAsync(),
            _ => CoursewareSession is not null && WorkspaceState is CoursewareWorkspaceState.AnalysisReady or CoursewareWorkspaceState.AnalysisFailed or CoursewareWorkspaceState.Canceled);
        _cancelAnalysisCommand = new RelayCommand(
            _ => _workflowCancellationTokenSource?.Cancel(),
            _ => WorkspaceState == CoursewareWorkspaceState.AnalyzingCourseware);
        BackToAnalysisCommand = new RelayCommand(_ => CurrentPage = PrototypePage.CoursewareAnalysis);
        OpenDemoCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is string folderPath)
                {
                    _ = OpenCoursewareFolderAsync(folderPath);
                }
            },
            _ => !IsCoursewareLoading);
        OpenOtherCoursewareCommand = new RelayCommand(_ => ResetCourseware());
        ShowDemoStageCommand = new RelayCommand(ShowDemoStage);
        SendDemoMessageCommand = new RelayCommand(_ => AddDemoReply());
    }

    /// <summary>
    /// Gets the courseware title shown by the prototype.
    /// </summary>
    public string CoursewareTitle => CoursewareSession?.InputPackage.CoursewareName ?? "尚未打开课件";

    /// <summary>
    /// Gets the prototype product title.
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
    /// Gets the rendering log for the selected page.
    /// </summary>
    public string RenderingLog => CoursewareUiStrings.PrototypeRenderingLog;

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
    public string PageGenerationCapabilityText => FormatCapabilityStatus(
        CoursewareSession?.ThemeAnalysisResult?.CapabilityStates.PageGeneration ?? CoursewareCapabilityStatus.NotRequested);

    /// <summary>
    /// Gets the presentation thumbnails and workspace pages.
    /// </summary>
    public ObservableCollection<DemoSlideViewModel> Slides { get; }

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
    public ObservableCollection<DemoThemeColorViewModel> ThemeColors { get; }

    /// <summary>
    /// Gets the demonstration typography hierarchy.
    /// </summary>
    public ObservableCollection<DemoTypographyLevelViewModel> TypographyLevels { get; }

    /// <summary>
    /// Gets the recommended layout principles.
    /// </summary>
    public ObservableCollection<string> LayoutPrinciples { get; }

    /// <summary>
    /// Gets the demonstration recommendations for each slide type.
    /// </summary>
    public ObservableCollection<DemoPageTypeRecommendationViewModel> PageTypeRecommendations { get; }

    /// <summary>
    /// Gets the demonstration rules for mathematical content presentation.
    /// </summary>
    public ObservableCollection<DemoThemeRuleViewModel> ContentRules { get; }

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
    /// Gets the demonstration chat messages for the slide workspace.
    /// </summary>
    public ObservableCollection<DemoChatMessageViewModel> ChatMessages { get; }

    /// <summary>
    /// Gets or sets the selected slide in the workspace.
    /// </summary>
    public DemoSlideViewModel? SelectedSlide
    {
        get => _selectedSlide;
        set
        {
            if (SetProperty(ref _selectedSlide, value))
            {
                OnPropertyChanged(nameof(SelectedSlideNumberText));
                OnPropertyChanged(nameof(SelectedSlideSummary));
                OnPropertyChanged(nameof(SelectedSlideXml));
            }
        }
    }

    /// <summary>
    /// Gets the current slide number shown in the workspace header.
    /// </summary>
    public string SelectedSlideNumberText => SelectedSlide is null
        ? "尚未选择页面"
        : $"第 {SelectedSlide.Number} / {Slides.Count} 页";

    /// <summary>
    /// Gets the demonstration summary for the selected slide.
    /// </summary>
    public string SelectedSlideSummary => SelectedSlide is null
        ? "请选择课件页面。"
        : string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            CoursewareUiStrings.PrototypeSlideSummaryFormat,
            SelectedSlide.Title);

    /// <summary>
    /// Gets the demonstration SlideML content for the selected slide.
    /// </summary>
    public string SelectedSlideXml => SelectedSlide is null
        ? string.Empty
        : $"<Slide Width=\"1280\" Height=\"720\">{Environment.NewLine}" +
          $"  <Title Text=\"{SelectedSlide.Title}\" Theme=\"MathReasoning\" />{Environment.NewLine}" +
          $"  <Content Layout=\"GeometrySteps\" Accent=\"#F29A38\" />{Environment.NewLine}" +
          "</Slide>";

    /// <summary>
    /// Gets or sets the demonstration chat input.
    /// </summary>
    public string DemoInputText
    {
        get => _demoInputText;
        set => SetProperty(ref _demoInputText, value);
    }

    /// <summary>
    /// Gets or sets the page displayed by the prototype shell.
    /// </summary>
    public PrototypePage CurrentPage
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
    public bool IsAnalysisPage => CurrentPage == PrototypePage.CoursewareAnalysis;

    /// <summary>
    /// Gets a value indicating whether the slide workspace is visible.
    /// </summary>
    public bool IsWorkspacePage => CurrentPage == PrototypePage.SlideWorkspace;

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
    public ICommand EnterWorkspaceCommand => _enterWorkspaceCommand;

    /// <summary>
    /// Gets the command that returns to the courseware analysis page.
    /// </summary>
    public ICommand BackToAnalysisCommand { get; }

    /// <summary>
    /// Gets the command that opens the built-in demonstration courseware.
    /// </summary>
    public ICommand OpenDemoCommand { get; }

    /// <summary>
    /// Gets the command that cancels the active theme analysis.
    /// </summary>
    public ICommand CancelAnalysisCommand => _cancelAnalysisCommand;

    /// <summary>
    /// Gets the command that restarts the demonstration analysis.
    /// </summary>
    public ICommand ReanalyzeCommand => _reanalyzeCommand;

    /// <summary>
    /// Gets the command that retries the failed demonstration analysis.
    /// </summary>
    public ICommand RetryDemoCommand => _reanalyzeCommand;

    /// <summary>
    /// Gets the command that resets the prototype to another demonstration courseware.
    /// </summary>
    public ICommand OpenOtherCoursewareCommand { get; }

    /// <summary>
    /// Gets the command that directly previews a prototype analysis state.
    /// </summary>
    public ICommand ShowDemoStageCommand { get; }

    /// <summary>
    /// Gets the command that appends a demonstration chat response.
    /// </summary>
    public ICommand SendDemoMessageCommand { get; }

    /// <summary>
    /// Loads the selected courseware export folder without starting theme analysis or page generation.
    /// </summary>
    /// <param name="folderPath">The selected courseware export folder.</param>
    /// <returns>A task that represents the loading operation.</returns>
    public async Task OpenCoursewareFolderAsync(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        var previousCancellationTokenSource = Interlocked.Exchange(ref _workflowCancellationTokenSource, cancellationTokenSource);
        previousCancellationTokenSource?.Cancel();
        previousCancellationTokenSource?.Dispose();

        await _dispatcher.InvokeAsync(() =>
        {
            WorkspaceState = CoursewareWorkspaceState.LoadingCourseware;
            LoadErrorMessage = null;
            LoadErrorDetails = null;
        });

        try
        {
            var package = await _coursewareFolderLoader.LoadAsync(folderPath, cancellationTokenSource.Token).ConfigureAwait(true);
            var analysisRunId = Guid.NewGuid();
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

                CoursewareSession = new CoursewareWorkspaceSession(package)
                {
                    ActiveAnalysisRunId = analysisRunId,
                    AnalysisStartedAt = DateTimeOffset.UtcNow,
                };
                ClearAnalysisPresentation();
                WorkspaceState = CoursewareWorkspaceState.AnalyzingCourseware;
                OnPropertyChanged(nameof(SlideCountText));
                OnPropertyChanged(nameof(InputHealthText));
            });

            var progress = CreateAnalysisProgress(package, analysisRunId);
            var analysisResult = await _themeAnalysisService.AnalyzeAsync(
                package,
                progress,
                CreateAnalysisMessageProgress(package, analysisRunId),
                cancellationTokenSource.Token).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() => PublishAnalysisResult(package, analysisRunId, analysisResult));
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
        catch (Exception ex) when (ex is DirectoryNotFoundException or FileNotFoundException or InvalidDataException or IOException or UnauthorizedAccessException or JsonException)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                CoursewareSession = null;
                CoursewareThumbnails.Clear();
                LoadErrorMessage = ex.Message;
                LoadErrorDetails = ex.ToString();
                WorkspaceState = CoursewareWorkspaceState.LoadFailed;
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
        {
            await _dispatcher.InvokeAsync(() =>
            {
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

    private async Task ReanalyzeAsync()
    {
        var session = CoursewareSession;
        if (session is null)
        {
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        var analysisRunId = Guid.NewGuid();
        var previousCancellationTokenSource = Interlocked.Exchange(ref _workflowCancellationTokenSource, cancellationTokenSource);
        previousCancellationTokenSource?.Cancel();
        previousCancellationTokenSource?.Dispose();

        await _dispatcher.InvokeAsync(() =>
        {
            session.ThemeAnalysisResult = null;
            session.ActiveAnalysisRunId = analysisRunId;
            session.AnalysisStartedAt = DateTimeOffset.UtcNow;
            ClearAnalysisPresentation();
            LoadErrorMessage = null;
            LoadErrorDetails = null;
            WorkspaceState = CoursewareWorkspaceState.AnalyzingCourseware;
            OnPropertyChanged(nameof(ShowsThemeResult));
        });

        try
        {
            var progress = CreateAnalysisProgress(session.InputPackage, analysisRunId);
            var analysisResult = await _themeAnalysisService.AnalyzeAsync(
                session.InputPackage,
                progress,
                CreateAnalysisMessageProgress(session.InputPackage, analysisRunId),
                cancellationTokenSource.Token).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() => PublishAnalysisResult(session.InputPackage, analysisRunId, analysisResult));
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
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
        {
            await _dispatcher.InvokeAsync(() =>
            {
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
        Guid analysisRunId,
        CoursewareThemeAnalysisResult analysisResult)
    {
        if (CoursewareSession is null
            || !ReferenceEquals(CoursewareSession.InputPackage, inputPackage)
            || CoursewareSession.ActiveAnalysisRunId != analysisRunId)
        {
            return;
        }

        CoursewareSession.ThemeAnalysisResult = analysisResult;
        ApplyThemePresentation(analysisResult.Theme);
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
        Guid analysisRunId)
    {
        return new AnalysisProgress<CoursewareAnalysisEvent>(analysisEvent =>
        {
            _ = _dispatcher.InvokeAsync(() =>
            {
                if (CoursewareSession is null
                    || !ReferenceEquals(CoursewareSession.InputPackage, inputPackage)
                    || CoursewareSession.ActiveAnalysisRunId != analysisRunId)
                {
                    return;
                }

                UpdateAnalysisStage(analysisEvent);
            });
        });
    }

    private IProgress<CopilotChatMessage> CreateAnalysisMessageProgress(
        CoursewareInputPackage inputPackage,
        Guid analysisRunId)
    {
        return new AnalysisProgress<CopilotChatMessage>(message =>
        {
            _ = _dispatcher.InvokeAsync(() =>
            {
                if (CoursewareSession is null
                    || !ReferenceEquals(CoursewareSession.InputPackage, inputPackage)
                    || CoursewareSession.ActiveAnalysisRunId != analysisRunId)
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
            ThemeColors.Add(new DemoThemeColorViewModel(color.Usage, color.Name, color.HexValue));
        }

        TypographyLevels.Clear();
        foreach (var level in theme.Typography.EnumerateLevels())
        {
            TypographyLevels.Add(new DemoTypographyLevelViewModel(
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
            PageTypeRecommendations.Add(new DemoPageTypeRecommendationViewModel(
                recommendation.Name,
                recommendationNumber.ToString("00"),
                recommendation.Description));
            recommendationNumber++;
        }

        ContentRules.Clear();
        foreach (var rule in theme.ContentPresentationRules)
        {
            ContentRules.Add(new DemoThemeRuleViewModel("内容呈现", rule));
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

    private static IEnumerable<DemoSlideViewModel> CreateSlides()
    {
        var titles = new[]
        {
            "封面", "目录", "章节导入", "学习目标", "全等三角形", "性质探究", "判定方法", "例题解析",
            "课堂练习", "知识梳理", "几何证明", "课堂活动", "性质应用", "方法总结", "综合训练", "易错辨析",
            "拓展思考", "小组讨论", "课堂检测", "本章小结", "作业布置", "学习评价", "拓展阅读", "结束页",
        };

        for (var index = 0; index < DemoSlideCount; index++)
        {
            var title = titles[index % titles.Length];
            yield return new DemoSlideViewModel(index + 1, title, index % 8, index is 9 or 14 or 22 or 35 or 47 or 58);
        }
    }

    private void ResetCourseware()
    {
        _workflowCancellationTokenSource?.Cancel();
        CoursewareSession = null;
        CoursewareThumbnails.Clear();
        ClearAnalysisPresentation();
        LoadErrorMessage = null;
        LoadErrorDetails = null;
        WorkspaceState = CoursewareWorkspaceState.Welcome;
    }

    private void ShowDemoStage(object? parameter)
    {
        if (parameter is string stageName && Enum.TryParse<CoursewareWorkspaceState>(stageName, out var stage))
        {
            WorkspaceState = stage;
        }
    }

    private void AddDemoReply()
    {
        var message = string.IsNullOrWhiteSpace(DemoInputText)
            ? "请继续优化当前页面的视觉层级。"
            : DemoInputText.Trim();
        DemoInputText = string.Empty;

        ChatMessages.Add(new DemoChatMessageViewModel("你", message, true));
        ChatMessages.Add(new DemoChatMessageViewModel(
            "Copilot",
            "已记录本次演示调整。真实生成流程尚未接入，当前页面仍会沿用已确认的全局主题、字体层级与安全区。",
            false));
    }
}

/// <summary>
/// Represents a demonstration courseware slide.
/// </summary>
public sealed record DemoSlideViewModel(int Number, string Title, int LayoutVariant, bool HasWarning)
{
    /// <summary>
    /// Gets the accessible slide label.
    /// </summary>
    public string AccessibleName => $"第 {Number} 页，{Title}";

    /// <summary>
    /// Gets the short page number label.
    /// </summary>
    public string NumberText => Number.ToString("00");
}

/// <summary>
/// Represents a color in the demonstration theme.
/// </summary>
public sealed record DemoThemeColorViewModel(string Usage, string Name, string HexValue);

/// <summary>
/// Represents one level in the demonstration typography hierarchy.
/// </summary>
public sealed record DemoTypographyLevelViewModel(string Name, string Specification, string Purpose);

/// <summary>
/// Represents the demonstration recommendation for one slide type.
/// </summary>
public sealed record DemoPageTypeRecommendationViewModel(string Name, string Number, string Description);

/// <summary>
/// Represents one demonstration content presentation rule.
/// </summary>
public sealed record DemoThemeRuleViewModel(string Title, string Description);

/// <summary>
/// Represents a completed analysis stage.
/// </summary>
public sealed record DemoAnalysisStepViewModel(string Title, string Description);

/// <summary>
/// Represents a demonstration chat message.
/// </summary>
public sealed record DemoChatMessageViewModel(string Author, string Text, bool IsUser);

