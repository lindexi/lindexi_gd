using System.IO;
using System.Text.Json;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareThemeAnalysisSnapshotStoreTests
{
    [TestMethod(DisplayName = "保存主题分析快照应生成可独立恢复的完整目录")]
    [Timeout(60_000)]
    public async Task SaveAsyncShouldCreateSelfContainedRestorableDirectory()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页", "第一页内容"))
            .AddSlide("slide-second", CreateSlideMarkdown("第二页", "第二页内容"), hasScreenshot: false)
            .AddResource("resource-existing", "image", "existing.png")
            .AddResource("resource-missing", "audio", "missing.mp3", exists: false)
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var analysisResult = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package);
        var outputRoot = CreateOutputRoot();
        var createdAt = DateTimeOffset.Parse("2026-07-22T03:44:47.123+08:00");
        var store = new CoursewareThemeAnalysisSnapshotStore(outputRoot.FullName, () => createdAt);

        var snapshotDirectory = await store.SaveAsync(package, analysisResult);
        var restored = await store.LoadAsync(snapshotDirectory.FullName);

        Assert.AreEqual("CoursewareThemeAnalysis_20260722_034447_123", snapshotDirectory.Name);
        Assert.IsTrue(File.Exists(Path.Join(snapshotDirectory.FullName, CoursewareThemeAnalysisSnapshotManifest.FileName)));
        Assert.IsTrue(File.Exists(Path.Join(snapshotDirectory.FullName, "Courseware.json")));
        Assert.IsTrue(File.Exists(Path.Join(snapshotDirectory.FullName, "Slides", "Slide000.md")));
        Assert.IsTrue(File.Exists(Path.Join(snapshotDirectory.FullName, "Slides", "Slide000.jpg")));
        Assert.IsTrue(File.Exists(Path.Join(snapshotDirectory.FullName, "Slides", "Slide001.md")));
        Assert.IsFalse(File.Exists(Path.Join(snapshotDirectory.FullName, "Slides", "Slide001.jpg")));
        Assert.IsTrue(File.Exists(Path.Join(snapshotDirectory.FullName, "Resources", "Resources.json")));
        Assert.IsTrue(File.Exists(Path.Join(snapshotDirectory.FullName, "Resources", "existing.png")));
        Assert.IsFalse(File.Exists(Path.Join(snapshotDirectory.FullName, "Resources", "missing.mp3")));
        Assert.AreEqual(package.CoursewareName, restored.InputPackage.CoursewareName);
        Assert.AreEqual(package.SlideCount, restored.InputPackage.SlideCount);
        Assert.AreEqual(analysisResult.Theme.Title, restored.AnalysisResult.Theme.Title);
        Assert.AreEqual(analysisResult.ReferenceCanvas.CanvasWidth, restored.AnalysisResult.ReferenceCanvas.CanvasWidth);
        CollectionAssert.AreEquivalent(
            new[] { "MissingScreenshotFile", "MissingResourceFile" },
            restored.InputPackage.Warnings.Select(warning => warning.Code).ToArray());
        var manifestText = await File.ReadAllTextAsync(Path.Join(
            snapshotDirectory.FullName,
            CoursewareThemeAnalysisSnapshotManifest.FileName));
        Assert.IsFalse(manifestText.Contains(exportDirectory.FullName, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod(DisplayName = "同一毫秒连续保存主题分析应保留两个独立快照")]
    [Timeout(60_000)]
    public async Task SaveAsyncShouldAppendSuffixWhenTimestampDirectoryAlreadyExists()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页", "第一页内容"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var analysisResult = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package);
        var outputRoot = CreateOutputRoot();
        var createdAt = DateTimeOffset.Parse("2026-07-22T03:44:47.123+08:00");
        var store = new CoursewareThemeAnalysisSnapshotStore(outputRoot.FullName, () => createdAt);

        var first = await store.SaveAsync(package, analysisResult);
        var second = await store.SaveAsync(package, analysisResult);

        Assert.AreEqual("CoursewareThemeAnalysis_20260722_034447_123", first.Name);
        Assert.AreEqual("CoursewareThemeAnalysis_20260722_034447_123_1", second.Name);
        Assert.IsTrue(Directory.Exists(first.FullName));
        Assert.IsTrue(Directory.Exists(second.FullName));
    }

    [TestMethod(DisplayName = "修改快照页面 Markdown 后恢复应因源事实指纹不一致而失败")]
    [Timeout(60_000)]
    public async Task LoadAsyncShouldRejectTamperedMarkdown()
    {
        var snapshotDirectory = await CreateSnapshotAsync();
        await File.AppendAllTextAsync(
            Path.Join(snapshotDirectory.FullName, "Slides", "Slide000.md"),
            "\n\n已篡改内容");
        var store = new CoursewareThemeAnalysisSnapshotStore(snapshotDirectory.Parent!.FullName);

        var exception = await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, "源事实指纹");
    }

    [TestMethod(DisplayName = "修改快照参考画布后恢复应明确拒绝")]
    [Timeout(60_000)]
    public async Task LoadAsyncShouldRejectMismatchedReferenceCanvas()
    {
        var snapshotDirectory = await CreateSnapshotAsync();
        var manifestPath = Path.Join(snapshotDirectory.FullName, CoursewareThemeAnalysisSnapshotManifest.FileName);
        CoursewareThemeAnalysisSnapshotManifest manifest;
        await using (var readStream = File.OpenRead(manifestPath))
        {
            manifest = (await JsonSerializer.DeserializeAsync(
                readStream,
                CoursewareThemeAnalysisSnapshotJsonSerializerContext.Default.CoursewareThemeAnalysisSnapshotManifest))!;
        }

        var tamperedManifest = manifest with
        {
            AnalysisResult = manifest.AnalysisResult with
            {
                ReferenceCanvas = new SlideDocumentContext(1024, 768),
            },
        };
        await using (var writeStream = new FileStream(manifestPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(
                writeStream,
                tamperedManifest,
                CoursewareThemeAnalysisSnapshotJsonSerializerContext.Default.CoursewareThemeAnalysisSnapshotManifest);
        }
        var store = new CoursewareThemeAnalysisSnapshotStore(snapshotDirectory.Parent!.FullName);

        var exception = await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, "参考画布");
    }

    [TestMethod(DisplayName = "快照版本不受支持时恢复应明确拒绝")]
    [Timeout(60_000)]
    public async Task LoadAsyncShouldRejectUnsupportedSchemaVersion()
    {
        var snapshotDirectory = await CreateSnapshotAsync();
        var manifest = await ReadManifestAsync(snapshotDirectory);
        await WriteManifestAsync(snapshotDirectory, manifest with { SchemaVersion = "unsupported/v99" });
        var store = new CoursewareThemeAnalysisSnapshotStore(snapshotDirectory.Parent!.FullName);

        var exception = await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, "不支持的主题分析快照版本");
    }

    [TestMethod(DisplayName = "快照主题不合法时恢复应明确拒绝")]
    [Timeout(60_000)]
    public async Task LoadAsyncShouldRejectInvalidTheme()
    {
        var snapshotDirectory = await CreateSnapshotAsync();
        var manifest = await ReadManifestAsync(snapshotDirectory);
        await WriteManifestAsync(snapshotDirectory, manifest with
        {
            AnalysisResult = manifest.AnalysisResult with
            {
                Theme = manifest.AnalysisResult.Theme with { Title = string.Empty },
            },
        });
        var store = new CoursewareThemeAnalysisSnapshotStore(snapshotDirectory.Parent!.FullName);

        var exception = await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, "主题未通过校验");
    }

    [TestMethod(DisplayName = "快照缺少页面输入文件时恢复应明确失败")]
    [Timeout(60_000)]
    public async Task LoadAsyncShouldRejectMissingCopiedInputFile()
    {
        var snapshotDirectory = await CreateSnapshotAsync();
        File.Delete(Path.Join(snapshotDirectory.FullName, "Slides", "Slide000.md"));
        var store = new CoursewareThemeAnalysisSnapshotStore(snapshotDirectory.Parent!.FullName);

        var exception = await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => store.LoadAsync(snapshotDirectory.FullName));

        StringAssert.Contains(exception.Message, "Markdown 文件不存在");
    }

    [TestMethod(DisplayName = "快照写入中途失败后不应遗留半成品目录")]
    [Timeout(60_000)]
    public async Task SaveAsyncShouldRemoveTemporaryDirectoryWhenCopyFails()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页", "第一页内容"))
            .AddResource("resource-first", "image", "shared.png")
            .AddResource("resource-second", "image", "shared.png")
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var analysisResult = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package);
        var outputRoot = CreateOutputRoot();
        var store = new CoursewareThemeAnalysisSnapshotStore(outputRoot.FullName);

        await Assert.ThrowsExactlyAsync<IOException>(() => store.SaveAsync(package, analysisResult));

        Assert.IsEmpty(outputRoot.EnumerateDirectories());
    }

    [TestMethod(DisplayName = "取消快照保存不应创建主题分析目录")]
    [Timeout(60_000)]
    public async Task SaveAsyncShouldNotCreateDirectoryWhenCanceledBeforeStart()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页", "第一页内容"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var analysisResult = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package);
        var outputRoot = CreateOutputRoot();
        var store = new CoursewareThemeAnalysisSnapshotStore(outputRoot.FullName);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => store.SaveAsync(package, analysisResult, cancellationTokenSource.Token));

        Assert.IsEmpty(outputRoot.EnumerateDirectories());
    }

    private static async Task<DirectoryInfo> CreateSnapshotAsync()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页", "第一页内容"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var analysisResult = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package);
        var outputRoot = CreateOutputRoot();
        var store = new CoursewareThemeAnalysisSnapshotStore(outputRoot.FullName);
        return await store.SaveAsync(package, analysisResult);
    }

    private static async Task<CoursewareThemeAnalysisSnapshotManifest> ReadManifestAsync(DirectoryInfo snapshotDirectory)
    {
        await using var stream = File.OpenRead(Path.Join(
            snapshotDirectory.FullName,
            CoursewareThemeAnalysisSnapshotManifest.FileName));
        return (await JsonSerializer.DeserializeAsync(
            stream,
            CoursewareThemeAnalysisSnapshotJsonSerializerContext.Default.CoursewareThemeAnalysisSnapshotManifest))!;
    }

    private static async Task WriteManifestAsync(
        DirectoryInfo snapshotDirectory,
        CoursewareThemeAnalysisSnapshotManifest manifest)
    {
        await using var stream = new FileStream(
            Path.Join(snapshotDirectory.FullName, CoursewareThemeAnalysisSnapshotManifest.FileName),
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);
        await JsonSerializer.SerializeAsync(
            stream,
            manifest,
            CoursewareThemeAnalysisSnapshotJsonSerializerContext.Default.CoursewareThemeAnalysisSnapshotManifest);
    }

    private static DirectoryInfo CreateOutputRoot()
    {
        return Directory.CreateDirectory(Path.Join(
            Path.GetTempPath(),
            $"CoursewareThemeAnalysisSnapshotStoreTests_{Guid.NewGuid():N}"));
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 元素简要信息\n\n- 文本.1: (100, 80) 400×60\n\n---\n\n## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }
}
