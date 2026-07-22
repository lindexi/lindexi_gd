using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGenerator.Core.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CoursewareExecutableDesignSystemTests
{
    [TestMethod(DisplayName = "结构化事实应提取几何字体字号颜色资源和布局簇")]
    [Timeout(60_000)]
    public void StructuredFactsShouldExtractMarkdownEvidenceAndLayoutClusters()
    {
        var package = CreatePackage();
        var analysisInput = new CoursewareAnalysisInputBuilder().Build(package);

        var report = new CoursewareStructuredFactBuilder().Build(analysisInput);
        var samples = new CoursewareVisualSampleSelector().Select(package, report);

        Assert.HasCount(2, report.Slides);
        Assert.AreEqual(2, report.Slides[0].ElementCount);
        Assert.AreEqual(1, report.Slides[0].TextElementCount);
        Assert.AreEqual(1, report.Slides[0].ImageElementCount);
        Assert.AreEqual("Microsoft YaHei", report.Slides[0].Fonts[0].Value);
        Assert.AreEqual(32d, report.Slides[0].FontSizes[0].Value);
        Assert.AreEqual("#123456", report.Slides[0].Colors[0].Value);
        CollectionAssert.Contains(report.Slides[0].ResourceIds.ToArray(), "img-1");
        Assert.IsNotNull(report.Slides[0].ContentBounds);
        Assert.IsNotEmpty(report.LayoutClusters);
        Assert.IsTrue(samples.Count <= CoursewareVisualSampleSelector.DefaultMaximumSampleCount);
        Assert.AreEqual(samples.Count, samples.Select(sample => sample.SlideId).Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod(DisplayName = "设计系统草稿应拒绝旧 Revision 并阻止无图片视觉证据")]
    [Timeout(60_000)]
    public void DraftShouldRejectStaleRevisionAndVisualEvidenceWithoutImages()
    {
        var draft = new CoursewareDesignSystemDraftBuilder(["slide-1"], ["img-1"], visualAnalysisExecuted: false);
        var begin = draft.Begin("design-system", new CoursewareDesignIntent { Name = "测试", Summary = "测试", Rationale = "测试" });

        var first = draft.SetCanvasAndGrid(
            begin.DraftId,
            begin.Revision,
            [new CoursewareCanvasDesignProfile { CanvasId = "primary", Width = 1280, Height = 720, IsPrimary = true }],
            new CoursewareGridSystem { ColumnCount = 12, Gutter = 24, Baseline = 8 });
        var stale = draft.SetTypography(begin.DraftId, begin.Revision, new CoursewareTypographySystem());

        Assert.IsTrue(first.Success);
        Assert.IsFalse(stale.Success);
        Assert.AreEqual("RevisionConflict", stale.Diagnostics[0].Code);

        var designSystem = CreateValidDesignSystem(includeVisualEvidence: true);
        var validation = new CoursewareDesignSystemValidator().Validate(designSystem, ["slide-1"], ["img-1"], visualAnalysisExecuted: false);
        Assert.IsFalse(validation.IsValid);
        Assert.IsTrue(validation.Diagnostics.Any(item => item.Code == "VisualEvidenceWithoutImages"));
    }

    [TestMethod(DisplayName = "模板编译器应解析令牌和槽位并拒绝未知资源")]
    [Timeout(60_000)]
    public void TemplateCompilerShouldResolveTokensAndRejectUnknownResources()
    {
        var designSystem = CreateValidDesignSystem(includeVisualEvidence: false);
        var template = designSystem.PageTemplates[0];
        var compiler = new CoursewareTemplateCompiler();

        var success = compiler.Compile(
            template,
            designSystem,
            designSystem.CanvasProfiles[0],
            new Dictionary<string, string> { ["title"] = "A&B <课堂>" },
            new HashSet<string>(["img-1"], StringComparer.Ordinal));
        var failed = compiler.Compile(
            template with { SlideMlTemplate = "<Page><Image Id=\"image\" Source=\"unknown\" /></Page>" },
            designSystem,
            designSystem.CanvasProfiles[0],
            new Dictionary<string, string> { ["title"] = "标题" },
            new HashSet<string>(["img-1"], StringComparer.Ordinal));

        Assert.IsTrue(success.Success, string.Join(";", success.Diagnostics.Select(item => item.Message)));
        StringAssert.Contains(success.CompiledSlideMl, "A&amp;B &lt;课堂&gt;");
        StringAssert.Contains(success.CompiledSlideMl, "FontSize=\"48\"");
        Assert.IsFalse(failed.Success);
        Assert.IsTrue(failed.Diagnostics.Any(item => item.Code == "UnknownTemplateResource"));
    }

    internal static CoursewareDesignSystem CreateValidDesignSystem(bool includeVisualEvidence)
    {
        return new CoursewareDesignSystem
        {
            DesignSystemId = "design-system",
            DesignIntent = new CoursewareDesignIntent { Name = "测试", Summary = "测试", Rationale = "测试" },
            CanvasProfiles = [new CoursewareCanvasDesignProfile { CanvasId = "primary", Width = 1280, Height = 720, IsPrimary = true }],
            Grid = new CoursewareGridSystem { SafeLeft = 64, SafeTop = 48, SafeRight = 64, SafeBottom = 48, ColumnCount = 12, Gutter = 24, Baseline = 8 },
            Spacing = new CoursewareSpacingScale { Tokens = [new CoursewareSpacingToken { TokenId = "space-md", Value = 24, Purpose = "间距" }] },
            Typography = new CoursewareTypographySystem
            {
                Tokens = [new CoursewareTypographyToken { TokenId = "title", EastAsianFontStack = ["Microsoft YaHei"], LatinFontStack = ["Arial"], FontSize = 48, FontWeight = "Bold", LineHeight = 1.2, Purpose = "标题", AllowedPageTypeIds = ["content"] }],
            },
            Colors = new CoursewareColorSystem
            {
                Tokens =
                [
                    new CoursewareColorToken { TokenId = "background-page", HexValue = "#FFFFFF", Purpose = "背景", SourceKind = CoursewareEvidenceSourceKind.DesignInference },
                    new CoursewareColorToken { TokenId = "text-primary", HexValue = "#123456", Purpose = "文字", AllowedBackgroundTokenIds = ["background-page"], SourceKind = CoursewareEvidenceSourceKind.DesignInference },
                ],
            },
            Effects = new CoursewareEffectSystem(),
            Components = [new CoursewareComponentSpecification { ComponentId = "title-block", Name = "标题", Purpose = "标题", TokenIds = ["title", "text-primary"] }],
            AssetPolicy = new CoursewareAssetUsagePolicy { ResourceRules = [new CoursewareAssetUsageRule { ResourceId = "img-1", CandidateUses = ["插图"] }] },
            PageTypes =
            [
                new CoursewarePageTypeContract
                {
                    PageTypeId = "content",
                    Name = "内容页",
                    Purpose = "内容",
                    EvidenceReferences = [new CoursewareEvidenceReference { SourceKind = CoursewareEvidenceSourceKind.SlideMarkdownFact, SlideId = "slide-1", PageNumber = 1, Description = "测试" }],
                    DensityRange = new CoursewareContentDensityRange { Minimum = 0, Maximum = 1 },
                    Slots = [new CoursewareTemplateSlot { SlotId = "title", SlotKind = "Text", IsRequired = true, Purpose = "标题" }],
                    ComponentIds = ["title-block"],
                    OverflowStrategy = "Split",
                },
            ],
            PageTypeAssignments = [new CoursewarePageTypeAssignment { SlideId = "slide-1", PageTypeId = "content" }],
            PageTemplates =
            [
                new CoursewarePageTemplate
                {
                    TemplateId = "content-template",
                    PageTypeId = "content",
                    Name = "内容模板",
                    Purpose = "内容",
                    DensityRange = new CoursewareContentDensityRange { Minimum = 0, Maximum = 1 },
                    Slots = [new CoursewareTemplateSlot { SlotId = "title", SlotKind = "Text", IsRequired = true, Purpose = "标题" }],
                    SlideMlTemplate = "<Page Id=\"page\" Background=\"{{token:background-page}}\"><TextElement Id=\"title\" X=\"64\" Y=\"48\" Width=\"1152\" Height=\"120\" Text=\"{{slot:title}}\" FontName=\"{{token:title.fontName}}\" FontSize=\"{{token:title}}\" Foreground=\"{{token:text-primary}}\" /></Page>",
                },
            ],
            Accessibility = new CoursewareAccessibilityRules { MinimumBodyFontSize = 24, MinimumNormalTextContrastRatio = 4.5, MinimumLargeTextContrastRatio = 3 },
            Consistency = new CoursewareConsistencyRules(),
            Evidence = includeVisualEvidence
                ? [new CoursewareDesignDecisionEvidence { DecisionId = "visual", DecisionKind = "Color", SelectedValue = "蓝色", Confidence = 0.8, Sources = [new CoursewareEvidenceReference { SourceKind = CoursewareEvidenceSourceKind.VisualObservation, SlideId = "slide-1", PageNumber = 1, Description = "截图观察" }] }]
                : [],
        };
    }

    private static CoursewareInputPackage CreatePackage()
    {
        var root = new DirectoryInfo(Path.Join(Path.GetTempPath(), $"courseware-facts-{Guid.NewGuid():N}"));
        root.Create();
        var screenshot1 = new FileInfo(Path.Join(root.FullName, "Slide001.jpg"));
        var screenshot2 = new FileInfo(Path.Join(root.FullName, "Slide002.jpg"));
        File.WriteAllBytes(screenshot1.FullName, [1]);
        File.WriteAllBytes(screenshot2.FullName, [1]);
        return new CoursewareInputPackage
        {
            RootDirectory = root,
            CoursewareName = "测试课件",
            Resources = [new CoursewareResourceEntry { ResourceId = "img-1", ResourceType = "image", Exists = true }],
            Slides =
            [
                CreateSlide(root, screenshot1, 0, "slide-1", "- 文本.1: (100, 80) 400×60\n- 图片.1: (600, 100) 500×400", "字号: 32px | 字体: Microsoft YaHei | 颜色: #123456\nSource: img-1\n#### 内容\n```\n测试标题与内容\n```"),
                CreateSlide(root, screenshot2, 1, "slide-2", "- 文本.1: (100, 80) 1000×500", "字号: 24px | 字体: Microsoft YaHei\n#### 内容\n```\n第二页内容\n```"),
            ],
        };
    }

    private static CoursewareSlideInput CreateSlide(DirectoryInfo root, FileInfo screenshot, int index, string slideId, string elements, string details)
    {
        var markdown = $"## 页面信息\n\n- Id: {slideId}\n- 尺寸: 1280×720\n- 序号(1-base): {index + 1}\n\n---\n\n## 元素简要信息\n\n{elements}\n\n---\n\n## 元素细节\n\n{details}";
        return new CoursewareSlideInput
        {
            SlideIndex = index,
            PageNumber = index + 1,
            SlideId = slideId,
            Width = 1280,
            Height = 720,
            MarkdownFile = new FileInfo(Path.Join(root.FullName, $"Slide{index + 1:D3}.md")),
            ScreenshotFile = screenshot,
            MarkdownText = markdown,
        };
    }
}
