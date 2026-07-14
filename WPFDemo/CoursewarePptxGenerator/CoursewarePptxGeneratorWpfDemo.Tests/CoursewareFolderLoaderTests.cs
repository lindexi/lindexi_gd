using System.IO;
using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareFolderLoaderTests
{
    [TestMethod(DisplayName = "加载课件导出目录时应解析页面和素材资源")]
    [Timeout(60_000)]
    public async Task LoadAsyncShouldResolveSlidesAndResources()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页 Markdown。"))
            .AddSlide("slide-second", CreateSlideMarkdown("第二页 Markdown。"), hasScreenshot: false)
            .AddResource("img_1", "image", "img_1.png")
            .AddResource("video_1", "video", "video_1.mp4", exists: false)
            .Build();

        var loader = new CoursewareFolderLoader();

        var package = await loader.LoadAsync(exportDirectory.FullName);

        Assert.AreEqual(2, package.SlideCount);
        Assert.AreEqual("测试课件", package.CoursewareName);
        Assert.AreEqual("slide-first", package.Slides[0].SlideId);
        Assert.IsTrue(package.Slides[0].ScreenshotFile?.Exists);
        Assert.AreEqual("slide-second", package.Slides[1].SlideId);
        Assert.IsNull(package.Slides[1].ScreenshotFile);
        StringAssert.Contains(package.Slides[0].MarkdownText, "第一页 Markdown");
        Assert.HasCount(2, package.Resources);
        Assert.IsTrue(package.Resources[0].Exists);
        Assert.IsFalse(package.Resources[1].Exists);
        Assert.AreEqual("img_1", package.Resources[0].ResourceId);
        Assert.AreEqual("image", package.Resources[0].ResourceType);
        StringAssert.Contains(package.Resources[0].ResolvedFilePath!, Path.Join("Resources", "img_1.png"));
        Assert.IsTrue(package.Warnings.Any(warning => warning.Code == "MissingScreenshotFile"));
        Assert.IsTrue(package.Warnings.Any(warning => warning.Code == "MissingResourceFile"));
    }

    [TestMethod(DisplayName = "课件清单路径越界时应阻止加载")]
    [Timeout(60_000)]
    public async Task LoadAsyncShouldRejectPathTraversal()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "# 第一页")
            .Build();
        var manifestPath = Path.Join(exportDirectory.FullName, "Courseware.json");
        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        manifestJson = manifestJson.Replace("Slides/Slide000.md", "Slides/../Slide000.md", StringComparison.Ordinal);
        await File.WriteAllTextAsync(manifestPath, manifestJson);
        var loader = new CoursewareFolderLoader();

        var exception = await ThrowsAsync<InvalidDataException>(() => loader.LoadAsync(exportDirectory.FullName));

        StringAssert.Contains(exception.Message, "不能包含空、. 或 .. 路径片段");
    }

    [TestMethod(DisplayName = "课件清单使用反斜杠路径时应兼容加载")]
    [Timeout(60_000)]
    public async Task LoadAsyncShouldAcceptBackslashPath()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页"))
            .Build();
        var manifestPath = Path.Join(exportDirectory.FullName, "Courseware.json");
        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        manifestJson = manifestJson.Replace("Slides/Slide000.md", "Slides\\\\Slide000.md", StringComparison.Ordinal);
        await File.WriteAllTextAsync(manifestPath, manifestJson);
        var loader = new CoursewareFolderLoader();

        var package = await loader.LoadAsync(exportDirectory.FullName);

        Assert.HasCount(1, package.Slides);
        Assert.AreEqual("slide-first", package.Slides[0].SlideId);
        Assert.IsTrue(package.Slides[0].MarkdownFile.Exists);
    }

    [TestMethod(DisplayName = "资源索引使用对象包装时应阻止加载")]
    [Timeout(60_000)]
    public async Task LoadAsyncShouldRejectObjectWrappedResources()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页"))
            .AddResource("img_1", "image", "img_1.png")
            .Build();
        var resourcesPath = Path.Join(exportDirectory.FullName, "Resources", "Resources.json");
        var resourcesJson = await File.ReadAllTextAsync(resourcesPath);
        await File.WriteAllTextAsync(resourcesPath, $"{{\"Resources\":{resourcesJson}}}");
        var loader = new CoursewareFolderLoader();

        var exception = await ThrowsAsync<InvalidDataException>(() => loader.LoadAsync(exportDirectory.FullName));

        StringAssert.Contains(exception.Message, "必须是资源条目数组");
    }

    [TestMethod(DisplayName = "资源类型不在文档范围内时应阻止加载")]
    [Timeout(60_000)]
    public async Task LoadAsyncShouldRejectUnknownResourceType()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页"))
            .AddResource("document_1", "document", "document_1.pdf")
            .Build();
        var loader = new CoursewareFolderLoader();

        var exception = await ThrowsAsync<InvalidDataException>(() => loader.LoadAsync(exportDirectory.FullName));

        StringAssert.Contains(exception.Message, "ResourceType 无效");
    }

    private static string CreateSlideMarkdown(string content)
    {
        return $"## 页面信息\n\n- Id: slide-id\n- 尺寸: 1280×720\n- 序号(1-base): 1\n\n---\n\n## 元素简要信息\n\n- 文本.1: (100, 80) 400×60\n\n---\n\n## 元素细节\n\n### 文本.1\n字号: 32px | 字体: Microsoft YaHei\n#### 内容\n```\n{content}\n```";
    }

    private static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (TException ex)
        {
            return ex;
        }

        Assert.Fail($"应抛出 {typeof(TException).Name}。");
        throw new InvalidOperationException("异常断言未按预期终止。");
    }
}
