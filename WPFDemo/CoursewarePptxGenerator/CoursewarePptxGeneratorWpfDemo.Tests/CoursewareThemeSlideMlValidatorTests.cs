using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareThemeSlideMlValidatorTests
{
    private const string EmptyPage = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Page />";

    [TestMethod(DisplayName = "完整文档校验应拒绝非法XML")]
    [Timeout(5000)]
    public async Task ValidateAsyncShouldRejectInvalidXmlAsync()
    {
        var result = await CreateValidator().ValidateAsync(CreateTheme("<?xml version=\"1.0\"?><Page>"), CreateCanvas(), EmptyResources);

        Assert.IsTrue(result.Errors.Any(error => error.Contains("XML 无效", StringComparison.Ordinal)));
    }

    [TestMethod(DisplayName = "完整文档校验应拒绝缺少声明和根外内容")]
    [Timeout(5000)]
    public async Task ValidateAsyncShouldRejectMissingDeclarationAndOuterContentAsync()
    {
        var theme = CreateTheme("<!--outside--><Page />");

        var result = await CreateValidator().ValidateAsync(theme, CreateCanvas(), EmptyResources);

        Assert.IsTrue(result.Errors.Any(error => error.Contains("必须包含 XML 声明", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("根外注释", StringComparison.Ordinal)));
    }

    [TestMethod(DisplayName = "完整文档校验应拒绝流式属性和Remove元素")]
    [Timeout(5000)]
    public async Task ValidateAsyncShouldRejectStreamingProtocolAsync()
    {
        const string slideMl = "<?xml version=\"1.0\"?><Page><Panel Id=\"p\" StyleFrom=\"old\"><Remove TargetId=\"x\" /></Panel></Page>";

        var result = await CreateValidator().ValidateAsync(CreateTheme(slideMl), CreateCanvas(), EmptyResources);

        Assert.IsTrue(result.Errors.Any(error => error.Contains("StyleFrom", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("流式协议元素 Remove", StringComparison.Ordinal)));
    }

    [TestMethod(DisplayName = "完整文档校验应拒绝未知标签属性和错误父子层级")]
    [Timeout(5000)]
    public async Task ValidateAsyncShouldRejectUnknownSchemaContentAsync()
    {
        const string slideMl = "<?xml version=\"1.0\"?><Page Unknown=\"x\"><TextElement Id=\"t\" Text=\"x\"><Rect Id=\"r\" /></TextElement><Unknown /></Page>";

        var result = await CreateValidator().ValidateAsync(CreateTheme(slideMl), CreateCanvas(), EmptyResources);

        Assert.IsTrue(result.Errors.Any(error => error.Contains("Page 不支持属性 Unknown", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("Rect 不能作为 TextElement", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("未知标签 Unknown", StringComparison.Ordinal)));
    }

    [TestMethod(DisplayName = "完整文档校验应拒绝未知图片资源")]
    [Timeout(5000)]
    public async Task ValidateAsyncShouldRejectUnknownImageResourceAsync()
    {
        const string slideMl = "<?xml version=\"1.0\"?><Page><Image Id=\"image\" Source=\"missing\" Width=\"10\" Height=\"10\" /></Page>";

        var result = await CreateValidator().ValidateAsync(CreateTheme(slideMl), CreateCanvas(), new HashSet<string>(StringComparer.Ordinal) { "known" });

        Assert.IsTrue(result.Errors.Any(error => error.Contains("missing 不在可用资源 ID", StringComparison.Ordinal)));
    }

    [TestMethod(DisplayName = "完整文档校验应报告真实解析器失败")]
    [Timeout(5000)]
    public async Task ValidateAsyncShouldReportRealParserFailureAsync()
    {
        const string slideMl = "<?xml version=\"1.0\"?><Page><TextElement Id=\"title\" FontSize=\"bad\" /></Page>";
        var validator = new CoursewareThemeSlideMlValidator();

        var result = await validator.ValidateAsync(CreateTheme(slideMl), CreateCanvas(), EmptyResources);

        Assert.IsTrue(result.Errors.Any(error => error.Contains("必须包含 Text 属性或 Span", StringComparison.Ordinal)));
    }

    [STATestMethod(DisplayName = "真实WPF校验应拒绝明显画布越界和父容器裁剪")]
    [Timeout(10000)]
    public async Task ValidateAsyncShouldRejectOutOfBoundsAndClippingAsync()
    {
        const string slideMl = "<?xml version=\"1.0\"?><Page><Panel Id=\"panel\" X=\"750\" Y=\"550\" Width=\"100\" Height=\"100\"><Rect Id=\"rect\" X=\"90\" Y=\"90\" Width=\"20\" Height=\"20\" Fill=\"#000000\" /></Panel></Page>";
        var validator = new CoursewareThemeSlideMlValidator();

        var result = await validator.ValidateAsync(CreateTheme(slideMl), CreateCanvas(), EmptyResources);

        Assert.IsTrue(result.Errors.Any(error => error.Contains("超出画布", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("超出父容器", StringComparison.Ordinal)));
    }

    [STATestMethod(DisplayName = "真实WPF校验应拒绝固定高度文本溢出")]
    [Timeout(10000)]
    public async Task ValidateAsyncShouldRejectTextOverflowAsync()
    {
        const string slideMl = "<?xml version=\"1.0\"?><Page><TextElement Id=\"text\" X=\"10\" Y=\"10\" Width=\"100\" Height=\"10\" Text=\"这是一段明显无法放入固定高度的文本内容\" FontSize=\"32\" /></Page>";
        var validator = new CoursewareThemeSlideMlValidator();

        var result = await validator.ValidateAsync(CreateTheme(slideMl), CreateCanvas(), EmptyResources);

        Assert.IsTrue(result.Errors.Any(error => error.Contains("文本将被裁剪", StringComparison.Ordinal)));
    }

    [STATestMethod(DisplayName = "真实WPF校验应接受合法非标准画布")]
    [Timeout(10000)]
    public async Task ValidateAsyncShouldAcceptValidNonStandardCanvasAsync()
    {
        const string slideMl = "<?xml version=\"1.0\"?><Page Background=\"#FFFFFF\"><TextElement Id=\"title\" X=\"24\" Y=\"20\" Width=\"300\" Height=\"60\" Text=\"非标准画布\" FontSize=\"28\" Foreground=\"#111111\" /></Page>";
        var validator = new CoursewareThemeSlideMlValidator();

        var result = await validator.ValidateAsync(CreateTheme(slideMl), new SlideDocumentContext(1024, 577), EmptyResources);

        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [TestMethod(DisplayName = "深度校验应同时校验Cover和Content")]
    [Timeout(5000)]
    public async Task ValidateAsyncShouldValidateCoverAndContentAsync()
    {
        var validator = new CoursewareThemeSlideMlValidator();
        var theme = CreateTheme(
            "<?xml version=\"1.0\"?><Page UnknownCover=\"x\" />",
            "<?xml version=\"1.0\"?><Page UnknownContent=\"x\" />");

        var result = await validator.ValidateAsync(theme, CreateCanvas(), EmptyResources);

        Assert.IsTrue(result.Errors.Any(error => error.Contains("CoverPageSlideMl", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("ContentPageSlideMl", StringComparison.Ordinal)));
    }

    [TestMethod(DisplayName = "主题校验应汇总字段错误和SlideML错误")]
    [Timeout(5000)]
    public async Task ThemeValidatorShouldAggregateFieldAndSlideMlErrorsAsync()
    {
        var slideMlValidator = new FakeCoursewareThemeSlideMlValidator
        {
            Result = new CoursewareThemeValidationResult { Errors = ["CoverPageSlideMl: 深度错误"] },
        };
        var validator = new CoursewareThemeValidator(slideMlValidator);
        var theme = CreateTheme(EmptyPage) with { LayoutPrinciples = string.Empty };

        var result = await validator.ValidateAsync(theme, CreateCanvas(), EmptyResources);

        Assert.IsTrue(result.Errors.Contains("LayoutPrinciples 不能为空。"));
        Assert.IsTrue(result.Errors.Contains("CoverPageSlideMl: 深度错误"));
    }

    private static readonly IReadOnlySet<string> EmptyResources = new HashSet<string>(StringComparer.Ordinal);

    private static CoursewareThemeSlideMlValidator CreateValidator()
    {
        return new CoursewareThemeSlideMlValidator();
    }

    private static SlideDocumentContext CreateCanvas()
    {
        return new SlideDocumentContext(800, 600);
    }

    private static CoursewareTheme CreateTheme(string coverSlideMl, string? contentSlideMl = null)
    {
        return new CoursewareTheme
        {
            ColorSuggestions =
            [
                new CoursewareColorSuggestion { Name = "背景", Usage = "背景", Hex = "#FFFFFF" },
                new CoursewareColorSuggestion { Name = "正文", Usage = "正文", Hex = "#111111" },
                new CoursewareColorSuggestion { Name = "强调", Usage = "强调", Hex = "#2563EB" },
            ],
            Fonts = new CoursewareFontSuggestions { Chinese = "微软雅黑", Western = "Arial" },
            Style = "简洁",
            SafeArea = new CoursewareSafeAreaRatios { LeftRatio = 0.05, TopRatio = 0.05, RightRatio = 0.05, BottomRatio = 0.05 },
            SpacingAndVisualEffects = "留白",
            LayoutPrinciples = "对齐",
            CoverPageSlideMl = coverSlideMl,
            ContentPageSlideMl = contentSlideMl ?? EmptyPage,
        };
    }

}