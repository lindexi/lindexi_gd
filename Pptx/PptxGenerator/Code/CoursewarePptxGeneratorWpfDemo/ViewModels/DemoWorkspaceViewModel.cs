using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using CoursewarePptxGeneratorWpfDemo.Models;
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
/// Identifies the presentation-only state of the courseware analysis page.
/// </summary>
public enum CoursewareAnalysisStage
{
    /// <summary>
    /// No courseware has been opened.
    /// </summary>
    Welcome,

    /// <summary>
    /// The demonstration courseware is being loaded.
    /// </summary>
    LoadingCourseware,

    /// <summary>
    /// The demonstration theme is being analyzed.
    /// </summary>
    AnalyzingTheme,

    /// <summary>
    /// The demonstration theme is ready.
    /// </summary>
    AnalysisReady,

    /// <summary>
    /// The demonstration analysis failed.
    /// </summary>
    AnalysisFailed,

    /// <summary>
    /// The demonstration analysis was canceled.
    /// </summary>
    Canceled,
}

/// <summary>
/// Provides presentation-only data and navigation for the courseware analysis prototype.
/// </summary>
public sealed class DemoWorkspaceViewModel : ObservableObject
{
    private const int DemoSlideCount = 60;
    private readonly CoursewareFolderLoader _coursewareFolderLoader;
    private readonly IViewModelDispatcher _dispatcher;
    private readonly RelayCommand _enterWorkspaceCommand;
    private DemoSlideViewModel? _selectedSlide;
    private string _demoInputText = string.Empty;
    private PrototypePage _currentPage = PrototypePage.CoursewareAnalysis;
    private CoursewareAnalysisStage _analysisStage = CoursewareAnalysisStage.Welcome;
    private CoursewareWorkspaceState _workspaceState = CoursewareWorkspaceState.Welcome;
    private CoursewareWorkspaceSession? _coursewareSession;
    private string? _loadErrorMessage;
    private string? _loadErrorDetails;
    private CancellationTokenSource? _loadCancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoWorkspaceViewModel" /> class.
    /// </summary>
    public DemoWorkspaceViewModel()
        : this(new CoursewareFolderLoader())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoWorkspaceViewModel" /> class.
    /// </summary>
    /// <param name="coursewareFolderLoader">The courseware export folder loader.</param>
    /// <param name="dispatcher">The dispatcher used for ViewModel state updates.</param>
    public DemoWorkspaceViewModel(
        CoursewareFolderLoader coursewareFolderLoader,
        IViewModelDispatcher? dispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(coursewareFolderLoader);

        _coursewareFolderLoader = coursewareFolderLoader;
        _dispatcher = dispatcher ?? WpfViewModelDispatcher.Instance;
        Slides = new ObservableCollection<DemoSlideViewModel>(CreateSlides());
        CoursewareThumbnails = new ObservableCollection<CoursewareThumbnailItemViewModel>();
        ThemeColors = new ObservableCollection<DemoThemeColorViewModel>(CreateThemeColors());
        TypographyLevels = new ObservableCollection<DemoTypographyLevelViewModel>(CreateTypographyLevels());
        LayoutPrinciples = new ObservableCollection<string>(
        [
            "标题区保持稳定高度和对齐起点",
            "每页只保留一个主要视觉焦点",
            "公式、图形和说明文字遵守统一对齐线",
            "例题、步骤和结论使用一致的视觉节奏",
            "章节页与普通内容页形成明确变化",
        ]);
        PageTypeRecommendations = new ObservableCollection<DemoPageTypeRecommendationViewModel>(CreatePageTypeRecommendations());
        ContentRules = new ObservableCollection<DemoThemeRuleViewModel>(CreateContentRules());
        AnalysisSteps = new ObservableCollection<DemoAnalysisStepViewModel>(CreateAnalysisSteps());
        ChatMessages = new ObservableCollection<DemoChatMessageViewModel>(
        [
            new("Copilot", "我已读取当前页面内容和全局主题。可以继续调整例题讲解结构、视觉层级或生成新的 SlideML。", false),
            new("你", "请让公式推导更清晰，并突出本页的核心结论。", true),
            new("Copilot", "已按全局主题强化标题层级，将推导拆分为三个步骤，并使用强调色突出核心结论。", false),
        ]);

        _selectedSlide = Slides[4];
        _enterWorkspaceCommand = new RelayCommand(
            _ => CurrentPage = PrototypePage.SlideWorkspace,
            _ => AnalysisStage == CoursewareAnalysisStage.AnalysisReady);
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
        ContinueLoadingCommand = new RelayCommand(_ => AnalysisStage = CoursewareAnalysisStage.AnalyzingTheme);
        CompleteAnalysisCommand = new RelayCommand(_ => AnalysisStage = CoursewareAnalysisStage.AnalysisReady);
        CancelDemoCommand = new RelayCommand(_ => AnalysisStage = CoursewareAnalysisStage.Canceled);
        ReanalyzeCommand = new RelayCommand(_ => AnalysisStage = CoursewareAnalysisStage.AnalyzingTheme);
        RetryDemoCommand = new RelayCommand(_ => AnalysisStage = CoursewareAnalysisStage.AnalyzingTheme);
        OpenOtherCoursewareCommand = new RelayCommand(_ => ResetDemoCourseware());
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
    public string ThemeTitle => "数学推导与理性空间";

    /// <summary>
    /// Gets the generated courseware theme summary.
    /// </summary>
    public string ThemeDescription => "以清晰推导和理性表达为核心，通过稳定的蓝色体系、明确的标题层级和规整的图文对齐，建立适合初中数学教学的现代课堂视觉风格。";

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
    /// Gets the demonstration rendering log for the selected page.
    /// </summary>
    public string DemoRenderingLog => "演示模式：当前页面已应用全局主题。本轮不会调用 SlideML 渲染、生成或保存服务。";

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
                OnPropertyChanged(nameof(IsCoursewareLoaded));
                OnPropertyChanged(nameof(IsCoursewareLoadFailed));
                OnPropertyChanged(nameof(AnalysisStatus));
                OnPropertyChanged(nameof(AnalysisCaption));
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
    /// Gets a value indicating whether a real courseware package is available.
    /// </summary>
    public bool IsCoursewareLoaded => WorkspaceState == CoursewareWorkspaceState.CoursewareLoaded;

    /// <summary>
    /// Gets a value indicating whether the last real courseware load failed.
    /// </summary>
    public bool IsCoursewareLoadFailed => WorkspaceState == CoursewareWorkspaceState.LoadFailed;

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
    /// Gets the completed analysis stages.
    /// </summary>
    public ObservableCollection<DemoAnalysisStepViewModel> AnalysisSteps { get; }

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
        : $"{SelectedSlide.Title} · 已应用“数学推导与理性空间”全局主题";

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
    /// Gets or sets the presentation-only state of the analysis page.
    /// </summary>
    public CoursewareAnalysisStage AnalysisStage
    {
        get => _analysisStage;
        private set
        {
            if (SetProperty(ref _analysisStage, value))
            {
                CurrentPage = PrototypePage.CoursewareAnalysis;
                OnPropertyChanged(nameof(IsWelcome));
                OnPropertyChanged(nameof(IsLoadingCourseware));
                OnPropertyChanged(nameof(IsAnalyzingTheme));
                OnPropertyChanged(nameof(IsAnalysisReady));
                OnPropertyChanged(nameof(IsAnalysisFailed));
                OnPropertyChanged(nameof(IsCanceled));
                OnPropertyChanged(nameof(ShowsCoursewareContext));
                OnPropertyChanged(nameof(ShowsThemeResult));
                OnPropertyChanged(nameof(AnalysisStatus));
                OnPropertyChanged(nameof(AnalysisCaption));
                _enterWorkspaceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the welcome state is visible.
    /// </summary>
    public bool IsWelcome => AnalysisStage == CoursewareAnalysisStage.Welcome;

    /// <summary>
    /// Gets a value indicating whether the loading state is visible.
    /// </summary>
    public bool IsLoadingCourseware => AnalysisStage == CoursewareAnalysisStage.LoadingCourseware;

    /// <summary>
    /// Gets a value indicating whether the analyzing state is visible.
    /// </summary>
    public bool IsAnalyzingTheme => AnalysisStage == CoursewareAnalysisStage.AnalyzingTheme;

    /// <summary>
    /// Gets a value indicating whether the theme result is ready.
    /// </summary>
    public bool IsAnalysisReady => AnalysisStage == CoursewareAnalysisStage.AnalysisReady;

    /// <summary>
    /// Gets a value indicating whether the failed state is visible.
    /// </summary>
    public bool IsAnalysisFailed => AnalysisStage == CoursewareAnalysisStage.AnalysisFailed;

    /// <summary>
    /// Gets a value indicating whether the canceled state is visible.
    /// </summary>
    public bool IsCanceled => AnalysisStage == CoursewareAnalysisStage.Canceled;

    /// <summary>
    /// Gets a value indicating whether courseware context is available.
    /// </summary>
    public bool ShowsCoursewareContext => AnalysisStage != CoursewareAnalysisStage.Welcome;

    /// <summary>
    /// Gets a value indicating whether a complete theme result is available.
    /// </summary>
    public bool ShowsThemeResult => AnalysisStage == CoursewareAnalysisStage.AnalysisReady;

    /// <summary>
    /// Gets the current analysis status.
    /// </summary>
    public string AnalysisStatus => WorkspaceState switch
    {
        CoursewareWorkspaceState.LoadingCourseware => "正在读取课件",
        CoursewareWorkspaceState.CoursewareLoaded => "课件已加载",
        CoursewareWorkspaceState.LoadFailed => "课件读取失败",
        _ => AnalysisStage switch
        {
            CoursewareAnalysisStage.Welcome => "等待打开课件",
            CoursewareAnalysisStage.LoadingCourseware => "正在读取课件",
            CoursewareAnalysisStage.AnalyzingTheme => "正在分析全课件主题",
            CoursewareAnalysisStage.AnalysisReady => "全课件分析已完成",
            CoursewareAnalysisStage.AnalysisFailed => "主题分析失败",
            CoursewareAnalysisStage.Canceled => "主题分析已取消",
            _ => string.Empty,
        },
    };

    /// <summary>
    /// Gets the secondary analysis status text.
    /// </summary>
    public string AnalysisCaption => WorkspaceState switch
    {
        CoursewareWorkspaceState.LoadingCourseware => "正在解析清单、Markdown、资源和截图",
        CoursewareWorkspaceState.CoursewareLoaded => $"已加载 {CoursewareThumbnails.Count} 页，等待后续主题分析",
        CoursewareWorkspaceState.LoadFailed => "请选择有效的课件 Markdown 导出文件夹",
        _ => AnalysisStage switch
        {
            CoursewareAnalysisStage.Welcome => "打开课件后将先形成统一视觉主题",
            CoursewareAnalysisStage.LoadingCourseware => $"正在准备 {Slides.Count} 页演示课件内容",
            CoursewareAnalysisStage.AnalyzingTheme => "正在归纳内容层级、配色、字体与版式",
            CoursewareAnalysisStage.AnalysisReady => $"已完整查看 {Slides.Count} 页课件并形成统一主题",
            CoursewareAnalysisStage.AnalysisFailed => "演示错误：主题结构校验未通过",
            CoursewareAnalysisStage.Canceled => "已保留课件概览，可随时重新分析",
            _ => string.Empty,
        },
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
    /// Gets the command that advances loading to theme analysis.
    /// </summary>
    public ICommand ContinueLoadingCommand { get; }

    /// <summary>
    /// Gets the command that completes the demonstration analysis.
    /// </summary>
    public ICommand CompleteAnalysisCommand { get; }

    /// <summary>
    /// Gets the command that cancels the demonstration analysis.
    /// </summary>
    public ICommand CancelDemoCommand { get; }

    /// <summary>
    /// Gets the command that restarts the demonstration analysis.
    /// </summary>
    public ICommand ReanalyzeCommand { get; }

    /// <summary>
    /// Gets the command that retries the failed demonstration analysis.
    /// </summary>
    public ICommand RetryDemoCommand { get; }

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
        var previousCancellationTokenSource = Interlocked.Exchange(ref _loadCancellationTokenSource, cancellationTokenSource);
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
            var package = await _coursewareFolderLoader.LoadAsync(folderPath, cancellationTokenSource.Token).ConfigureAwait(false);
            var thumbnails = new List<CoursewareThumbnailItemViewModel>(package.Slides.Count);
            foreach (var slide in package.Slides)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                thumbnails.Add(CoursewareThumbnailItemViewModel.Create(slide));
            }

            await _dispatcher.InvokeAsync(() =>
            {
                CoursewareThumbnails.Clear();
                foreach (var thumbnail in thumbnails)
                {
                    CoursewareThumbnails.Add(thumbnail);
                }

                CoursewareSession = new CoursewareWorkspaceSession(package);
                WorkspaceState = CoursewareWorkspaceState.CoursewareLoaded;
                OnPropertyChanged(nameof(SlideCountText));
                OnPropertyChanged(nameof(InputHealthText));
            });
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
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
        finally
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref _loadCancellationTokenSource, null, cancellationTokenSource), cancellationTokenSource))
            {
                cancellationTokenSource.Dispose();
            }
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

    private static IEnumerable<DemoThemeColorViewModel> CreateThemeColors()
    {
        yield return new DemoThemeColorViewModel("主色", "教学蓝", "#2563EB");
        yield return new DemoThemeColorViewModel("强调色", "重点橙", "#F59E0B");
        yield return new DemoThemeColorViewModel("页面背景", "雾白", "#F8FAFC");
        yield return new DemoThemeColorViewModel("主文字", "深墨", "#0F172A");
        yield return new DemoThemeColorViewModel("次文字", "石板灰", "#475569");
    }

    private static IEnumerable<DemoTypographyLevelViewModel> CreateTypographyLevels()
    {
        yield return new DemoTypographyLevelViewModel("一级标题", "32 pt / Bold", "建立课题与章节焦点");
        yield return new DemoTypographyLevelViewModel("二级标题", "24 pt / SemiBold", "组织推导步骤与知识模块");
        yield return new DemoTypographyLevelViewModel("正文", "18 pt / Regular", "保证教室投影下的远距离阅读");
        yield return new DemoTypographyLevelViewModel("辅助文字", "14 pt / Regular", "仅用于注释、来源和次要说明");
    }

    private static IEnumerable<DemoPageTypeRecommendationViewModel> CreatePageTypeRecommendations()
    {
        yield return new DemoPageTypeRecommendationViewModel("封面页", "01", "聚焦课题名称，以几何线框建立学科识别，减少说明文字。");
        yield return new DemoPageTypeRecommendationViewModel("章节页", "02", "使用大标题、章节编号和留白，形成清晰的课堂节奏变化。");
        yield return new DemoPageTypeRecommendationViewModel("内容页", "03", "优先展示推导关系，图形、公式和解释沿统一基线组织。");
        yield return new DemoPageTypeRecommendationViewModel("练习与总结", "04", "练习页突出任务边界，总结页集中呈现方法与关键结论。");
    }

    private static IEnumerable<DemoThemeRuleViewModel> CreateContentRules()
    {
        yield return new DemoThemeRuleViewModel("公式与推导", "按步骤逐层展开；等号与关键符号对齐；最终结论使用橙色强调带。");
        yield return new DemoThemeRuleViewModel("几何图形", "图形与条件说明成组摆放，辅助线使用低饱和蓝，避免与结论争夺焦点。");
        yield return new DemoThemeRuleViewModel("课堂节奏", "导入保持轻量，例题强化步骤，练习减少装饰，总结回收核心方法。");
    }

    private static IEnumerable<DemoAnalysisStepViewModel> CreateAnalysisSteps()
    {
        yield return new DemoAnalysisStepViewModel("1. 课件读取", "已读取课件基本信息");
        yield return new DemoAnalysisStepViewModel("2. 页面结构识别", "已识别封面、章节页和内容页结构");
        yield return new DemoAnalysisStepViewModel("3. 内容层级归纳", "已归纳数学课件的内容层级与讲解节奏");
        yield return new DemoAnalysisStepViewModel("4. 视觉重点分析", "已分析页面比例、图文分布和视觉重点");
        yield return new DemoAnalysisStepViewModel("5. 主题建议生成", "已形成配色、字体和版式建议");
        yield return new DemoAnalysisStepViewModel("6. 主题结构校验", "已完成主题结构校验");
    }

    private void ResetDemoCourseware()
    {
        AnalysisStage = CoursewareAnalysisStage.Welcome;
    }

    private void ShowDemoStage(object? parameter)
    {
        if (parameter is string stageName && Enum.TryParse<CoursewareAnalysisStage>(stageName, out var stage))
        {
            AnalysisStage = stage;
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
