using System.IO;
using System.Text.Json;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareThemeAnalysisSnapshotStoreTests
{
    private static readonly DateTimeOffset FixedTimestamp = DateTimeOffset.Parse("2026-07-22T03:44:47.123+08:00");

    [TestMethod(DisplayName = "快照 v3 应自包含恢复必要课件数据和字号规则且不保存旧分析字段")]
    [Timeout(60_000)]
    public async Task Version3SnapshotShouldRestoreSelfContainedDataAndFontSizeRulesWithoutLegacyFields()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容 resource-first"), width: 1600, height: 900)
            .AddSlide("slide-second", CreateSlideMarkdown("第二页标题", "第二页内容"), hasScreenshot: false, width: 1600, height: 900)
            .AddResource("resource-first", "image", "resource-first.png")
            .AddResource("resource-missing", "image", "resource-missing.png", exists: false)
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var result = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package);
        var outputRoot = CreateOutputRoot();
        var store = new CoursewareThemeAnalysisSnapshotStore(outputRoot.FullName, FixedTimestamp);

        var snapshotDirectory = await store.SaveAsync(package, result);
        Directory.Delete(exportDirectory.FullName, recursive: true);
        var restored = await store.LoadAsync(snapshotDirectory.FullName);
        var manifestText = await File.ReadAllTextAsync(Path.Join(snapshotDirectory.FullName, store.ManifestFileName));

        Assert.AreEqual(snapshotDirectory.FullName, restored.InputPackage.RootDirectory.FullName);
        Assert.HasCount(2, restored.InputPackage.Slides);
        Assert.AreEqual(1600d, restored.InputPackage.Slides[0].Width);
        Assert.AreEqual(900d, restored.InputPackage.Slides[0].Height);
        Assert.IsTrue(restored.InputPackage.Slides[0].ScreenshotFile?.Exists);
        Assert.IsNull(restored.InputPackage.Slides[1].ScreenshotFile);
        Assert.IsTrue(restored.InputPackage.Resources.Single(resource => resource.ResourceId == "resource-first").Exists);
        Assert.IsFalse(restored.InputPackage.Resources.Single(resource => resource.ResourceId == "resource-missing").Exists);
        Assert.IsFalse(File.Exists(Path.Join(snapshotDirectory.FullName, "Resources", "resource-missing.png")));
        Assert.AreEqual(result.Theme.Style, restored.AnalysisResult.Theme.Style);
        Assert.AreEqual(result.Theme.FontSizeRules, restored.AnalysisResult.Theme.FontSizeRules);
        CollectionAssert.AreEqual(
            result.Theme.ColorSuggestions.ToArray(),
            restored.AnalysisResult.Theme.ColorSuggestions.ToArray());
        var themeText = await File.ReadAllTextAsync(Path.Join(snapshotDirectory.FullName, "Theme", "Theme.json"));
        StringAssert.Contains(manifestText, "\"Version\":3");
        StringAssert.Contains(themeText, nameof(CoursewareTheme.FontSizeRules));
        var persistedTheme = JsonSerializer.Deserialize(
            themeText,
            CoursewareExportJsonSerializerContext.Default.CoursewareTheme);
        Assert.IsNotNull(persistedTheme);
        Assert.AreEqual(result.Theme.FontSizeRules, persistedTheme.FontSizeRules);
        Assert.IsFalse(manifestText.Contains("SourceFingerprint", StringComparison.Ordinal));
        Assert.IsFalse(manifestText.Contains("AnalysisViewFingerprint", StringComparison.Ordinal));
        Assert.IsFalse(manifestText.Contains("DesignSystem", StringComparison.Ordinal));
        Assert.IsFalse(manifestText.Contains("StructuredFacts", StringComparison.Ordinal));
        Assert.IsFalse(manifestText.Contains("ValidationReport", StringComparison.Ordinal));
        Assert.IsFalse(manifestText.Contains("Token", StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "快照加载应明确拒绝 v1 并在错误中包含版本")]
    [Timeout(60_000)]
    public async Task LoadShouldRejectVersion1WithVersionInMessage()
    {
        var directory = CreateOutputRoot();
        await File.WriteAllTextAsync(Path.Join(directory.FullName, "CoursewareThemeAnalysis.json"), "{\"Version\":1}");
        var store = new CoursewareThemeAnalysisSnapshotStore(CreateOutputRoot().FullName);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(directory.FullName));

        StringAssert.Contains(exception.Message, "1");
    }

    [TestMethod(DisplayName = "快照加载应明确拒绝 v2 并在错误中包含实际版本")]
    [Timeout(60_000)]
    public async Task LoadShouldRejectVersion2WithActualVersionInMessage()
    {
        var directory = CreateOutputRoot();
        await File.WriteAllTextAsync(Path.Join(directory.FullName, "CoursewareThemeAnalysis.json"), "{\"Version\":2}");
        var store = new CoursewareThemeAnalysisSnapshotStore(CreateOutputRoot().FullName);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(directory.FullName));

        StringAssert.Contains(exception.Message, "2");
        StringAssert.Contains(exception.Message, "v3");
    }

    [TestMethod(DisplayName = "快照加载应明确拒绝未知版本并在错误中包含版本")]
    [Timeout(60_000)]
    public async Task LoadShouldRejectUnknownVersionWithVersionInMessage()
    {
        var directory = CreateOutputRoot();
        await File.WriteAllTextAsync(Path.Join(directory.FullName, "CoursewareThemeAnalysis.json"), "{\"Version\":99}");
        var store = new CoursewareThemeAnalysisSnapshotStore(CreateOutputRoot().FullName);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(directory.FullName));

        StringAssert.Contains(exception.Message, "99");
    }

    [TestMethod(DisplayName = "快照缺少必要文件时应明确失败")]
    [Timeout(60_000)]
    public async Task LoadShouldFailWhenRequiredFileIsMissing()
    {
        var (store, snapshotDirectory) = await CreateSnapshotAsync();
        File.Delete(Path.Join(snapshotDirectory.FullName, "Slides", "Slide000.md"));

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, "Markdown");
    }

    [TestMethod(DisplayName = "快照清单路径越界时应明确失败")]
    [Timeout(60_000)]
    public async Task LoadShouldRejectManifestPathTraversal()
    {
        var (store, snapshotDirectory) = await CreateSnapshotAsync();
        await RewriteManifestAsync(snapshotDirectory, themeFile: "../Theme.json");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, "路径");
    }

    [TestMethod(DisplayName = "快照 Theme 2.1 字段非法时应明确失败")]
    [Timeout(60_000)]
    public async Task LoadShouldRejectInvalidTheme()
    {
        var (store, snapshotDirectory) = await CreateSnapshotAsync();
        var themePath = Path.Join(snapshotDirectory.FullName, "Theme", "Theme.json");
        var theme = await ReadThemeAsync(themePath);
        await WriteThemeAsync(themePath, theme with { ColorSuggestions = [] });

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, "ColorSuggestions");
    }

    [TestMethod(DisplayName = "快照 Theme 2.1 的 SlideML 非法时应明确失败")]
    [Timeout(60_000)]
    public async Task LoadShouldRejectInvalidSlideMl()
    {
        var (store, snapshotDirectory) = await CreateSnapshotAsync();
        var themePath = Path.Join(snapshotDirectory.FullName, "Theme", "Theme.json");
        var theme = await ReadThemeAsync(themePath);
        await WriteThemeAsync(themePath, theme with { CoverPageSlideMl = "<?xml version=\"1.0\"?><Page>" });

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, "CoverPageSlideMl");
    }

    [TestMethod(DisplayName = "快照 v3 缺少字号规则时应明确失败")]
    [Timeout(60_000)]
    public async Task LoadShouldRejectMissingFontSizeRules()
    {
        var (store, snapshotDirectory) = await CreateSnapshotAsync();
        var themePath = Path.Join(snapshotDirectory.FullName, "Theme", "Theme.json");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(themePath));
        var properties = document.RootElement.EnumerateObject()
            .Where(property => !string.Equals(property.Name, nameof(CoursewareTheme.FontSizeRules), StringComparison.Ordinal))
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
        await File.WriteAllTextAsync(themePath, JsonSerializer.Serialize(properties));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, nameof(CoursewareTheme.FontSizeRules));
    }

    [TestMethod(DisplayName = "快照应使用首张页面的非标准画布校验 SlideML")]
    [Timeout(60_000)]
    public async Task LoadShouldValidateSlideMlAgainstFirstSlideNonStandardCanvas()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"), width: 800, height: 600)
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var result = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package) with
        {
            Theme = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package).Theme with
            {
                CoverPageSlideMl = "<?xml version=\"1.0\"?><Page><Rect Id=\"r\" X=\"900\" Y=\"10\" Width=\"20\" Height=\"20\" Fill=\"#000000\" /></Page>",
            },
        };
        var store = new CoursewareThemeAnalysisSnapshotStore(CreateOutputRoot().FullName);
        var snapshotDirectory = await store.SaveAsync(package, result);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, "超出画布");
    }

    [TestMethod(DisplayName = "保存取消时不应创建快照目录")]
    [Timeout(60_000)]
    public async Task SaveCancellationShouldNotCreateSnapshotDirectory()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var outputRoot = CreateOutputRoot();
        var store = new CoursewareThemeAnalysisSnapshotStore(outputRoot.FullName);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => store.SaveAsync(
            package,
            FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package),
            cancellationTokenSource.Token));

        Assert.IsEmpty(outputRoot.EnumerateDirectories());
    }

    [TestMethod(DisplayName = "保存失败时应清理临时半成品")]
    [Timeout(60_000)]
    public async Task SaveFailureShouldCleanTemporaryDirectory()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        File.Delete(Path.Join(exportDirectory.FullName, "Courseware.json"));
        var outputRoot = CreateOutputRoot();
        var store = new CoursewareThemeAnalysisSnapshotStore(outputRoot.FullName);

        await Assert.ThrowsAsync<FileNotFoundException>(() => store.SaveAsync(
            package,
            FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package)));

        Assert.IsEmpty(outputRoot.EnumerateDirectories());
    }

    [TestMethod(DisplayName = "同毫秒保存应追加后缀且不覆盖旧快照")]
    [Timeout(60_000)]
    public async Task SameMillisecondSaveShouldAppendSuffix()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var outputRoot = CreateOutputRoot();
        var store = new CoursewareThemeAnalysisSnapshotStore(outputRoot.FullName, FixedTimestamp);
        var result = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package);

        var first = await store.SaveAsync(package, result);
        var second = await store.SaveAsync(package, result);

        Assert.AreEqual("CoursewareThemeAnalysis_20260722_034447_123", first.Name);
        Assert.AreEqual("CoursewareThemeAnalysis_20260722_034447_123_1", second.Name);
        Assert.IsTrue(first.Exists);
        Assert.IsTrue(second.Exists);
    }

    [TestMethod(DisplayName = "快照 Markdown 被修改但仍符合导出格式时应按当前内容恢复")]
    [Timeout(60_000)]
    public async Task LoadShouldRestoreCurrentMarkdownWithoutFingerprintValidation()
    {
        var (store, snapshotDirectory) = await CreateSnapshotAsync();
        var markdownPath = Path.Join(snapshotDirectory.FullName, "Slides", "Slide000.md");
        var originalMarkdown = await File.ReadAllTextAsync(markdownPath);
        var modifiedMarkdown = originalMarkdown.Replace("第一页内容", "修改后的当前内容", StringComparison.Ordinal);
        await File.WriteAllTextAsync(markdownPath, modifiedMarkdown);

        var restored = await store.LoadAsync(snapshotDirectory.FullName);

        StringAssert.Contains(restored.InputPackage.Slides[0].MarkdownText, "修改后的当前内容");
    }

    private static async Task<(CoursewareThemeAnalysisSnapshotStore Store, DirectoryInfo SnapshotDirectory)> CreateSnapshotAsync()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var store = new CoursewareThemeAnalysisSnapshotStore(CreateOutputRoot().FullName, FixedTimestamp);
        var snapshotDirectory = await store.SaveAsync(package, FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package));
        return (store, snapshotDirectory);
    }

    private static DirectoryInfo CreateOutputRoot()
    {
        return Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"CoursewareSnapshotStoreTests_{Guid.NewGuid():N}"));
    }

    private static async Task RewriteManifestAsync(DirectoryInfo snapshotDirectory, string themeFile)
    {
        var manifest = new CoursewareThemeAnalysisSnapshotManifest
        {
            CreatedAt = FixedTimestamp,
            CoursewareManifestFile = "Courseware.json",
            ThemeFile = themeFile,
        };
        await File.WriteAllTextAsync(
            Path.Join(snapshotDirectory.FullName, "CoursewareThemeAnalysis.json"),
            JsonSerializer.Serialize(manifest, CoursewareExportJsonSerializerContext.Default.CoursewareThemeAnalysisSnapshotManifest));
    }

    private static async Task<CoursewareTheme> ReadThemeAsync(string themePath)
    {
        await using var stream = File.OpenRead(themePath);
        return await JsonSerializer.DeserializeAsync(stream, CoursewareExportJsonSerializerContext.Default.CoursewareTheme)
               ?? throw new InvalidDataException("测试 Theme 为空。");
    }

    private static async Task WriteThemeAsync(string themePath, CoursewareTheme theme)
    {
        await using var stream = new FileStream(themePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await JsonSerializer.SerializeAsync(stream, theme, CoursewareExportJsonSerializerContext.Default.CoursewareTheme);
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 页面信息\n\n- Id: slide-id\n- 尺寸: 1280×720\n- 序号(1-base): 1\n\n---\n\n## 元素简要信息\n\n- 文本.1: (100, 80) 400×60\n\n---\n\n## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }
}
