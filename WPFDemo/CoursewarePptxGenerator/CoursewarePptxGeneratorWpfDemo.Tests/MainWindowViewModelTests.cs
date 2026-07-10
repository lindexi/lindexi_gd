using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class MainWindowViewModelTests
{
    [TestMethod(DisplayName = "打开课件文件夹后应准备页面列表截图附件和美化提示词")]
    [Timeout(60_000)]
    public async Task OpenCoursewareFolderAsyncShouldPrepareSlidesAttachmentsAndPrompt()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-alpha", "# Slide 1\n\n## 第一页主题\n第一页 Markdown 内容。")
            .AddSlide("slide-beta", "# Slide 2\n\n## 第二页主题\n第二页 Markdown 内容。")
            .AddResource("img_1", "picture.png", "picture.png")
            .Build();
        var chatManagerFactory = new FakeSlideChatManagerFactory();
        var firstChatManager = await chatManagerFactory.CreateAsync();
        var viewModel = new MainWindowViewModel(
            chatManagerFactory,
            firstChatManager,
            new CoursewareFolderLoader(),
            new CoursewareSlideSummaryService(),
            new ImmediateViewModelDispatcher());

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.HasCount(2, viewModel.Slides);
        Assert.AreEqual("slide-alpha", viewModel.Slides[0].SlideId);
        Assert.AreEqual("slide-beta", viewModel.Slides[1].SlideId);
        Assert.AreSame(viewModel.Slides[0], viewModel.SelectedSlide);
        Assert.HasCount(1, viewModel.AttachedImageFiles);
        Assert.AreEqual(viewModel.Slides[0].SourceScreenshotFilePath, viewModel.AttachedImageFiles[0].FullName);
        StringAssert.Contains(viewModel.InputText, "页面 Id：slide-alpha");
        StringAssert.Contains(viewModel.InputText, "第一页 Markdown 内容");
        StringAssert.Contains(viewModel.StatusText, "已加载 2 页");

        viewModel.SelectedSlide = viewModel.Slides[1];

        Assert.HasCount(1, viewModel.AttachedImageFiles);
        Assert.AreEqual(viewModel.Slides[1].SourceScreenshotFilePath, viewModel.AttachedImageFiles[0].FullName);
        StringAssert.Contains(viewModel.InputText, "页面 Id：slide-beta");
        StringAssert.Contains(viewModel.InputText, "第二页 Markdown 内容");
    }

    [TestMethod(DisplayName = "打开课件文件夹时聊天管理器初始化失败也应显示页面和美化提示词")]
    [Timeout(60_000)]
    public async Task OpenCoursewareFolderAsyncShouldStillShowSlidesWhenSlideChatManagerCreationFails()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-alpha", "# Slide 1\n\n## 第一页主题\n第一页 Markdown 内容。")
            .Build();
        var firstChatManager = await new FakeSlideChatManagerFactory().CreateAsync();
        var viewModel = new MainWindowViewModel(
            new FailingSlideChatManagerFactory(),
            firstChatManager,
            new CoursewareFolderLoader(),
            new CoursewareSlideSummaryService(),
            new ImmediateViewModelDispatcher());

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.HasCount(1, viewModel.Slides);
        Assert.AreEqual("slide-alpha", viewModel.Slides[0].SlideId);
        Assert.AreSame(viewModel.Slides[0], viewModel.SelectedSlide);
        StringAssert.Contains(viewModel.InputText, "页面 Id：slide-alpha");
        StringAssert.Contains(viewModel.InputText, "第一页 Markdown 内容");
        StringAssert.Contains(viewModel.StatusText, "已加载 1 页");
    }

}
