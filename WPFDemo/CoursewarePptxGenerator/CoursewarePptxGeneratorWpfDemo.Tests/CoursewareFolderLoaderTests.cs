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
            .AddSlide("slide-first", "# 第一页\n\n- SlideIndex: 0\n- SlideId: slide-first\n\n## 页面主题\n这里是第一页 Markdown。")
            .AddSlide("slide-second", "# 第二页\n\n这里是第二页 Markdown。", hasScreenshot: false)
            .AddResource("img_1", "source.png", "img_1.png")
            .AddResource("img_2", "missing.png", "missing.png", exists: false)
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
        Assert.AreEqual(2, package.Resources.Count);
        Assert.IsTrue(package.Resources[0].Exists);
        Assert.IsFalse(package.Resources[1].Exists);
        StringAssert.Contains(package.Resources[0].ResolvedFilePath!, Path.Join("resources", "img_1.png"));
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
        var manifestPath = Path.Join(exportDirectory.FullName, "courseware.json");
        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        manifestJson = manifestJson.Replace("slides/Slide_001.md", "../Slide_001.md", StringComparison.Ordinal);
        await File.WriteAllTextAsync(manifestPath, manifestJson);
        var loader = new CoursewareFolderLoader();

        var exception = await ThrowsAsync<InvalidDataException>(() => loader.LoadAsync(exportDirectory.FullName));

        StringAssert.Contains(exception.Message, "路径越界");
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
