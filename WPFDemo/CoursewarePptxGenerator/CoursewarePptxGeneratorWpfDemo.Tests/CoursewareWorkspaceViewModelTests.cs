using System.IO;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareWorkspaceViewModelTests
{
    [TestMethod(DisplayName = "打开课件文件夹后应加载真实缩略图而不启动页面会话")]
    [Timeout(60_000)]
    public async Task OpenCoursewareFolderAsyncShouldLoadRealThumbnailsWithoutSlideChatManagers()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .AddSlide("slide-second", CreateSlideMarkdown("第二页标题", "第二页内容"), hasScreenshot: false)
            .Build();
        var viewModel = CreateViewModel();

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.AreEqual(CoursewareWorkspaceState.CoursewareLoaded, viewModel.WorkspaceState);
        Assert.IsNotNull(viewModel.CoursewareSession);
        Assert.AreEqual("测试课件", viewModel.CoursewareTitle);
        Assert.HasCount(2, viewModel.CoursewareThumbnails);
        Assert.AreEqual("slide-first", viewModel.CoursewareThumbnails[0].SlideId);
        Assert.AreEqual("第 1 页", viewModel.CoursewareThumbnails[0].AccessibleName);
        Assert.IsTrue(viewModel.CoursewareThumbnails[0].HasScreenshot);
        Assert.AreEqual("slide-second", viewModel.CoursewareThumbnails[1].SlideId);
        Assert.IsFalse(viewModel.CoursewareThumbnails[1].HasScreenshot);
        Assert.IsTrue(viewModel.CoursewareThumbnails[1].HasWarning);
    }

    [TestMethod(DisplayName = "打开无效课件文件夹后应清空缩略图并显示错误")]
    [Timeout(60_000)]
    public async Task OpenCoursewareFolderAsyncShouldClearThumbnailsAndShowErrorWhenLoadingFails()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var invalidDirectory = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"InvalidCourseware_{Guid.NewGuid():N}"));
        var viewModel = CreateViewModel();
        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        await viewModel.OpenCoursewareFolderAsync(invalidDirectory.FullName);

        Assert.AreEqual(CoursewareWorkspaceState.LoadFailed, viewModel.WorkspaceState);
        Assert.IsNull(viewModel.CoursewareSession);
        Assert.IsEmpty(viewModel.CoursewareThumbnails);
        StringAssert.Contains(viewModel.LoadErrorMessage, "Courseware.json");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.LoadErrorDetails));
    }

    [TestMethod(DisplayName = "未选择课件文件夹时应保持欢迎状态")]
    public async Task OpenCoursewareFolderAsyncShouldKeepWelcomeStateWhenFolderIsNotSelected()
    {
        var viewModel = CreateViewModel();

        await viewModel.OpenCoursewareFolderAsync(null);

        Assert.AreEqual(CoursewareWorkspaceState.Welcome, viewModel.WorkspaceState);
        Assert.IsNull(viewModel.CoursewareSession);
        Assert.IsEmpty(viewModel.CoursewareThumbnails);
    }

    private static CoursewareWorkspaceViewModel CreateViewModel()
    {
        return new CoursewareWorkspaceViewModel(
            new CoursewareFolderLoader(),
            new ImmediateViewModelDispatcher());
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 页面信息\n\n- Id: slide-id\n- 尺寸: 1280×720\n- 序号(1-base): 1\n\n---\n\n## 元素简要信息\n\n- 文本.1: (100, 80) 400×60\n\n---\n\n## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }
}